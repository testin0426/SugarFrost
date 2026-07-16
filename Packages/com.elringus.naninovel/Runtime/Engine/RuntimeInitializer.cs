using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Default engine initializer for runtime environment.
    /// </summary>
    public class RuntimeInitializer : MonoBehaviour
    {
        [SerializeField] private bool initializeOnAwake = true;

        private const string initPrefabName = "EngineInitializationUI";
        private static readonly AsyncSource initSrc = new(completed: true);
        private static bool fastInitRequested;

        /// <summary>
        /// Invokes default engine initialization routine.
        /// </summary>
        /// <param name="configurationProvider">Configuration provider to use for engine initialization.</param>
        /// <param name="customInitializationData">Use to inject services without <see cref="InitializeAtRuntimeAttribute"/>.</param>
        /// <param name="time">Time service to use by the engine.</param>
        public static async Awaitable Initialize (IConfigurationProvider configurationProvider = null,
            IEnumerable<ServiceInitializationData> customInitializationData = null, ITime time = null)
        {
            if (Engine.Initialized) return;

            if (!initSrc.Completed)
            {
                await initSrc.WaitCompletion();
                return;
            }

            initSrc.Reset();

            configurationProvider ??= new ProjectConfigurationProvider();
            var engineConfig = configurationProvider.GetConfiguration<EngineConfiguration>();

            var initializationUI = default(ScriptableUIBehaviour);
            if (engineConfig.ShowInitializationUI)
            {
                var initPrefab = engineConfig.CustomInitializationUI
                    ? engineConfig.CustomInitializationUI
                    : Engine.LoadInternalResource<ScriptableUIBehaviour>(initPrefabName);
                initializationUI = Instantiate(initPrefab);
                initializationUI.Show();
            }

            var initData = customInitializationData?.ToList() ?? new List<ServiceInitializationData>();
            var overridenTypes = initData.Where(d => d.Override != null).Select(d => d.Override).ToList();
            foreach (var type in Engine.Types.InitializeAtRuntime)
            {
                var initAttribute = Attribute.GetCustomAttribute(type, typeof(InitializeAtRuntimeAttribute), false) as InitializeAtRuntimeAttribute;
                if (initAttribute is null) throw new Error($"Failed to initialize '{type.FullName}' engine service: missing attribute.");
                initData.Add(new(type, initAttribute));
                if (initAttribute.Override != null)
                    overridenTypes.Add(initAttribute.Override);
            }
            initData = initData.Where(d => !overridenTypes.Contains(d.Type)).ToList(); // Exclude services overriden by user.

            bool IsService (Type t) => typeof(IEngineService).IsAssignableFrom(t);
            bool IsBehaviour (Type t) => typeof(IEngineBehaviour).IsAssignableFrom(t);
            bool IsConfig (Type t) => typeof(Configuration).IsAssignableFrom(t);

            // Order by initialization priority and then perform topological order to make sure ctor references initialized before they're used.
            // ReSharper disable once AccessToModifiedClosure (false positive: we're assigning a result of the closure to the variable in question)
            IEnumerable<ServiceInitializationData> GetDependencies (ServiceInitializationData d) =>
                d.CtorArgs.Where(IsService).SelectMany(argType => initData.Where(dd => d != dd && argType.IsAssignableFrom(dd.Type)));
            initData = initData.OrderBy(d => d.Priority).TopologicalOrder(GetDependencies).ToList();

            var behaviour = RuntimeBehaviour.Create(engineConfig.SceneIndependent);
            var services = new List<IEngineService>();
            var ctorParams = new List<object>();
            foreach (var data in initData)
            {
                foreach (var argType in data.CtorArgs)
                    if (IsService(argType)) ctorParams.Add(services.First(s => argType.IsInstanceOfType(s)));
                    else if (IsBehaviour(argType)) ctorParams.Add(behaviour);
                    else if (IsConfig(argType)) ctorParams.Add(configurationProvider.GetConfiguration(argType));
                    else
                        throw new Error($"Only '{nameof(Configuration)}', '{nameof(IEngineBehaviour)}' and '{nameof(IEngineService)}' " +
                                        $"with an '{nameof(InitializeAtRuntimeAttribute)}' can be requested in an engine service constructor.");
                var service = Activator.CreateInstance(data.Type, ctorParams.ToArray()) as IEngineService;
                services.Add(service);
                ctorParams.Clear();
            }

            await Engine.Initialize(new() {
                Services = services,
                ConfigurationProvider = configurationProvider,
                Behaviour = behaviour,
                Time = time ?? new UnityTime()
            });

            if (!Engine.Initialized) // In case terminated in the midst of initialization.
            {
                if (initializationUI)
                    ObjectUtils.DestroyOrImmediate(initializationUI.gameObject);
                initSrc.Complete();
                return;
            }

            if (engineConfig.EnableDevelopmentConsole &&
                (!Engine.Configuration.DebugOnlyConsole || Debug.isDebugBuild))
                ConsoleCommands.InitializeConsole();

            if (initializationUI)
                initializationUI.ChangeVisibility(false, token: Engine.DestroyToken)
                    .Then(() => ObjectUtils.DestroyOrImmediate(initializationUI.gameObject)).Forget();

            Engine.GetServiceOrErr<IInputManager>().Muted = false;

            await PlayInitScript();

            if (!fastInitRequested) await PlayIntroMovie();
            if (!fastInitRequested && TitleScreen.Enabled) await TitleScreen.Enter();

            fastInitRequested = false;
            initSrc.Complete();
        }

        /// <summary>
        /// Requests next <see cref="Initialize"/> to be performed as fast as possible,
        /// without invoking various non-essential post-engine initialization routines,
        /// such as playing the intro movie and Title script.
        /// </summary>
        public static void RequestFastInitialization ()
        {
            fastInitRequested = true;
        }

        private static async Awaitable PlayInitScript ()
        {
            var scripts = Engine.GetServiceOrErr<IScriptManager>();
            var path = scripts.Configuration.InitializationScript;
            if (string.IsNullOrEmpty(path) || !scripts.ScriptLoader.Exists(path)) return;

            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            using (new InteractionBlocker())
                await player.MainTrack.LoadAndPlay(path);
            while (player.MainTrack.Playing)
                await Async.NextFrame(Engine.DestroyToken);
        }

        private static async Awaitable PlayIntroMovie ()
        {
            var cfg = Engine.GetConfiguration<MoviesConfiguration>();
            if (!cfg.PlayIntroMovie) return;
            // Keep duration = 0 to prevent the user from activating input (eg, showing pause UI) while UI is fading-out.
            await new PlayMovie { MoviePath = cfg.IntroMovieName, Duration = 0 }
                .Execute(new(Engine.GetServiceOrErr<IScriptPlayer>().MainTrack, Engine.DestroyToken));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration ()
        {
            initSrc.Complete();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnApplicationLoaded ()
        {
            var engineConfig = Configuration.GetOrDefault<EngineConfiguration>();
            if (engineConfig.InitializeOnApplicationLoad)
                Initialize().Forget();
        }

        private void Awake ()
        {
            if (initializeOnAwake)
                Initialize().Forget();
        }
    }
}

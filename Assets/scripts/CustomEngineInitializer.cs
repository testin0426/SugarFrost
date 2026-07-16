using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Boot
{
    /// <summary>
    /// Drives Naninovel engine + service initialization from a custom UI.
    /// Mount this on a UI GameObject (loading screen) and wire the inspector
    /// fields. Call <see cref="StartInitialization"/> from a button or Awake.
    /// Stops once the engine and all services are ready — does NOT play the
    /// intro movie or the initialization script. The first @stop script must
    /// be triggered explicitly afterwards (see <see cref="PlayScript"/>).
    /// </summary>
    public class CustomEngineInitializer : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private UnityEngine.UI.Text progressLabel;
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private bool initializeOnAwake;
        [SerializeField] private bool hidePanelWhenDone = true;

        [Header("Optional after-init")]
        [Tooltip("Scene GameObjects to enable AFTER the engine is fully initialized. " +
                 "Use this for Naninovel-aware UIs that touch engine services in Awake (e.g. TitleMenu). " +
                 "Leave them disabled in the scene and Naninovel-aware Awake runs only after init completes.")]
        [SerializeField] private GameObject[] activateAfterInit;
        [Tooltip("If non-empty, this Naninovel script is played after engine init completes.")]
        [SerializeField] private string scriptToPlayAfterInit;

        public bool IsInitializing { get; private set; }
        public bool IsInitialized => Engine.Initialized;

        public event Action<float> ProgressChanged;
        public event Action InitializationStarted;
        public event Action InitializationFinished;

        private async void Awake ()
        {
            if (initializeOnAwake)
                await StartInitialization();
        }

        private void OnDestroy ()
        {
            Engine.OnInitializationProgress -= OnEngineProgress;
            Engine.OnInitializationStarted -= OnEngineStarted;
            Engine.OnInitializationFinished -= OnEngineFinished;
        }

        /// <summary>
        /// Wrapper for Unity Button.onClick (UnityEvent only sees void methods).
        /// Hook this to your "New Game" button's OnClick.
        /// </summary>
        public void StartInitializationFromButton ()
        {
            StartInitialization().Forget();
        }

        public async Awaitable StartInitialization ()
        {
            if (Engine.Initialized || Engine.Initializing || IsInitializing) return;

            IsInitializing = true;
            if (rootPanel) rootPanel.SetActive(true);
            UpdateProgressUI(0f);

            Engine.OnInitializationProgress += OnEngineProgress;
            Engine.OnInitializationStarted += OnEngineStarted;
            Engine.OnInitializationFinished += OnEngineFinished;

            try
            {
                await InitializeEngineAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[CustomEngineInitializer] Engine initialization failed: {e}");
                IsInitializing = false;
                throw;
            }

            IsInitializing = false;

            if (hidePanelWhenDone && rootPanel)
                rootPanel.SetActive(false);

            if (activateAfterInit != null)
                foreach (var go in activateAfterInit)
                    if (go) go.SetActive(true);

            if (!string.IsNullOrEmpty(scriptToPlayAfterInit))
                await PlayScript(scriptToPlayAfterInit);
        }

        public static async Awaitable PlayScript (string scriptPath)
        {
            if (!Engine.Initialized) throw new InvalidOperationException("Engine is not initialized.");
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            await player.MainTrack.LoadAndPlay(scriptPath);
        }

        private static async Awaitable InitializeEngineAsync ()
        {
            var configurationProvider = new ProjectConfigurationProvider();
            var engineConfig = configurationProvider.GetConfiguration<EngineConfiguration>();

            var initData = new List<ServiceInitializationData>();
            var overridenTypes = new List<Type>();
            foreach (var type in Engine.Types.InitializeAtRuntime)
            {
                var attr = Attribute.GetCustomAttribute(type, typeof(InitializeAtRuntimeAttribute), false) as InitializeAtRuntimeAttribute;
                if (attr is null) throw new Exception($"Failed to initialize '{type.FullName}': missing InitializeAtRuntime attribute.");
                initData.Add(new ServiceInitializationData(type, attr));
                if (attr.Override != null) overridenTypes.Add(attr.Override);
            }
            initData = initData.Where(d => !overridenTypes.Contains(d.Type)).ToList();

            bool IsService (Type t) => typeof(IEngineService).IsAssignableFrom(t);
            bool IsBehaviour (Type t) => typeof(IEngineBehaviour).IsAssignableFrom(t);
            bool IsConfig (Type t) => typeof(Configuration).IsAssignableFrom(t);

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
                    else throw new Exception(
                        $"Only Configuration, IEngineBehaviour and IEngineService with [InitializeAtRuntime] " +
                        $"can be requested in service '{data.Type.FullName}' constructor (got '{argType.FullName}').");
                var service = Activator.CreateInstance(data.Type, ctorParams.ToArray()) as IEngineService;
                services.Add(service);
                ctorParams.Clear();
            }

            await Engine.Initialize(new EngineParams {
                Services = services,
                ConfigurationProvider = configurationProvider,
                Behaviour = behaviour,
                Time = new UnityTime()
            });

            if (!Engine.Initialized) return;

            Engine.GetServiceOrErr<IInputManager>().Muted = false;
        }

        private void OnEngineStarted () { InitializationStarted?.Invoke(); UpdateProgressUI(0f); }
        private void OnEngineProgress (float p) { UpdateProgressUI(p); ProgressChanged?.Invoke(p); }
        private void OnEngineFinished () { UpdateProgressUI(1f); InitializationFinished?.Invoke(); }

        private void UpdateProgressUI (float value)
        {
            if (progressBar) progressBar.value = value;
            if (progressLabel) progressLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}

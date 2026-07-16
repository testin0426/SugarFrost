using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

// Make sure none of the assembly types are stripped when building with IL2CPP.
[assembly: AlwaysLinkAssembly, Preserve]

namespace Naninovel
{
    /// <summary>
    /// Central access point to the core Naninovel APIs.
    /// </summary>
    public static class Engine
    {
        /// <summary>
        /// Invoked when the engine initialization is started.
        /// </summary>
        public static event Action OnInitializationStarted;
        /// <summary>
        /// Invoked when the engine initialization is finished.
        /// </summary>
        public static event Action OnInitializationFinished;
        /// <summary>
        /// Invoked when the engine initialization progress is changed (in 0.0 to 1.0 range).
        /// </summary>
        public static event Action<float> OnInitializationProgress;
        /// <summary>
        /// Invoked when the engine is destroyed.
        /// </summary>
        public static event Action OnDestroyed;

        /// <inheritdoc cref="EngineTypes"/>
        /// <remarks>It's safe to access this property when the engine is not initialized and in the editor.</remarks>
        public static EngineTypes Types { get; } = TypeCache.Load();
        /// <summary>
        /// Configuration object used to initialize the engine.
        /// </summary>
        public static EngineConfiguration Configuration { get; private set; }
        /// <summary>
        /// Proxy <see cref="MonoBehaviour"/> used by the engine.
        /// </summary>
        public static IEngineBehaviour Behaviour { get; private set; }
        /// <summary>
        /// Proxy time service used by the engine.
        /// </summary>
        public static ITime Time { get; private set; } = new UnityTime();
        /// <summary>
        /// Initialized engine services.
        /// </summary>
        public static IReadOnlyCollection<IEngineService> Services => services;
        /// <summary>
        /// Composition root, containing all the engine-related game objects.
        /// </summary>
        public static GameObject Root => Behaviour.GetRoot();
        /// <summary>
        /// Whether the engine is initialized and ready.
        /// </summary>
        public static bool Initialized { get; private set; }
        /// <summary>
        /// Whether the engine is currently being initialized.
        /// </summary>
        public static bool Initializing => !initSrc.Completed;
        /// <summary>
        /// Token which is canceled while the engine is destroyed.
        /// </summary>
        public static CancellationToken DestroyToken => destroySrc.Token;

        private static readonly List<Object> objects = new();
        private static readonly List<IEngineService> services = new();
        private static readonly Dictionary<Type, IEngineService> cachedGetServiceResults = new();
        private static readonly List<Func<Awaitable>> preInitializationTasks = new();
        private static readonly List<Func<Awaitable>> postInitializationTasks = new();
        private static readonly AsyncSource initSrc = new(completed: true);
        private static readonly AsyncSource destroySrc = new(completed: true);
        private static IConfigurationProvider configurationProvider;
        private static ILogger logger = new UnityLogger();

        static Engine () { } // keep the ctor for consistent lazy init of the static props

        /// <summary>
        /// Adds an async function delegate to invoke before the engine initialization.
        /// Added delegates will be invoked and awaited in order before starting the initialization.
        /// </summary>
        public static void AddPreInitializationTask (Func<Awaitable> task) => preInitializationTasks.Add(task);

        /// <summary>
        /// Removes a delegate added via <see cref="AddPreInitializationTask(Func{Awaitable})"/>.
        /// </summary>
        public static void RemovePreInitializationTask (Func<Awaitable> task) => preInitializationTasks.Remove(task);

        /// <summary>
        /// Adds an async function delegate to invoke after the engine initialization.
        /// Added delegates will be invoked and awaited in order before finishing the initialization.
        /// </summary>
        public static void AddPostInitializationTask (Func<Awaitable> task) => postInitializationTasks.Add(task);

        /// <summary>
        /// Removes a delegate added via <see cref="AddPostInitializationTask(Func{Awaitable})"/>.
        /// </summary>
        public static void RemovePostInitializationTask (Func<Awaitable> task) => postInitializationTasks.Remove(task);

        /// <summary>
        /// Initializes the engine behaviour and services.
        /// </summary>
        /// <param name="params">Parameters required for engine initialization.</param>
        public static async Awaitable Initialize (EngineParams @params)
        {
            if (Initialized) return;

            if (!initSrc.Completed)
            {
                await initSrc.WaitCompletion();
                return;
            }

            Initialized = false;
            destroySrc.Reset();
            initSrc.Reset();
            OnInitializationStarted?.Invoke();

            for (int i = preInitializationTasks.Count - 1; i >= 0; i--)
            {
                OnInitializationProgress?.Invoke(.25f * (1 - i / (float)preInitializationTasks.Count));
                await preInitializationTasks[i]();
                if (!Initializing) return; // In case the initialization process was terminated (eg, exited play mode).
            }

            configurationProvider = @params.ConfigurationProvider;
            Configuration = GetConfiguration<EngineConfiguration>();
            Behaviour = @params.Behaviour;
            Behaviour.OnDestroy += Destroy;
            Time = @params.Time;

            if (Application.isEditor && Configuration.CheckUnityVersion)
                CheckUnityVersion();

            objects.Clear();
            services.ReplaceWith(@params.Services);

            for (var i = 0; i < services.Count; i++)
            {
                OnInitializationProgress?.Invoke(.25f + .5f * (i / (float)services.Count));
                await services[i].InitializeService();
                if (!Initializing) return;
            }

            for (int i = 0; i < postInitializationTasks.Count; i++)
            {
                OnInitializationProgress?.Invoke(.75f + .25f * (i / (float)postInitializationTasks.Count));
                await postInitializationTasks[i]();
                if (!Initializing) return;
            }

            Initialized = true;
            initSrc.Complete();
            OnInitializationFinished?.Invoke();
        }

        /// <summary>
        /// Resets the state of all the engine services.
        /// </summary>
        public static void Reset () => services.ForEach(s => s.ResetService());

        /// <summary>
        /// Resets the state of engine services.
        /// </summary>
        /// <param name="exclude">Type of the engine services (interfaces) to exclude from reset.</param>
        public static void Reset (IReadOnlyCollection<Type> exclude)
        {
            if (services.Count == 0) return;

            foreach (var service in services)
                if (exclude is null || exclude.Count == 0 || !exclude.Any(t => t.IsInstanceOfType(service)))
                    service.ResetService();
        }

        /// <inheritdoc cref="Reset()"/>
        public static void Reset (params Type[] exclude) => Reset((IReadOnlyCollection<Type>)exclude);

        /// <summary>
        /// Deconstructs all the engine services and stops the behaviour.
        /// </summary>
        public static void Destroy ()
        {
            Initialized = false;
            initSrc.Complete();

            services.ForEach(s => s.DestroyService());
            services.Clear();
            cachedGetServiceResults.Clear();

            if (Behaviour != null)
            {
                Behaviour.OnDestroy -= Destroy;
                Behaviour.Destroy();
                Behaviour = null;
            }

            foreach (var obj in objects)
            {
                if (!obj) continue;
                var go = obj as GameObject ?? (obj as Component)?.gameObject;
                ObjectUtils.DestroyOrImmediate(go);
            }
            objects.Clear();

            Configuration = null;
            configurationProvider = null;

            destroySrc.Complete();
            OnDestroyed?.Invoke();
        }

        /// <summary>
        /// Attempts to provide a <see cref="Naninovel.Configuration"/> object of the specified type 
        /// via <see cref="IConfigurationProvider"/> used to initialize the engine.
        /// </summary>
        /// <typeparam name="T">Type of the requested configuration object.</typeparam>
        public static T GetConfiguration<T> () where T : Configuration => GetConfiguration(typeof(T)) as T;

        /// <summary>
        /// Attempts to provide a <see cref="Naninovel.Configuration"/> object of the specified type 
        /// via <see cref="IConfigurationProvider"/> used to initialize the engine.
        /// </summary>
        /// <param name="type">Type of the requested configuration object.</param>
        public static Configuration GetConfiguration (Type type)
        {
            if (configurationProvider is null)
                throw new Error($"Failed to provide '{type.Name}' configuration object: " +
                                "Configuration provider is not available or the engine is not initialized.");

            return configurationProvider.GetConfiguration(type);
        }

        /// <summary>
        /// Attempts to resolve an <see cref="IEngineService"/> of the specified type.
        /// </summary>
        /// <remarks>
        /// Results per requested types are cached, so it's fine to use this method frequently.
        /// </remarks>
        /// <typeparam name="TService">Type of the requested service.</typeparam>
        /// <returns>First matching service or null, when no matches found.</returns>
        [CanBeNull]
        public static TService GetService<TService> ()
            where TService : class, IEngineService
        {
            return GetService(typeof(TService)) as TService;
        }

        /// <inheritdoc cref="GetService{TService}()"/>
        /// <returns>Whether the service was found.</returns>
        public static bool TryGetService<TService> (out TService result)
            where TService : class, IEngineService
        {
            result = GetService<TService>();
            return result != null;
        }

        /// <summary>
        /// Resolves an <see cref="IEngineService"/> of the specified type; throws when fails.
        /// </summary>
        /// <remarks>
        /// Results per requested types are cached, so it's fine to use this method frequently.
        /// </remarks>
        /// <typeparam name="TService">Type of the requested service.</typeparam>
        /// <returns>First matching service.</returns>
        public static TService GetServiceOrErr<TService> ()
            where TService : class, IEngineService
        {
            return GetService<TService>()
                   ?? throw new Error($"Failed to get '{typeof(TService).FullName}' engine service: " +
                                      "not found or engine is not initialized.");
        }

        /// <inheritdoc cref="GetService{TService}()"/>
        /// <param name="serviceType">Type of the service to resolve.</param>
        [CanBeNull]
        public static IEngineService GetService (Type serviceType)
        {
            if (cachedGetServiceResults.TryGetValue(serviceType, out var cachedResult))
                return cachedResult;
            var result = services.FirstOrDefault(serviceType.IsInstanceOfType);
            if (result is null) return null;
            cachedGetServiceResults[serviceType] = result;
            return result;
        }

        /// <summary>
        /// Attempts to resolve first matching <see cref="IEngineService"/> object from
        /// the services list using specified <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="TService">Type of the requested service.</typeparam>
        /// <param name="predicate">Additional filter to apply when looking for a match.</param>
        /// <returns>First matching service or null, when no matches found.</returns>
        [CanBeNull]
        public static TService FindService<TService> (Predicate<TService> predicate)
            where TService : class, IEngineService
        {
            foreach (var service in services)
                if (service is TService engineService && predicate(engineService))
                    return engineService;
            return null;
        }

        /// <inheritdoc cref="FindService{TService}"/>
        /// <param name="state">Use instead of capturing outside context to prevent allocations.</param>
        [CanBeNull]
        public static TService FindService<TService, TState> (TState state, Func<TService, TState, bool> predicate)
            where TService : class, IEngineService
        {
            foreach (var service in services)
                if (service is TService engineService && predicate(engineService, state))
                    return engineService;
            return null;
        }

        /// <summary>
        /// Collects all the matching <see cref="IEngineService"/> to the specified collection.
        /// </summary>
        /// <typeparam name="TService">Type of the requested services.</typeparam>
        public static void FindAllServices<TService> (ICollection<TService> services)
            where TService : class, IEngineService
        {
            foreach (var service in Engine.services)
                if (service is TService s)
                    services.Add(s);
        }

        /// <inheritdoc cref="FindAllServices{TService}(System.Collections.Generic.ICollection{TService})"/>
        /// <param name="predicate">Additional filter to apply when looking for a match.</param>
        public static void FindAllServices<TService> (ICollection<TService> services,
            Predicate<TService> predicate) where TService : class, IEngineService
        {
            foreach (var service in Engine.services)
                if (service is TService s && predicate(s))
                    services.Add(s);
        }

        /// <inheritdoc cref="FindAllServices{TService}(System.Collections.Generic.ICollection{TService})"/>
        /// <param name="state">Use instead of capturing outside context to prevent allocations.</param>
        /// <param name="predicate">Additional filter to apply when looking for a match.</param>
        public static void FindAllServices<TService, TState> (ICollection<TService> services,
            TState state, Func<TService, TState, bool> predicate) where TService : class, IEngineService
        {
            foreach (var service in Engine.services)
                if (service is TService s && predicate(s, state))
                    services.Add(s);
        }

        /// <summary>
        /// Instantiates the prototype without blocking the main thread and parents it under the engine game object.
        /// </summary>
        /// <param name="prototype">Prototype of the object to instantiate.</param>
        /// <param name="options">Optional parameters.</param>
        public static async Awaitable<T> Instantiate<T> (T prototype, InstantiateOptions options = default,
            AsyncToken token = default) where T : Object
        {
            if (Behaviour is null)
                throw new Error($"Failed to instantiate '{options.Name ?? prototype.name}': engine is not ready. " +
                                "Make sure you're not attempting to instantiate and object inside an engine service " +
                                $"constructor (use '{nameof(IEngineService.InitializeService)}' method instead).");

            var root = options.Parent ? options.Parent : Root.transform;
            var obj = default(T);
            if (Configuration.AsyncInstantiation)
            {
                var ip = new InstantiateParameters { originalImmutable = true, parent = root };
                var handle = Object.InstantiateAsync(prototype, options.Position, options.Rotation, ip);
                while (!handle.isDone) await Async.NextFrame(token);
                if (handle.Result is null || !handle.Result[0]) throw new OperationCanceledException();
                obj = handle.Result[0];
            }
            else obj = Object.Instantiate(prototype, options.Position, options.Rotation, root);

            var go = (obj as GameObject ?? (obj as Component)?.gameObject)!;
            if (options.Scale is { } scale) go.transform.localScale = scale;
            if (!string.IsNullOrEmpty(options.Name)) obj.name = options.Name;

            if (options.Layer.HasValue) go.ForEachDescendant(obj => obj.layer = options.Layer.Value);
            else if (Configuration.OverrideObjectsLayer)
                go.ForEachDescendant(obj => obj.layer = Configuration.ObjectsLayer);

            objects.Add(obj);

            return obj;
        }

        /// <summary>
        /// Creates a new object, making it a child of the engine object and (optionally) adding specified components.
        /// </summary>
        /// <param name="options">Optional parameters.</param>
        /// <param name="components">Components to add on the created object.</param>
        public static GameObject CreateObject (InstantiateOptions options = default, params Type[] components)
        {
            if (Behaviour is null)
                throw new Error($"Failed to create '{options.Name ?? string.Empty}' object: engine is not ready. " +
                                "Make sure you're not attempting to create and object inside an engine service " +
                                $"constructor (use '{nameof(IEngineService.InitializeService)}' method instead).");

            var objName = options.Name ?? "NaninovelObject";
            GameObject go;
            if (components != null) go = new(objName, components);
            else go = new(objName);
            go.transform.SetParent(options.Parent ? options.Parent : Root.transform);
            go.transform.position = options.Position;
            go.transform.rotation = options.Rotation;
            if (options.Scale is { } scale) go.transform.localScale = scale;

            if (options.Layer.HasValue) go.ForEachDescendant(obj => obj.layer = options.Layer.Value);
            else if (Configuration.OverrideObjectsLayer)
                go.ForEachDescendant(obj => obj.layer = Configuration.ObjectsLayer);

            objects.Add(go);

            return go;
        }

        /// <summary>
        /// Creates a new object, making it a child of the engine object and adding the specified component type.
        /// </summary>
        /// <param name="options">Optional parameters.</param>
        public static T CreateObject<T> (InstantiateOptions options = default) where T : Component
        {
            if (Behaviour is null)
                throw new Error(
                    $"Failed to create '{options.Name ?? string.Empty}' object of type '{typeof(T).Name}': " +
                    "engine is not ready. Make sure you're not creating an object inside an engine service " +
                    $"constructor (use '{nameof(IEngineService.InitializeService)}' method instead).");

            var go = new GameObject(options.Name ?? typeof(T).Name);
            go.transform.SetParent(options.Parent ? options.Parent : Root.transform);
            go.transform.position = options.Position;
            go.transform.rotation = options.Rotation;
            if (options.Scale is { } scale) go.transform.localScale = scale;

            if (options.Layer.HasValue) go.ForEachDescendant(obj => obj.layer = options.Layer.Value);
            else if (Configuration.OverrideObjectsLayer)
                go.ForEachDescendant(obj => obj.layer = Configuration.ObjectsLayer);

            objects.Add(go);

            return go.AddComponent<T>();
        }

        /// <summary>
        /// Attempts to find an engine object with the specified name.
        /// Returns null if not found.
        /// </summary>
        public static GameObject FindObject (string name)
        {
            foreach (var obj in objects)
                if (obj && obj is GameObject go && go.name == name)
                    return go;
            return null;
        }

        /// <summary>
        /// Attempts to <see cref="Resources.Load{T}"/> using the specified path, prefixed with "Naninovel".
        /// In case the resource is not found, will raise an exception mentioning the package was probably
        /// modified or is corrupted.
        /// </summary>
        /// <param name="relativePath">Relative (to "Naninovel") path to the resource.</param>
        /// <typeparam name="T">Type of the resource to load.</typeparam>
        public static T LoadInternalResource<T> (string relativePath) where T : Object
        {
            var fullPath = $"Naninovel/{relativePath}";
            var asset = Resources.Load<T>(fullPath);
            if (!asset) throw new Error($"Failed loading internal Naninovel asset at 'Naninovel/Resources/{fullPath}'.");
            return asset;
        }

        /// <summary>
        /// Injects custom logging handler.
        /// </summary>
        public static void UseLogger (ILogger logger)
        {
            Engine.logger = logger;
        }

        /// <summary>
        /// Logs an information object with the current logger.
        /// </summary>
        public static void Log ([CanBeNull] object obj) => logger.Log(obj?.ToString());
        /// <summary>
        /// Logs an information message with the current logger.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="spot">When specified, will prepend the associated script path and line index to the message.</param>
        public static void Log (string message, PlaybackSpot? spot = null) => logger.Log(Format(message, spot));
        /// <summary>
        /// Logs a warning with the current logger.
        /// </summary>
        /// <inheritdoc cref="Log"/>
        public static void Warn (string message, PlaybackSpot? spot = null) => logger.Warn(Format(message, spot));
        /// <summary>
        /// Logs an error message with the current logger.
        /// </summary>
        /// <inheritdoc cref="Log"/>
        public static void Err (string message, PlaybackSpot? spot = null) => logger.Err(Format(message, spot));
        /// <summary>
        /// Creates an exception with the specified message optionally annotated with the specified playback spot.
        /// </summary>
        public static Error Fail (string message, PlaybackSpot? spot = null) => new(Format(message, spot));

        private static string Format (string message, PlaybackSpot? spot)
        {
            if (!spot.HasValue) return message;
            var line = spot.Value.InlineIndex >= 0
                ? $"{spot.Value.LineNumber}.{spot.Value.InlineIndex}"
                : $"{spot.Value.LineNumber}";
            return $"Scenario script '{spot.Value.ScriptPath}' at line #{line}: {message}";
        }

        private static void CheckUnityVersion ()
        {
            var unity = Application.unityVersion;
            if (!ParseUtils.TryInvariantInt(unity.GetBefore("."), out var major) ||
                !ParseUtils.TryInvariantInt(unity.GetBetween("."), out var minor) ||
                !ParseUtils.TryInvariantInt(new(unity.GetAfter(".").TakeWhile(char.IsDigit).ToArray()), out var patch))
                throw Fail($"Failed to parse '{unity}' Unity version.");

            Check((6000, 0, 66), (6000, 3, 6));

            void Check (params (int Major, int Minor, int Patch)[] supported)
            {
                foreach (var ok in supported)
                    if (major == ok.Major && minor == ok.Minor && patch >= ok.Patch)
                        return;
                var nv = EngineVersion.LoadFromResources().Version;
                var sup = string.Join(", ", supported.Select(v => $"{v.Major}.{v.Minor} (patch {v.Patch} or later)"));
                Warn($"Unity {major}.{minor}.{patch} is not supported by Naninovel {nv}. " +
                     $"Supported Unity releases: {sup}. " +
                     "You can mute this warning by disabling 'Check Unity Version' in the engine configuration.");
            }
        }
    }
}

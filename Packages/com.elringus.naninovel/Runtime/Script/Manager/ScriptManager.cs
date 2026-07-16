using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptManager"/>
    [InitializeAtRuntime]
    public class ScriptManager : IStatefulService<GameStateMap>, IScriptManager
    {
        [Serializable]
        public class GameState
        {
            [CanBeNull] public TransientScriptProvider.State Transients;
        }

        public virtual ScriptsConfiguration Configuration { get; }
        public virtual IResourceLoader<Script> ScriptLoader => Loader;
        public virtual IResourceLoader<Script> ExternalScriptLoader => ExternalLoader;
        public virtual int TotalCommandsCount { get; private set; }

        protected virtual IResourceProviderManager Providers { get; }
        protected virtual List<string> AvailableScriptPaths { get; } = new();
        protected virtual EndpointResolver Resolver { get; } = new(Compiler.Symbols);
        protected virtual EndpointSerializer Serializer { get; } = new(Compiler.Symbols);
        protected virtual TransientScriptProvider Transients { get; }
        protected virtual ResourceLoader<Script> Loader { get; private set; }
        protected virtual ResourceLoader<Script> ExternalLoader { get; private set; }

        public ScriptManager (ScriptsConfiguration config, IResourceProviderManager providers)
        {
            Configuration = config;
            Providers = providers;
            Transients = new(this);
        }

        public virtual Awaitable InitializeService ()
        {
            Loader = new(Providers.CollectProviders(Configuration.Loader.ProviderTypes)
                .Append(Transients), Providers, Configuration.Loader.PathPrefix);
            ExternalLoader = Configuration.ExternalLoader.CreateFor<Script>(Providers);
            Loader.CollectPaths(AvailableScriptPaths);
            ExternalLoader.CollectPaths(AvailableScriptPaths);
            TotalCommandsCount = ProjectStats.GetOrDefault().TotalCommandCount;
            return Async.Completed;
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            Loader?.ReleaseAll(this);
            ExternalLoader?.ReleaseAll(this);
        }

        public virtual void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState {
                Transients = Transients.Serialize()
            };
            stateMap.SetState(state);
        }

        public virtual Awaitable LoadServiceState (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            Transients.Deserialize(state?.Transients);
            return Async.Completed;
        }

        public virtual void AddTransientScript (string path, string text)
        {
            if (Transients.Exists(path))
                throw new Error($"Failed to add '{path}' transient script: already registered.");
            Transients.SetScriptText(path, text);
        }

        public virtual void RemoveTransientScript (string path)
        {
            if (!Transients.ScriptExists(path))
                throw new Error($"Failed to remove '{path}' transient script: not registered.");
            Transients.RemoveScript(path);
            Loader.Unload(path);
        }

        public virtual bool HasTransientScript (string path)
        {
            return Transients.ScriptExists(path);
        }

        public virtual Endpoint? ResolveEndpoint (string syntax, string host)
        {
            return Resolver.Resolve(syntax, host, AvailableScriptPaths);
        }

        public virtual string SerializeEndpoint (Endpoint endpoint, string host, EndpointSyntaxTraits traits = default)
        {
            return Serializer.Serialize(endpoint, host, traits);
        }
    }
}

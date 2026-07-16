using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <inheritdoc cref="IResourceProviderManager"/>
    [InitializeAtRuntime]
    public class ResourceProviderManager : IResourceProviderManager
    {
        public virtual ResourceProviderConfiguration Configuration { get; }

        protected virtual Dictionary<string, IResourceProvider> ProviderByType { get; } = new();
        protected virtual Dictionary<Object, HashSet<object>> HoldersByObj { get; } = new();

        public ResourceProviderManager (ResourceProviderConfiguration cfg)
        {
            Configuration = cfg;
        }

        public virtual async Awaitable InitializeService ()
        {
            #if ADDRESSABLES_AVAILABLE
            await UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            #else
            await Async.NextFrame();
            #endif
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            foreach (var provider in ProviderByType.Values)
                provider?.UnloadAll();
            Configuration.MasterProvider?.UnloadAll();
        }

        public virtual bool IsProviderInitialized (string type)
        {
            return ProviderByType.ContainsKey(type);
        }

        public virtual IResourceProvider GetProvider (string type)
        {
            if (!ProviderByType.ContainsKey(type))
                ProviderByType[type] = InitializeProvider(type);
            return ProviderByType[type];
        }

        public virtual void CollectProviders (IList<IResourceProvider> providers, IReadOnlyList<string> types)
        {
            if (Configuration.MasterProvider != null)
                providers.Add(Configuration.MasterProvider);
            foreach (var type in types)
            {
                var provider = GetProvider(type);
                if (provider != null) providers.Add(provider);
            }
        }

        public virtual int Hold (Object obj, object holder)
        {
            if (!obj)
                throw new Error($"Failed to hold '{obj}' object by '{holder}': " +
                                "specified object is invalid (null or destroyed).");
            var holders = GetHolders(obj);
            holders.Add(holder);
            return holders.Count;
        }

        public virtual int Release (Object obj, object holder)
        {
            if (!obj) return 0;
            var holders = GetHolders(obj);
            holders.Remove(holder);
            return holders.Count;
        }

        public virtual int CountHolders (Object obj)
        {
            if (!obj) return 0;
            return GetHolders(obj).Count;
        }

        protected virtual IResourceProvider InitializeProjectProvider ()
        {
            return new ProjectResourceProvider();
        }

        [CanBeNull]
        protected virtual IResourceProvider InitializeAddressableProvider ()
        {
            #if ADDRESSABLES_AVAILABLE
            return new AddressableResourceProvider();
            #else
            return null;
            #endif
        }

        protected virtual IResourceProvider InitializeLocalProvider ()
        {
            var local = new LocalResourceProvider(Configuration.LocalRootPath, Configuration.ReloadScripts);
            local.AddConverter(new NaniToScriptAssetConverter());
            local.AddConverter(new JpgOrPngToTextureConverter());
            local.AddConverter(new WavToAudioClipConverter());
            local.DiscoverResources();
            return local;
        }

        [CanBeNull]
        protected virtual IResourceProvider InitializeProvider (string type)
        {
            var provider = default(IResourceProvider);

            switch (type)
            {
                case ResourceProviderConfiguration.ProjectTypeName:
                    provider = InitializeProjectProvider();
                    break;
                case ResourceProviderConfiguration.AddressableTypeName:
                    provider = InitializeAddressableProvider();
                    break;
                case ResourceProviderConfiguration.LocalTypeName:
                    provider = InitializeLocalProvider();
                    break;
                default:
                    var customType = Type.GetType(type);
                    if (customType is null) throw new Error($"Failed to initialize '{type}' resource provider. Make sure provider types are set correctly in 'Loader' properties of the Naninovel configuration menus.");
                    provider = (IResourceProvider)Activator.CreateInstance(customType);
                    if (provider is null) throw new Error($"Failed to initialize '{type}' custom resource provider. Make sure the implementation has a parameterless constructor.");
                    break;
            }

            return provider;
        }

        protected virtual HashSet<object> GetHolders (Object obj)
        {
            if (HoldersByObj.TryGetValue(obj, out var holders)) return holders;
            holders = new();
            HoldersByObj[obj] = holders;
            return holders;
        }
    }
}

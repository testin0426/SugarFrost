using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <summary>
    /// An empty loader implementation that does nothing
    /// and returns default/null/empty results for all operations.
    /// </summary>
    public class NullResourceLoader : IResourceLoader
    {
        public string GetFullPath (string localPath) => localPath;
        public string GetLocalPath (string fullPath) => fullPath;
        public void CollectLocalPaths (Object obj, ICollection<string> paths) { }
        public string GetLocalPath (Object obj) => null;
        public bool Exists (string path) => false;
        public void CollectPaths (ICollection<string> paths, string prefix = null) { }
        public bool IsLoaded (string path) => false;
        public Resource GetLoaded (string path) => Resource.Invalid;
        public void CollectLoaded (ICollection<Resource> resources) { }
        public IReadOnlyCollection<Resource> CollectLoaded () => Array.Empty<Resource>();
        public Awaitable<Resource> Load (string path, object holder = null) => Async.Result(Resource.Invalid);
        public Awaitable<IReadOnlyCollection<Resource>> LoadAll (string prefix = null, object holder = null) =>
            Async.Result<IReadOnlyCollection<Resource>>(Array.Empty<Resource>());
        public void Hold (string path, object holder) { }
        public void Release (string path, object holder, bool unload = true) { }
        public void ReleaseAll (object holder, bool unload = true) { }
        public bool IsHeldBy (string path, object holder) => false;
        public int CountHolders (string path = null) => 0;
        public void CollectHolders (ISet<object> holders, string path = null) { }
        public Awaitable<Resource> Reload (string path) => Async.Result(Resource.Invalid);
        public void Update (string path, Object obj) { }
    }
}

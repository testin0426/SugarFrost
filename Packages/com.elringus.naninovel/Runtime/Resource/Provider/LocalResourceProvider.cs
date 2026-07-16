using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    public sealed class LocalResourceProvider : ResourceProvider
    {
        private readonly string root;
        private readonly List<IResourceConverter> converters = new();
        private readonly Dictionary<string, FileInfo> fileByResourcePath = new();

        /// <param name="root">
        /// An absolute path to the folder where the resources are located,
        /// or a relative path with one of the available origins:
        /// - %DATA% - <see cref="Application.dataPath"/>
        /// - %PDATA% - <see cref="Application.persistentDataPath"/>
        /// - %STREAM% - <see cref="Application.streamingAssetsPath"/>
        /// - %SPECIAL{F}%, where F is a value from <see cref="Environment.SpecialFolder"/>/>
        /// </param>
        /// <param name="reload">Whether to perform hot-reload when the played '.nani' file is changed.</param>
        public LocalResourceProvider (string root, bool reload = false)
        {
            this.root = ResolveRoot(root);
            if (reload) WatchScripts($"{this.root}/{ScriptsConfiguration.DefaultPathPrefix}");
        }

        /// <summary>
        /// Adds a converter extending the supported resource types.
        /// </summary>
        public void AddConverter (IResourceConverter converter)
        {
            converters.Add(converter);
        }

        /// <summary>
        /// Refreshes the available resources by scanning the root directory using added converters.
        /// </summary>
        public void DiscoverResources ()
        {
            Paths.Clear();
            fileByResourcePath.Clear();

            var rootDir = new DirectoryInfo(root);
            if (!rootDir.Exists) return;

            foreach (var file in rootDir.EnumerateFiles("*", SearchOption.AllDirectories))
            foreach (var converter in converters)
                if (converter.Supports(file.Extension))
                {
                    var resPath = PathUtils.FormatPath(file.FullName).GetAfter(root + "/").GetBeforeLast(".");
                    Paths.Add(resPath);
                    fileByResourcePath[resPath] = file;
                    break;
                }
        }

        protected override async Awaitable<UnityEngine.Object> LoadObject (string resourcePath)
        {
            var file = fileByResourcePath[resourcePath];
            foreach (var converter in converters)
                if (converter.Supports(file.Extension))
                    return converter.Convert(await File.ReadAllBytesAsync(file.FullName), resourcePath);
            throw new Error($"Failed to load '{resourcePath}' resource object: missing local provider converter.");
        }

        protected override void DisposeResource (Resource resource)
        {
            ObjectUtils.DestroyOrImmediate(resource.Object);
        }

        private static string ResolveRoot (string root)
        {
            if (root.Contains("%DATA%")) root = root.Replace("%DATA%", Application.dataPath);
            else if (root.Contains("%PDATA%")) root = root.Replace("%PDATA%", Application.persistentDataPath);
            else if (root.Contains("%STREAM%")) root = root.Replace("%STREAM%", Application.streamingAssetsPath);
            else if (root.Contains("%SPECIAL{"))
            {
                var spec = root.GetBetween("%SPECIAL{", "}%");
                if (!Enum.TryParse<Environment.SpecialFolder>(spec, true, out var folder))
                    throw new Error($"Failed to parse '{root}' special folder for local resource provider root.");
                root = Environment.GetFolderPath(folder) + root.GetAfterFirst("}%");
            }
            return PathUtils.FormatPath(root);
        }

        private static void WatchScripts (string root)
        {
            if (!Directory.Exists(root)) return;
            Engine.GetServiceOrErr<IScriptPlayer>().ReloadEnabled = true;
            var ctx = SynchronizationContext.Current;
            var watcher = new FileSystemWatcher();
            watcher.Path = root;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.nani";
            watcher.Changed += (_, e) => ctx.Post(_ => {
                var path = PathUtils.FormatPath(e.FullPath).GetBetween($"{root}/", ".nani");
                if (Engine.TryGetService<IScriptPlayer>(out var player) && player.MainTrack.PlayedScript?.Path == path)
                    player.Reload().Forget();
            }, null);
            watcher.EnableRaisingEvents = true;
        }
    }
}

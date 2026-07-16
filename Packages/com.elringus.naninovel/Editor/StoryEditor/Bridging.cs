using System;
using Naninovel.Bridging;

namespace Naninovel.StoryEditor
{
    public static class Bridging
    {
        private static readonly VirtualFiles files = new();
        private static readonly Server server = new(files, new JsonSerializer(), new UnityLogger());
        private static CString read, list;

        public static void Initialize ()
        {
            BridgingService.AddServer("StoryEditor", server);
            read = new(10 * 1024); // 10KB
            list = new(10 * 1024);
            files.OnFileChanged += HandleFileChanged;
        }

        public static void Deinitialize ()
        {
            try { BridgingService.RemoveServer("StoryEditor"); }
            catch { }
            read?.Dispose();
            list?.Dispose();
            files.OnFileChanged -= HandleFileChanged;
        }

        public static IntPtr Read (string name)
        {
            return read.Write(files.Read(name));
        }

        public static void Write (string name, string content)
        {
            ThreadUtils.InvokeOnUnityThread(() => files.Write(name, content));
        }

        public static IntPtr List ()
        {
            using var _ = ListPool<string>.Rent(out var names);
            files.List(names);
            return list.Write(string.Join('|', names));
        }

        private static void HandleFileChanged (string name)
        {
            if (StoryEditor.Initialized)
                Native.HandleBridgingChanges(name);
        }
    }
}

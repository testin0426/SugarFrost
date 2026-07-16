using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Naninovel.StoryEditor
{
    /// <summary>
    /// Manages the story editor embedded webview.
    /// </summary>
    public static class StoryEditor
    {
        /// <summary>
        /// Whether the editor is fully initialized and ready to process all commands.
        /// </summary>
        public static bool Initialized => State == StoryEditorState.Initialized;
        /// <summary>
        /// Whether the editor's native library is ready to process a subset of commands.
        /// </summary>
        public static bool NativeReady => (int)State >= 2 && (int)State < 4;
        /// <summary>
        /// Describes the editor's current initialization phase.
        /// </summary>
        public static StoryEditorState State { get; private set; }

        [CanBeNull] private static CancellationTokenSource initCts;
        [CanBeNull] private static string pendingShowScriptPath;
        private static string homeUrl = "https://naninovel.com/editor/";

        /// <summary>
        /// Initializes and embeds the editor webview into the Unity tab.
        /// </summary>
        public static async Task Initialize ()
        {
            if (EditorUtils.IsDomainReloadEnabled) return; // otherwise it crashes Unity editor

            if (State != StoryEditorState.Standby) return;

            State = StoryEditorState.Initializing;
            var cts = initCts = new();

            try
            {
                if (await FindWindow(cts.Token) is not { } window) return;
                if (cts.Token.IsCancellationRequested) return;

                if (EngineVersion.LoadFromResources().Preview) homeUrl = "https://pre.naninovel.com/editor/";
                // homeUrl = "http://localhost:5173/editor/";
                var dataDir = PathUtils.Combine(Application.persistentDataPath, "Naninovel");
                var userDir = PathUtils.Combine(Application.persistentDataPath, "Naninovel/User");
                var projDir = Application.dataPath;
                var filters = string.Join('|',
                    ToProjectFileUri(PackagePath.EditorDataPath),
                    ToProjectFileUri(PackagePath.ScenarioRoot)
                );

                Native.Init(userDir, projDir, filters, HandleAssetsDirty,
                    Bridging.Read, Bridging.Write, Bridging.List, HandleHotReload,
                    window, homeUrl, dataDir, HandleViewNavigation, HandleFocus, HandleViewInitialized);
                State = StoryEditorState.NativeReady;

                Bridging.Initialize();
                while (State != StoryEditorState.Initialized && !cts.IsCancellationRequested)
                    await Task.Delay(33);
                if (cts.IsCancellationRequested) return;

                Selection.selectionChanged += HandleSelectionChanged;
                if (pendingShowScriptPath != null)
                {
                    ShowScript(pendingShowScriptPath);
                    pendingShowScriptPath = null;
                }
            }
            catch (Exception e)
            {
                Deinitialize();
                throw new Error($"Failed to initialize Story Editor: {e}");
            }
            finally
            {
                cts.Dispose();
                if (initCts == cts) initCts = null;
            }
        }

        /// <summary>
        /// Deinitializes the editor and terminates the embedded webview thread.
        /// </summary>
        public static void Deinitialize ()
        {
            if (State == StoryEditorState.Deinitializing) return;
            if (NativeReady)
            {
                State = StoryEditorState.Deinitializing;
                Bridging.Deinitialize();
                Native.Deinit(HandleDeinitialized);
            }
            else State = StoryEditorState.Standby;
            try { initCts?.Cancel(); }
            catch (ObjectDisposedException) { }
            initCts = null;
            Selection.selectionChanged -= HandleSelectionChanged;
        }

        /// <summary>
        /// Opens scenario script with the specified asset path inside the story editor.
        /// </summary>
        public static void ShowScript (string assetPath)
        {
            if (Initialized) Native.PreviewFile(ToProjectFileUri(assetPath));
            else pendingShowScriptPath = assetPath;
            Window.DockWithInspector();
        }

        private static void HandleViewInitialized ()
        {
            ThreadUtils.InvokeOnUnityThread(() => {
                if (State == StoryEditorState.NativeReady)
                    State = StoryEditorState.Initialized;
            });
        }

        private static void HandleDeinitialized ()
        {
            ThreadUtils.InvokeOnUnityThread(() => {
                if (State == StoryEditorState.Deinitializing)
                    State = StoryEditorState.Standby;
            });
        }

        private static void HandleFocus (byte focused)
        {
            ThreadUtils.InvokeOnUnityThread(() => {
                EditorProxy.StoryEditorFocused = focused == 1;
            });
        }

        private static void HandleAssetsDirty ()
        {
            ThreadUtils.InvokeOnUnityThread(AssetDatabase.Refresh);
        }

        private static void HandleHotReload (string text)
        {
            ThreadUtils.InvokeOnUnityThread(() => {
                if (!Engine.Initialized || Engine.Behaviour is not RuntimeBehaviour) return;
                var scripts = Engine.GetServiceOrErr<IScriptManager>();
                var player = Engine.GetServiceOrErr<IScriptPlayer>();
                if (!scripts.Configuration.HotReloadScripts || !player.MainTrack.PlayedScript) return;
                var path = player.MainTrack.PlayedScript.Path;
                scripts.ScriptLoader.Update(path, Compiler.CompileScript(path, text));
                player.Reload().Forget();
            });
        }

        private static byte HandleViewNavigation (string url)
        {
            if (url.StartsWithOrdinal(homeUrl)) return 1;
            ThreadUtils.InvokeOnUnityThread(() => Application.OpenURL(url));
            return 0;
        }

        private static async Task<IntPtr?> FindWindow (CancellationToken token)
        {
            var window = IntPtr.Zero;
            while (!token.IsCancellationRequested && window == IntPtr.Zero)
                if ((window = Native.FindWindow(ResolveTitle())) == IntPtr.Zero)
                    await Task.Delay(100, token);
            return window == IntPtr.Zero ? null : window;

            static string ResolveTitle ()
            {
                // On Windows, HWND title equals the C# full type name of the tab (both docked und undocked).
                if (Application.platform == RuntimePlatform.WindowsEditor) return typeof(Window).FullName;
                // On macOS, when undocked, the window title equals the display title of the tab.
                // When docked, the tab is an NSView without any explicit identifiable traits,
                // so we use the tab's screen-space center position to find the associated NSView.
                var tab = EditorWindow.GetWindow<Window>();
                if (!tab.docked) return tab.titleContent.text;
                return FormattableString.Invariant($"{tab.position.center.x}|{tab.position.center.y}");
            }
        }

        private static void HandleSelectionChanged ()
        {
            if (!Initialized) return;
            if (!Configuration.GetOrDefault<ScriptsConfiguration>().ShowSelectedScript) return;
            if (Selection.activeObject is not Script script || Selection.objects.Length > 1) return;
            var assetPath = AssetDatabase.GetAssetPath(script);
            if (!string.IsNullOrEmpty(assetPath))
                Native.PreviewFile(ToProjectFileUri(assetPath));
        }

        private static string ToProjectFileUri (string assetPath)
        {
            return assetPath.GetAfterFirst("Assets");
        }
    }
}

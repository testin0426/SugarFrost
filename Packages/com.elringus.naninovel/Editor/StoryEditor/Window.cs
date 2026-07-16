using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel.StoryEditor
{
    /// <summary>
    /// An editor window embedding the <see cref="StoryEditor"/> app.
    /// </summary>
    public class Window : EditorWindow
    {
        private readonly ILogger logger = new UnityLogger();
        private VisualElement root;
        private Rect lastRect;
        private bool lastMaximized;

        /// <summary>
        /// Opens a new Story Editor window and docks it under the tabset which has an Inspector window docked.
        /// </summary>
        [MenuItem(MenuPath.Root + "/Story Editor", priority = 2)]
        public static void DockWithInspector ()
        {
            if (Application.platform == RuntimePlatform.LinuxEditor ||
                Application.platform == RuntimePlatform.OSXEditor &&
                RuntimeInformation.ProcessArchitecture != Architecture.Arm64 ||
                Application.platform == RuntimePlatform.WindowsEditor &&
                RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                Engine.Warn("Story Editor is not supported on this platform. Supported platforms include " +
                            "Windows with an x86-64 CPU and macOS with an Apple Silicon (ARM64) CPU. " +
                            "Consider using the web version instead: naninovel.com/editor");
                return;
            }

            var window = GetWindow<Window>(Type.GetType("UnityEditor.InspectorWindow, UnityEditor"));
            window.titleContent = new("Story Editor", GUIContents.ScriptAssetIcon.image);
            window.minSize = new(400, 300);
            window.Update(); // required to force the editor init when opening on a first package import
        }

        /// <summary>
        /// Closes all Story Editor windows and deinitializes the editor app.
        /// </summary>
        public static void CloseAll ()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<Window>())
                window.Close();
            StoryEditor.Deinitialize();
        }

        private void CreateGUI ()
        {
            root = rootVisualElement;
            root.style.flexGrow = 1;
            lastMaximized = maximized;
        }

        private void Update ()
        {
            if (StoryEditor.State == StoryEditorState.Standby)
            {
                lastRect = default;
                StoryEditor.Initialize().Forget(logger);
            }
        }

        private void OnGUI ()
        {
            if (EditorUtils.IsDomainReloadEnabled)
            {
                EditorGUILayout.HelpBox(
                    "Disable domain reload to use Story Editor: go to 'Edit -> Project Settings -> Editor' and set " +
                    "'When entering Play Mode' to 'Do not reload Domain or Scene'.", MessageType.Warning);
                return;
            }

            if (lastMaximized != maximized)
            {
                lastMaximized = maximized;
                lastRect = default;
                StoryEditor.Deinitialize();
                return;
            }

            if (StoryEditor.NativeReady)
                SyncWindowRect();
        }

        private void OnBeforeRemovedAsTab ()
        {
            StoryEditor.Deinitialize();
        }

        private void OnDisable ()
        {
            StoryEditor.Deinitialize();
        }

        private void OnBecameVisible ()
        {
            if (StoryEditor.NativeReady)
                Native.SetViewVisible(1);
        }

        private void OnBecameInvisible ()
        {
            if (StoryEditor.NativeReady)
                Native.SetViewVisible(0);
        }

        private void SyncWindowRect ()
        {
            var rect = root.layout;
            if (rect == lastRect) return;
            lastRect = rect;
            Native.ResizeView(rect.x, rect.y, rect.width, rect.height);
        }
    }
}

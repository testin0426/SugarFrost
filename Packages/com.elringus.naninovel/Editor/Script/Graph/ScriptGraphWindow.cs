using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Naninovel
{
    /// <summary>
    /// Hosts <see cref="ScriptGraphView"/>.
    /// </summary>
    public class ScriptGraphWindow : EditorWindow
    {
        private ScriptGraphView graphView;

        [MenuItem(MenuPath.Root + "/Legacy/Script Graph")]
        public static void OpenWindow ()
        {
            GetWindow<ScriptGraphWindow>("Script Graph", true);
        }

        private void OnEnable ()
        {
            var cfg = Configuration.GetOrDefault<ScriptsConfiguration>();
            var state = ScriptGraphState.LoadOrDefault();
            using var _ = Assets.RentWithPrefix(cfg.Loader.PathPrefix, out var assets);
            var scripts = new List<Script>();
            for (int i = 0; i < assets.Count; i++)
            {
                var resource = assets[i];
                var progress = i / (float)assets.Count;
                EditorUtility.DisplayProgressBar(ScriptGraphView.ProgressBarTitle,
                    $"Loading '{resource.Path}' script...", progress);

                var path = AssetDatabase.GUIDToAssetPath(resource.Guid);
                if (string.IsNullOrEmpty(path)) continue;

                var script = AssetDatabase.LoadAssetAtPath<Script>(path);
                if (!ObjectUtils.IsValid(script)) continue;
                scripts.Add(script);
            }

            graphView = new(cfg, state, scripts);
            graphView.name = "Script Graph";
            rootVisualElement.Add(graphView);
            graphView.StretchToParentSize();
        }

        private void OnDisable ()
        {
            graphView?.SerializeState();
            rootVisualElement.Remove(graphView);
        }
    }
}

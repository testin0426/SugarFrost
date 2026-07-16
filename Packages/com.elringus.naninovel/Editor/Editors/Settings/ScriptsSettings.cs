using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Cfg = Naninovel.ScriptsConfiguration;

namespace Naninovel
{
    public class ScriptsSettings : ResourcefulSettings<ScriptsConfiguration>
    {
        protected override string HelpUri => "guide/scenario-scripting";
        protected override Type ResourcesTypeConstraint => typeof(Script);
        protected override string ResourcesPrefix => Configuration.Loader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@goto %name%` in scenario scripts to load and start playing the selected scenario script.";

        private static readonly string[] implementations, labels;
        private static readonly GUIContent rootContent = new("Scenario Root", "The common ancestor directory of all the scenario scripts ('.nani' files). The location is resolved automatically based on the existing scenario files in the project.");
        private static readonly GUIContent refreshContent = new("Refresh", "Re-discover the scenario root and update resource paths of all the scenario script assets in the project.");

        static ScriptsSettings ()
        {
            InitializeImplementationOptions<IScriptCompiler>(ref implementations, ref labels);
        }

        protected override void DrawConfigurationEditor ()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(rootContent, new GUIContent(PackagePath.ScenarioRoot));
                if (GUILayout.Button(refreshContent, EditorStyles.miniButton, GUILayout.Width(65)))
                    ScriptPathRefresher.RefreshAll();
            }
            base.DrawConfigurationEditor();
        }

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var d = base.OverrideConfigurationDrawers();
            d[nameof(Cfg.ScriptCompiler)] = p => DrawImplementations(implementations, labels, p);
            d[nameof(Cfg.WatchScripts)] = p => OnChanged(ScriptFileWatcher.Initialize, p);
            d[nameof(Cfg.ExternalLoader)] = p => DrawWhen(Configuration.EnableCommunityModding, p);

            d[nameof(Cfg.ShowSelectedScript)] = p => DrawWhen(!IsLegacy, p);

            d[nameof(Cfg.EnableVisualEditor)] = p => DrawWhen(IsLegacy, p);
            d[nameof(Cfg.HideUnusedParameters)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.InsertLineKey)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.InsertLineModifier)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.SaveScriptKey)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.SaveScriptModifier)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.EditorPageLength)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.EditorCustomStyleSheet)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.SelectPlayedScript)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.RewindMouseButton)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.RewindModifier)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.IndentLineKey)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.IndentLineModifier)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.UnindentLineKey)] = p => DrawWhen(IsLegacyEditor, p);
            d[nameof(Cfg.UnindentLineModifier)] = p => DrawWhen(IsLegacyEditor, p);

            d[nameof(Cfg.GraphOrientation)] = p => DrawWhen(IsLegacy, p);
            d[nameof(Cfg.GraphAutoAlignPadding)] = p => DrawWhen(IsLegacy, p);
            d[nameof(Cfg.ShowSynopsis)] = p => DrawWhen(IsLegacy, p);
            d[nameof(Cfg.GraphCustomStyleSheet)] = p => DrawWhen(IsLegacy, p);

            d[nameof(Cfg.CompilerLocalization)] = DrawCompilerLocalizationProperty;
            return d;
        }

        private bool IsLegacy => !Configuration.EnableStoryEditor;
        private bool IsLegacyEditor => IsLegacy && Configuration.EnableVisualEditor;

        [MenuItem(MenuPath.Root + "/Resources/Scripts")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();

        private void DrawCompilerLocalizationProperty (SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property);
            if (property.objectReferenceValue) return;

            var path = PathUtils.Combine(PackagePath.PrefabsPath, "DefaultCompiler.asset");
            var asset = AssetDatabase.LoadAssetAtPath<CompilerLocalization>(path);
            property.objectReferenceValue = asset;
        }
    }
}

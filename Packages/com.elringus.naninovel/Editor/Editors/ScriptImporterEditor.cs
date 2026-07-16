using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    [CustomEditor(typeof(ScriptImporter))]
    public class ScriptImporterEditor : ScriptedImporterEditor
    {
        public override bool showImportedObject => false;
        [CanBeNull] public ScriptView VisualEditor { get; private set; }
        [CanBeNull] public Script ScriptAsset { get; private set; }

        private static readonly string legacyInfo = "Looking for the legacy visual script editor?\nYou can re-enable it by disabling Story Editor in the scripts configuration.\nNote that the legacy editor will be removed in the next Naninovel release.";
        private static readonly GUIContent openEditorContent = new("Open in Story Editor", "Show the scenario script inside the Story Editor.");

        private static readonly MethodInfo drawHeaderMethod;
        private static ScriptsConfiguration cfg;
        [CanBeNull] private string scriptText;

        static ScriptImporterEditor ()
        {
            drawHeaderMethod = typeof(Editor)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "DrawHeaderGUI" && m.GetParameters().Length == 2);
        }

        public override void OnEnable ()
        {
            base.OnEnable();

            if (!cfg) cfg = Configuration.GetOrDefault<ScriptsConfiguration>();
            if (cfg.EnableStoryEditor || !cfg.EnableVisualEditor) return;

            ScriptAsset = assetTarget as Script;
            if (!ScriptAsset) return;

            scriptText = File.ReadAllText(AssetDatabase.GetAssetPath(ScriptAsset));
            VisualEditor = new(cfg, ApplyRevertHackGUI, ApplyAndImportChecked);
            VisualEditor.GenerateForScript(scriptText, ScriptAsset);
            ScriptFileWatcher.OnModified += HandleScriptModified;
        }

        public override void OnDisable ()
        {
            if (ScriptAsset && ScriptView.ScriptModified &&
                EditorUtility.DisplayDialog("Save changes?",
                    $"Script '{ScriptAsset.Path}' has some un-saved changes. " +
                    "Would you like to keep or revert them?", "Save", "Revert"))
                ApplyAndImportChecked();

            base.OnDisable();

            if (VisualEditor != null) ScriptFileWatcher.OnModified -= HandleScriptModified;
            ScriptAsset = null;
        }

        public override VisualElement CreateInspectorGUI ()
        {
            return VisualEditor;
        }

        public override bool HasModified ()
        {
            if (VisualEditor == null) return base.HasModified();
            return false;
        }

        protected override void Apply ()
        {
            base.Apply();

            if (VisualEditor is null) return;

            var scriptText = VisualEditor.GenerateText();
            var scriptPath = AssetDatabase.GetAssetPath(ScriptAsset);
            File.WriteAllText(scriptPath, scriptText);
            ScriptView.ScriptModified = false;
        }

        public override void OnInspectorGUI ()
        {
            if (VisualEditor != null)
            {
                ApplyRevertHackGUI();
                return;
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(openEditorContent, GUIStyles.NavigationButton) && assetTarget is Script script)
            {
                var assetPath = AssetDatabase.GetAssetPath(script);
                if (!string.IsNullOrEmpty(assetPath))
                    StoryEditor.StoryEditor.ShowScript(assetPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(legacyInfo, MessageType.Info);

            ApplyRevertGUI();
        }

        public void ApplyRevertHackGUI ()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(99999); // Hide the apply-revert buttons.
            ApplyRevertGUI(); // Required to prevent errors in the editor.
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(-EditorGUIUtility.singleLineHeight); // Hide empty line.
        }

        protected override void OnHeaderGUI ()
        {
            if (!ScriptAsset || drawHeaderMethod == null)
            {
                base.OnHeaderGUI();
                return;
            }

            var headerRect = (Rect)drawHeaderMethod.Invoke(null, new object[] { this, ScriptAsset.Path + (ScriptView.ScriptModified ? "*" : string.Empty) });
            using (new EditorGUI.DisabledScope(!ScriptView.ScriptModified))
            {
                var applyButtonRect = new Rect(new(headerRect.xMax - 98, headerRect.yMax - 26), new(48, 25));
                if (GUI.Button(applyButtonRect, "Apply", EditorStyles.miniButton))
                {
                    GUI.FocusControl(null);
                    ApplyAndImportChecked();
                }
                var revertButtonRect = new Rect(new(headerRect.xMax - 150, headerRect.yMax - 26), new(50, 25));
                if (GUI.Button(revertButtonRect, "Revert", EditorStyles.miniButton))
                {
                    GUI.FocusControl(null);
                    DiscardChanges();
                    if (ScriptAsset && VisualEditor != null)
                        VisualEditor.GenerateForScript(scriptText, ScriptAsset, true);
                }
            }
        }

        private void ApplyAndImportChecked ()
        {
            if (VisualEditor == null || !ScriptView.ScriptModified || !ObjectUtils.IsValid(ScriptAsset)) return;

            // Make sure the script actually changed before invoking `ApplyAndImport()`;
            // in case the generated script text will be the same as it was, Unity editor will 
            // fail internally and loose reference to the edited asset target.
            var scriptPath = AssetDatabase.GetAssetPath(ScriptAsset);
            var modifiedScriptText = VisualEditor.GenerateText();
            var savedScriptText = File.ReadAllText(scriptPath);
            if (modifiedScriptText == savedScriptText)
            {
                ScriptView.ScriptModified = false;
                return;
            }
            SaveChanges();
        }

        private void HandleScriptModified (string assetPath)
        {
            if (VisualEditor == null) return;

            var curPath = AssetDatabase.GetAssetPath(ScriptAsset);
            if (curPath != assetPath) return;

            ScriptAsset = AssetDatabase.LoadAssetAtPath<Script>(assetPath);
            scriptText = File.ReadAllText(AssetDatabase.GetAssetPath(ScriptAsset));
            VisualEditor.GenerateForScript(scriptText, ScriptAsset);
        }
    }
}

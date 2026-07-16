using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Naninovel.Syntax;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class TextIdentifierWindow : EditorWindow
    {
        private static readonly GUIContent scriptsContent = new("Scripts", "Assign scripts to modify. Will modify all scripts in the project when not assigned.");
        private const string identifyAllLabel = "Identify All Scripts";
        private const string identifySelectedLabel = "Identify Selected Scripts";
        private const string unIdentifyAllLabel = "Un-Identify All Scripts";
        private const string unIdentifySelectedLabel = "Un-Identify Selected Scripts";

        private bool assignedScripts => scripts != null && scripts.Count > 0;

        [SerializeField] private List<Script> scripts = new();

        private readonly ScriptTextIdentifier identifier = new();
        private ScriptRevisions revisions;
        private SerializedObject so;
        private SerializedProperty scriptsProp;

        [MenuItem(MenuPath.Root + "/Tools/Text Identifier")]
        public static void OpenWindow ()
        {
            GetWindow<TextIdentifierWindow>("Text Identifier");
        }

        private void OnEnable ()
        {
            revisions = ScriptRevisions.LoadOrDefault();
            so = new SerializedObject(this);
            scriptsProp = so.FindProperty(nameof(scripts));
        }

        private void OnGUI ()
        {
            using var _ = EditorUtils.ScrollView(nameof(CharacterExtractorWindow));

            so.Update();

            EditorGUILayout.LabelField("Text Identifier Utility", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Assigns unique identifiers to the text in the scenario scripts so that localization, voice over and other mapping persist scenario edits.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(scriptsProp, scriptsContent);
            EditorGUILayout.Space();

            if (GUILayout.Button(assignedScripts ? identifySelectedLabel : identifyAllLabel, GUIStyles.NavigationButton))
                try { Identify(); }
                finally { EditorUtility.ClearProgressBar(); }

            if (GUILayout.Button(assignedScripts ? unIdentifySelectedLabel : unIdentifyAllLabel, GUIStyles.NavigationButton))
                try { UnIdentify(); }
                finally { EditorUtility.ClearProgressBar(); }

            EditorGUILayout.Space();

            so.ApplyModifiedProperties();
        }

        private void Identify ()
        {
            if (!EditorUtility.DisplayDialog("Identify scenario text?",
                    "Are you sure you want to identify the scenario text?\n\n" +
                    "This will modify the scenario script assets and append text identifiers to the identifiable text. Existing text identifiers won't be modified, unless a conflict is detected. Make sure to backup the project before proceeding.", "Identify", "Cancel")) return;
            var scripts = assignedScripts ? this.scripts : GetAllScripts();
            for (int i = 0; i < scripts.Count; i++)
                if (!Progress(scripts.Count, i, scripts[i].Path))
                    throw new OperationCanceledException("Identification cancelled by the user.");
                else IdentifyScript(scripts[i], i);
            revisions.SaveAsset();
        }

        private void UnIdentify ()
        {
            if (!EditorUtility.DisplayDialog("Un-Identify scenario text?",
                    "Are you sure you want to un-identify the scenario text?\n\n" +
                    "This will modify the scenario script assets and remove text identifiers from the identifiable text. Any existing mappings, such as localization and voice over will break. Make sure to backup the project before proceeding.", "Un-Identify", "Cancel")) return;
            var scripts = assignedScripts ? this.scripts : GetAllScripts();
            for (int i = 0; i < scripts.Count; i++)
                if (!Progress(scripts.Count, i, scripts[i].Path))
                    throw new OperationCanceledException("Identification cancelled by the user.");
                else UnIdentifyScript(scripts[i], i);
            revisions.SaveAsset();
        }

        private void IdentifyScript (Script script, int idx)
        {
            var assetPath = AssetDatabase.GetAssetPath(script);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var options = new ScriptTextIdentifier.Options(revisions.GetRevision(guid), assetPath);
            var result = identifier.Identify(script, options);
            if (result.ModifiedLines.Count == 0) return;
            FormatModifiedLines(assetPath, script, result.ModifiedLines);
            revisions.SetRevision(guid, result.Revision);
        }

        private void UnIdentifyScript (Script script, int idx)
        {
            var assetPath = AssetDatabase.GetAssetPath(script);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var options = new ScriptTextIdentifier.Options(0, assetPath);
            var result = identifier.UnIdentify(script, options);
            if (result.ModifiedLines.Count == 0) return;
            FormatModifiedLines(assetPath, script, result.ModifiedLines);
            revisions.SetRevision(guid, 0);
            EditorUtility.SetDirty(script);
            AssetDatabase.SaveAssetIfDirty(script);
        }

        private bool Progress (int length, int index, string path)
        {
            var info = $"Processing {path}...";
            var progress = index / (float)length;
            return !EditorUtility.DisplayCancelableProgressBar("Identifying Scenario", info, progress);
        }

        private static List<Script> GetAllScripts ()
        {
            return AssetDatabase.FindAssets("t:Naninovel.Script", new[] { PackagePath.ScenarioRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Script>)
                .ToList();
        }

        private void FormatModifiedLines (string assetPath, Script script, IReadOnlyList<int> modifiedLineIndexes)
        {
            var scriptText = File.ReadAllText(assetPath);
            var lines = ScriptParser.SplitText(scriptText);
            foreach (var modifiedLineIndex in modifiedLineIndexes)
                if (lines.IsIndexValid(modifiedLineIndex) && script.Lines.IsIndexValid(modifiedLineIndex))
                    lines[modifiedLineIndex] = Compiler.ScriptFormatter.Format(script.Lines[modifiedLineIndex], script.TextMap);
                else Engine.Warn($"Failed to identify '{EditorUtils.BuildAssetLink(script, modifiedLineIndex)}' script text: incorrect line index.");
            File.WriteAllText(assetPath, string.Join("\n", lines));
        }
    }
}

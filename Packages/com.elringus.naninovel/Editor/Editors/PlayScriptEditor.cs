using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomEditor(typeof(PlayScript)), CanEditMultipleObjects]
    public class PlayScriptEditor : Editor
    {
        private static readonly GUIContent optionsContent = new("Playback Options", "Optional preferences for the 'Script Text' execution.");
        private static bool showOptions;

        private SerializedProperty scriptText;
        private SerializedProperty gotoScript;
        private SerializedProperty gotoLabel;
        private SerializedProperty playOnAwake;
        private SerializedProperty completeOnContinue;
        private SerializedProperty disableAwaitInput;
        private SerializedProperty disableAutoPlay;
        private SerializedProperty disableSkip;

        private void OnEnable ()
        {
            scriptText = serializedObject.FindProperty("scriptText");
            gotoScript = serializedObject.FindProperty("gotoScript");
            gotoLabel = serializedObject.FindProperty("gotoLabel");
            playOnAwake = serializedObject.FindProperty("playOnAwake");
            completeOnContinue = serializedObject.FindProperty("completeOnContinue");
            disableAwaitInput = serializedObject.FindProperty("disableAwaitInput");
            disableAutoPlay = serializedObject.FindProperty("disableAutoPlay");
            disableSkip = serializedObject.FindProperty("disableSkip");
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(scriptText);
            EditorGUILayout.PropertyField(gotoScript);
            if (!string.IsNullOrEmpty(gotoScript.stringValue))
                EditorGUILayout.PropertyField(gotoLabel);
            if (showOptions = EditorGUILayout.Foldout(showOptions, optionsContent, true))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(playOnAwake);
                EditorGUILayout.PropertyField(completeOnContinue);
                EditorGUILayout.PropertyField(disableAwaitInput);
                EditorGUILayout.PropertyField(disableAutoPlay);
                EditorGUILayout.PropertyField(disableSkip);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

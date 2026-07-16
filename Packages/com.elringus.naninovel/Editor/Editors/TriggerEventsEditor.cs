using JetBrains.Annotations;
using UnityEditor;

namespace Naninovel
{
    [CustomEditor(typeof(TriggerEvents)), CanEditMultipleObjects]
    public class TriggerEventsEditor : Editor
    {
        private SerializedProperty activatable;
        private SerializedProperty collideWith;
        private SerializedProperty raycastFrom;
        [CanBeNull] private SerializedProperty performInput;
        private SerializedProperty hoverWithPointer;
        private SerializedProperty triggerActivated;
        private SerializedProperty triggerConstraintsMet;
        private SerializedProperty triggerConstraintsUnmet;
        private SerializedProperty triggerColliderEntered;
        private SerializedProperty triggerColliderExited;
        private SerializedProperty triggerRaycastEntered;
        private SerializedProperty triggerRaycastExited;
        private SerializedProperty triggerPointerEntered;
        private SerializedProperty triggerPointerExited;

        private void OnEnable ()
        {
            activatable = serializedObject.FindProperty("Activatable");
            collideWith = serializedObject.FindProperty("CollideWith");
            raycastFrom = serializedObject.FindProperty("RaycastFrom");
            performInput = serializedObject.FindProperty("PerformInput");
            hoverWithPointer = serializedObject.FindProperty("HoverWithPointer");
            triggerActivated = serializedObject.FindProperty("TriggerActivated");
            triggerConstraintsMet = serializedObject.FindProperty("TriggerConstraintsMet");
            triggerConstraintsUnmet = serializedObject.FindProperty("TriggerConstraintsUnmet");
            triggerColliderEntered = serializedObject.FindProperty("TriggerColliderEntered");
            triggerColliderExited = serializedObject.FindProperty("TriggerColliderExited");
            triggerRaycastEntered = serializedObject.FindProperty("TriggerRaycastEntered");
            triggerRaycastExited = serializedObject.FindProperty("TriggerRaycastExited");
            triggerPointerEntered = serializedObject.FindProperty("TriggerPointerEntered");
            triggerPointerExited = serializedObject.FindProperty("TriggerPointerExited");
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(activatable);
            EditorGUILayout.PropertyField(collideWith);
            EditorGUILayout.PropertyField(raycastFrom);
            if (performInput != null) EditorGUILayout.PropertyField(performInput);
            EditorGUILayout.PropertyField(hoverWithPointer);

            EditorGUILayout.PropertyField(triggerActivated);

            if (!string.IsNullOrWhiteSpace(collideWith.stringValue) ||
                !string.IsNullOrWhiteSpace(raycastFrom.stringValue) ||
                hoverWithPointer.boolValue)
            {
                EditorGUILayout.PropertyField(triggerConstraintsMet);
                EditorGUILayout.PropertyField(triggerConstraintsUnmet);
            }

            if (!string.IsNullOrWhiteSpace(collideWith.stringValue))
            {
                EditorGUILayout.PropertyField(triggerColliderEntered);
                EditorGUILayout.PropertyField(triggerColliderExited);
            }

            if (!string.IsNullOrWhiteSpace(raycastFrom.stringValue))
            {
                EditorGUILayout.PropertyField(triggerRaycastEntered);
                EditorGUILayout.PropertyField(triggerRaycastExited);
            }

            if (hoverWithPointer.boolValue)
            {
                EditorGUILayout.PropertyField(triggerPointerEntered);
                EditorGUILayout.PropertyField(triggerPointerExited);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

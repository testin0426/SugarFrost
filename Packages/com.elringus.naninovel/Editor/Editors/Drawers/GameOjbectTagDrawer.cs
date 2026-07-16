using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Naninovel
{
    [CustomPropertyDrawer(typeof(GameObjectTagAttribute))]
    public class GameObjectTagDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            var tags = InternalEditorUtility.tags;
            var displayTags = new GUIContent[tags.Length];
            for (int i = 0; i < tags.Length; i++)
                displayTags[i] = tags[i] == "Untagged" ? new("None (Disabled)") : new(tags[i]);
            var idx = Array.IndexOf(tags, string.IsNullOrEmpty(property.stringValue) ? "Untagged" : property.stringValue);
            idx = EditorGUI.Popup(position, label, idx, displayTags);
            property.stringValue = tags[idx] == "Untagged" ? "" : tags[idx];
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Editor drawer helpers for the <see cref="Assets"/> registry.
    /// </summary>
    public static class AssetPathDrawer
    {
        /// <inheritdoc cref="DrawPathPopup(Rect,SerializedProperty,string,string,string)"/>
        public static void DrawPathPopup (SerializedProperty property, string prefix,
            string emptyOption = null, GUIContent label = null)
        {
            DrawPathPopup(EditorGUILayout.GetControlRect(), property, prefix, emptyOption, label);
        }

        /// <summary>
        /// Draws a dropdown selection list of strings fed by existing asset paths records.
        /// </summary>
        /// <param name="property">The property for which to assign value of the selected element.</param>
        /// <param name="prefix">The type of resources to include.</param>
        /// <param name="emptyOption">When specified, will include an additional option with the specified name and <see cref="string.Empty"/> value to the list.</param>
        public static void DrawPathPopup (Rect rect, SerializedProperty property, string prefix,
            string emptyOption = null, GUIContent label = null)
        {
            using var _ = Assets.RentWithPrefix(prefix, out var assets);
            if (assets.Count == 0)
            {
                EditorGUI.PropertyField(rect, property, label ?? new(property.displayName, property.tooltip), true);
                return;
            }

            var value = property.stringValue;
            var menu = new GenericMenu();
            menu.allowDuplicateNames = true;
            if (emptyOption != null)
            {
                AddMenuItem(emptyOption);
                menu.AddSeparator("");
            }
            foreach (var asset in assets)
                AddMenuItem(asset.Path);

            label = EditorGUI.BeginProperty(Rect.zero, label ?? new(property.displayName, property.tooltip), property);
            rect = EditorGUI.PrefixLabel(rect, label);

            if (EditorGUI.DropdownButton(rect, new(string.IsNullOrEmpty(value) ? emptyOption : value), default))
                menu.DropDown(rect);
            EditorGUI.EndProperty();

            void AddMenuItem (string path)
            {
                var selected = path == value || path == emptyOption && string.IsNullOrEmpty(value);
                menu.AddItem(new(path), selected, OnItemSelected, path);
            }

            void OnItemSelected (object item)
            {
                var path = (string)item;
                property.stringValue = path == emptyOption ? string.Empty : path;
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }
    }
}

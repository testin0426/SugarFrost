using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomPropertyDrawer(typeof(ResourcePopupAttribute))]
    [CustomPropertyDrawer(typeof(ActorPopupAttribute))]
    public class ResourcesPopupPropertyDrawer : PropertyDrawer
    {
        private static readonly GUIContent tButtonContent = new("T", "Input the value manually as text.");
        private static readonly float tButtonWidth = 20f;

        private bool tEnabled;

        public override void OnGUI (Rect rect, SerializedProperty property, GUIContent label)
        {
            DrawTButton(rect);
            DrawProperty(rect, property, label);
        }

        private void DrawTButton (Rect rect)
        {
            var buttonRect = new Rect(rect.xMax - tButtonWidth, rect.y, tButtonWidth, rect.height);
            tEnabled = GUI.Toggle(buttonRect, tEnabled, tButtonContent, EditorStyles.miniButton);
        }

        private void DrawProperty (Rect rect, SerializedProperty property, GUIContent label)
        {
            var propRect = new Rect(rect.x, rect.y, rect.width - tButtonWidth - 2f, rect.height);
            if (tEnabled) EditorGUI.PropertyField(propRect, property, label);
            else if (attribute is ResourcePopupAttribute a)
                AssetPathDrawer.DrawPathPopup(propRect, property, a.Prefix, ResourcePopupAttribute.EmptyValue, label);
        }
    }
}

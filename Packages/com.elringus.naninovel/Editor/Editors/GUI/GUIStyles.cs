using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class GUIStyles
    {
        public static readonly GUIStyle NavigationButton;
        public static readonly GUIStyle IconButton;
        public static readonly GUIStyle RichLabelStyle;
        public static readonly GUIStyle ResourceTooltipStyle;

        static GUIStyles ()
        {
            NavigationButton = new("AC Button");
            NavigationButton.stretchWidth = true;
            NavigationButton.fixedWidth = 0;

            IconButton = GetStyle("IconButton");

            RichLabelStyle = new(GUI.skin.label);
            RichLabelStyle.richText = true;

            ResourceTooltipStyle = GetStyle("HelpBox");
            ResourceTooltipStyle.margin = new();
            ResourceTooltipStyle.padding = new(5, 5, 5, 5);
        }

        private static GUIStyle GetStyle (string styleName)
        {
            var style = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (style is null) throw new Error($"Missing built-in editor GUI style '{styleName}'.");
            return style;
        }
    }
}

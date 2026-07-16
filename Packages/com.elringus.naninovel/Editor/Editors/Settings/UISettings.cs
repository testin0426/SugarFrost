using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class UISettings : ResourcefulSettings<UIConfiguration>
    {
        protected override string HelpUri => "guide/user-interface#ui-customization";

        protected override Type ResourcesTypeConstraint => typeof(GameObject);
        protected override string ResourcesPrefix => Configuration.UILoader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@showUI %name%` to show and `@hideUI %name%` to hide the UI.";

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(UIConfiguration.ObjectsLayer)] = p => {
                if (!Configuration.OverrideObjectsLayer) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, p);
                p.intValue = EditorGUILayout.LayerField(label, p.intValue);
                EditorGUI.EndProperty();
            };
            return drawers;
        }

        [MenuItem(MenuPath.Root + "/Resources/UI")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}

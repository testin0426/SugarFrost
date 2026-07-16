using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class InputSettings : ConfigurationSettings<InputConfiguration>
    {
        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        private bool inputSystemInstalled => true;
        #else
        private bool inputSystemInstalled => false;
        #endif

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(InputConfiguration.SpawnEventSystem)] = p => DrawWhen(inputSystemInstalled, p);
            drawers[nameof(InputConfiguration.EventSystem)] = p => DrawWhen(inputSystemInstalled && Configuration.SpawnEventSystem, p);
            drawers[nameof(InputConfiguration.ActionMaps)] = p => DrawWhen(inputSystemInstalled, p);
            drawers[nameof(InputConfiguration.DetectInputMode)] = p => DrawWhen(inputSystemInstalled, p);
            return drawers;
        }

        protected override void DrawConfigurationEditor ()
        {
            base.DrawConfigurationEditor();

            if (!inputSystemInstalled)
                EditorGUILayout.HelpBox("Unity's Input System package is not installed. If you're using a custom input solution, " +
                                        "make sure to override the 'IInputManager' engine service, otherwise the input won't work. " +
                                        "Find more information on the 'Input Processing' page of the guide.", MessageType.Warning);
        }
    }
}

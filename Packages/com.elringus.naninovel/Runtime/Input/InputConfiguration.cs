using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    [EditInProjectSettings]
    public class InputConfiguration : Configuration
    {
        [Tooltip("Whether to spawn a Naninovel-specific event system; required for uGUI interactions. Disable, if you'd like to initialize the event system yourself.")]
        public bool SpawnEventSystem = true;
        [Tooltip("A prefab with `EventSystem` component to spawn on engine init and use for input processing. Will use the default event system when not assigned.")]
        public EventSystem EventSystem;
        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        [Tooltip("When Unity's input system is installed, assign input actions asset here.\n\nTo map input actions to Naninovel's input samplers, create `Naninovel` action map and add actions with names equal to the input names.\n\nWill use the default input actions when not assigned.")]
        public UnityEngine.InputSystem.InputActionAsset InputActions;
        #endif
        [Tooltip("Input action map names in the specified 'Input Actions' asset to register with the Naninovel input.")]
        public string[] ActionMaps = { "Naninovel" };
        [Tooltip("Whether to change input mode when associated device is activated. Eg, switch to gamepad when any gamepad button is pressed and switch back to mouse when mouse button clicked.")]
        public bool DetectInputMode = true;
        [Tooltip("Whether to disable input processing by default when the engine is initialized. Useful when Naninovel is integrated as a drop-in dialogue system and shouldn't react to user input after initialization.")]
        public bool DisableInput;
    }
}

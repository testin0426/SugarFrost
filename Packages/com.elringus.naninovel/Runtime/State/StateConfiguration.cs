using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class StateConfiguration : Configuration
    {
        [Tooltip("The folder will be created in the game data folder.")]
        public string SaveFolderName = "Saves";
        [Tooltip("The name of the settings save file.")]
        public string DefaultSettingsSlotId = "Settings";
        [Tooltip("The name of the global save file.")]
        public string DefaultGlobalSlotId = "GlobalSave";
        [Tooltip("Mask used to name save slots.")]
        public string SaveSlotMask = "GameSave{0:000}";
        [Tooltip("Mask used to name quick save slots.")]
        public string QuickSaveSlotMask = "GameQuickSave{0:000}";
        [Tooltip("Mask used to name auto save slots.")]
        public string AutoSaveSlotMask = "GameAutoSave{0:000}";
        [Tooltip("Maximum number of save slots."), Range(1, 999)]
        public int SaveSlotLimit = 99;
        [Tooltip("Maximum number of quick save slots."), Range(1, 999)]
        public int QuickSaveSlotLimit = 18;
        [Tooltip("Maximum number of auto save slots."), Range(1, 999)]
        public int AutoSaveSlotLimit = 18;
        [Tooltip("Whether to auto-save the game before exiting to title or when the application is closed while not in title menu (doesn't work in editor).")]
        public bool AutoSaveOnQuit = true;
        [Tooltip("Whether to compress and store the saves as binary files (.nson) instead of text files (.json). This will significantly reduce the files size and make them harder to edit (to prevent cheating), but will consume more memory and CPU time when saving and loading.")]
        public bool BinarySaveFiles = true;
        [Tooltip("Whether to reset state of the engine services when loading another script via [@goto] command. Can be used instead of [@resetState] command to automatically unload all the resources on each goto.")]
        public bool ResetOnGoto;
        [Tooltip("Whether to automatically show `ILoadingUI` while loading the game state.")]
        public bool ShowLoadingUI = true;

        [Header("State Rollback")]
        [Tooltip("Whether to enable the state rollback feature that allows the player to rewind the script backwards.\n\nNote that the rollback feature has a performance cost, as it effectively serializes the entire game state on each player interaction, resulting in many heap allocations. If your game does not require the rollback feature, disable it here instead of simply removing the rollback input.\n\nBe aware that even when disabled here, rollback remains enabled in the Unity Editor, as it is required for the hot reload feature; the configuration is respected in player builds.")]
        public bool EnableStateRollback = true;
        [Tooltip("The number of state snapshots to keep at runtime; determines how far back the rollback (rewind) can be performed. Increasing this value will consume more memory.")]
        public int StateRollbackSteps = 1024;
        [Tooltip("The number of state snapshots to serialize (save) under the save game slots; determines how far back the rollback can be performed after loading a saved game. Increasing this value will enlarge save game files.")]
        public int SavedRollbackSteps = 128;
        [Tooltip("Whether to rollback to the start of the played script when loading a game state where the script was modified after the save was made.")]
        public bool RecoveryRollback = true;

        [Header("Serialization Handlers")]
        [Tooltip("Implementation responsible for de-/serializing local (session-specific) game state; see `State Management` guide on how to add custom serialization handlers.")]
        public string GameStateHandler = typeof(UniversalGameStateSerializer).AssemblyQualifiedName;
        [Tooltip("Implementation responsible for de-/serializing global game state; see `State Management` guide on how to add custom serialization handlers.")]
        public string GlobalStateHandler = typeof(UniversalGlobalStateSerializer).AssemblyQualifiedName;
        [Tooltip("Implementation responsible for de-/serializing game settings; see `State Management` guide on how to add custom serialization handlers.")]
        public string SettingsStateHandler = typeof(UniversalSettingsStateSerializer).AssemblyQualifiedName;

        /// <summary>
        /// Generates save slot ID using specified index and <see cref="SaveSlotMask"/>.
        /// </summary>
        public string IndexToSaveSlotId (int index) => string.Format(SaveSlotMask, index);
        /// <summary>
        /// Generates quick save slot ID using specified index and <see cref="QuickSaveSlotMask"/>.
        /// </summary>
        public string IndexToQuickSaveSlotId (int index) => string.Format(QuickSaveSlotMask, index);
        /// <summary>
        /// Generates auto save slot ID using specified index and <see cref="AutoSaveSlotMask"/>.
        /// </summary>
        public string IndexToAutoSaveSlotId (int index) => string.Format(AutoSaveSlotMask, index);
    }
}

using UnityEditor;

namespace Naninovel
{
    public static class SetupMinimalMode
    {
        [MenuItem(MenuPath.Root + "/Set Up Minimal Mode", priority = 9998)]
        public static void ExecuteWithPrompt ()
        {
            if (EditorUtility.DisplayDialog("Switch to Minimal Mode?",
                    "Are you sure you want to set up Minimal Mode? " +
                    "This mode is optimized for integrating Naninovel into existing projects as a drop-in dialogue or cutscene system. " +
                    "Proceeding will modify default configurations, remove most built-in UIs, and disable features to reduce the engine to its bare minimum.",
                    "Proceed", "Cancel")) Execute();
        }

        public static void Execute ()
        {
            var engine = ConfigurationSettings.LoadOrDefaultAndSave<EngineConfiguration>();
            engine.ShowInitializationUI = false;
            engine.OverrideObjectsLayer = true;
            engine.ObjectsLayer = EditorUtils.GetOrCreateLayerIndex("Naninovel");
            EditorUtility.SetDirty(engine);

            var scripts = ConfigurationSettings.LoadOrDefaultAndSave<ScriptsConfiguration>();
            scripts.TitleScript = null;
            scripts.StartGameScript = null;
            EditorUtility.SetDirty(scripts);

            var player = ConfigurationSettings.LoadOrDefaultAndSave<ScriptPlayerConfiguration>();
            player.ShowLoadingUI = false;
            EditorUtility.SetDirty(player);

            var state = ConfigurationSettings.LoadOrDefaultAndSave<StateConfiguration>();
            state.EnableStateRollback = false;
            state.AutoSaveOnQuit = false;
            EditorUtility.SetDirty(state);

            var input = ConfigurationSettings.LoadOrDefaultAndSave<InputConfiguration>();
            input.SpawnEventSystem = false;
            input.DisableInput = true;
            EditorUtility.SetDirty(input);

            var camera = ConfigurationSettings.LoadOrDefaultAndSave<CameraConfiguration>();
            camera.DisableRendering = true;
            EditorUtility.SetDirty(camera);

            var vars = ConfigurationSettings.LoadOrDefaultAndSave<CustomVariablesConfiguration>();
            vars.DefaultScope = CustomVariableScope.Global;
            EditorUtility.SetDirty(vars);

            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "TitleUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "LoadingUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "CGGalleryUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "ExternalScriptsUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "RollbackUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "SaveLoadUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "SettingsUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "TipsUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "PauseUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "BacklogUI");
            RemoveResource(UIConfiguration.DefaultUIPathPrefix, "ContinueInputUI");

            void RemoveResource (string prefix, string path)
            {
                var fullPath = Resource.BuildFullPath(prefix, path);
                var guid = Assets.Get(fullPath)?.Guid;
                if (string.IsNullOrEmpty(guid)) return;
                Assets.UnregisterWithGuid(guid);
                Addressables.UnregisterAsset(guid);
            }
        }
    }
}

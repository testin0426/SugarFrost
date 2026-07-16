using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class BackgroundsSettings : OrthoActorManagerSettings<BackgroundsConfiguration, IBackgroundActor, BackgroundMetadata>
    {
        protected override string HelpUri => "guide/backgrounds";
        protected override string ResourcesSelectionTooltip => GetTooltip(EditedActorId, AllowMultipleResources);
        protected override MetadataEditor<IBackgroundActor, BackgroundMetadata> MetadataEditor { get; } = new BackgroundMetadataEditor();
        protected override HashSet<string> LockedActorIds => new() { BackgroundsConfiguration.MainActorId };

        private static bool editMainRequested;

        public override void OnGUI (string searchContext)
        {
            if (editMainRequested)
            {
                editMainRequested = false;
                MetadataMapEditor.SelectEditedMetadata(BackgroundsConfiguration.MainActorId);
            }

            base.OnGUI(searchContext);
        }

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(CharactersConfiguration.SharedPoses)] = ActorPosesEditor.Draw;
            return drawers;
        }

        public static string GetTooltip (string actorId, bool allowMultipleResources)
        {
            if (actorId == BackgroundsConfiguration.MainActorId && allowMultipleResources)
                return "Use '@back %name%' in scenario scripts to show main background with the selected appearance.";
            if (allowMultipleResources)
                return $"Use '@back %name% id:{actorId}' in scenario scripts to show this background with the selected appearance.";
            return $"Use '@back id:{actorId}' in scenario scripts to show this background.";
        }

        [MenuItem(MenuPath.Root + "/Resources/Backgrounds")]
        private static void OpenResourcesWindow ()
        {
            // Automatically open main background editor when opened via resources context menu.
            editMainRequested = true;
            OpenResourcesWindowImpl();
        }
    }
}

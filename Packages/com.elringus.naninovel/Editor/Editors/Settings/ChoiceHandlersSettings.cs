using UnityEditor;

namespace Naninovel
{
    public class ChoiceHandlersSettings : ActorManagerSettings<ChoiceHandlersConfiguration, IChoiceHandlerActor, ChoiceHandlerMetadata>
    {
        protected override string HelpUri => "guide/choices";
        protected override string ResourcesSelectionTooltip => GetTooltip(EditedActorId, Configuration);
        protected override MetadataEditor<IChoiceHandlerActor, ChoiceHandlerMetadata> MetadataEditor { get; } = new ChoiceHandlerMetadataEditor();

        public static string GetTooltip (string actorId, ChoiceHandlersConfiguration cfg)
        {
            if (actorId == cfg.DefaultHandlerId)
                return "Use '@choice \"Choice summary text.\"' in scenario scripts to add a choice with this handler.";
            return $"Use '@choice \"Choice summary text.\" handler:{actorId}' in scenario scripts to add a choice with this handler.";
        }

        [MenuItem(MenuPath.Root + "/Resources/Choice Handlers")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}

using UnityEditor;

namespace Naninovel
{
    public class UnlockablesSettings : ResourcefulSettings<UnlockablesConfiguration>
    {
        protected override string ResourcesPrefix => Configuration.Loader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "In scenario scripts use `@unlock %name%` to unlock or `@lock %name%` to lock selected unlockable item.";

        [MenuItem(MenuPath.Root + "/Resources/Unlockables")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}

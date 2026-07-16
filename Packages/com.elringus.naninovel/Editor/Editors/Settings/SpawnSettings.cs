using System;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class SpawnSettings : ResourcefulSettings<SpawnConfiguration>
    {
        protected override string HelpUri => "guide/special-effects";

        protected override Type ResourcesTypeConstraint => typeof(GameObject);
        protected override string ResourcesPrefix => Configuration.Loader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@spawn %name%` to instantiate and `@despawn %name%` to destroy the prefab.";

        [MenuItem(MenuPath.Root + "/Resources/Spawn")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}

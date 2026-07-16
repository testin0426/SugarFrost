using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class ResourceProviderSettings : ConfigurationSettings<ResourceProviderConfiguration>
    {
        protected override string HelpUri => "guide/resource-providers";

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(ResourceProviderConfiguration.LazyBuffer)] = p => DrawWhen(Configuration.ResourcePolicy == ResourcePolicy.Lazy, p);
            drawers[nameof(ResourceProviderConfiguration.LazyPriority)] = p => DrawWhen(Configuration.ResourcePolicy == ResourcePolicy.Lazy, p);
            drawers[nameof(ResourceProviderConfiguration.UseAddressables)] = p => {
                if (!Configuration.EnableBuildProcessing)
                {
                    EditorGUILayout.HelpBox("While processing is disabled, assets assigned as Naninovel resources may not be available in the build. In case using a custom build handler, consider invoking `BuildProcessor.PreprocessBuild()` and `BuildProcessor.PostprocessBuild()` methods to replicate Naninovel's processing.", MessageType.Warning);
                    return;
                }
                if (Addressables.Available)
                {
                    EditorGUILayout.PropertyField(p);
                    if (!Configuration.UseAddressables)
                        EditorGUILayout.HelpBox("When `Use Addressables` is disabled, all the assets assigned as Naninovel resources are copied and re-imported on build, which significantly increases the build time.", MessageType.Warning);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Toggle(p.displayName, false);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.HelpBox("Consider installing Unity's Addressable Asset System. When the system is not available, all the assets assigned as Naninovel resources are copied and re-imported on build, which significantly increases build time.", MessageType.Warning);
                }
            };
            drawers[nameof(ResourceProviderConfiguration.AutoBuildBundles)] = p => DrawWhen(Addressables.Available && Configuration.EnableBuildProcessing && Configuration.UseAddressables, p);
            drawers[nameof(ResourceProviderConfiguration.LabelByScripts)] = p => DrawWhen(Addressables.Available && Configuration.EnableBuildProcessing && Configuration.UseAddressables, p);
            return drawers;
        }
    }
}

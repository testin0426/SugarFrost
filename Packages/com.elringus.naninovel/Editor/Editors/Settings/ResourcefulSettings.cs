using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    /// <summary>
    /// Derive from this class to create custom combined editors for <see cref="Configuration"/> assets
    /// and associated resources stored in <see cref="Naninovel.EditorResources"/>.
    /// </summary>
    /// <typeparam name="TConfig">Type of the configuration asset this editor is built for.</typeparam>
    public abstract class ResourcefulSettings<TConfig> : ConfigurationSettings<TConfig> where TConfig : Configuration
    {
        protected static bool ShowResourcesEditor { get; set; }

        protected virtual GUIContent ToResourcesButtonContent { get; private set; }
        protected virtual GUIContent FromResourcesButtonContent { get; } = new("◀  Back to Configuration");
        protected abstract string ResourcesPrefix { get; }
        protected virtual bool AllowRename => true;
        protected virtual string ResourcesGroup => null;
        protected virtual string SingleResourcePath => null;
        protected virtual Type ResourcesTypeConstraint => null;
        protected virtual string ResourcesSelectionTooltip => null;

        public override void OnActivate (string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            ToResourcesButtonContent = new($"Manage {EditorTitle} Resources");
            // delay required, as the update causes side effects requiring fields init in the derived classes
            EditorApplication.delayCall += () => AssetsEditor.Update(ResourcesPrefix, ResourcesGroup);
        }

        protected override void DrawConfigurationEditor ()
        {
            if (ShowResourcesEditor)
            {
                if (GUILayout.Button(FromResourcesButtonContent, GUIStyles.NavigationButton))
                    ShowResourcesEditor = false;
                else
                {
                    EditorGUILayout.Space();
                    AssetsEditor.DrawGUILayout(ResourcesPrefix, ResourcesGroup, AllowRename, SingleResourcePath, ResourcesTypeConstraint, ResourcesSelectionTooltip);
                }
            }
            else
            {
                DrawDefaultEditor();

                EditorGUILayout.Space();
                if (GUILayout.Button(ToResourcesButtonContent, GUIStyles.NavigationButton))
                    ShowResourcesEditor = true;
            }
        }

        protected static void OpenResourcesWindowImpl ()
        {
            ShowResourcesEditor = true;
            SettingsService.OpenProjectSettings(SettingsPath);
        }
    }
}

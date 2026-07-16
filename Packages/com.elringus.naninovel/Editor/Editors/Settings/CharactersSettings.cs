using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class CharactersSettings : OrthoActorManagerSettings<CharactersConfiguration, ICharacterActor, CharacterMetadata>
    {
        protected override string HelpUri => "guide/characters";
        protected override string ResourcesSelectionTooltip => GetTooltip(EditedActorId, AllowMultipleResources);
        protected override MetadataEditor<ICharacterActor, CharacterMetadata> MetadataEditor { get; } = new CharacterMetadataEditor();

        private static readonly GUIContent avatarsEditorContent = new("Avatar Resources",
            "Use 'CharacterId/Appearance' name to map avatar texture to a character appearance. Use 'CharacterId/Default' to map a default avatar to the character.");

        private bool avatarsEditorExpanded;

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(CharactersConfiguration.AvatarLoader)] = DrawAvatarsEditor;
            drawers[nameof(CharactersConfiguration.ArrangeRange)] = DrawArrangeRangeEditor;
            drawers[nameof(CharactersConfiguration.SharedPoses)] = ActorPosesEditor.Draw;
            return drawers;
        }

        public static string GetTooltip (string actorId, bool allowMultipleResources)
        {
            return allowMultipleResources
                ? $"Use '@char {actorId}.%name%' in scenario scripts to show the character with selected appearance."
                : $"Use '@char {actorId}' in scenario scripts to show this character.";
        }

        private void DrawAvatarsEditor (SerializedProperty avatarsLoaderProperty)
        {
            EditorGUILayout.PropertyField(avatarsLoaderProperty);

            avatarsEditorExpanded = EditorGUILayout.Foldout(avatarsEditorExpanded, avatarsEditorContent, true);
            if (!avatarsEditorExpanded) return;
            AssetsEditor.DrawGUILayout(Configuration.AvatarLoader.PathPrefix, null, AllowRename, null, typeof(Texture2D),
                "Use '@char CharacterID avatar:%name%' in scenario scripts to assign selected avatar texture for the character.");
        }

        private static void DrawArrangeRangeEditor (SerializedProperty serializedProperty)
        {
            EditorGUILayout.PropertyField(serializedProperty);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var value = serializedProperty.vector2Value;
            GUILayout.Space(EditorGUIUtility.labelWidth);
            EditorGUILayout.MinMaxSlider(ref value.x, ref value.y, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                serializedProperty.vector2Value = value;
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem(MenuPath.Root + "/Resources/Characters")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}

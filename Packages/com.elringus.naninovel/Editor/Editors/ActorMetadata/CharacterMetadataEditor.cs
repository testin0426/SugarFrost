using System;
using UnityEditor;

namespace Naninovel
{
    public class CharacterMetadataEditor : OrthoMetadataEditor<ICharacterActor, CharacterMetadata>
    {
        public override void Draw (SerializedProperty serializedProperty, CharacterMetadata metadata)
        {
            if (metadata.Implementation == typeof(PlaceholderCharacter).AssemblyQualifiedName)
                EnsureDefaultPlaceholderAppearanceAdded(metadata);
            base.Draw(serializedProperty, metadata);
        }

        protected override Action<SerializedProperty> GetCustomDrawer (string propertyName) => propertyName switch {
            nameof(CharacterMetadata.BakedLookDirection) => DrawWhen(HasResources),
            nameof(CharacterMetadata.DisplayName) => DrawWhen(Metadata.HasName),
            nameof(CharacterMetadata.NameColor) => DrawWhen(Metadata.UseCharacterColor),
            nameof(CharacterMetadata.MessageColor) => DrawWhen(Metadata.UseCharacterColor),
            nameof(CharacterMetadata.HighlightWhenSpeaking) => DrawWhen(HasResources),
            nameof(CharacterMetadata.HighlightCharacterCount) => DrawWhen(HasResources && Metadata.HighlightWhenSpeaking),
            nameof(CharacterMetadata.SpeakingPose) => DrawWhen(HasResources && Metadata.HighlightWhenSpeaking),
            nameof(CharacterMetadata.NotSpeakingPose) => DrawWhen(HasResources && Metadata.HighlightWhenSpeaking),
            nameof(CharacterMetadata.PlaceOnTop) => DrawWhen(HasResources && Metadata.HighlightWhenSpeaking),
            nameof(CharacterMetadata.HighlightDuration) => DrawWhen(HasResources && Metadata.HighlightWhenSpeaking),
            nameof(CharacterMetadata.HighlightEasing) => DrawWhen(HasResources && Metadata.HighlightWhenSpeaking),
            nameof(CharacterMetadata.MessageSoundPlayback) => DrawWhen(!string.IsNullOrEmpty(Metadata.MessageSound)),
            nameof(CharacterMetadata.VoiceSource) => DrawWhen(HasResources),
            nameof(CharacterMetadata.Poses) => DrawWhen(HasResources, ActorPosesEditor.Draw),
            nameof(CharacterMetadata.Anchors) => DrawWhen(HasResources),
            _ => base.GetCustomDrawer(propertyName)
        };

        protected virtual void EnsureDefaultPlaceholderAppearanceAdded (CharacterMetadata metadata)
        {
            if (metadata.GetCustomData<PlaceholderCharacterMetadata>() is not { } data ||
                data.PlaceholderAppearances == null || data.PlaceholderAppearances.Length == 0)
                metadata.SetCustomMetadata(new PlaceholderCharacterMetadata {
                    PlaceholderAppearances = PlaceholderCharacterMetadata.GetDefaultAppearances()
                });
        }
    }
}

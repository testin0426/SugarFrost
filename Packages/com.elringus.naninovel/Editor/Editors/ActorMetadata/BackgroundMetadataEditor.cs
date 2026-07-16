using System;
using UnityEditor;
using static Naninovel.PlaceholderBackgroundAppearance;

namespace Naninovel
{
    public class BackgroundMetadataEditor : OrthoMetadataEditor<IBackgroundActor, BackgroundMetadata>
    {
        public override void Draw (SerializedProperty serializedProperty, BackgroundMetadata metadata)
        {
            if (metadata.Implementation == typeof(PlaceholderBackground).AssemblyQualifiedName)
                EnsureDefaultPlaceholderAppearanceAdded(metadata);
            base.Draw(serializedProperty, metadata);
        }

        public static void EnsureDefaultPlaceholderAppearanceAdded (BackgroundMetadata metadata)
        {
            if (metadata.GetCustomData<PlaceholderBackgroundMetadata>() is not { } data ||
                data.PlaceholderAppearances == null || data.PlaceholderAppearances.Length == 0)
                metadata.SetCustomMetadata(new PlaceholderBackgroundMetadata {
                    PlaceholderAppearances = new[] { Black, White, Light, Dark, City, Desert, Snow, Mist, Cosmos }
                });
        }

        protected override Action<SerializedProperty> GetCustomDrawer (string propertyName) => propertyName switch {
            nameof(BackgroundMetadata.MatchMode) => DrawWhen(HasResources && !IsGeneric),
            nameof(BackgroundMetadata.CustomMatchRatio) => DrawWhen(!IsGeneric && Metadata.MatchMode == AspectMatchMode.Custom),
            nameof(BackgroundMetadata.Poses) => DrawWhen(HasResources, ActorPosesEditor.Draw),
            nameof(BackgroundMetadata.ScenePathRoot) => DrawWhen(Metadata.Implementation == typeof(SceneBackground).AssemblyQualifiedName, p => EditorUtils.FolderField(p)),
            _ => base.GetCustomDrawer(propertyName)
        };
    }
}

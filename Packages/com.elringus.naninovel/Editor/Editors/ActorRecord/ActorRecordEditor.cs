using System;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public abstract class ActorRecordEditor<TEditor, TMeta, TConfig> : Editor
        where TEditor : MetadataEditor, new()
        where TMeta : ActorMetadata, new()
        where TConfig : ActorManagerConfiguration<TMeta>
    {
        private const string showResourcesKey = "NaninovelActorRecordShowResources";
        private static bool showResources { get => PlayerPrefs.GetInt(showResourcesKey, 1) == 1; set => PlayerPrefs.SetInt(showResourcesKey, value ? 1 : 0); }
        private static readonly GUIContent resourcesContent = new("Resources", "Associated actor resources.");

        private readonly MetadataEditor metadataEditor = new TEditor();
        private SerializedObject serializedConfiguration;
        private SerializedProperty actorIdProperty;
        private SerializedProperty metadataProperty;
        private TConfig configuration;
        private TMeta metadata;
        private bool dirty;

        private string implementation;
        private string actorId;
        private Type resourcesTypeConstraint;
        private bool allowMultipleResources;
        private string resourcesGroup;
        private string resourcesPrefix;
        private string singleResourcePath;
        private string tooltip;

        public override void OnInspectorGUI ()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(actorIdProperty);
            if (string.IsNullOrWhiteSpace(actorIdProperty.stringValue))
                actorIdProperty.stringValue = target.name;
            metadataEditor.Draw(metadataProperty, metadata);
            DrawResources(metadata);
            if (serializedObject.hasModifiedProperties && serializedObject.ApplyModifiedProperties() ||
                EditorGUI.EndChangeCheck() || EditorUtility.IsDirty(target))
                Dirty();

            if (dirty && Event.current.keyCode == KeyCode.S &&
                (Event.current.modifiers == EventModifiers.Command || Event.current.modifiers == EventModifiers.Control))
                WriteToConfiguration();
        }

        private void DrawResources (ActorMetadata metadata)
        {
            if (resourcesTypeConstraint == null || serializedObject.isEditingMultipleObjects) return;
            if (showResources = EditorGUILayout.Foldout(showResources, resourcesContent, true))
            {
                EditorGUILayout.Space();
                AssetsEditor.DrawGUILayout(resourcesPrefix, resourcesGroup, true, singleResourcePath, resourcesTypeConstraint, tooltip);
            }
        }

        private void Dirty ()
        {
            dirty = true;
            EditorUtility.SetDirty(target);
            if (metadata.Implementation != implementation)
                ResolveImplementationProps();
        }

        private void OnEnable ()
        {
            dirty = false;
            actorIdProperty = serializedObject.FindProperty("actorId");
            metadataProperty = serializedObject.FindProperty("metadata");
            metadata = ((ActorRecord<TMeta>)target).Metadata ?? new TMeta();
            configuration = Configuration.GetOrDefault<TConfig>();
            serializedConfiguration = new(configuration);
            ResolveImplementationProps();
            AssetsEditor.Update(resourcesPrefix, resourcesGroup);
        }

        private void OnDisable ()
        {
            if (dirty || !configuration.MetadataMap.ContainsId(actorId))
                WriteToConfiguration();
        }

        private void ResolveImplementationProps ()
        {
            actorId = ((ActorRecord)target).ActorId;
            ActorImplementations.TryGetResourcesAttribute(metadata.Implementation, out var attr);
            allowMultipleResources = attr?.AllowMultiple ?? false;
            resourcesTypeConstraint = attr?.TypeConstraint;
            resourcesGroup = metadata.GetResourceGroup();
            resourcesPrefix = allowMultipleResources ? $"{metadata.Loader.PathPrefix}/{actorId}" : metadata.Loader.PathPrefix;
            singleResourcePath = allowMultipleResources ? null : actorId;
            implementation = metadata.Implementation;
            tooltip =
                configuration is BackgroundsConfiguration ? BackgroundsSettings.GetTooltip(actorId, allowMultipleResources) :
                configuration is CharactersConfiguration ? CharactersSettings.GetTooltip(actorId, allowMultipleResources) :
                configuration is ChoiceHandlersConfiguration choice ? ChoiceHandlersSettings.GetTooltip(actorId, choice) :
                configuration is TextPrintersConfiguration printer ? TextPrintersSettings.GetTooltip(actorId, printer) : null;
        }

        private void WriteToConfiguration ()
        {
            foreach (var target in targets)
            {
                var record = (ActorRecord<TMeta>)target;
                var id = record.ActorId;
                var meta = record.Metadata;
                if (IsGuidCollides(id, meta.Guid))
                    meta.RegenerateGuid(); // happens when duplicating actor record assets
                configuration.ActorMetadataMap[id] = meta;
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }
            serializedConfiguration.Update();
            EditorUtility.SetDirty(configuration);
            AssetDatabase.SaveAssetIfDirty(configuration);
        }

        private bool IsGuidCollides (string actorId, string guid)
        {
            using var _ = configuration.ActorMetadataMap.RentIds(out var ids);
            foreach (var id in ids)
                if (id != actorId && configuration.ActorMetadataMap.GetMetaById(id)?.Guid == guid)
                    return true;
            return false;
        }
    }

    [CustomEditor(typeof(CharacterRecord)), CanEditMultipleObjects]
    public class CharacterRecordEditor : ActorRecordEditor<CharacterMetadataEditor, CharacterMetadata, CharactersConfiguration> { }

    [CustomEditor(typeof(BackgroundRecord)), CanEditMultipleObjects]
    public class BackgroundRecordEditor : ActorRecordEditor<BackgroundMetadataEditor, BackgroundMetadata, BackgroundsConfiguration> { }

    [CustomEditor(typeof(TextPrinterRecord)), CanEditMultipleObjects]
    public class TextPrinterRecordEditor : ActorRecordEditor<TextPrinterMetadataEditor, TextPrinterMetadata, TextPrintersConfiguration> { }

    [CustomEditor(typeof(ChoiceHandlerRecord)), CanEditMultipleObjects]
    public class ChoiceHandlerRecordEditor : ActorRecordEditor<ChoiceHandlerMetadataEditor, ChoiceHandlerMetadata, ChoiceHandlersConfiguration> { }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public abstract class MetadataEditor
    {
        public abstract void Draw (SerializedProperty serializedProperty, ActorMetadata metadata);
    }

    public abstract class MetadataEditor<TActor, TMeta> : MetadataEditor
        where TActor : IActor
        where TMeta : ActorMetadata
    {
        protected TMeta Metadata { get; private set; }
        protected bool HasResources { get; private set; }
        protected bool IsGeneric { get; private set; }

        private static readonly string[] implementations;
        private static readonly string[] implementationLabels;
        private static readonly Action<SerializedProperty> defaultDrawer = p => EditorGUILayout.PropertyField(p);
        private static readonly Action<SerializedProperty> noneDrawer = _ => { };

        static MetadataEditor ()
        {
            implementations = ActorImplementations.GetImplementations<TActor>().Select(t => t.AssemblyQualifiedName).ToArray();
            implementationLabels = implementations.Select(s => s.GetBefore(",")).ToArray();
        }

        public override void Draw (SerializedProperty serializedProperty, ActorMetadata metadata)
        {
            Draw(serializedProperty, metadata as TMeta);
        }

        public virtual void Draw (SerializedProperty serializedProperty, TMeta metadata)
        {
            SetEditedMetadata(metadata);

            var prop = serializedProperty.Copy();
            var endProp = prop.GetEndProperty();
            var customProp = default(SerializedProperty);

            prop.NextVisible(true);
            do
            {
                if (SerializedProperty.EqualContents(prop, endProp)) break;
                if (IsCustomDataProperty(prop)) customProp = prop.Copy();
                else if (TryDrawCustomProperty(prop)) continue;
                else if (TryDrawImplementationPopup(prop)) continue;
                else EditorGUILayout.PropertyField(prop, true);
            }
            while (prop.NextVisible(false));

            if (customProp != null)
                DrawCustomData(customProp);
        }

        protected virtual Action<SerializedProperty> GetCustomDrawer (string propertyName) => propertyName switch {
            nameof(ActorMetadata.Loader) => DrawWhen(HasResources),
            _ => null
        };

        protected Action<SerializedProperty> DrawWhen (bool condition)
        {
            return DrawWhen(condition, defaultDrawer);
        }

        protected Action<SerializedProperty> DrawWhen (bool condition, Action<SerializedProperty> drawer)
        {
            return condition ? drawer : noneDrawer;
        }

        protected Action<SerializedProperty> DrawNothing ()
        {
            return noneDrawer;
        }

        private void SetEditedMetadata (TMeta metadata)
        {
            Metadata = metadata;
            var implementation = Metadata.Implementation;
            HasResources = ActorImplementations.TryGetResourcesAttribute(implementation, out var a) && a.TypeConstraint != null;
            IsGeneric = HasResources && typeof(GenericActorBehaviour).IsAssignableFrom(a.TypeConstraint);
        }

        private bool TryDrawCustomProperty (SerializedProperty property)
        {
            var customDrawer = GetCustomDrawer(property.name);
            if (customDrawer is null) return false;
            customDrawer.Invoke(property);
            return true;
        }

        private bool TryDrawImplementationPopup (SerializedProperty property)
        {
            if (!property.propertyPath.EndsWithOrdinal(nameof(ActorMetadata.Implementation))) return false;
            EditorGUI.BeginChangeCheck();
            var label = EditorGUI.BeginProperty(Rect.zero, null, property);
            var curIndex = implementations.IndexOf(property.stringValue ?? string.Empty);
            var newIndex = EditorGUILayout.Popup(label, curIndex, implementationLabels);
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                property.stringValue = implementations.IsIndexValid(newIndex) ? implementations[newIndex] : string.Empty;
            return true;
        }

        private void DrawCustomData (SerializedProperty propertyCopy)
        {
            if (!ActorImplementations.TryGetCustomDataType(Metadata.Implementation, out var dataType)) return;

            if (!propertyCopy.hasVisibleChildren || propertyCopy.managedReferenceFullTypename?.GetAfter(" ")?.Replace("/", "+") != dataType.FullName)
                propertyCopy.managedReferenceValue = Activator.CreateInstance(dataType);

            var customMetaProperty = propertyCopy.Copy();
            var endCustomMetaProperty = customMetaProperty.GetEndProperty();
            customMetaProperty.NextVisible(true);
            do
            {
                if (SerializedProperty.EqualContents(customMetaProperty, endCustomMetaProperty)) break;
                EditorGUILayout.PropertyField(customMetaProperty, true);
            }
            while (customMetaProperty.NextVisible(false));
        }

        private bool IsCustomDataProperty (SerializedProperty property)
        {
            return property.propertyPath.EndsWithOrdinal("customData");
        }
    }
}

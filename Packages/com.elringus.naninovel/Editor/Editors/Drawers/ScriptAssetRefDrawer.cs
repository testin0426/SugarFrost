using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ScriptAssetRefAttribute"/>
    [CustomPropertyDrawer(typeof(ScriptAssetRefAttribute))]
    public class ScriptAssetRefDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, Object> cachedObjects = new();

        public override void OnGUI (Rect rect, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, prop);
            var oldGuid = prop.stringValue;
            var oldObj = string.IsNullOrEmpty(oldGuid) ? null : GetCachedOrLoadObject(oldGuid);
            EditorGUI.BeginChangeCheck();
            var newObj = EditorGUI.ObjectField(rect, label, oldObj, typeof(Script), false);
            if (EditorGUI.EndChangeCheck())
                if (!newObj) prop.stringValue = null;
                else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newObj, out var newGuid, out long _))
                    prop.stringValue = newGuid;
            EditorGUI.EndProperty();
        }

        [CanBeNull]
        private Object GetCachedOrLoadObject (string guid)
        {
            if (cachedObjects.TryGetValue(guid, out var cachedObj))
                if (cachedObj) return cachedObj;
                else cachedObjects.Remove(guid);
            if (EditorUtils.LoadAssetByGuid<Script>(guid) is not { } obj) return null;
            return cachedObjects[guid] = obj;
        }
    }
}

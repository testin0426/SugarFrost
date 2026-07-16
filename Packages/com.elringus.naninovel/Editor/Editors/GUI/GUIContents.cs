using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class GUIContents
    {
        public static GUIContent HelpIcon => Get();
        public static GUIContent EditIcon => Get();
        public static GUIContent NaninovelIcon => Get();
        public static GUIContent ScriptAssetIcon => Get();

        private static readonly Dictionary<string, GUIContent> cache = new();

        private static GUIContent Get ([CallerMemberName] string id = "") =>
            cache.GetValueOrDefault(id) ?? (cache[id] = id switch {
                nameof(HelpIcon) => LoadUnityIcon("helpIcon"),
                nameof(EditIcon) => LoadIcon("EditMetaIcon", "Edit actor metadata."),
                nameof(NaninovelIcon) => LoadIcon(WithX("NaninovelIcon")),
                nameof(ScriptAssetIcon) => LoadIcon("ScriptAssetIcon"),
                _ => throw new Error($"Failed to load '{id}' GUI icon: unknown icon.")
            }) ?? GUIContent.none;

        private static string WithX (string iconPath) =>
            $"{iconPath}@{(EditorGUIUtility.pixelsPerPoint >= 2 ? "2x" : "1x")}";

        [CanBeNull] // retry on nulls, as this may be invoked while the assets are not imported yet
        private static GUIContent LoadIcon (string iconPath, [CanBeNull] string tooltip = null)
        {
            var assetPath = $"{PackagePath.EditorResourcesPath}/{iconPath}.png";
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            return asset ? new(asset, tooltip) : null;
        }

        private static GUIContent LoadUnityIcon (string propertyName) =>
            typeof(EditorGUI).GetNestedType("GUIContents", BindingFlags.NonPublic)
                ?.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null) as GUIContent;
    }
}

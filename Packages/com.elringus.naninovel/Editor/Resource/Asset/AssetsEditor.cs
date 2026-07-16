using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Editor for resources stored in <see cref="Assets"/> registry.
    /// </summary>
    public static class AssetsEditor
    {
        public class Adapter : ScriptableObject
        {
            public List<Asset> Assets = new();
        }

        private class Context
        {
            public string Prefix { get; }
            [CanBeNull] public string Group { get; }
            public bool AllowRename { get; }
            [CanBeNull] public string SinglePath { get; }
            [CanBeNull] public Type TypeConstraint { get; }
            [CanBeNull] public string SelectionTooltip { get; }
            public bool SingleMode => SinglePath != null;
            public Adapter Adapter { get; }
            public SerializedObject Serde { get; }
            public ReorderableList List { get; }
            [CanBeNull] public string SelectedName { get; set; }
            public AssetDropHandler DropHandler { get; }

            public Context (string prefix, string group = null, bool allowRename = true,
                string singlePath = null, Type typeConstraint = null, string selectionTooltip = null)
            {
                Prefix = prefix;
                Group = group;
                AllowRename = allowRename;
                SinglePath = singlePath;
                TypeConstraint = typeConstraint;
                SelectionTooltip = selectionTooltip;

                Adapter = ScriptableObject.CreateInstance<Adapter>();
                Serde = new(Adapter);

                List = new(Serde, Serde.FindProperty("Assets"), true, true, true, true);
                List.draggable = false;
                List.drawHeaderCallback = rect => DrawHeader(rect, this);
                List.drawElementCallback = (rect, idx, sel, fc) => DrawElement(rect, idx, sel, fc, this);
                List.drawFooterCallback = rect => DrawFooter(rect, this);
                List.onAddCallback = _ => HandleElementAdded(this);
                List.onRemoveCallback = _ => HandleElementRemoved(this);
                List.onSelectCallback = _ => HandleElementSelected(this);

                DropHandler = new(ass => AddDroppedAssets(ass, this));
                DropHandler.TypeConstraint = TypeConstraint;

                ReadAssets(this);
                Undo.undoRedoPerformed += () => WriteAssets(this);
            }
        }

        private const float headerLeftMargin = 5;
        private const float paddingWidth = 5;
        private static readonly GUIContent pathContent = new("Name  <i><size=10>(hover for hotkey info)</size></i>", "Local path (relative to the path prefix) of the resource.\n\nHotkeys:\n • Delete key — Remove selected record.\n • Up/Down keys — Navigate selected records.\n\nIt's possible to add resources in batch by drag-dropping multiple assets or folders to an area below the list (appears when compatible assets are dragged).");
        private static readonly GUIContent objectContent = new("Object", "Object of the resource.\n\nThe assigned objects are loaded only when hovered for better performance.");
        private static readonly GUIContent invalidObjectContent = new("", "Assign a valid object or remove the record.");
        private static readonly GUIContent duplicateNameContent = new("", "Duplicate names could cause issues. Change name for one of the affected records.");
        private static readonly Color invalidObjectColor = new(1, .8f, .8f);
        private static readonly Color duplicateNameColor = new(1, 1, .8f);
        private static readonly Dictionary<string, Context> ctxByKey = new();
        private static readonly Dictionary<string, UnityEngine.Object> objByGuid = new();
        private static readonly HashSet<string> names = new();

        /// <summary>
        /// Draws the editor GUI using layout system.
        /// </summary>
        /// <param name="prefix">Path prefix (type discriminator) of the edited resources.</param>
        /// <param name="group">Group ID of the edited resources or null when none.</param>
        /// <param name="allowRename">Whether to allow renaming resource names (local paths).</param>
        /// <param name="singlePath">When specified, will draw a single-element editor for a resource with the specified local path.</param>
        /// <param name="typeConstraint">Type constraint to apply for resource objects.</param>
        /// <param name="selectionTooltip">The tooltip template for selected records; %name% tags will be replaced with the name of the selected resource.</param>
        public static void DrawGUILayout (string prefix, string group = null, bool allowRename = true,
            string singlePath = null, Type typeConstraint = null, string selectionTooltip = null)
        {
            var ctx = GetOrCreateContext(prefix, group, allowRename, singlePath, typeConstraint, selectionTooltip);
            ctx.Serde.Update();
            EnsureListSize(ctx);
            ctx.List.DoLayoutList();
            DrawSelectionTooltip(ctx);
            if (ctx.Serde.ApplyModifiedProperties())
                WriteAssets(ctx);
        }

        /// <summary>
        /// Pulls the associated asset data into the editor.
        /// Use to actualize the editor data with any potential asset changes occured outside the editor.
        /// </summary>
        public static void Update (string prefix, string group = null)
        {
            if (ctxByKey.TryGetValue(ResolveContextKey(prefix, group), out var ctx))
                ReadAssets(ctx);
        }

        /// <summary>
        /// Attempts to remove a resources group with the specified ID and all the associated records.
        /// </summary>
        public static void RemoveGroup (string group)
        {
            Assets.UnregisterWithGroup(group);
            if (!ctxByKey.TryGetValue(group, out var ctx)) return;
            for (int i = 0; i < ctx.List.count; i++)
                ctx.List.serializedProperty.DeleteArrayElementAtIndex(i);
            ctx.Serde.ApplyModifiedProperties();
        }

        private static string ResolveContextKey (string prefix, string group = null)
        {
            return string.IsNullOrEmpty(group) ? prefix : group;
        }

        private static Context GetOrCreateContext (string prefix, string group = null, bool allowRename = true,
            string singlePath = null, Type typeConstraint = null, string selectionTooltip = null)
        {
            var key = ResolveContextKey(prefix, group);
            return ctxByKey.TryGetValue(key, out var ctx) && ctx.Adapter ? ctx :
                ctxByKey[key] = new(prefix, group, allowRename, singlePath, typeConstraint, selectionTooltip);
        }

        private static void ReadAssets (Context ctx)
        {
            if (!ctx.Serde.targetObject) return;
            ctx.Adapter.Assets.Clear();
            using (Assets.RentWithPrefix(ctx.Prefix, out var assets))
                foreach (var asset in assets)
                    if (asset.Group == ctx.Group)
                        ctx.Adapter.Assets.Add(new(asset.Guid, asset.Prefix, asset.Path, asset.Group));
        }

        private static void WriteAssets (Context ctx)
        {
            if (ctx.Group is { } group) Assets.UnregisterWithGroup(group);
            else Assets.UnregisterWithPrefix(ctx.Prefix);
            Assets.RegisterMany(ctx.Adapter.Assets
                .Where(a => !string.IsNullOrEmpty(a.Guid) && !string.IsNullOrEmpty(a.Path))
                .Select(a => new Asset(a.Guid, ctx.Prefix, a.Path, ctx.Group)));
        }

        private static void EnsureListSize (Context ctx)
        {
            if (!ctx.SingleMode) return;
            var prop = ctx.List.serializedProperty;
            if (prop.arraySize != 1)
            {
                prop.arraySize = 1;
                ctx.List.index = 0;
            }
        }

        private static void DrawHeader (Rect rect, Context ctx)
        {
            var propertyRect = new Rect(headerLeftMargin + rect.x, rect.y, (rect.width / 2f) - paddingWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(propertyRect, pathContent, GUIStyles.RichLabelStyle);
            propertyRect.x += propertyRect.width + paddingWidth;
            EditorGUI.LabelField(propertyRect, objectContent);
        }

        private static void DrawElement (Rect rect, int index, bool selected, bool focused, Context ctx)
        {
            if (index == 0) names.Clear();
            if (index < 0 || index >= ctx.List.serializedProperty.arraySize) return;

            var elementPathProperty = GetPathPropertyAt(index, ctx.List);
            var elementPrefixProperty = GetPrefixPropertyAt(index, ctx.List);
            var elementGuidProperty = GetGuidPropertyAt(index, ctx.List);

            var elementPath = elementPathProperty.stringValue;
            var elementPrefix = elementPrefixProperty.stringValue;
            var elementGuid = elementGuidProperty.stringValue;
            var elementHovered = rect.Contains(Event.current.mousePosition);

            // Delete record when pressing delete key while an element is selected, but not editing text field.
            if (ctx.List.index == index && Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Delete && selected && !EditorGUIUtility.editingTextField)
            {
                HandleElementRemoved(ctx);
                Event.current.Use();
                return;
            }

            // Select list row when clicking on the inlined properties.
            if (Event.current.type == EventType.MouseDown && elementHovered)
            {
                ctx.List.index = index;
                ctx.List.onSelectCallback?.Invoke(ctx.List);
            }

            EditorGUI.BeginChangeCheck();

            var propRect = new Rect(rect.x,
                rect.y + EditorGUIUtility.standardVerticalSpacing,
                rect.width / 2f - paddingWidth, EditorGUIUtility.singleLineHeight);

            // Set resource path prefix property.
            if (elementPrefix != ctx.Prefix)
                elementPrefixProperty.stringValue = ctx.Prefix;

            // Draw resource name (local path) property.
            var initialNameColor = GUI.color;
            var duplicate = names.Contains(elementPath);
            if (duplicate) GUI.color = duplicateNameColor;
            EditorGUI.LabelField(propRect, duplicate ? duplicateNameContent : GUIContent.none);
            EditorGUI.BeginDisabledGroup(ctx.SingleMode || !ctx.AllowRename);
            var oldPath = elementPath;
            var newPath = EditorGUI.DelayedTextField(propRect, GUIContent.none,
                ctx.SingleMode ? ctx.SinglePath : oldPath);
            newPath = PathUtils.FormatPath(newPath);
            if (oldPath != newPath)
                elementPathProperty.stringValue = newPath;
            EditorGUI.EndDisabledGroup();
            GUI.color = initialNameColor;
            names.Add(newPath);

            propRect.x += propRect.width + paddingWidth;

            // Draw resource GUID property.
            if (elementHovered || IsAssetLoadedOrCached(elementGuid) || string.IsNullOrEmpty(elementGuid))
            {
                var objectType = ctx.TypeConstraint ?? typeof(UnityEngine.Object);
                var oldObject = string.IsNullOrEmpty(elementGuid) ? default :
                    GetCachedOrLoadAssetByGuid(elementGuid, objectType);

                var initialGuidColor = GUI.color;
                if (!ObjectUtils.IsValid(oldObject) && !string.IsNullOrEmpty(newPath))
                {
                    GUI.color = invalidObjectColor;
                    EditorGUI.LabelField(propRect, invalidObjectContent);
                }
                var newObject = EditorGUI.ObjectField(propRect, GUIContent.none, oldObject, objectType, false);
                GUI.color = initialGuidColor;

                if (oldObject != newObject)
                {
                    if (newObject is null)
                        elementGuidProperty.stringValue = string.Empty;
                    else
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newObject, out var newGuid, out long _);
                        elementGuidProperty.stringValue = newGuid;
                    }
                }

                // Auto-assign default name when object is set, but name is empty or rename is not allowed.
                if (newObject && (string.IsNullOrEmpty(newPath) || !ctx.AllowRename))
                    elementPathProperty.stringValue = newObject.name;
            }
            else
            {
                var path = AssetDatabase.GUIDToAssetPath(elementGuid);
                if (path.Contains("/")) path = path.GetAfter("/");
                if (path.Length > 30) path = path[^30..];
                EditorGUI.LabelField(propRect, path, EditorStyles.objectField);
            }

            if (EditorGUI.EndChangeCheck())
                UpdateSelectedName(ctx);
        }

        private static void DrawFooter (Rect rect, Context ctx)
        {
            if (ctx.SingleMode) return;
            if (ctx.DropHandler.CanHandleDraggedObjects()) ctx.DropHandler.DrawDropArea(rect);
            else ReorderableList.defaultBehaviours.DrawFooter(rect, ctx.List);
        }

        private static void DrawSelectionTooltip (Context ctx)
        {
            // To prevent https://forum.unity.com/threads/135021/#post-914872 when undoing list items removal.
            if (Event.current.type == EventType.KeyDown) return;
            if (ctx.DropHandler.CanHandleDraggedObjects() ||
                string.IsNullOrEmpty(ctx.SelectionTooltip) ||
                string.IsNullOrEmpty(ctx.SelectedName)) return;
            var message = ctx.SelectionTooltip.Replace("%name%", ctx.SelectedName);
            SelectableTooltip.Draw(message);
        }

        private static void HandleElementAdded (Context ctx)
        {
            ctx.List.serializedProperty.arraySize++;
            ctx.List.index = ctx.List.serializedProperty.arraySize - 1;

            // Reset values of the added element (they're duplicated from a previous element by default).
            GetPathPropertyAt(ctx.List.index, ctx.List).stringValue = string.Empty;
            GetPrefixPropertyAt(ctx.List.index, ctx.List).stringValue = string.Empty;
            GetGuidPropertyAt(ctx.List.index, ctx.List).stringValue = string.Empty;

            UpdateSelectedName(ctx);
        }

        private static void HandleElementRemoved (Context ctx)
        {
            ctx.List.serializedProperty.DeleteArrayElementAtIndex(ctx.List.index);
            if (ctx.List.index >= ctx.List.serializedProperty.arraySize - 1)
                ctx.List.index = ctx.List.serializedProperty.arraySize - 1;

            UpdateSelectedName(ctx);
        }

        private static void HandleElementSelected (Context ctx)
        {
            UpdateSelectedName(ctx);
        }

        private static void UpdateSelectedName (Context ctx)
        {
            var valid = ctx.List.count > 0 && ctx.List.index > -1 && ctx.List.index < ctx.List.count;
            ctx.SelectedName = valid ? GetPathPropertyAt(ctx.List.index, ctx.List)?.stringValue : null;
        }

        private static void AddDroppedAssets (DroppedAsset[] assets, Context ctx)
        {
            foreach (var asset in assets)
                AddDroppedAsset(asset, ctx);
            ctx.List.index = ctx.List.serializedProperty.arraySize - 1;
            UpdateSelectedName(ctx);
        }

        private static void AddDroppedAsset (DroppedAsset asset, Context ctx)
        {
            if (!ctx.AllowRename && names.Contains(asset.Asset.name))
            {
                Engine.Warn($"Failed to add '{asset.RelativePath}' asset to '{ctx.Prefix}' resources list: " +
                            "asset with the same name is already added.");
                return;
            }

            ctx.List.serializedProperty.arraySize += 1;
            var idx = ctx.List.serializedProperty.arraySize - 1;
            GetGuidPropertyAt(idx, ctx.List).stringValue = asset.Guid;
            GetPathPropertyAt(idx, ctx.List).stringValue = asset.RelativePath;
            GetPrefixPropertyAt(idx, ctx.List).stringValue = ctx.Prefix;
        }

        private static UnityEngine.Object GetCachedOrLoadAssetByGuid (string guid, Type type)
        {
            if (objByGuid.TryGetValue(guid, out var cachedObj)) return cachedObj;
            var obj = EditorUtils.LoadAssetByGuid(guid, type);
            if (obj is null) return null;
            objByGuid[guid] = obj;
            return obj;
        }

        private static bool IsAssetLoadedOrCached (string guid)
        {
            return objByGuid.ContainsKey(guid) ||
                   AssetDatabase.IsMainAssetAtPathLoaded(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static SerializedProperty GetPathPropertyAt (int idx, ReorderableList list)
            => list.serializedProperty.GetArrayElementAtIndex(idx).FindPropertyRelative("path");
        private static SerializedProperty GetPrefixPropertyAt (int idx, ReorderableList list)
            => list.serializedProperty.GetArrayElementAtIndex(idx).FindPropertyRelative("prefix");
        private static SerializedProperty GetGuidPropertyAt (int idx, ReorderableList list)
            => list.serializedProperty.GetArrayElementAtIndex(idx).FindPropertyRelative("guid");
    }
}

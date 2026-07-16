using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Naninovel.UI;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class ResourceHeaderEditor
    {
        private enum TargetKind
        {
            UI,
            Font,
            Text,
            Audio,
            Video,
            Texture,
            Spine,
            Live2D,
            DicedAtlas,
            GenericCharacter,
            GenericBackground,
            LayeredCharacter,
            LayeredBackground,
            SceneBackground,
            TextPrinter,
            ChoiceHandler,
            ChoiceButton,
            Prefab
        }

        private class Context
        {
            public GenericMenu Menu;
            public bool IsPrefab;
            public string Label;
        }

        // instancing required, as multiple inspectors may draw at the same time
        private static readonly ConditionalWeakTable<Editor, Context> ctxByEditor = new();
        private static GUIContent emptyButtonContent;
        private static GUIStyle buttonStyle;

        public static void Initialize ()
        {
            Editor.finishedDefaultHeaderGUI -= RenderHeader;
            Editor.finishedDefaultHeaderGUI += RenderHeader;
            Assets.OnModified -= ctxByEditor.Clear;
            Assets.OnModified += ctxByEditor.Clear;
        }

        private static void RenderHeader (Editor editor)
        {
            if (GetContext(editor) is { } ctx)
                if (DrawButton(ctx))
                    ctx.Menu.ShowAsContext();
        }

        [CanBeNull]
        private static Context GetContext (Editor editor)
        {
            if (editor == null) return null;
            if (ctxByEditor.TryGetValue(editor, out var ctx)) return ctx;
            if (ResolveKind(editor.targets) is not { } kind) return null;

            if (buttonStyle == null)
            {
                buttonStyle = new(EditorStyles.popup);
                buttonStyle.padding.left = 6;
                emptyButtonContent = new(" ", GUIContents.NaninovelIcon.image, "Assign Naninovel Resource");
            }

            var selected = ResolveSelected(editor.targets);
            ctxByEditor.Add(editor, ctx = new() {
                Menu = BuildMenu(kind, editor.targets, selected),
                IsPrefab = editor.target.GetType().Name == "PrefabImporter",
                Label = selected
            }); // stale entries are auto-removed on GC

            return ctx;

            static TargetKind? ResolveKind (UnityEngine.Object[] targets)
            {
                if (ResolveObject(targets.FirstOrDefault()) is not { } kind) return null;
                for (int i = 1; i < targets.Length; i++)
                    if (ResolveObject(targets[i]) != kind)
                        return null;
                return kind;

                static TargetKind? ResolveObject (UnityEngine.Object obj) => obj?.GetType().Name switch {
                    "TextScriptImporter" => TargetKind.Text,
                    "AudioImporter" => TargetKind.Audio,
                    "VideoClipImporter" => TargetKind.Video,
                    "TextureImporter" => TargetKind.Texture,
                    "DicedSpriteAtlas" => TargetKind.DicedAtlas,
                    "SceneAsset" => TargetKind.SceneBackground,
                    "TMP_FontAsset" => TargetKind.Font,
                    "PrefabImporter" => ResolvePrefab(obj),
                    _ => null
                };

                static TargetKind? ResolvePrefab (UnityEngine.Object obj)
                {
                    if (obj is not AssetImporter imp) return null;
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(imp.assetPath) is not { } ass) return null;
                    if (ass.TryGetComponent<UITextPrinterPanel>(out _)) return TargetKind.TextPrinter;
                    if (ass.TryGetComponent<ChoiceHandlerPanel>(out _)) return TargetKind.ChoiceHandler;
                    if (ass.TryGetComponent<ChoiceHandlerButton>(out _)) return TargetKind.ChoiceButton;
                    if (ass.TryGetComponent<CustomUI>(out _)) return TargetKind.UI; // keep after other types inherited from 'CustomUI'
                    if (ass.TryGetComponent<GenericCharacterBehaviour>(out _)) return TargetKind.GenericCharacter;
                    if (ass.TryGetComponent<GenericBackgroundBehaviour>(out _)) return TargetKind.GenericBackground;
                    if (ass.TryGetComponent<LayeredCharacterBehaviour>(out _)) return TargetKind.LayeredCharacter;
                    if (ass.TryGetComponent<LayeredBackgroundBehaviour>(out _)) return TargetKind.LayeredBackground;
                    #if NANINOVEL_ENABLE_LIVE2D
                    if (ass.TryGetComponent<Live2DController>(out _)) return TargetKind.Live2D;
                    #endif
                    #if NANINOVEL_ENABLE_SPINE
                    if (ass.TryGetComponent<SpineController>(out _)) return TargetKind.Spine;
                    #endif
                    return TargetKind.Prefab;
                }
            }

            static string ResolveSelected (UnityEngine.Object[] targets)
            {
                var selected = "";
                foreach (var target in targets)
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long _))
                    {
                        using var _ = Assets.RentWithGuid(guid, out var res);
                        if (res.Count == 0)
                        {
                            if (selected != "") return "—";
                            continue;
                        }
                        var label = string.Join(", ", res.Select(GetLabel));
                        if (selected == "") selected = label;
                        else if (selected != label) return "—";
                        else continue;
                    }
                return selected;

                static string GetLabel (Asset asset)
                {
                    // Label is always the resource's prefix (doesn't include the local path),
                    // except when the resource is for a single-resource actor (eg, generic actor),
                    // in which case the prefix will only contain the resource's type, so we have to use
                    // the full path to distinguish between actors.
                    if (Assets.Get(asset.FullPath) is not { } ex) return ""; // unregistered resource, can't be selected
                    if (ex.Group is not { } group || !group.Contains("/")) return asset.Prefix; // a non-actor resource
                    var guid = ex.Group.GetAfterFirst("/"); // actor metadata GUID is stored in the group after the resource type
                    var impl = // resolve the actor's implementation to check whether it supports multiple resources
                        Configuration.GetOrDefault<CharactersConfiguration>().Metadata.GetMetaByGuid(guid)?.Implementation ??
                        Configuration.GetOrDefault<BackgroundsConfiguration>().Metadata.GetMetaByGuid(guid)?.Implementation;
                    if (impl == null || Type.GetType(impl) is not { } type) return asset.FullPath; // printers and choices use single resource
                    return ActorImplementations.TryGetResourcesAttribute(type.AssemblyQualifiedName, out var attr) &&
                           attr.AllowMultiple ? asset.Prefix : asset.FullPath;
                }
            }

            static GenericMenu BuildMenu (TargetKind kind, UnityEngine.Object[] targets, string selected)
            {
                var menu = new GenericMenu();
                menu.AddItem(new("None (Unassign)"), selected == "", () => UnassignAll(targets));
                menu.AddSeparator("");
                switch (kind)
                {
                    case TargetKind.UI:
                        AddItem(UIConfiguration.DefaultUIPathPrefix);
                        break;
                    case TargetKind.Font:
                        AddItem(UIConfiguration.DefaultFontPathPrefix);
                        break;
                    case TargetKind.Text:
                        AddItem(ManagedTextConfiguration.DefaultPathPrefix);
                        break;
                    case TargetKind.Audio:
                        AddItem(AudioConfiguration.DefaultAudioPathPrefix);
                        AddItem(AudioConfiguration.DefaultVoicePathPrefix);
                        break;
                    case TargetKind.Video:
                        AddItem(MoviesConfiguration.DefaultPathPrefix);
                        AddActorItems<CharactersConfiguration, VideoCharacter>();
                        AddActorItems<BackgroundsConfiguration, VideoBackground>();
                        break;
                    case TargetKind.Texture:
                        AddActorItems<CharactersConfiguration, SpriteCharacter>();
                        AddActorItems<BackgroundsConfiguration, SpriteBackground>();
                        break;
                    case TargetKind.Spine:
                        #if NANINOVEL_ENABLE_SPINE
                        AddActorItems<CharactersConfiguration, SpineCharacter>();
                        AddActorItems<BackgroundsConfiguration, SpineBackground>();
                        #endif
                        break;
                    case TargetKind.Live2D:
                        #if NANINOVEL_ENABLE_LIVE2D
                        AddActorItems<CharactersConfiguration, Live2DCharacter>();
                        #endif
                        break;
                    case TargetKind.DicedAtlas:
                        #if SPRITE_DICING_AVAILABLE
                        AddActorItems<CharactersConfiguration, DicedSpriteCharacter>();
                        AddActorItems<BackgroundsConfiguration, DicedSpriteBackground>();
                        #endif
                        break;
                    case TargetKind.GenericCharacter:
                        AddActorItems<CharactersConfiguration, GenericCharacter>();
                        break;
                    case TargetKind.GenericBackground:
                        AddActorItems<BackgroundsConfiguration, GenericBackground>();
                        break;
                    case TargetKind.LayeredCharacter:
                        AddActorItems<CharactersConfiguration, LayeredCharacter>();
                        break;
                    case TargetKind.LayeredBackground:
                        AddActorItems<BackgroundsConfiguration, LayeredBackground>();
                        break;
                    case TargetKind.SceneBackground:
                        AddActorItems<BackgroundsConfiguration, SceneBackground>();
                        break;
                    case TargetKind.TextPrinter:
                        AddActorItems<TextPrintersConfiguration, UITextPrinter>();
                        break;
                    case TargetKind.ChoiceHandler:
                        AddActorItems<ChoiceHandlersConfiguration, UIChoiceHandler>();
                        break;
                    case TargetKind.ChoiceButton:
                        AddItem(ChoiceHandlersConfiguration.DefaultButtonPathPrefix);
                        break;
                    case TargetKind.Prefab:
                        AddItem(SpawnConfiguration.DefaultPathPrefix);
                        AddItem(UnlockablesConfiguration.DefaultPathPrefix);
                        break;
                }
                return menu;

                void AddActorItems<TCfg, TImpl> () where TCfg : ActorManagerConfiguration
                {
                    var impl = typeof(TImpl).AssemblyQualifiedName;
                    var map = Configuration.GetOrDefault<TCfg>().MetadataMap;
                    var multi = ActorImplementations.TryGetResourcesAttribute(impl, out var attr) &&
                                attr.AllowMultiple;
                    using var _ = map.RentIds(out var ids);
                    foreach (var id in ids)
                        if (map.GetMetadata(id) is { } meta && meta.Implementation == impl)
                            if (multi) AddItem($"{meta.Loader.PathPrefix}/{id}", group: meta.GetResourceGroup());
                            else AddItem(meta.Loader.PathPrefix, id, meta.GetResourceGroup());
                }

                void AddItem (string prefix, string path = null, string group = null)
                {
                    var label = path != null ? Resource.BuildFullPath(prefix, path) : prefix;
                    var on = selected == label || selected.Contains(label + ", ") || selected.Contains(", " + label);
                    menu.AddItem(new(label), on, on
                        ? () => Unassign(targets, prefix, path)
                        : () => Assign(targets, prefix, path, group));
                }
            }
        }

        private static bool DrawButton (Context ctx)
        {
            var content = string.IsNullOrEmpty(ctx.Label) ? emptyButtonContent
                : new($" {ctx.Label}  ", GUIContents.NaninovelIcon.image, "Assign Naninovel Resource");
            return EditorGUI.DropdownButton(GetRect(ctx, content), content, default, buttonStyle);

            static Rect GetRect (Context ctx, GUIContent content)
            {
                // estimating the button rect inside the header controls GUI,
                // because Editor.OnHeaderControlsGUI is internal
                const float headerControlsY = 28;
                const float headerControlsHeight = 30;
                const float headerControlsPadding = 3;
                var inspectorWidth = GUILayoutUtility.GetRect(0, 0).width;
                var headerControlsWidth = (ctx.IsPrefab ? 91 : 44) + headerControlsPadding;
                var buttonWidth = buttonStyle.CalcSize(content).x;
                if (EditorGUIUtility.pixelsPerPoint >= 2) buttonWidth -= 16; // compensate 2x icon size
                var x = inspectorWidth - headerControlsWidth - buttonWidth;
                return new(x, headerControlsY, buttonWidth, headerControlsHeight);
            }
        }

        private static void Assign (UnityEngine.Object[] targets, string prefix,
            string path = null, string group = null)
        {
            foreach (var target in targets)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long _))
                {
                    var lp = path ?? Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                    Assets.Register(new(guid, prefix, lp, group));
                    Addressables.Register(guid, prefix, lp);
                }
            AssetDatabase.SaveAssets();
        }

        private static void Unassign (UnityEngine.Object[] targets, string prefix, string path = null)
        {
            foreach (var target in targets)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long _))
                {
                    var lp = path ?? Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                    var fullPath = Resource.BuildFullPath(prefix, lp);
                    Assets.Unregister(fullPath);
                    Addressables.UnregisterResource(fullPath);
                }
            AssetDatabase.SaveAssets();
        }

        private static void UnassignAll (UnityEngine.Object[] targets)
        {
            foreach (var target in targets)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long _))
                {
                    Assets.UnregisterWithGuid(guid);
                    Addressables.UnregisterAsset(guid);
                }
            AssetDatabase.SaveAssets();
        }
    }
}

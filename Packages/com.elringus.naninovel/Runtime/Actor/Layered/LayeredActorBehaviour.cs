using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// When applied to a <see cref="GameObject"/>, containing child objects with <see cref="Renderer"/> components (layers), 
    /// handles the composition (layers enabled state) and rendering to a texture in back to front order based on z-position and sort order.
    /// Will prevent the child renderers from being rendered by the cameras at play mode.
    /// </summary>
    [ExecuteAlways]
    public abstract class LayeredActorBehaviour : MonoBehaviour
    {
        [Serializable]
        public class CompositionMapItem
        {
            public string Key;
            [TextArea(1, 5)]
            public string Composition;
        }

        public virtual bool Animated => animated;
        public virtual string DefaultAppearance => defaultAppearance;
        public virtual bool RenderOnly => renderOnly;

        protected virtual bool Reversed => reversed;
        protected virtual Material SharedRenderMaterial => renderMaterial;
        protected virtual Camera RenderCamera => renderCamera;
        protected virtual int CameraMask => cameraMask;
        protected virtual LayeredDrawer Drawer { get; private set; }
        protected virtual string ActorId => transform.parent?.name ?? "";

        [Tooltip("Whether the actor should be rendered every frame. Enable when animating the layers or implementing other dynamic behaviour.")]
        [SerializeField] private bool animated;
        [Tooltip("Whether to render the layers in a reversed order.")]
        [SerializeField] private bool reversed;
        [Tooltip("Shared material to use when rendering the layers. Will use layer renderer's material when not assigned.")]
        [SerializeField] private Material renderMaterial;
        [Tooltip("When assigned, will render the prefab content with the camera instead of procedural renderer. Less optimized, but supports more features, such as particle systems.")]
        [SerializeField] private Camera renderCamera;
        [Tooltip("Additional layers to include when rendering the actor in camera mode."), LayerMask]
        [SerializeField] private int cameraMask;
        [Tooltip("Allows to map layer composition expressions to keys; the keys can then be used to specify layered actor appearances instead of the full expressions.")]
        [SerializeField] private List<CompositionMapItem> compositionMap = new();
        [Tooltip("Appearance to use by default. Will use layered expression of the initial prefab state when not specified.")]
        [SerializeField] private string defaultAppearance;
        [Tooltip("Whether to disable layer-related behaviour and just render the prefab content. Enable when controlling appearance via external means (eg, with Animator).")]
        [SerializeField] private bool renderOnly;
        [Tooltip("Invoked when appearance of the actor is changed.")]
        [SerializeField] private StringUnityEvent onAppearanceChanged;
        [Tooltip("Invoked when visibility of the actor is changed.")]
        [SerializeField] private BoolUnityEvent onVisibilityChanged;

        [CanBeNull] private LayeredCompositor compositor;

        /// <summary>
        /// Returns current actor layer composition.
        /// </summary>
        public virtual string GetComposition ()
        {
            if (Drawer == null || Drawer.Layers.Count == 0) return "";
            compositor ??= new(ActorId, GetCompositionMap());
            return compositor.Compose(Drawer.Layers);
        }

        /// <summary>
        /// Returns all the composition expressions mapped to keys via <see cref="compositionMap"/> serialized field.
        /// Records with duplicate keys are ignored.
        /// </summary>
        public virtual Dictionary<string, string> GetCompositionMap ()
        {
            var map = new Dictionary<string, string>();
            foreach (var item in compositionMap)
                map[item.Key] = item.Composition;
            return map;
        }

        /// <summary>
        /// Fires the <see cref="onAppearanceChanged"/> event.
        /// </summary>
        public virtual void NotifyAppearanceChanged (string appearance)
        {
            onAppearanceChanged?.Invoke(appearance);
        }

        /// <summary>
        /// Notifies when the actor becomes visible or completely invisible on the screen.
        /// </summary>
        public virtual void NotifyPerceivedVisibilityChanged (bool visible)
        {
            if (!visible) Drawer.ReleaseCameraLayer();
            onVisibilityChanged?.Invoke(visible);
        }

        /// <summary>
        /// Applies specified layer composition expression to the actor.
        /// </summary>
        public virtual void ApplyComposition (string composition)
        {
            if (Drawer.Layers is null || Drawer.Layers.Count == 0) return;
            compositor ??= new(ActorId, GetCompositionMap());
            compositor.Apply(composition, Drawer.Layers);
        }

        /// <summary>
        /// Rebuilds the layers and associated rendering parameters.
        /// </summary>
        [ContextMenu("Rebuild Layers")]
        public virtual void RebuildLayers () => Drawer?.BuildLayers();

        /// <summary>
        /// Renders the enabled layers scaled by <paramref name="pixelsPerUnit"/> to the specified or a temporary <see cref="RenderTexture"/>.
        /// Don't forget to release unused render textures.
        /// </summary>
        /// <param name="pixelsPerUnit">PPU to use when rendering.</param>
        /// <param name="renderTexture">Render texture to render the content into; when not specified, will create a temporary one.</param>
        /// <returns>Temporary render texture created when no render texture is specified.</returns>
        public virtual RenderTexture Render (float pixelsPerUnit, RenderTexture renderTexture = default)
        {
            return Drawer.DrawLayers(pixelsPerUnit, renderTexture);
        }

        protected virtual void Awake ()
        {
            Drawer = CreateDrawer();
        }

        protected virtual void OnDestroy ()
        {
            Drawer?.Dispose();
        }

        protected virtual void OnDrawGizmos ()
        {
            Drawer.DrawGizmos();
        }

        protected virtual void OnValidate ()
        {
            // Drawer is null when entering-exiting play mode while
            // a layered prefab is opened in edit mode (Awake is not invoked).
            Drawer ??= CreateDrawer();
        }

        protected virtual LayeredDrawer CreateDrawer ()
        {
            return new(transform, RenderCamera, CameraMask, SharedRenderMaterial, Reversed);
        }
    }
}

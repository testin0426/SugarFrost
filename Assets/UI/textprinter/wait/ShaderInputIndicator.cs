using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// Drop-in replacement for <see cref="PingPongInputIndicator"/> that displays a
    /// looping loading shader instead of ping-pong tinting. The shader runs on a
    /// per-instance material clone (the shared material asset is never mutated and
    /// each printer animates independently), and is pluggable via
    /// <see cref="ILoadingShader"/> so different loading shaders can be swapped
    /// without touching this component. The hosting text printer positions this
    /// indicator's RectTransform at the end of the revealed text, so it tracks
    /// where the text ends while the shader loops.
    /// </summary>
    public class ShaderInputIndicator : ScriptableUIComponent<RawImage>, IInputIndicator
    {
        protected virtual float RevealTime => revealTime;
        protected virtual ILoadingShader LoadingShader => loadingShader as ILoadingShader;

        [Tooltip("Material running one of the loading shaders. A per-instance clone is created at runtime; the asset itself is left untouched.")]
        [SerializeField] private Material sourceMaterial;
        [Tooltip("Component implementing 'ILoadingShader' that drives the shader. Leave unset for shaders that animate on their own (eg, via _Time).")]
        [SerializeField] private MonoBehaviour loadingShader;
        [Tooltip("Fade-in duration (in seconds) when the indicator is shown.")]
        [SerializeField] private float revealTime = 0.5f;

        private Material materialInstance;
        private float showTime;

        public override void Show ()
        {
            showTime = Engine.Time.UnscaledTime;
            ChangeVisibility(true, RevealTime).Forget();
        }

        public override void Hide () => Visible = false;

        protected override void Awake ()
        {
            base.Awake();

            if (loadingShader && LoadingShader == null)
                throw new Error($"'{loadingShader.GetType().Name}' assigned as loading shader on '{name}' doesn't implement '{nameof(ILoadingShader)}'.");

            var source = sourceMaterial ? sourceMaterial : UIComponent.material;
            if (!source) throw new Error($"Source material is not assigned on '{name}' shader input indicator.");

            materialInstance = new Material(source);
            UIComponent.material = materialInstance;
            LoadingShader?.Initialize(materialInstance);
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            ObjectUtils.DestroyOrImmediate(materialInstance);
        }

        protected virtual void Update ()
        {
            if (Visible)
                LoadingShader?.Tick(Engine.Time.UnscaledTime - showTime);
        }
    }
}

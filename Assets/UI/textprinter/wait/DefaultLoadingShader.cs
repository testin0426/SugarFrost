using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// <see cref="ILoadingShader"/> for the shaders under this folder
    /// (Xiangyun_Planet, Baishuang_Atom, Lian_Watch, Qianhong_Snowflake) — they
    /// share the same _Zoom / _BgAlpha properties and animate off the global
    /// _Time, so the loop needs no per-frame driving.
    /// </summary>
    public class DefaultLoadingShader : MonoBehaviour, ILoadingShader
    {
        protected virtual float Zoom => zoom;
        protected virtual float BackgroundAlpha => backgroundAlpha;

        [Tooltip("Pushed into the shader's '_Zoom' property on the owned material instance.")]
        [SerializeField] private float zoom = 2.8f;
        [Tooltip("Pushed into the shader's '_BgAlpha' property on the owned material instance.")]
        [SerializeField, Range(0f, 1f)] private float backgroundAlpha = 1f;

        private static readonly int zoomId = Shader.PropertyToID("_Zoom");
        private static readonly int bgAlphaId = Shader.PropertyToID("_BgAlpha");

        public virtual void Initialize (Material material)
        {
            material.SetFloat(zoomId, Zoom);
            material.SetFloat(bgAlphaId, BackgroundAlpha);
        }

        public virtual void Tick (float elapsed) { }
    }
}

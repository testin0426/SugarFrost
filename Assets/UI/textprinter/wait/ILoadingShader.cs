using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Drives a looping "loading" shader on the per-instance material owned by
    /// <see cref="ShaderInputIndicator"/>. Implement once per shader (or reuse
    /// <see cref="DefaultLoadingShader"/> for shaders that self-animate via _Time)
    /// so the indicator stays shader-agnostic and shaders can be swapped freely.
    /// </summary>
    public interface ILoadingShader
    {
        /// <summary>
        /// Invoked once with the material clone the indicator owns.
        /// Cache property IDs and push per-instance defaults here.
        /// </summary>
        void Initialize (Material material);

        /// <summary>
        /// Invoked every frame while the indicator is visible.
        /// <paramref name="elapsed"/> is unscaled seconds since the indicator was
        /// last shown — feed it to a shader property if the animation must restart
        /// (loop) per show. No-op for shaders that animate off the global _Time.
        /// </summary>
        void Tick (float elapsed);
    }
}

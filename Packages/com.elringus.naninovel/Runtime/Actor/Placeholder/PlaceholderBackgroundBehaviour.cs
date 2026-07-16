using UnityEngine;

namespace Naninovel
{
    public class PlaceholderBackgroundBehaviour : LayeredBackgroundBehaviour
    {
        [Header("Setup")]
        [SerializeField] private MeshRenderer mesh;
        [SerializeField] private PlaceholderBackgroundText text;

        private static readonly Vector4[] black = { Color.black };
        private static readonly int colorsId = Shader.PropertyToID("_Colors");
        private static readonly int colorCountId = Shader.PropertyToID("_ColorCount");
        private static readonly int angleId = Shader.PropertyToID("_Angle");
        private static readonly int scrollSpeedId = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int radialId = Shader.PropertyToID("_Radial");

        private readonly Vector4[] colors = new Vector4[32];
        private MaterialPropertyBlock mat;
        private ICameraManager cam;

        public virtual void SetAppearance (PlaceholderBackgroundAppearance bg)
        {
            mat ??= new();
            mesh.GetPropertyBlock(mat);

            if (bg.Colors != null && bg.Colors.Length > 0)
            {
                var count = Mathf.Min(bg.Colors.Length, colors.Length);
                for (var i = 0; i < count; i++)
                    colors[i] = bg.Colors[i];

                // When scrolling or radial, append the first color for a seamless loop.
                var append = !Mathf.Approximately(bg.Speed, 0) || bg.Radial;
                if (append && count > 0 && count < colors.Length)
                    colors[count++] = colors[0];

                mat.SetVectorArray(colorsId, colors);
                mat.SetInt(colorCountId, count);
            }
            else
            {
                mat.SetVectorArray(colorsId, black);
                mat.SetInt(colorCountId, 1);
            }

            mat.SetFloat(angleId, bg.Angle);
            mat.SetFloat(scrollSpeedId, bg.Speed);
            mat.SetFloat(radialId, bg.Radial ? 1f : 0f);
            mesh.SetPropertyBlock(mat);

            text.Initialize(bg.Name, bg.TextSpeed, bg.TextSize, bg.TextColor);
        }

        private void Update ()
        {
            if (!Engine.Initialized) return;
            cam ??= Engine.GetServiceOrErr<ICameraManager>();
            var height = cam.OrthographicSize * 2f;
            var width = height * cam.Camera.aspect;
            transform.parent.localScale = new(width, height, 1f);
        }
    }
}

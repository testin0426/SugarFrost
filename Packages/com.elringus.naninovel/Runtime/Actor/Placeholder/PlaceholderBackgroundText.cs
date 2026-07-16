using TMPro;
using UnityEngine;

namespace Naninovel
{
    [RequireComponent(typeof(MeshRenderer))]
    public class PlaceholderBackgroundText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro tmp;

        private static readonly int mainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int tileScaleId = Shader.PropertyToID("_TileScale");
        private static readonly int scrollSpeedId = Shader.PropertyToID("_ScrollSpeed");

        private MaterialPropertyBlock mat;
        private MeshRenderer mesh;
        private Bounds lastBounds;
        private RenderTexture rt;
        private Vector2 scrollSpeed;
        private bool dirty;

        public void Initialize (string text, Vector2 speed, float size, Color color)
        {
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            scrollSpeed = speed;
            dirty = true;
        }

        private void Awake ()
        {
            mesh = GetComponent<MeshRenderer>();
            mat = new();
        }

        private void OnDestroy ()
        {
            DisposeRenderTexture();
        }

        private void Update ()
        {
            if (mesh.bounds != lastBounds || dirty)
            {
                dirty = false;
                lastBounds = mesh.bounds;
                RenderText();
            }
        }

        private void RenderText ()
        {
            tmp.ForceMeshUpdate();
            mesh.GetPropertyBlock(mat);
            DisposeRenderTexture();
            CreateRenderTexture();
            RenderToTexture();
            UpdateTiling();
            mesh.SetPropertyBlock(mat);
        }

        private void CreateRenderTexture ()
        {
            const int minSize = 128;
            var baseSize = Mathf.CeilToInt(minSize * Mathf.Max(1, tmp.fontSize));
            var textAspectRatio = tmp.preferredWidth / tmp.preferredHeight;
            var width = Mathf.CeilToInt(baseSize * textAspectRatio);
            var height = baseSize;
            width = Mathf.Clamp(width, minSize, SystemInfo.maxTextureSize);
            height = Mathf.Clamp(height, minSize, SystemInfo.maxTextureSize);
            rt = new(width, height, 0, RenderTextureFormat.ARGB32) { wrapMode = TextureWrapMode.Repeat };
            rt.Create();
            mat.SetTexture(mainTextureId, rt);
        }

        private void RenderToTexture ()
        {
            RenderUtils.EnsureGLTarget();

            var previousRT = RenderTexture.active;
            RenderTexture.active = rt;

            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, rt.width, 0, rt.height, -999, 999));
            GL.modelview = Matrix4x4.identity;

            var scale = Vector3.one * (rt.height / tmp.preferredHeight);
            var trs = Matrix4x4.TRS(new(rt.width / 2f, rt.height / 2f, 0), tmp.transform.rotation, scale);

            foreach (var meshInfo in tmp.textInfo.meshInfo)
            {
                if (!meshInfo.mesh || meshInfo.mesh.vertexCount == 0) continue;
                meshInfo.material.SetPass(0);
                Graphics.DrawMeshNow(meshInfo.mesh, trs);
            }

            GL.PopMatrix();
            RenderTexture.active = previousRT;
        }

        private void UpdateTiling ()
        {
            var aspect = tmp.preferredWidth / tmp.preferredHeight;
            var width = tmp.fontSize * aspect;
            var tilesX = mesh.bounds.size.x / width;
            var tilesY = mesh.bounds.size.y / tmp.fontSize;
            mat.SetVector(tileScaleId, new(tilesX, tilesY));

            var speedX = mesh.bounds.size.x > 0 ? scrollSpeed.x / mesh.bounds.size.x : 0;
            var speedY = mesh.bounds.size.y > 0 ? scrollSpeed.y / mesh.bounds.size.y : 0;
            mat.SetVector(scrollSpeedId, new(speedX, speedY));
        }

        private void DisposeRenderTexture ()
        {
            if (!rt) return;
            rt.Release();
            DestroyImmediate(rt);
            rt = null;
        }
    }
}

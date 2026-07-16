using System.Threading;
using Naninovel;
using UnityEngine;

[DisallowMultipleComponent]
public class FadeInOnFirstActivate : MonoBehaviour
{
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Renderer targetRenderer;

    private Material mat;
    private bool didFade;
    private CancellationTokenSource cts;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            Debug.LogError($"[FadeInOnFirstActivate] {name} has no Renderer.", this);
            enabled = false;
            return;
        }

        mat = targetRenderer.material;
        if (!mat.HasProperty(TintColorId))
        {
            Debug.LogError($"[FadeInOnFirstActivate] Material on {name} has no _TintColor.", this);
            enabled = false;
            return;
        }

        var c = mat.GetColor(TintColorId);
        c.a = 0f;
        mat.SetColor(TintColorId, c);
    }

    private void Start()
    {
        if (didFade) return;
        didFade = true;
        cts = new CancellationTokenSource();
        FadeIn(cts.Token).Forget();
    }

    private void OnDestroy()
    {
        if (cts == null) return;
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }

    private async Awaitable FadeIn(CancellationToken ct)
    {
        var c = mat.GetColor(TintColorId);
        float startA = c.a;
        float dur = Mathf.Max(duration, 0.0001f);
        float elapsed = 0f;

        while (elapsed < dur)
        {
            if (ct.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startA, 1f, Mathf.Clamp01(elapsed / dur));
            mat.SetColor(TintColorId, c);
            await Async.NextFrame(ct);
        }

        c.a = 1f;
        mat.SetColor(TintColorId, c);
    }
}

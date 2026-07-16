using System.Threading;
using Naninovel;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GenericActorBehaviour))]
public class VisibilityFadeBridge : MonoBehaviour
{
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    [Tooltip("The plane (or any child) whose alpha and tint will be faded. Must start with activeSelf=false in the prefab.")]
    [SerializeField] private GameObject plane;
    [SerializeField] private Renderer planeRenderer;

    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("Tint color while hidden. RGB only — alpha is driven by _Alpha separately.")]
    [SerializeField] private Color hiddenTint = Color.black;

    [Tooltip("Log every visibility event and fade boundary. Turn off once stable.")]
    [SerializeField] private bool verbose = true;

    // The RT plane this rig displays on; @rtfx resolves through this instead of
    // scanning child renderers (see FXStateService.FindRenderer).
    public Renderer PlaneRenderer => planeRenderer;

    private GenericActorBehaviour behaviour;
    private Material mat;
    private Color shownTint = Color.white;
    private CancellationTokenSource cts;
    private bool? lastVisible;
    private bool fadeRunning;

    // Snap instantly (no fade) while Naninovel is fast-forwarding or restoring state.
    private bool ShouldSnapInstant
    {
        get
        {
            if (!Engine.Initialized) return false;
            var player = Engine.GetService<IScriptPlayer>();
            var state = Engine.GetService<IStateManager>();
            return (player != null && player.Skipping) || (state != null && state.RollbackInProgress);
        }
    }

    private void Awake()
    {
        behaviour = GetComponent<GenericActorBehaviour>();

        if (plane == null && planeRenderer != null) plane = planeRenderer.gameObject;
        if (planeRenderer == null && plane != null) planeRenderer = plane.GetComponent<Renderer>();

        if (plane == null || planeRenderer == null)
        {
            Debug.LogError($"[VisibilityFadeBridge] {name}: assign 'plane' (and it must have a Renderer).", this);
            enabled = false;
            return;
        }

        mat = planeRenderer.material;
        if (!mat.HasProperty(AlphaId) || !mat.HasProperty(TintColorId))
        {
            Debug.LogError($"[VisibilityFadeBridge] {name}: material needs both _Alpha and _TintColor.", this);
            enabled = false;
            return;
        }

        shownTint = mat.GetColor(TintColorId);

        behaviour.OnVisibilityChanged += HandleVisibilityChanged;
    }

    private void OnDestroy()
    {
        if (behaviour != null) behaviour.OnVisibilityChanged -= HandleVisibilityChanged;
        CancelRunning();
    }

    public void HandleVisibilityChanged(bool visible)
    {
        // Only de-dupe when state is coherent (no fade running, and not snapping for skip/rollback
        // where the material may be at a stale partial value that still needs correcting).
        if (lastVisible == visible && !fadeRunning && !ShouldSnapInstant)
        {
            if (verbose) Debug.Log($"[VisFade {name}] visibility={visible} → deduped (already settled)", this);
            return;
        }
        lastVisible = visible;

        CancelRunning();

        // Skip / rollback: land on the final state immediately, no async fade to race against.
        if (ShouldSnapInstant)
        {
            SnapTo(visible);
            if (verbose) Debug.Log($"[VisFade {name}] visibility={visible} → SNAP (skip/rollback)", this);
            return;
        }

        cts = new CancellationTokenSource();

        if (verbose) Debug.Log($"[VisFade {name}] visibility={visible} → start fade (a={mat.GetFloat(AlphaId):F2} tint={ColorToHex(mat.GetColor(TintColorId))})", this);

        if (visible) ShowAndFadeIn(cts.Token).Forget();
        else FadeOutAndHide(cts.Token).Forget();
    }

    // Forces the exact final state with no animation. Shown = (shownTint, 1); hidden = (hiddenTint, 0) + plane off.
    private void SnapTo(bool visible)
    {
        if (visible)
        {
            if (!plane.activeSelf) plane.SetActive(true);
            SetState(1f, shownTint);
        }
        else
        {
            SetState(0f, hiddenTint);
            if (plane.activeSelf) plane.SetActive(false);
        }
    }

    private static string ColorToHex(Color c) => $"#{ColorUtility.ToHtmlStringRGB(c)}";

    private void CancelRunning()
    {
        if (cts == null) return;
        cts.Cancel();
        cts.Dispose();
        cts = null;
    }

    private async Awaitable ShowAndFadeIn(CancellationToken ct)
    {
        fadeRunning = true;
        try
        {
            SetState(0f, hiddenTint);
            if (!plane.activeSelf) plane.SetActive(true);
            await Lerp(0f, 1f, hiddenTint, shownTint, fadeInDuration, ct);
            if (verbose) Debug.Log($"[VisFade {name}] fade IN complete", this);
        }
        finally { fadeRunning = false; }
    }

    private async Awaitable FadeOutAndHide(CancellationToken ct)
    {
        if (!plane.activeSelf) return;
        fadeRunning = true;
        try
        {
            float startA = mat.GetFloat(AlphaId);
            Color startTint = mat.GetColor(TintColorId);
            await Lerp(startA, 0f, startTint, hiddenTint, fadeOutDuration, ct);
            if (!ct.IsCancellationRequested)
            {
                plane.SetActive(false);
                if (verbose) Debug.Log($"[VisFade {name}] fade OUT complete → plane disabled", this);
            }
        }
        finally { fadeRunning = false; }
    }

    private async Awaitable Lerp(float fromA, float toA, Color fromTint, Color toTint, float duration, CancellationToken ct)
    {
        float dur = Mathf.Max(duration, 0.0001f);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            if (ct.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            SetState(Mathf.Lerp(fromA, toA, t), Color.Lerp(fromTint, toTint, t));
            await Async.NextFrame(ct);
        }
        SetState(toA, toTint);
    }

    private void SetState(float a, Color tint)
    {
        mat.SetFloat(AlphaId, a);
        mat.SetColor(TintColorId, tint);
    }
}

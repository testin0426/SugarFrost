using System.Collections.Generic;
using Naninovel;
using UnityEngine;

// Holds the *logical* state of the screen FX so Naninovel can roll it back.
//
// @playFX / @playUIFX animate a material/graphic directly. Naninovel's rollback
// only restores the serialized state of engine services, so without this the FX
// value set on a later line "residues" after rolling back to an earlier line and
// keeps covering the text. Commands record their final value here; on rollback
// Naninovel restores the snapshot and we re-apply it (instantly) to the scene.
//
// @rtfx records generic (object, property) -> value entries the same way; the
// special object name "global" maps to Shader.SetGlobal* (shared masks).
[InitializeAtRuntime]
public class FXStateService : IStatefulService<GameStateMap>
{
    [System.Serializable]
    public class PropState
    {
        public const int KindFloat = 0;
        public const int KindColor = 1;
        public const int KindVisible = 2;

        public string Obj;
        public string Prop; // "__visible" for visibility entries
        public int Kind;
        public float FloatValue;
        public Color ColorValue;
        public bool BoolValue;
    }

    [System.Serializable]
    public class GameState
    {
        public bool HasFx;
        public float FxThreshold;
        public bool HasUiFx;
        public float UiFxAlpha;
        public List<PropState> Props;
    }

    public const string GlobalTarget = "global";
    public const string VisibleProp = "__visible";

    private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");

    // null == "never set in this game", so we leave the scene at its authored value.
    private float? fxThreshold;
    private float? uiFxAlpha;
    private readonly List<PropState> props = new List<PropState>();

    public Awaitable InitializeService () => Async.Completed;

    public void ResetService ()
    {
        // New game / title: drop any recorded FX so it doesn't bleed across sessions.
        fxThreshold = null;
        uiFxAlpha = null;
        props.Clear();
    }

    public void DestroyService () { }

    // Called by the commands with the value they animate toward. This is the value
    // that gets snapshotted for rollback.
    public void RecordFx (float threshold) => fxThreshold = threshold;
    public void RecordUiFx (float alpha) => uiFxAlpha = alpha;

    public void RecordPropFloat (string obj, string prop, float value)
        => RecordProp(new PropState { Obj = obj, Prop = prop, Kind = PropState.KindFloat, FloatValue = value });

    public void RecordPropColor (string obj, string prop, Color value)
        => RecordProp(new PropState { Obj = obj, Prop = prop, Kind = PropState.KindColor, ColorValue = value });

    public void RecordVisibility (string obj, bool visible)
        => RecordProp(new PropState { Obj = obj, Prop = VisibleProp, Kind = PropState.KindVisible, BoolValue = visible });

    private void RecordProp (PropState entry)
    {
        // One entry per (object, property): later commands overwrite earlier ones.
        var index = props.FindIndex(p => p.Obj == entry.Obj && p.Prop == entry.Prop);
        if (index >= 0) props[index] = entry;
        else props.Add(entry);
    }

    public void SaveServiceState (GameStateMap stateMap)
    {
        stateMap.SetState(new GameState {
            HasFx = fxThreshold.HasValue,
            FxThreshold = fxThreshold ?? 0f,
            HasUiFx = uiFxAlpha.HasValue,
            UiFxAlpha = uiFxAlpha ?? 0f,
            Props = new List<PropState>(props)
        });
    }

    public Awaitable LoadServiceState (GameStateMap stateMap)
    {
        var state = stateMap.GetState<GameState>();
        fxThreshold = state is { HasFx: true } ? state.FxThreshold : (float?)null;
        uiFxAlpha = state is { HasUiFx: true } ? state.UiFxAlpha : (float?)null;
        props.Clear();
        if (state?.Props != null) props.AddRange(state.Props);

        // Re-apply instantly (rollback is not animated).
        if (fxThreshold.HasValue) ApplyFx(fxThreshold.Value);
        if (uiFxAlpha.HasValue) ApplyUiFx(uiFxAlpha.Value);
        foreach (var prop in props) ApplyProp(prop);
        return Async.Completed;
    }

    public static void ApplyProp (PropState p)
    {
        if (p.Obj == GlobalTarget)
        {
            if (p.Kind == PropState.KindFloat) Shader.SetGlobalFloat(p.Prop, p.FloatValue);
            else if (p.Kind == PropState.KindColor) Shader.SetGlobalColor(p.Prop, p.ColorValue);
            return;
        }

        if (p.Kind == PropState.KindVisible)
        {
            ApplyVisibility(p.Obj, p.BoolValue);
            return;
        }

        var renderer = FindRenderer(p.Obj, p.Prop);
        if (renderer == null) return;
        var mat = renderer.material;
        if (p.Kind == PropState.KindFloat) mat.SetFloat(p.Prop, p.FloatValue);
        else if (p.Kind == PropState.KindColor) mat.SetColor(p.Prop, p.ColorValue);
    }

    // Resolves the renderer @rtfx should drive. The object name may point at a
    // prefab root (e.g. a character rig with the RT plane as a child), so this
    // searches the object and its children for the first renderer whose material
    // has the requested property. Probes sharedMaterial so no material instances
    // are created on unrelated renderers (e.g. the rig's skinned meshes).
    public static Renderer FindRenderer (string obj, string requiredProp)
    {
        var go = GameObject.Find(obj);
        if (go == null) return null;

        // Character rigs carry a VisibilityFadeBridge with the RT plane authored
        // in — trust that reference over scanning, since rig meshes could
        // coincidentally expose a property with the same name.
        var bridge = go.GetComponentInChildren<VisibilityFadeBridge>(true);
        if (bridge != null && bridge.PlaneRenderer != null &&
            bridge.PlaneRenderer.sharedMaterial != null &&
            bridge.PlaneRenderer.sharedMaterial.HasProperty(requiredProp))
            return bridge.PlaneRenderer;

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(requiredProp))
                return r;
        return null;
    }

    // Visibility toggles every renderer on/under the object: for character rigs
    // that's the RT plane plus the capture-side meshes — either way the layer
    // disappears, and one bool restores it. renderer.enabled (not SetActive) so
    // GameObject.Find still resolves the object afterwards. Returns false when
    // the object is missing or has no renderers at all.
    public static bool ApplyVisibility (string obj, bool visible)
    {
        var go = GameObject.Find(obj);
        if (go == null) return false;
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = visible;
        return renderers.Length > 0;
    }

    private static void ApplyFx (float threshold)
    {
        var fx = GameObject.Find("FX");
        if (fx == null || !fx.TryGetComponent<Renderer>(out var renderer)) return;
        var mat = renderer.material;
        if (mat.HasProperty(ThresholdId)) mat.SetFloat(ThresholdId, threshold);
    }

    private static void ApplyUiFx (float alpha)
    {
        var uiFx = GameObject.Find("UIFX");
        if (uiFx == null) return;
        if (!uiFx.TryGetComponent<UnityEngine.UI.Graphic>(out var graphic))
            graphic = uiFx.GetComponentInChildren<UnityEngine.UI.Graphic>(true);
        if (graphic == null) return;
        var c = graphic.color;
        graphic.color = new Color(c.r, c.g, c.b, alpha);
    }
}

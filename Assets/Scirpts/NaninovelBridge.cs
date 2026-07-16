using Naninovel;
using Naninovel.UI;
using UnityEngine;

/// <summary>
/// Glue between stock Unity UI (Button.OnClick, etc.) and Naninovel services.
/// All public methods are UnityEvent-friendly (no-arg or single string), so you
/// can wire them straight from the Inspector with no custom button subclass.
///
/// "Goto" mirrors the @goto command (Goto.cs): LoadAndPlay / LoadAndPlayAtLabel
/// with an optional state reset. Show/Hide UI mirrors @showUI / @hideUI, which
/// resolve a managed UI by EXACT, case-sensitive resource name.
/// </summary>
[DisallowMultipleComponent]
public class NaninovelBridge : MonoBehaviour
{
    [Header("Goto target (used by the no-arg GotoScript())")]
    [Tooltip("Script path, optionally with a label: \"Chapter2\" or \"Chapter2.AfterStorm\".")]
    [SerializeField] private string scriptPath;

    [Tooltip("Reset engine state before navigating (use for starting a fresh playthrough; " +
             "leave off to jump mid-game like a plain @goto without reset).")]
    [SerializeField] private bool resetState = true;

    [Tooltip("Also hide the title screen when navigating (typical for a 'Start' button).")]
    [SerializeField] private bool hideTitleOnGoto = true;

    [Tooltip("Direct reference to the title screen UI when it is a scene object (enabled via " +
             "activateAfterInit) rather than a Naninovel-managed UI. Used as fallback when " +
             "GetUI<ITitleUI>() returns null — assign the Camera prefab root or its TitleMenu " +
             "component in the inspector.")]
    [SerializeField] private CustomUI sceneTitleUI;

    [Tooltip("Log every action and any failed UI name lookups. Turn off once stable.")]
    [SerializeField] private bool verbose = true;

    private IScriptPlayer Player => Engine.GetServiceOrErr<IScriptPlayer>();
    private IStateManager State => Engine.GetServiceOrErr<IStateManager>();
    private IUIManager UI => Engine.GetServiceOrErr<IUIManager>();

    // ---- Goto -----------------------------------------------------------

    /// <summary>Navigate to the Inspector-configured <see cref="scriptPath"/>. Wire to Button.OnClick().</summary>
    public void GotoScript () => GotoScript(scriptPath);

    /// <summary>Navigate to an explicit path ("Script" or "Script.Label"). Also UnityEvent-friendly.</summary>
    public void GotoScript (string path)
    {
        if (!Engine.Initialized)
        {
            Debug.LogError($"[NaninovelBridge] {name}: engine not initialized; cannot goto '{path}'.", this);
            return;
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogError($"[NaninovelBridge] {name}: scriptPath is empty.", this);
            return;
        }

        var dot = path.IndexOf('.');
        var script = dot >= 0 ? path.Substring(0, dot) : path;
        var label = dot >= 0 ? path.Substring(dot + 1) : null;

        if (hideTitleOnGoto)
        {
            // Try UIManager first (for Naninovel-managed title UIs).
            // If the title menu is a scene object activated via activateAfterInit it is NOT
            // registered with UIManager, so GetUI returns null and the fallback is used instead.
            var managedTitle = UI.GetUI<ITitleUI>() as CustomUI;
            var titleToHide = managedTitle ?? sceneTitleUI;
            if (titleToHide != null) titleToHide.Hide();
            else if (verbose) Debug.LogWarning($"[NaninovelBridge] {name}: could not find title UI to hide. " +
                "Assign 'Scene Title UI' in the inspector if it is a scene object.", this);
        }

        if (verbose)
            Debug.Log($"[NaninovelBridge] goto script='{script}' label='{label}' reset={resetState}", this);

        if (resetState)
            State.ResetState(() => Play(script, label)).Forget();
        else
            Play(script, label).Forget();
    }

    private Awaitable Play (string script, string label) => string.IsNullOrEmpty(label)
        ? Player.MainTrack.LoadAndPlay(script)
        : Player.MainTrack.LoadAndPlayAtLabel(script, label);

    // ---- Show / Hide a custom UI by name --------------------------------

    /// <summary>Show a managed/custom UI by its resource name (exact, case-sensitive).</summary>
    public void ShowUI (string uiName) => SetUIVisible(uiName, true);

    /// <summary>Hide a managed/custom UI by its resource name (exact, case-sensitive).</summary>
    public void HideUI (string uiName) => SetUIVisible(uiName, false);

    private void SetUIVisible (string uiName, bool visible)
    {
        var ui = UI.GetUI(uiName);
        if (ui == null)
        {
            // Same failure @showUI/@hideUI hit: prefab not in the UI config Custom UI
            // list, or the name doesn't exactly match the prefab's resource path.
            Debug.LogError($"[NaninovelBridge] {name}: no managed UI named '{uiName}'. " +
                           $"Add the prefab to Naninovel > Configuration > UI > Custom UI, " +
                           $"and pass the prefab's exact (case-sensitive) resource name.", this);
            return;
        }
        if (verbose) Debug.Log($"[NaninovelBridge] {(visible ? "show" : "hide")} UI '{uiName}'", this);
        ui.ChangeVisibility(visible).Forget();
    }
}

using UnityEngine;

/// <summary>
/// Contract between the custom '@char' armature parameters and a character's
/// armature rig (see <see cref="CharacterArmatureBridge"/>). Reconstructed after
/// the 1.21 upgrade: the original declaration lived inside the (since overwritten)
/// in-package copy of the @char command.
/// </summary>
public interface IArmatureBridge
{
    /// <summary>Last rotation requested via SetScriptedRotation (target euler, degrees).</summary>
    Vector3 ScriptedRotation { get; }

    /// <summary>Toggles the cursor-follow behaviour (on by default).</summary>
    void SetMouseFollow (bool enabled);

    /// <summary>Rotates the armature bone (additively over the animation pose) to the
    /// specified euler angles, lerped over the specified duration in seconds.</summary>
    void SetScriptedRotation (Vector3 euler, float duration);
}

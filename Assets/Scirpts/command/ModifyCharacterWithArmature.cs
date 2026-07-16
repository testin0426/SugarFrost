using Naninovel;
using Naninovel.Commands;
using UnityEngine;

// Restores the custom armature extension of @char that was previously hacked into
// the engine package (lost when upgrading to 1.21): rotates a character's bone on
// top of the animation pose and toggles cursor-follow (see CharacterArmatureBridge).
//
//   @char Sora armature:10,25,0 time:0.5
//   @char Sora mouseFollow:false
//
// Declaring the same "char" alias overrides the built-in command, so existing
// .nani scripts keep working unchanged.
[Alias("char")]
public class ModifyCharacterWithArmature : ModifyCharacter
{
    [Alias("armature")]
    public DecimalListParameter ArmatureRotation;
    [Alias("mouseFollow")]
    public BooleanParameter MouseFollow;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        await base.Execute(ctx);
        if (!Assigned(ArmatureRotation) && !Assigned(MouseFollow)) return;

        var id = IdAndAppearance?.Name;
        if (string.IsNullOrEmpty(id) || id == "*")
        {
            Warn("@char armature/mouseFollow requires a specific character ID.");
            return;
        }

        var bridge = FindBridge(id);
        if (bridge == null)
        {
            Warn($"@char {id}: no IArmatureBridge component found on the actor; armature/mouseFollow ignored.");
            return;
        }

        if (Assigned(MouseFollow)) bridge.SetMouseFollow(MouseFollow);
        if (Assigned(ArmatureRotation))
            bridge.SetScriptedRotation(new Vector3(Elem(0), Elem(1), Elem(2)), AssignedDuration);
    }

    private float Elem (int i) =>
        ArmatureRotation.Length > i && ArmatureRotation[i] != null && ArmatureRotation[i].HasValue
            ? ArmatureRotation[i].Value : 0f;

    private static IArmatureBridge FindBridge (string actorId)
    {
        var manager = Engine.GetService<ICharacterManager>();
        if (manager != null && manager.ActorExists(actorId) &&
            manager.GetActor(actorId) is MonoBehaviourActor<CharacterMetadata> mono && mono.GameObject)
            return mono.GameObject.GetComponentInChildren<IArmatureBridge>(true);

        // Fallback: rigs resolved by scene object name (same convention as FXStateService).
        var go = GameObject.Find(actorId);
        return go ? go.GetComponentInChildren<IArmatureBridge>(true) : null;
    }
}

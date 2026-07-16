// ReSharper disable RedundantUsingDirective

using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Controls a [Timeline](https://docs.unity3d.com/Manual/com.unity.timeline.html) via a
[Director](https://docs.unity3d.com/ScriptReference/Playables.PlayableDirector.html)
component on a scene game object with the specified name. By default the command will
make the director start playing, unless 'stop', 'pause' or 'resume' flags are specified.",
        null,
        @"
; Makes a director component attached to a 'Cutscene001' game object on the scene
; start playing the associated timeline and waits for completion.
@timeline Cutscene001 wait!",
        @"
; Stops a director attached to a 'The Other Cutscene' game object.
@timeline ""The Other Cutscene"" stop!"
    )]
    [Serializable, Alias("timeline"), VisualsGroup]
    public class ControlTimeline : Command
    {
        [Doc("Name of an active scene game object with a 'Playable Director' component attached.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Name;
        [Doc("Whether to stop the director.")]
        public BooleanParameter Stop;
        [Doc("Whether to pause the director.")]
        public BooleanParameter Pause;
        [Doc("Whether to resume the director.")]
        public BooleanParameter Resume;
        [Doc("Whether to wait until the director stops playing before proceeding with the script execution.")]
        public BooleanParameter Wait;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            #if TIMELINE_AVAILABLE
            if (UnityEngine.Object.FindObjectsByType<UnityEngine.Playables.PlayableDirector>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None).FirstOrDefault(d => d.name == Name) is not { } director)
                throw Fail($"Playable director component on a '{Name}' game object is not found on scene.");
            if (Stop) director.Stop();
            else if (Pause) director.Pause();
            else if (Resume) director.Resume();
            else director.Play();
            return Wait ? WaitWhilePlaying() : Async.Completed;

            async Awaitable WaitWhilePlaying ()
            {
                while (ctx.Token.EnsureNotCanceled() && director.state == UnityEngine.Playables.PlayState.Playing)
                    await Async.NextFrame(ctx.Token);
            }
            #else
            throw Fail("Timeline package is not installed.");
            #endif
        }
    }
}

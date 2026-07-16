using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Stops playing an SFX (sound effect) track with the specified name.",
        @"
When sound effect track name (SfxPath) is not specified, will stop all the currently played tracks.",
        @"
; Stop playing an SFX with the name 'Rain', fading-out for 15 seconds.
@stopSfx Rain fade:15",
        @"
; Stops all the currently played sound effect tracks.
@stopSfx"
    )]
    [Serializable, AudioGroup, Icon("MusicNoteSlashDuo")]
    public class StopSfx : AudioCommand
    {
        [Doc("Path to the sound effect to stop.")]
        [Alias(NamelessParameterAlias), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter SfxPath;
        [Doc("Duration of the volume fade-out before stopping playback, in seconds (0.35 by default).")]
        [Alias("fade"), ParameterDefaultValue("0.35")]
        public DecimalParameter FadeOutDuration;
        [Doc("Whether to wait for the SFX fade-out animation to finish before playing next command.")]
        public BooleanParameter Wait;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            return WaitOrForget(Stop, Wait, ctx);
        }

        protected virtual async Awaitable Stop (ExecutionContext ctx)
        {
            var duration = GetAssignedOrDefault(FadeOutDuration, AudioManager.Configuration.DefaultFadeDuration);
            if (Assigned(SfxPath)) await AudioManager.StopSfx(SfxPath, duration, ctx.Token);
            else await AudioManager.StopAllSfx(duration, ctx.Token);
        }
    }
}

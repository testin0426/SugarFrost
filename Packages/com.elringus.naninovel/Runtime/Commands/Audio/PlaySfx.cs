using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Plays or modifies currently played [SFX (sound effect)](/guide/audio#sound-effects) track with the specified name.",
        @"
Sound effect tracks are not looped by default.
When sfx track name (SfxPath) is not specified, will affect all the currently played tracks.
When invoked for a track that is already playing, the playback won't be affected (track won't start playing from the start),
but the specified parameters (volume and whether the track is looped) will be applied.",
        @"
; Plays an SFX with the name 'Explosion' once.
@sfx Explosion",
        @"
; Plays an SFX with the name 'Rain' in a loop and fades-in over 30 seconds.
@sfx Rain loop! fade:30",
        @"
; Changes volume of all the played SFX tracks to 75% over 2.5 seconds
; and disables looping for all of them.
@sfx volume:0.75 !loop time:2.5"
    )]
    [Serializable, Alias("sfx"), AudioGroup, Icon("MusicNote")]
    public class PlaySfx : AudioCommand, Command.IPreloadable
    {
        [Doc("Path to the sound effect asset to play.")]
        [Alias(NamelessParameterAlias), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter SfxPath;
        [Doc("Volume of the sound effect.")]
        [ParameterDefaultValue("1")]
        public DecimalParameter Volume;
        [Doc("Whether to play the sound effect in a loop.")]
        [ParameterDefaultValue("false")]
        public BooleanParameter Loop;
        [Doc("Duration of the volume fade-in when starting playback, in seconds (0.0 by default); doesn't have effect when modifying a playing track.")]
        [Alias("fade"), ParameterDefaultValue("0")]
        public DecimalParameter FadeInDuration;
        [Doc("Audio mixer [group path](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.FindMatchingGroups) that should be used when playing the audio.")]
        [Alias("group")]
        public StringParameter GroupPath;
        [Doc("Duration (in seconds) of the modification.")]
        [Alias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;
        [Doc("Whether to wait for the SFX fade animation to finish before playing next command.")]
        public BooleanParameter Wait;

        public virtual async Awaitable PreloadResources ()
        {
            if (!Assigned(SfxPath) || SfxPath.DynamicValue) return;
            await AudioManager.AudioLoader.Load(SfxPath, this);
        }

        public virtual void ReleaseResources ()
        {
            if (!Assigned(SfxPath) || SfxPath.DynamicValue) return;
            AudioManager?.AudioLoader?.Release(SfxPath, this);
        }

        public override Awaitable Execute (ExecutionContext ctx)
        {
            if (ShouldSkip()) return Async.Completed;
            return WaitOrForget(Play, Wait, ctx);
        }

        protected virtual async Awaitable Play (ExecutionContext ctx)
        {
            var loop = GetAssignedOrDefault(Loop, false);
            var volume = GetAssignedOrDefault(Volume, 1f);
            var duration = GetAssignedOrDefault(Duration, AudioManager.Configuration.DefaultFadeDuration);
            var fadeDuration = GetAssignedOrDefault(FadeInDuration, 0f);
            if (Assigned(SfxPath)) await PlayOrModifyTrack(AudioManager, SfxPath, volume, loop, duration, fadeDuration, GroupPath, ctx.Token);
            else
            {
                using var _ = AudioManager.RentPlayedSfx(out var paths);
                using var __ = Async.Rent(out var tasks);
                foreach (var path in paths)
                    tasks.Add(PlayOrModifyTrack(AudioManager, path, volume, loop, duration, fadeDuration, null, ctx.Token));
                await Async.All(tasks);
            }
        }

        protected virtual bool ShouldSkip ()
        {
            if (AudioManager.Configuration.PlaySfxWhileSkipping) return false;
            if (Assigned(Loop) && Loop) return false;
            return Engine.GetServiceOrErr<IScriptPlayer>().Skipping;
        }

        protected virtual Awaitable PlayOrModifyTrack (IAudioManager manager, string path, float volume,
            bool loop, float time, float fade, string group, AsyncToken token)
        {
            if (manager.IsSfxPlaying(path)) return manager.ModifySfx(path, volume, loop, time, token);
            return manager.PlaySfx(path, volume, fade, loop, group, token);
        }
    }
}

using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(@"
Plays or modifies currently played [BGM (background music)](/guide/audio#background-music) track with the specified name.",
        @"
Music tracks are looped by default.
When music track name (BgmPath) is not specified, will affect all the currently played tracks.
When invoked for a track that is already playing, the playback won't be affected (track won't start playing from the start),
but the specified parameters (volume and whether the track is looped) will be applied.",
        @"
; Starts playing a music track with the name 'Sanctuary' in a loop.
@bgm Sanctuary",
        @"
; Same as above, but fades-in the volume over 10 seconds and plays once.
@bgm Sanctuary fade:10 !loop",
        @"
; Changes volume of all the played music tracks to 50% over 2.5 seconds
; and makes them play in a loop.
@bgm volume:0.5 loop! time:2.5",
        @"
; Plays 'BattleThemeIntro' once, then loops 'BattleThemeMain'.
@bgm BattleThemeMain intro:BattleThemeIntro"
    )]
    [Serializable, Alias("bgm"), AudioGroup, Icon("Music")]
    public class PlayBgm : AudioCommand, Command.IPreloadable
    {
        [Doc("Path to the music track to play.")]
        [Alias(NamelessParameterAlias), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter BgmPath;
        [Doc("Path to the intro music track to play once before the main track (not affected by the loop parameter).")]
        [Alias("intro"), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter IntroBgmPath;
        [Doc("Volume of the music track.")]
        [ParameterDefaultValue("1")]
        public DecimalParameter Volume;
        [Doc("Whether to play the track from beginning when it finishes.")]
        [ParameterDefaultValue("true")]
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
        [Doc("Whether to wait for the BGM fade animation to finish before playing next command.")]
        public BooleanParameter Wait;

        public virtual async Awaitable PreloadResources ()
        {
            if (!Assigned(BgmPath) || BgmPath.DynamicValue) return;
            await AudioManager.AudioLoader.Load(BgmPath, this);

            if (!Assigned(IntroBgmPath) || IntroBgmPath.DynamicValue) return;
            await AudioManager.AudioLoader.Load(IntroBgmPath, this);
        }

        public virtual void ReleaseResources ()
        {
            if (!Assigned(BgmPath) || BgmPath.DynamicValue) return;
            AudioManager?.AudioLoader?.Release(BgmPath, this);

            if (!Assigned(IntroBgmPath) || IntroBgmPath.DynamicValue) return;
            AudioManager?.AudioLoader?.Release(IntroBgmPath, this);
        }

        public override Awaitable Execute (ExecutionContext ctx)
        {
            return WaitOrForget(Play, Wait, ctx);
        }

        protected virtual async Awaitable Play (ExecutionContext ctx)
        {
            var loop = GetAssignedOrDefault(Loop, true);
            var volume = GetAssignedOrDefault(Volume, 1f);
            var duration = GetAssignedOrDefault(Duration, AudioManager.Configuration.DefaultFadeDuration);
            var fadeDuration = GetAssignedOrDefault(FadeInDuration, 0f);
            if (Assigned(BgmPath)) await PlayOrModifyTrack(AudioManager, BgmPath, volume, loop, duration, fadeDuration, IntroBgmPath, GroupPath, ctx.Token);
            else
            {
                using var _ = AudioManager.RentPlayedBgm(out var paths);
                using var __ = Async.Rent(out var tasks);
                foreach (var path in paths)
                    tasks.Add(PlayOrModifyTrack(AudioManager, path, volume, loop, duration, fadeDuration, IntroBgmPath, null, ctx.Token));
                await Async.All(tasks);
            }
        }

        protected virtual Awaitable PlayOrModifyTrack (IAudioManager manager, string path, float volume, bool loop, float time, float fade, string introPath, string group, AsyncToken token)
        {
            if (manager.IsBgmPlaying(path)) return manager.ModifyBgm(path, volume, loop, time, token);
            return manager.PlayBgm(path, volume, fade, loop, introPath, group, token);
        }
    }
}

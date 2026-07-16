using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Plays an [SFX (sound effect)](/guide/audio#sound-effects) track with the specified name.
Unlike [@sfx] command, the clip is played with minimum delay and is not serialized with the game state (won't be played after loading a game, even if it was played when saved).
The command can be used to play various transient audio clips, such as UI-related sounds (eg, on button click with [`Play Script` component](/guide/gui#play-script-on-unity-event)).",
        null,
        @"
; Plays an SFX with the name 'Click' once.
@sfxFast Click",
        @"
; Same as above, but allow concurrent playbacks of the same clip.
@sfxFast Click !restart"
    )]
    [Serializable, Alias("sfxFast"), AudioGroup]
    public class PlaySfxFast : AudioCommand, Command.IPreloadable
    {
        [Doc("Path to the sound effect asset to play.")]
        [Alias(NamelessParameterAlias), ResourceContext(AudioConfiguration.DefaultAudioPathPrefix)]
        public StringParameter SfxPath;
        [Doc("Volume of the sound effect.")]
        [ParameterDefaultValue("1")]
        public DecimalParameter Volume;
        [Doc("Whether to start playing the audio from start in case it's already playing.")]
        [ParameterDefaultValue("true")]
        public BooleanParameter Restart;
        [Doc("Whether to allow playing multiple instances of the same clip; has no effect when `restart` is enabled.")]
        [ParameterDefaultValue("true")]
        public BooleanParameter Additive;
        [Doc("Audio mixer [group path](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.FindMatchingGroups) that should be used when playing the audio.")]
        [Alias("group")]
        public StringParameter GroupPath;
        [Doc(SharedDocs.WaitParameter)]
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

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var path = SfxPath.Value;
            var wait = Assigned(Wait) && Wait.Value;
            var volume = GetAssignedOrDefault(Volume, 1f);
            var restart = GetAssignedOrDefault(Restart, true);
            var additive = GetAssignedOrDefault(Additive, true);
            await AudioManager.PlaySfxFast(path, volume, GroupPath, restart, additive);
            while (wait && AudioManager.IsSfxPlaying(path))
                await Async.NextFrame();
        }
    }
}

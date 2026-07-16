using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Plays a voice clip at the specified path.",
        null,
        @"
; Given a 'Rawr' voice resource is available, play it.
@voice Rawr"
    )]
    [Serializable, Alias("voice"), AudioGroup, Icon("Microphone")]
    public class PlayVoice : AudioCommand, Command.IPreloadable
    {
        [Doc("Path to the voice clip to play.")]
        [Alias(NamelessParameterAlias), RequiredParameter, ResourceContext(AudioConfiguration.DefaultVoicePathPrefix)]
        public StringParameter VoicePath;
        [Doc("Volume of the playback.")]
        [ParameterDefaultValue("1")]
        public DecimalParameter Volume;
        [Doc("Audio mixer [group path](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.FindMatchingGroups) that should be used when playing the audio.")]
        [Alias("group")]
        public StringParameter GroupPath;
        [Doc("ID of the character actor this voice belongs to. When specified and [per-author volume](/guide/voicing#author-volume) is used, volume will be adjusted accordingly.")]
        public StringParameter AuthorId;

        public virtual async Awaitable PreloadResources ()
        {
            if (!Assigned(VoicePath) || VoicePath.DynamicValue) return;
            await AudioManager.VoiceLoader.Load(VoicePath, this);
        }

        public virtual void ReleaseResources ()
        {
            if (!Assigned(VoicePath) || VoicePath.DynamicValue) return;
            AudioManager?.VoiceLoader?.Release(VoicePath, this);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var volume = GetAssignedOrDefault(Volume, 1f);
            await AudioManager.PlayVoice(VoicePath, volume, GroupPath, AuthorId);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IAudioManager"/>.
    /// </summary>
    public static class AudioManagerExtensions
    {
        /// <summary>
        /// Rents a pooled hash set and collects all the played BGM local resource paths.
        /// </summary>
        public static IDisposable RentPlayedBgm (this IAudioManager manager, out HashSet<string> paths)
        {
            var rent = SetPool<string>.Rent(out paths);
            manager.CollectPlayedBgm(paths);
            return rent;
        }

        /// <summary>
        /// Rents a pooled hash set and collects all the played SFX local resource paths.
        /// </summary>
        public static IDisposable RentPlayedSfx (this IAudioManager manager, out HashSet<string> paths)
        {
            var rent = SetPool<string>.Rent(out paths);
            manager.CollectPlayedSfx(paths);
            return rent;
        }

        /// <summary>
        /// Plays voice clips with the specified resource paths in sequence.
        /// </summary>
        /// <param name="pathList">Names (local paths) of the voice resources.</param>
        /// <param name="volume">Volume of the voice playback.</param>
        /// <param name="group">Path of an <see cref="AudioMixerGroup"/> of the current <see cref="AudioMixer"/> to use when playing the voice.</param>
        /// <param name="authorId">ID of the author (character actor) of the played voices.</param>
        public static async Awaitable PlayVoiceSequence (this IAudioManager manager, IReadOnlyCollection<string> pathList,
            float volume = 1f, string group = default, string authorId = default, AsyncToken token = default)
        {
            foreach (var path in pathList)
            {
                await manager.PlayVoice(path, volume, group, authorId, token);
                await Async.While(() => manager.IsVoicePlaying(path) && token.EnsureNotCanceled());
            }
        }
    }
}

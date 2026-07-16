using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    /// <summary>
    /// Manages the audio: SFX, BGM and voice.
    /// </summary>
    public interface IAudioManager : IEngineService<AudioConfiguration>
    {
        /// <summary>
        /// Occurs when a background music clip with the specified resource path starts playing.
        /// </summary>
        event Action<string> OnPlayBgm;
        /// <summary>
        /// Occurs when a background music clip with the specified resource path stops playing.
        /// </summary>
        event Action<string> OnStopBgm;
        /// <summary>
        /// Occurs when a sound effect clip with the specified resource path starts playing.
        /// </summary>
        event Action<string> OnPlaySfx;
        /// <summary>
        /// Occurs when a sound effect clip with the specified resource path stops playing.
        /// </summary>
        event Action<string> OnStopSfx;
        /// <summary>
        /// Occurs when a voice clip with the specified resource path starts playing.
        /// </summary>
        event Action<string> OnPlayVoice;
        /// <summary>
        /// Occurs when a voice clip with the specified resource path stops playing.
        /// </summary>
        event Action<string> OnStopVoice;

        /// <summary>
        /// Current volume (in 0.0 to 1.0 range) of a master channel.
        /// </summary>
        float MasterVolume { get; set; }
        /// <summary>
        /// Current volume (in 0.0 to 1.0 range) of a BGM channel.
        /// </summary>
        float BgmVolume { get; set; }
        /// <summary>
        /// Current volume (in 0.0 to 1.0 range) of an SFX channel.
        /// </summary>
        float SfxVolume { get; set; }
        /// <summary>
        /// Current volume (in 0.0 to 1.0 range) of a voice channel.
        /// </summary>
        float VoiceVolume { get; set; }
        /// <summary>
        /// Currently selected voice resources localization tag.
        /// </summary>
        string VoiceLocale { get; set; }
        /// <summary>
        /// Used by the service to load audio (BGM and SFX) resources.
        /// </summary>
        IResourceLoader AudioLoader { get; }
        /// <summary>
        /// Used by the service to load voice resources.
        /// </summary>
        IResourceLoader VoiceLoader { get; }

        /// <summary>
        /// Modifies properties of a BGM track with the specified resource path.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="volume">Volume to set for the modified audio.</param>
        /// <param name="loop">Whether the audio should loop.</param>
        /// <param name="time">Animation (fade) time of the modification.</param>
        Awaitable ModifyBgm (string path, float volume, bool loop, float time, AsyncToken token = default);
        /// <summary>
        /// Modifies properties of an SFX track with the specified resource path.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="volume">Volume to set for the modified audio.</param>
        /// <param name="loop">Whether the audio should loop.</param>
        /// <param name="time">Animation (fade) time of the modification.</param>
        Awaitable ModifySfx (string path, float volume, bool loop, float time, AsyncToken token = default);
        /// <summary>
        /// Plays an SFX track with the specified resource path; won't commit to the playback state.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="volume">Volume of the audio playback.</param>
        /// <param name="group">The audio mixer group path to use when playing the audio.</param>
        /// <param name="restart">Whether to start playing the audio from start in case it's already playing.</param>
        /// <param name="additive">Whether to allow playing multiple instances of the same clip; has no effect when restart is enabled.</param>
        Awaitable PlaySfxFast (string path, float volume = 1f, string group = default, bool restart = true, bool additive = true);
        /// <summary>
        /// Starts playing a BGM track with the specified path.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="volume">Volume of the audio playback.</param>
        /// <param name="fadeTime">Animation (fade-in) time to reach the target volume.</param>
        /// <param name="loop">Whether to loop the playback.</param>
        /// <param name="introPath">Local resource path to an audio resource to play before the main audio; can be used as an intro before looping the main audio clip.</param>
        /// <param name="group">The audio mixer group path to use when playing the audio.</param>
        Awaitable PlayBgm (string path, float volume = 1f, float fadeTime = 0f, bool loop = true, string introPath = null, string group = default, AsyncToken token = default);
        /// <summary>
        /// Stops playing a BGM track with the specified path.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="fadeTime">Animation (fade-out) time to reach zero volume before stopping the playback.</param>
        Awaitable StopBgm (string path, float fadeTime = 0f, AsyncToken token = default);
        /// <summary>
        /// Stops playback of all the BGM tracks.
        /// </summary>
        /// <param name="fadeTime">Animation (fade-out) time to reach zero volume before stopping the playback.</param>
        Awaitable StopAllBgm (float fadeTime = 0f, AsyncToken token = default);
        /// <summary>
        /// Starts playing an SFX track with the specified path.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="volume">Volume of the audio playback.</param>
        /// <param name="fadeTime">Animation (fade-in) time to reach the target volume.</param>
        /// <param name="loop">Whether to loop the playback.</param>
        /// <param name="group">The audio mixer group path to use when playing the audio.</param>
        Awaitable PlaySfx (string path, float volume = 1f, float fadeTime = 0f, bool loop = false, string group = default, AsyncToken token = default);
        /// <summary>
        /// Stops playing an SFX track with the specified path.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        /// <param name="fadeTime">Animation (fade-out) time to reach zero volume before stopping the playback.</param>
        Awaitable StopSfx (string path, float fadeTime = 0f, AsyncToken token = default);
        /// <summary>
        /// Stops playback of all the SFX tracks.
        /// </summary>
        /// <param name="fadeTime">Animation (fade-out) time to reach zero volume before stopping the playback.</param>
        Awaitable StopAllSfx (float fadeTime = 0f, AsyncToken token = default);
        /// <summary>
        /// Starts playing an SFX track with the specified path.
        /// </summary>
        /// <param name="path">Local resource path of the voice resource.</param>
        /// <param name="volume">Volume of the voice playback.</param>
        /// <param name="group">The audio mixer group path to use when playing the voice.</param>
        /// <param name="authorId">ID of the author (character actor) of the played voice.</param>
        Awaitable PlayVoice (string path, float volume = 1f, string group = default, string authorId = default, AsyncToken token = default);
        /// <summary>
        /// Stops currently played voice track (if any).
        /// </summary>
        void StopVoice ();
        /// <summary>
        /// Checks whether BGM with specified path is currently playing.
        /// </summary>
        /// <param name="path">Local resource path of the BGM resource.</param>
        bool IsBgmPlaying (string path);
        /// <summary>
        /// Checks whether SFX with specified path is currently playing.
        /// </summary>
        /// <param name="path">Local resource path of the SFX resource.</param>
        bool IsSfxPlaying (string path);
        /// <summary>
        /// Checks whether voice with specified path is currently playing.
        /// </summary>
        /// <param name="path">Local resource path of the voice resource.</param>
        bool IsVoicePlaying (string path);
        /// <summary>
        /// Collects currently played BGM local resource paths to the specified collection.
        /// </summary>
        void CollectPlayedBgm (ICollection<string> paths);
        /// <summary>
        /// Collects currently played SFX local resource paths to the specified collection.
        /// </summary>
        void CollectPlayedSfx (ICollection<string> paths);
        /// <summary>
        /// Returns currently played voice local resource path or null if not playing any.
        /// </summary>
        [CanBeNull]
        string GetPlayedVoice ();
        /// <summary>
        /// Returns <see cref="IAudioTrack"/> associated with a playing audio (SFX or BGM) resource 
        /// with the specified path; returns null if not found or the audio is not currently playing.
        /// </summary>
        /// <param name="path">Local resource path of the audio resource.</param>
        [CanBeNull]
        IAudioTrack GetAudioTrack (string path);
        /// <summary>
        /// Returns <see cref="IAudioTrack"/> associated with a playing voice resource 
        /// with the specified path; returns null if not found or the voice is not currently playing.
        /// </summary>
        /// <param name="path">Local resource path of the voice resource.</param>
        [CanBeNull]
        IAudioTrack GetVoiceTrack (string path);
        /// <summary>
        /// Returns current voice volume (in 0.0 to 1.0 range) of a printed message author (character actor) with the specified ID.
        /// When volume for an author with the specified ID not specified, returns -1.
        /// </summary>
        float GetAuthorVolume (string authorId);
        /// <summary>
        /// Sets current voice volume (in 0.0 to 1.0 range) of a printed message author (character actor) with the specified ID.
        /// </summary>
        void SetAuthorVolume (string authorId, float volume);
        /// <summary>
        /// Returns an audio mixer group with the specified path or null when the group is not available.
        /// </summary>
        [CanBeNull] AudioMixerGroup GetGroup (string groupPath);
    }
}

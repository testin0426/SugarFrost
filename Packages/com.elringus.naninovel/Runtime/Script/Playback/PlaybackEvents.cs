using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="IScriptPlayer"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Playback Events")]
    public class PlaybackEvents : UnityEvents
    {
        [Tooltip("Occurs when availability of the script player engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when playback of a script with the path is started.")]
        public StringUnityEvent StartedPlaying;
        [Tooltip("Occurs when playback of a script with the path is stopped.")]
        public StringUnityEvent StoppedPlaying;
        [Tooltip("Occurs when wait input mode is enabled or disabled.")]
        public BoolUnityEvent AwaitingInput;
        [Tooltip("Occurs when auto play mode is enabled or disabled.")]
        public BoolUnityEvent AutoPlaying;
        [Tooltip("Occurs when skip mode is enabled or disabled.")]
        public BoolUnityEvent Skipping;

        public void StartPlayback (string scriptPath)
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
                player.MainTrack.LoadAndPlay(scriptPath).Forget();
        }

        public void StopPlayback ()
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
                player.MainTrack.Stop();
        }

        public void ResumePlayback ()
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
                player.MainTrack.Resume();
        }

        public void SetAwaitInput (bool enabled)
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
                player.MainTrack.SetAwaitInput(enabled);
        }

        public void SetSkip (bool enabled)
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
                player.SetSkip(enabled);
        }

        public void SetAutoPlay (bool enabled)
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
                player.SetAutoPlay(enabled);
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IScriptPlayer>(out var player))
            {
                ServiceAvailable?.Invoke(true);

                player.OnPlay -= HandlePlay;
                player.OnPlay += HandlePlay;

                player.OnStop -= HandleStop;
                player.OnStop += HandleStop;

                player.OnAwaitInput -= HandleAwaitInput;
                player.OnAwaitInput += HandleAwaitInput;

                player.OnAutoPlay -= AutoPlaying.SafeInvoke;
                player.OnAutoPlay += AutoPlaying.SafeInvoke;

                player.OnSkip -= Skipping.SafeInvoke;
                player.OnSkip += Skipping.SafeInvoke;
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }

        protected virtual void HandlePlay (IScriptTrack track) => StartedPlaying?.Invoke(track.PlayedScript.Path);
        protected virtual void HandleStop (IScriptTrack track) => StoppedPlaying?.Invoke(track.PlayedScript.Path);
        protected virtual void HandleAwaitInput (IScriptTrack track) => AwaitingInput?.Invoke(track.AwaitingInput);
    }
}

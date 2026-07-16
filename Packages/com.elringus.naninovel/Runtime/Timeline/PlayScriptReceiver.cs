#if TIMELINE_AVAILABLE

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;

namespace Naninovel
{
    [AddComponentMenu("Naninovel/ Timeline/Play Script Receiver")]
    public class PlayScriptReceiver : MonoBehaviour, INotificationReceiver
    {
        [field: SerializeField, Tooltip("The director that will play the timeline containing the PlayScriptMarker events.")]
        [CanBeNull] public PlayableDirector Director { get; private set; }

        public virtual void OnNotify (Playable origin, INotification notification, object context)
        {
            if (notification is PlayScriptMarker marker)
                HandleNotification(marker).Forget();
        }

        protected virtual void Awake ()
        {
            if (!Director && TryGetComponent<PlayableDirector>(out var director))
                Director = director;
        }

        protected virtual async Awaitable HandleNotification (PlayScriptMarker marker)
        {
            if (marker.PauseDirector)
                if (Director) Director.Pause();
                else throw Engine.Fail("Failed to pause the timeline: 'Director' is not assigned.");

            if (marker.EnterDialogue) await Dialogue.Enter();

            await PlayScript(marker);

            if (marker.PauseDirector)
            {
                while (Dialogue.Active) await Async.NextFrame();
                Director?.Play();
            }
        }

        protected virtual async Awaitable PlayScript (PlayScriptMarker marker)
        {
            var player = Engine.GetServiceOrErr<IScriptPlayer>();

            if (!string.IsNullOrWhiteSpace(marker.ScriptText))
                await player.PlayTransient(marker.ScriptText, $"PlayScriptMarker-{name}", CreateOptions(marker));

            if (!string.IsNullOrEmpty(marker.GotoScript))
            {
                if (ScriptAssets.GetPath(marker.GotoScript) is not { } path)
                    throw Engine.Fail("Failed to navigate to a script asset with 'Play Script' timeline marker: " +
                                      $"script asset with '{marker.GotoScript}' GUID not found.");
                if (string.IsNullOrWhiteSpace(marker.GotoLabel)) await player.MainTrack.LoadAndPlay(path);
                else await player.MainTrack.LoadAndPlayAtLabel(path, marker.GotoLabel);
            }
        }

        protected virtual PlaybackOptions CreateOptions (PlayScriptMarker marker) => new() {
            CompleteOnContinue = marker.CompleteOnContinue,
            DisableAwaitInput = marker.DisableAwaitInput,
            DisableAutoPlay = marker.DisableAutoPlay,
            DisableSkip = marker.DisableSkip
        };
    }
}

#endif

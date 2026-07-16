using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Notifies when appearance with the specified name is applied to an actor with the specified ID.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Appearance Events")]
    public class AppearanceEvents : UnityEvents
    {
        [Tooltip("The identifier of the actor.")]
        public string ActorId;
        [Tooltip("The appearance name.")]
        public string Appearance;

        [Space]
        [Tooltip("Occurs when appearance with the specified name is applied to an actor with the specified ID.")]
        public UnityEvent AppearanceEntered;
        [Tooltip("Occurs when appearance with the specified name is no longer applied to an actor with the specified ID.")]
        public UnityEvent AppearanceExited;

        private string previousAppearance;

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<ICharacterManager>(out var chars))
            {
                chars.OnActorAdded -= HandleActorAdded;
                chars.OnActorAdded += HandleActorAdded;
            }
            if (Engine.TryGetService<IBackgroundManager>(out var backs))
            {
                backs.OnActorAdded -= HandleActorAdded;
                backs.OnActorAdded += HandleActorAdded;
            }
        }

        protected override void HandleEngineDestroyed () { }

        protected virtual void HandleActorAdded (string id)
        {
            if (id == ActorId)
            {
                var actor = Engine.GetService<ICharacterManager>()?.GetActor(id) as IActor ??
                            Engine.GetServiceOrErr<IBackgroundManager>().GetActorOrErr(id);
                actor.OnAppearanceChanged += HandleAppearanceChanged;
            }
        }

        protected virtual void HandleAppearanceChanged (string appearance)
        {
            if (previousAppearance == appearance) return;
            if (appearance == Appearance) AppearanceEntered?.Invoke();
            else if (previousAppearance == Appearance) AppearanceExited?.Invoke();
            previousAppearance = appearance;
        }
    }
}

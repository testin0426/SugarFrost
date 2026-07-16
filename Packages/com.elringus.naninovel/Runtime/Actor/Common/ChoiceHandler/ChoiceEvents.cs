using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="IChoiceHandlerManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Choice Events")]
    public class ChoiceEvents : UnityEvents
    {
        [ActorPopup(ChoiceHandlersConfiguration.DefaultPathPrefix), CanBeNull]
        [Tooltip("The identifier of the choice handler actor for the events and actions. Leave empty for default.")]
        public string ChoiceHandlerId;

        [Space]
        [Tooltip("Occurs when availability of the choice handler manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when visibility of a choice handler with the specified ID changes.")]
        public BoolUnityEvent HandlerVisibilityChanged;
        [Tooltip("Occurs when a choice with the summary is added.")]
        public StringUnityEvent ChoiceAdded;
        [Tooltip("Occurs when a choice with the summary is handled (selected).")]
        public StringUnityEvent ChoiceHandled;
        [Tooltip("Occurs when a choice with the summary is removed.")]
        public StringUnityEvent ChoiceRemoved;

        public async void AddChoice (string summary)
        {
            if (!Engine.TryGetService<IChoiceHandlerManager>(out var manager)) return;
            var handler = await manager.GetOrAddActor(GetActorIdOrDefault());
            handler.AddChoice(new(new DirectiveChoiceCallback(), new() { Summary = summary }));
        }

        public void HandleChoice (string summary)
        {
            if (!Engine.TryGetService<IChoiceHandlerManager>(out var manager)) return;
            using var _ = manager.RentActors(out var handlers);
            foreach (var handler in handlers)
                if (handler.FindChoice(c => c.Summary == summary) is { } choice)
                {
                    handler.HandleChoice(choice.Id);
                    break;
                }
        }

        public void RemoveChoice (string summary)
        {
            if (!Engine.TryGetService<IChoiceHandlerManager>(out var manager)) return;
            using var _ = manager.RentActors(out var handlers);
            foreach (var handler in handlers)
                if (handler.FindChoice(c => c.Summary == summary) is { } choice)
                {
                    handler.RemoveChoice(choice.Id);
                    break;
                }
        }

        public void RemoveAllChoices ()
        {
            if (!Engine.TryGetService<IChoiceHandlerManager>(out var manager)) return;
            using var _ = manager.RentActors(out var handlers);
            foreach (var handler in handlers)
            {
                using var __ = handler.RentChoices(out var choices);
                foreach (var choice in choices)
                    handler.RemoveChoice(choice.Id);
            }
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<IChoiceHandlerManager>(out var manager))
            {
                ServiceAvailable?.Invoke(true);
                manager.OnActorAdded -= HandleActorAdded;
                manager.OnActorAdded += HandleActorAdded;
                if (manager.ActorExists(GetActorIdOrDefault()))
                    HandleActorAdded(GetActorIdOrDefault());
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
        }

        protected virtual void HandleActorAdded (string id)
        {
            if (id != GetActorIdOrDefault()) return;
            var handler = Engine.GetServiceOrErr<IChoiceHandlerManager>().GetActorOrErr(id);
            handler.OnVisibilityChanged += HandlerVisibilityChanged.SafeInvoke;
            handler.OnChoiceAdded += c => ChoiceAdded?.Invoke(c.Summary);
            handler.OnChoiceRemoved += c => ChoiceRemoved?.Invoke(c.Summary);
            handler.OnChoiceHandled += c => ChoiceHandled?.Invoke(c.Summary);
        }

        private string GetActorIdOrDefault ()
        {
            if (!string.IsNullOrEmpty(ChoiceHandlerId)) return ChoiceHandlerId;
            return Engine.GetServiceOrErr<IChoiceHandlerManager>().DefaultHandlerId;
        }
    }
}

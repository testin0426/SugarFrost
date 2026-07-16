using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IChoiceHandlerActor"/> implementation using <see cref="UI.ChoiceHandlerPanel"/> to represent the actor.
    /// </summary>
    [ActorResources(typeof(ChoiceHandlerPanel), false)]
    public class UIChoiceHandler : MonoBehaviourActor<ChoiceHandlerMetadata>, IChoiceHandlerActor
    {
        public event Action<Choice> OnChoiceAdded;
        public event Action<Choice> OnChoiceRemoved;
        public event Action<Choice> OnChoiceHandled;

        public override GameObject GameObject => HandlerPanel.gameObject;
        public override bool Visible { get => HandlerPanel.Visible; set => base.Visible = HandlerPanel.Visible = value; }

        protected virtual ChoiceHandlerPanel HandlerPanel { get; private set; }
        protected virtual List<Choice> Choices { get; } = new();
        protected virtual IChoiceHandlerManager Handlers { get; }
        protected virtual IScriptPlayer Player { get; }
        protected virtual IStateManager State { get; }
        protected virtual IUIManager UIs { get; }

        public UIChoiceHandler (string id, ChoiceHandlerMetadata meta)
            : base(id, meta)
        {
            Handlers = Engine.GetServiceOrErr<IChoiceHandlerManager>();
            Player = Engine.GetServiceOrErr<IScriptPlayer>();
            State = Engine.GetServiceOrErr<IStateManager>();
            UIs = Engine.GetServiceOrErr<IUIManager>();
        }

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            var prefab = await LoadUIPrefab();
            HandlerPanel = await UIs.AddUI(prefab, group: BuildActorGroup()) as ChoiceHandlerPanel;
            if (!HandlerPanel) throw new Error($"Failed to initialize '{Id}' choice handler actor: choice panel UI instantiation failed.");
            HandlerPanel.OnChoice += HandleChoice;
            Visible = false;
        }

        public override Awaitable ChangeAppearance (string appearance, Tween tween,
            Transition? transition = default, AsyncToken token = default)
        {
            base.Appearance = appearance;
            return Async.Completed;
        }

        public override async Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default)
        {
            base.Visible = visible;
            if (HandlerPanel) await HandlerPanel.ChangeVisibility(visible, tween.Duration);
        }

        public virtual void AddChoice (Choice choice)
        {
            choice.Summary.Hold(this);
            Choices.Add(choice);
            HandlerPanel.AddChoiceButton(choice);
            OnChoiceAdded?.Invoke(choice);
        }

        public virtual void RemoveChoice (string id)
        {
            for (var i = Choices.Count - 1; i >= 0; i--)
            {
                var choice = Choices[i];
                if (choice.Id != id) continue;
                Choices.RemoveAt(i);
                choice.Summary.Release(this);
                OnChoiceRemoved?.Invoke(choice);
            }
            HandlerPanel.RemoveChoiceButton(id);
        }

        public virtual void HandleChoice (string id)
        {
            foreach (var choice in Choices)
                if (choice.Id == id)
                {
                    HandleChoice(choice);
                    return;
                }
            throw new Error($"Failed to handle choice with ID '{id}': choice not found.");
        }

        public virtual void CollectChoices (IList<Choice> choices)
        {
            foreach (var choice in Choices)
                choices.Add(choice);
        }

        public virtual Choice? FindChoice (Predicate<Choice> filter)
        {
            foreach (var choice in Choices)
                if (filter(choice))
                    return choice;
            return null;
        }

        public override void Dispose ()
        {
            base.Dispose();

            if (HandlerPanel)
            {
                UIs.RemoveUI(HandlerPanel);
                ObjectUtils.DestroyOrImmediate(HandlerPanel.gameObject);
                HandlerPanel = null;
            }
        }

        protected virtual async Awaitable<GameObject> LoadUIPrefab ()
        {
            var resources = Engine.GetServiceOrErr<IResourceProviderManager>();
            var l10n = Engine.GetServiceOrErr<ILocalizationManager>();
            return await ActorMeta.Loader.CreateLocalizableFor<GameObject>(resources, l10n).LoadOrErr(Id);
        }

        protected override GameObject CreateHostObject () => null;

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        protected virtual void HandleChoice (Choice choice)
        {
            if (!Choices.Exists(c => c.Id.EqualsOrdinal(choice.Id))) return;

            State.PeekRollbackStack()?.AllowPlayerRollback();
            AddChoiceToBacklog(choice);
            ClearChoices();

            if (HandlerPanel)
            {
                HandlerPanel.RemoveAllChoiceButtonsDelayed(); // Delayed to allow custom onClick logic.
                var hideTask = HandlerPanel.ChangeVisibility(false);
                if (ActorMeta.WaitHideOnChoice)
                {
                    hideTask.Then(() => ExecuteCallback(choice)).Forget();
                    return;
                }
                hideTask.Forget();
            }

            ExecuteCallback(choice).Forget();
            OnChoiceHandled?.Invoke(choice);
        }

        protected virtual async Awaitable ExecuteCallback (Choice choice)
        {
            switch (choice.CallbackType)
            {
                case ChoiceCallbackType.Directive: await ExecuteDirectiveCallback(choice.DirectiveCallback); break;
                case ChoiceCallbackType.Transient: await ExecuteTransientCallback(choice.TransientCallback); break;
                case ChoiceCallbackType.Nested: ExecuteNestedCallback(choice.NestedCallback); break;
            }
        }

        protected virtual async Awaitable ExecuteDirectiveCallback (DirectiveChoiceCallback clb)
        {
            var track = string.IsNullOrEmpty(clb.TrackId) ? Player.MainTrack : Player.GetTrackOrErr(clb.TrackId);
            if (!string.IsNullOrEmpty(clb.Set))
                await new SetCustomVariable { Expression = clb.Set, PlaybackSpot = track.PlaybackSpot }.Execute(new(track));
            if (!string.IsNullOrEmpty(clb.Goto))
                await new Goto { Path = clb.Goto, PlaybackSpot = track.PlaybackSpot }.Execute(new(track));
            else if (!string.IsNullOrEmpty(clb.Gosub))
                await new Gosub { Path = clb.Gosub, PlaybackSpot = track.PlaybackSpot }.Execute(new(track));
            else if (track.Playlist != null && !track.Playing)
                track.Resume(track.Playlist.MoveAt(track.PlayedIndex));
        }

        protected virtual async Awaitable ExecuteTransientCallback (TransientChoiceCallback clb)
        {
            await Player.PlayTransient(clb.Scenario, "ChoiceCallback", clb.Playback);
            if (!string.IsNullOrEmpty(clb.ResumeTrackId) && Player.GetTrack(clb.ResumeTrackId) is { Playing: false } track)
                track.Resume(track.Playlist.MoveAt(track.PlayedIndex));
        }

        protected virtual void ExecuteNestedCallback (NestedChoiceCallback clb)
        {
            var track = Player.GetTrackOrErr(clb.TrackId);

            // Record the spot where the playback should resume after the nested callback is executed.
            var continueAt = PlaybackSpot.Invalid;
            if (track!.Playing) continueAt = track.PlaybackSpot;
            else
            {
                var nextIdx = track.Playlist.MoveAt(track.PlayedIndex);
                if (track.Playlist.IsIndexValid(nextIdx))
                    continueAt = track.Playlist[nextIdx].PlaybackSpot;
                // Don't throw when the next index is invalid, as we may have @goto inside the nested callback.
            }
            Handlers.PushSelectedChoice(clb.HostedAt, continueAt);

            // Execute the nested callback.
            if (clb.HostedAt.ScriptPath != track.PlayedScript.Path)
                throw Engine.Fail("Nested choice callback from another script is not supported.", track.PlaybackSpot);
            var index = track.Playlist.IndexOf(clb.HostedAt) + 1;
            if (!track.Playlist.IsIndexValid(index))
                throw Engine.Fail("Failed navigating to choice callback: playlist index is invalid.", track.PlaybackSpot);
            track.Resume(index);
        }

        protected virtual void AddChoiceToBacklog (Choice state)
        {
            var backlog = UIs.GetUI<IBacklogUI>();
            if (backlog == null) return;
            var choices = Choices.Select(c => new BacklogChoice(c.Summary, c.Id == state.Id)).ToArray();
            backlog.AddChoice(choices);
        }

        protected virtual void ClearChoices ()
        {
            foreach (var choice in Choices)
                choice.Summary.Release(this);
            Choices.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class BacklogPanel : CustomUI, IBacklogUI, ILocalizableUI
    {
        [Serializable]
        public new class GameState
        {
            public List<BacklogMessageUI.State> Messages;
        }

        protected virtual BacklogMessageUI LastMessage => Messages.Last?.Value;
        protected virtual RectTransform MessagesContainer => messagesContainer;
        protected virtual ScrollRect ScrollRect => scrollRect;
        protected virtual BacklogMessageUI MessagePrefab => messagePrefab;
        protected virtual int Capacity => Mathf.Max(1, capacity);
        protected virtual int SaveCapacity => Mathf.Max(0, saveCapacity);
        protected virtual bool AddChoices => addChoices;
        protected virtual bool AllowReplayVoice => allowReplayVoice;
        protected virtual bool AllowRollback => allowRollback;
        protected virtual string ChoiceSeparator => choiceSeparator;

        protected const string ChoiceTemplateLiteral = "%SUMMARY%";
        protected virtual LinkedList<BacklogMessageUI> Messages { get; } = new();
        protected virtual Stack<BacklogMessageUI> MessagesPool { get; } = new();
        protected virtual List<LocalizableText> FormatPool { get; } = new();
        protected virtual IStateManager State { get; private set; }

        [SerializeField] private RectTransform messagesContainer;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private BacklogMessageUI messagePrefab;
        [Tooltip("How many messages should the backlog keep.")]
        [SerializeField] private int capacity = 300;
        [Tooltip("How many messages should the backlog keep when saving the game.")]
        [SerializeField] private int saveCapacity = 30;
        [Tooltip("Whether to add choices summary to the log.")]
        [SerializeField] private bool addChoices = true;
        [Tooltip("Template to use for selected choice summary. " + ChoiceTemplateLiteral + " will be replaced with the actual choice summary.")]
        [SerializeField] private string selectedChoiceTemplate = $"    <b>{ChoiceTemplateLiteral}</b>";
        [Tooltip("Template to use for other (not selected) choice summary. " + ChoiceTemplateLiteral + " will be replaced with the actual choice summary.")]
        [SerializeField] private string otherChoiceTemplate = $"    <color=#ffffff88>{ChoiceTemplateLiteral}</color>";
        [Tooltip("String added between consecutive choices.")]
        [SerializeField] private string choiceSeparator = "<br>";
        [Tooltip("Whether to allow replaying voices associated with the backlogged messages.")]
        [SerializeField] private bool allowReplayVoice = true;
        [Tooltip("Whether to allow rolling back to playback spots associated with the backlogged messages.")]
        [SerializeField] private bool allowRollback = true;

        public override Awaitable Initialize ()
        {
            BindInput(Inputs.ShowBacklog, Show, new() { WhenHidden = true });
            BindInput(Inputs.Cancel, Hide, new() { OnEnd = true });
            return Async.Completed;
        }

        public virtual void AddMessage (BacklogMessage message)
        {
            var voices = AllowReplayVoice && !string.IsNullOrEmpty(message.Voice) ? new[] { message.Voice } : null;
            SpawnMessage(new() {
                Text = message.Text,
                AuthorId = message.AuthorId,
                AuthorLabel = message.AuthorLabel ?? LocalizableText.Empty,
                Spot = ProcessRollbackSpot(message.Spot),
                Voices = voices
            });
        }

        public virtual void AppendMessage (LocalizableText text, string voicePath = null)
        {
            if (!LastMessage) return;
            LastMessage.Append(text, AllowReplayVoice ? voicePath : null);
        }

        public virtual void AddChoice (IReadOnlyList<BacklogChoice> choices)
        {
            if (AddChoices) SpawnMessage(new() { Text = FormatChoices(choices) });
        }

        public virtual void Clear ()
        {
            ClearMessages();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(messagesContainer, scrollRect, messagePrefab);

            State = Engine.GetService<IStateManager>();
        }

        protected virtual void SpawnMessage (BacklogMessageUI.State state)
        {
            var messageUI = default(BacklogMessageUI);

            if (Messages.Count >= Capacity)
            {
                messageUI = Messages.First.Value;
                messageUI.gameObject.SetActive(true);
                messageUI.transform.SetSiblingIndex(MessagesContainer.childCount - 1);
                Messages.RemoveFirst();
            }
            else
            {
                if (MessagesPool.Count > 0)
                {
                    messageUI = MessagesPool.Pop();
                    messageUI.gameObject.SetActive(true);
                    messageUI.transform.SetSiblingIndex(MessagesContainer.childCount - 1);
                }
                else messageUI = Instantiate(MessagePrefab, MessagesContainer, false);
            }
            Messages.AddLast(messageUI);

            messageUI.Initialize(state);
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            MessagesContainer.gameObject.SetActive(visible);
            if (visible) ScrollToBottom();
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);
            var state = new GameState {
                Messages = Messages.TakeLast(SaveCapacity).Select(m => m.GetState()).ToList()
            };
            stateMap.SetState(state);
        }

        protected override async Awaitable DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            using var _ = SetPool<LocalizableText>.Rent(out var deferRelease);
            ClearMessages(deferRelease);

            var state = stateMap.GetState<GameState>();
            if (state?.Messages != null)
            {
                using var __ = Async.Rent(out var tasks);
                foreach (var message in state.Messages)
                    tasks.Add(DeserializeMessage(message));
                await Async.All(tasks);
            }

            foreach (var deferred in deferRelease)
                deferred.Release(this);
        }

        protected virtual async Awaitable DeserializeMessage (BacklogMessageUI.State state)
        {
            await state.Text.Load(); // held by messageUI on init
            SpawnMessage(state);
        }

        protected virtual void ClearMessages (ISet<LocalizableText> deferRelease = null)
        {
            foreach (var message in Messages)
            {
                if (deferRelease != null)
                {
                    message.Text.Hold(this);
                    deferRelease.Add(message.Text);
                }
                message.Clear();
                message.gameObject.SetActive(false);
                MessagesPool.Push(message);
            }
            Messages.Clear();
        }

        protected virtual PlaybackSpot ProcessRollbackSpot (PlaybackSpot? spot)
        {
            if (!AllowRollback || !spot.HasValue || spot == PlaybackSpot.Invalid)
                return PlaybackSpot.Invalid;

            // Otherwise stored spots not associated with player input
            // won't serialize (eg, printed messages with [skipInput]).
            if (State.PeekRollbackStack()?.PlaybackSpot == spot)
                State.PeekRollbackStack()?.ForceSerialize();

            return spot.Value;
        }

        protected virtual LocalizableText FormatChoices (IReadOnlyList<BacklogChoice> choices)
        {
            FormatPool.Clear();
            foreach (var choice in choices)
                FormatPool.Add(FormatChoice(choice));
            return LocalizableText.Join(ChoiceSeparator, FormatPool);
        }

        protected virtual LocalizableText FormatChoice (BacklogChoice choice)
        {
            return choice.Selected
                ? LocalizableText.FromTemplate(selectedChoiceTemplate, ChoiceTemplateLiteral, choice.Summary)
                : LocalizableText.FromTemplate(otherChoiceTemplate, ChoiceTemplateLiteral, choice.Summary);
        }

        protected virtual async void ScrollToBottom ()
        {
            // Wait a frame and force rebuild layout before setting scroll position,
            // otherwise it's ignoring recently added messages.
            await Async.NextFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(ScrollRect.content);
            ScrollRect.verticalNormalizedPosition = 0;
        }

        protected override GameObject FindFocusObject ()
        {
            var message = Messages.Last;
            while (message != null)
            {
                if (message.Value.GetComponentInChildren<Selectable>() is { } selectable)
                    return selectable.gameObject;
                message = message.Previous;
            }
            return null;
        }
    }
}

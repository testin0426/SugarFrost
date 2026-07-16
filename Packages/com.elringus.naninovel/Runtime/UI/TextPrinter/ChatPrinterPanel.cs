using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation for a chat-style printer.
    /// </summary>
    public class ChatPrinterPanel : UITextPrinterPanel, ILocalizableUI
    {
        public override float RevealProgress { get => revealProgress; set => SetRevealProgress(value); }
        public override string Appearance { get; set; }

        protected virtual ScrollRect ScrollRect => scrollRect;
        protected virtual RectTransform MessagesContainer => messagesContainer;
        protected virtual ChatMessage MessagePrototype => messagePrototype;
        protected virtual int MessageLimit => messageLimit;
        protected virtual ScriptableUIBehaviour InputIndicator => inputIndicator;
        protected virtual float RevealDelayModifier => revealDelayModifier;
        protected virtual string ChoiceHandlerId => choiceHandlerId;
        protected virtual RectTransform ChoiceHandlerContainer => choiceHandlerContainer;
        protected virtual List<ChatMessage> ChatMessages { get; } = new();

        [Header("Chat Printer Setup")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform messagesContainer;
        [SerializeField] private ChatMessage messagePrototype;
        [SerializeField] private ScriptableUIBehaviour inputIndicator;
        [SerializeField] private RectTransform choiceHandlerContainer;
        [Header("Chat Printer Behaviour")]
        [Tooltip("Maximum number of chat messages to keep display; 0 means no limit.")]
        [SerializeField] private int messageLimit;
        [SerializeField] private float revealDelayModifier = 3f;
        [Tooltip("Associated choice handler actor ID to embed inside the printer; implementation is expected to be MonoBehaviourActor-derived.")]
        [SerializeField] private string choiceHandlerId = "ChatReply";

        private ICharacterManager characterManager;
        private IChoiceHandlerManager choiceManager;
        private float revealProgress;

        public override async Awaitable Initialize ()
        {
            await base.Initialize();
            await EmbedChoiceHandler();
        }

        public override void SetMessages (IReadOnlyList<PrintedMessage> messages)
        {
            if (MessageLimit > 0 && messages.Count > MessageLimit)
                messages = messages.Skip(messages.Count - MessageLimit).ToArray();
            Messages.ReplaceWith(messages);
            DestroyAllMessages();
            foreach (var message in messages)
                SpawnChatMessage(message);
            ScrollToBottom();
        }

        public override void AddMessage (PrintedMessage message)
        {
            Messages.Add(message);
            SpawnChatMessage(message);
            if (MessageLimit > 0 && Messages.Count > MessageLimit)
            {
                ObjectUtils.DestroyOrImmediate(ChatMessages[0].gameObject);
                ChatMessages.RemoveAt(0);
                Messages.RemoveAt(0);
            }
            ScrollToBottom();
        }

        public override void AppendText (LocalizableText text)
        {
            if (Messages.Count == 0)
            {
                AddMessage(new(text));
                return;
            }
            ObjectUtils.DestroyOrImmediate(ChatMessages[^1].gameObject);
            ChatMessages.RemoveAt(ChatMessages.Count - 1);
            Messages[^1] = new(Messages[^1].Text + text, Messages[^1].Author ?? default);
            AddMessage(Messages[^1]);
        }

        public override async Awaitable RevealMessages (float delay, AsyncToken token)
        {
            if (ChatMessages.Count == 0) return;

            var message = ChatMessages[^1];
            RevealProgress = 0;

            if (delay > 0)
            {
                var revealDuration = message.MessageText.Count(char.IsLetterOrDigit) * delay * revealDelayModifier;
                var revealStartTime = Engine.Time.Time;
                var revealFinishTime = revealStartTime + revealDuration;
                while (revealFinishTime > Engine.Time.Time && ChatMessages.Count > 0 && ChatMessages[^1] == message)
                {
                    RevealProgress = (Engine.Time.Time - revealStartTime) / revealDuration;
                    await Async.NextFrame(token);
                    if (token.Completed) break;
                }
            }

            RevealProgress = 1f;
        }

        public override void SetAwaitInputIndicatorVisible (bool visible)
        {
            if (visible) inputIndicator.Show();
            else inputIndicator.Hide();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(scrollRect, messagesContainer, messagePrototype, inputIndicator);

            characterManager = Engine.GetServiceOrErr<ICharacterManager>();
            choiceManager = Engine.GetServiceOrErr<IChoiceHandlerManager>();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (choiceManager.ActorExists(choiceHandlerId))
                choiceManager.RemoveActor(choiceHandlerId);
        }

        protected virtual async Awaitable EmbedChoiceHandler ()
        {
            if (string.IsNullOrEmpty(ChoiceHandlerId) || !ChoiceHandlerContainer) return;
            var handler = await choiceManager.GetOrAddActor(ChoiceHandlerId) as MonoBehaviourActor<ChoiceHandlerMetadata>;
            if (handler is null || !handler.GameObject) throw new Error($"Choice handler '{ChoiceHandlerId}' is not derived from MonoBehaviourActor or destroyed.");
            var rectTrs = handler.GameObject.GetComponentInChildren<RectTransform>();
            if (!rectTrs) throw new Error($"Choice handler '{ChoiceHandlerId}' is missing RectTransform component.");
            rectTrs.SetParent(ChoiceHandlerContainer, false);
            var ui = ChoiceHandlerContainer.GetComponentInChildren<IManagedUI>();
            if (ui is null) throw new Error($"Choice handler '{ChoiceHandlerId}' is missing IManagedUI component.");
            ui.OnVisibilityChanged += HandleChoiceVisibilityChanged;
        }

        protected virtual void HandleChoiceVisibilityChanged (bool visible)
        {
            ChoiceHandlerContainer.gameObject.SetActive(visible);
            ScrollToBottom();
        }

        protected virtual ChatMessage SpawnChatMessage (PrintedMessage message)
        {
            var chatMessage = Instantiate(messagePrototype, messagesContainer, false);
            chatMessage.MessageText = FormatMessage(message);

            if (message.Author?.Id is { } authorId)
            {
                chatMessage.ActorNameText = message.Author is { Label: { IsEmpty: false } label } ? label : characterManager.GetAuthorName(authorId);
                chatMessage.AvatarTexture = Characters.GetAvatarTextureFor(authorId);

                var meta = characterManager.Configuration.GetMetadataOrDefault(authorId);
                if (meta.UseCharacterColor)
                {
                    chatMessage.MessageColor = meta.MessageColor;
                    chatMessage.ActorNameTextColor = meta.NameColor;
                }
            }
            else
            {
                chatMessage.ActorNameText = string.Empty;
                chatMessage.AvatarTexture = null;
            }

            chatMessage.Visible = true;
            ChatMessages.Add(chatMessage);
            return chatMessage;
        }

        protected virtual void SetRevealProgress (float ratio)
        {
            revealProgress = ratio;
            if (ChatMessages.Count > 0)
                ChatMessages[^1].SetIsTyping(ratio <= .99f);
        }

        protected virtual void DestroyAllMessages ()
        {
            foreach (var msg in ChatMessages)
                ObjectUtils.DestroyOrImmediate(msg.gameObject);
            ChatMessages.Clear();
        }

        protected virtual async void ScrollToBottom ()
        {
            // Wait a frame and force rebuild layout before setting scroll position,
            // otherwise it's ignoring recently added messages.
            await Async.Frames(1);
            if (!scrollRect) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}

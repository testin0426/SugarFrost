using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation using <see cref="IRevealableText"/> to reveal text over time.
    /// </summary>
    /// <remarks>
    /// A <see cref="IRevealableText"/> component is expected on the underlying game object or one of its children.
    /// </remarks>
    public class RevealableTextPrinterPanel : UITextPrinterPanel, ILocalizableUI
    {
        [Serializable]
        protected class CharsToSfx
        {
            [Tooltip("The characters for which to trigger the SFX. Leave empty to trigger on any character.")]
            public string Characters;
            [Tooltip("The name (local path) of the SFX to trigger for the specified characters.")]
            [ResourcePopup(AudioConfiguration.DefaultAudioPathPrefix)]
            public string SfxName;
        }

        [Serializable]
        protected class CharsToPlaylist
        {
            [Tooltip("The characters for which to trigger the command. Leave empty to trigger on any character.")]
            public string Characters;
            [Tooltip("The text of the script command to execute for the specified characters.")]
            public string CommandText;
            public ScriptPlaylist Playlist { get; set; }
        }

        [Serializable]
        private class AuthorChangedEvent : UnityEvent<string> { }

        public virtual IRevealableText RevealableText => (IRevealableText)revealableText;
        public override float RevealProgress { get => RevealableText.RevealProgress; set => SetRevealProgress(value); }
        public override string Appearance { get => GetActiveAppearance(); set => SetActiveAppearance(value); }

        protected const string DefaultAppearanceName = "Default";
        protected virtual MessageAuthor Author { get; private set; }
        protected virtual ICharacterActor AuthorChara { get; private set; }
        protected virtual CharacterMetadata AuthorMeta { get; private set; }
        protected virtual IInputIndicator InputIndicator => (IInputIndicator)inputIndicator;
        protected virtual AuthorNamePanel AuthorNamePanel => authorNamePanel;
        protected virtual AuthorImage AuthorAvatarImage => authorAvatarImage;
        protected virtual bool PositionIndicatorOverText => positionIndicatorOverText;
        protected virtual List<CanvasGroup> Appearances => appearances;
        protected virtual List<CharsToSfx> CharsSfx => charsSfx;
        protected virtual int AutoLineBreaks => autoLineBreaks;
        protected virtual TextRevealer Revealer { get; private set; }
        protected virtual Color DefaultMessageColor { get; private set; }
        protected virtual Color DefaultNameColor { get; private set; }
        protected virtual IAudioManager Audio { get; private set; }
        protected virtual IScriptPlayer Player { get; private set; }
        protected virtual IInputHandle ContinueInput { get; private set; }

        [Header("Revealable Printer Setup")]
        [Tooltip("Revealable text component. Expected to implement 'IRevealableText' interface.")]
        [SerializeField] private MonoBehaviour revealableText;
        [Tooltip("Panel to display name of the currently printed text author (optional).")]
        [SerializeField] private AuthorNamePanel authorNamePanel;
        [Tooltip("Image to display avatar of the currently printed text author (optional).")]
        [SerializeField] private AuthorImage authorAvatarImage;
        [Tooltip("Object to use as an indicator when player is supposed to activate a 'Continue' input to progress further. Expected to implement 'IInputIndicator' interface.")]
        [SerializeField] private MonoBehaviour inputIndicator;
        [Header("Revealable Printer Behaviour")]
        [Tooltip("Whether to automatically move input indicator so it appears after the last revealed text character.")]
        [SerializeField] private bool positionIndicatorOverText = true;
        [Tooltip("Number of line breaks (<br> tags) to insert between adjacent messages when formatting the messages.")]
        [SerializeField] private int autoLineBreaks;
        [Tooltip("Assigned canvas groups will represent printer appearances. Game object name of the canvas group represents the appearance name. Alpha of the group will be set to 1 when the appearance is activated and vice-versa.")]
        [SerializeField] private List<CanvasGroup> appearances;
        [Tooltip("Allows binding an SFX to play when specific characters are revealed.")]
        [SerializeField] private List<CharsToSfx> charsSfx = new();
        [Tooltip("Invoked when author (character ID) of the currently printed text is changed.")]
        [SerializeField] private AuthorChangedEvent onAuthorChanged;
        [Tooltip("Invoked when text reveal is started.")]
        [SerializeField] private UnityEvent onRevealStarted;
        [Tooltip("Invoked when text reveal is finished.")]
        [SerializeField] private UnityEvent onRevealFinished;

        private readonly StringBuilder builder = new();

        public override async Awaitable Initialize ()
        {
            await base.Initialize();

            if (CharsSfx != null && CharsSfx.Count > 0)
            {
                using var _ = Async.Rent<Resource>(out var loadTasks);
                foreach (var charSfx in CharsSfx)
                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        loadTasks.Add(Audio.AudioLoader.LoadOrErr(charSfx.SfxName, this));
                await Async.All(loadTasks);
            }

            // Required for TMPro text to update the text info before applying actor state (reveal progress).
            await Async.NextFrame();
        }

        public override void SetMessages (IReadOnlyList<PrintedMessage> messages)
        {
            Messages.ReplaceWith(messages);
            SetMessageAuthor(messages.LastOrDefault().Author ?? default);
            if (messages.Count == 0)
            {
                RevealableText.Text = "";
                return;
            }
            if (messages.Count == 1)
            {
                RevealableText.Text = FormatMessage(messages[0]);
                return;
            }
            builder.Clear();
            for (var i = 0; i < messages.Count; i++)
                builder.Append(FormatMessage(messages[i], i > 0));
            RevealableText.Text = builder.ToString();
        }

        public override void AddMessage (PrintedMessage message)
        {
            Messages.Add(message);
            SetMessageAuthor(message.Author ?? default);
            RevealableText.Text += FormatMessage(message, Messages.Count > 1);
        }

        public override void AppendText (LocalizableText text)
        {
            if (Messages.Count == 0)
            {
                AddMessage(new(text));
                return;
            }
            Messages[^1] = new(Messages[^1].Text + text, Messages[^1].Author ?? default);
            using var _ = Messages.RentWith(out var messages);
            SetMessages(messages);
        }

        public override async Awaitable RevealMessages (float delay, AsyncToken token)
        {
            onRevealStarted?.Invoke();

            // Force-hide the indicator. Required when printing by non-played commands (eg, PlayScript component),
            // while the script player is actually waiting for input.
            SetAwaitInputIndicatorVisible(false);

            if (delay <= 0) RevealableText.RevealProgress = 1f;
            else
                while (!await Revealer.Reveal(delay, token))
                    await WaitForContinueRevealConfirmation(token);

            if (Player.AwaitingInput)
                SetAwaitInputIndicatorVisible(true);

            onRevealFinished?.Invoke();
        }

        protected virtual async Awaitable WaitForContinueRevealConfirmation (AsyncToken token)
        {
            SetAwaitInputIndicatorVisible(true);
            var next = ContinueInput.InterceptNext(token.CompletionToken);
            while (!next.IsCancellationRequested && !token.Completed)
                await Async.NextFrame(token);
            SetAwaitInputIndicatorVisible(false);
        }

        public override void SetAwaitInputIndicatorVisible (bool visible)
        {
            if (visible)
            {
                InputIndicator.Show();
                if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
            }
            else InputIndicator.Hide();
        }

        public override void SetFontSize (int dropdownIndex)
        {
            base.SetFontSize(dropdownIndex);
            if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
        }

        public override void SetFont (TMP_FontAsset font)
        {
            base.SetFont(font);
            if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
        }

        public override Awaitable HandleLocalizationChanged (LocaleChangedArgs _)
        {
            // Otherwise continue input indicator has wrong position and
            // visual reveal progress is wrong after changing locale.
            ForceUpdateRevealState();
            return Async.Completed;

            async void ForceUpdateRevealState ()
            {
                await Async.Frames(1);
                RevealMessages(0, default).Forget();
            }
        }

        protected override void Awake ()
        {
            base.Awake();

            if (RevealableText == null) throw new Error($"Revealable Text missing on {gameObject.name} text printer.");
            if (InputIndicator == null) throw new Error($"Input Indicator missing on {gameObject.name} text printer.");

            DefaultMessageColor = RevealableText.TextColor;
            DefaultNameColor = AuthorNamePanel ? AuthorNamePanel.TextColor : default;
            Audio = Engine.GetServiceOrErr<IAudioManager>();
            Player = Engine.GetServiceOrErr<IScriptPlayer>();
            ContinueInput = Engine.GetServiceOrErr<IInputManager>().GetContinue();
            Revealer = new(RevealableText, HandleCharRevealed);
            SetAuthorNameText(null);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            Characters.OnCharacterAvatarChanged += HandleAvatarChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (Characters != null)
                Characters.OnCharacterAvatarChanged -= HandleAvatarChanged;
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (CharsSfx != null && CharsSfx.Count > 0)
                foreach (var charSfx in CharsSfx)
                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        Audio?.AudioLoader?.Release(charSfx.SfxName, this);
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange();
            if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (!visible && AuthorAvatarImage && AuthorAvatarImage.isActiveAndEnabled)
                AuthorAvatarImage.ChangeTexture(null).Forget();
        }

        protected virtual void SetMessageAuthor (MessageAuthor author)
        {
            Author = author;
            AuthorMeta = Characters.GetActorMetaOrDefault(Author.Id);
            AuthorChara = Characters.GetActor(Author.Id);

            RevealableText.TextColor = AuthorMeta.UseCharacterColor ? AuthorMeta.MessageColor : DefaultMessageColor;
            SetAuthorNameText(Author.Label.IsEmpty ? Characters.GetAuthorName(Author.Id) : Author.Label);
            if (AuthorNamePanel)
                AuthorNamePanel.TextColor = AuthorMeta.UseCharacterColor ? AuthorMeta.NameColor : DefaultNameColor;
            if (AuthorAvatarImage)
                AuthorAvatarImage.ChangeTexture(Characters.GetAvatarTextureFor(Author.Id)).Forget();

            onAuthorChanged?.Invoke(Author.Id);
        }

        protected virtual async void PlaceInputIndicatorOverText ()
        {
            // Wait for TMPro to update text info (force-update doesn't work).
            await Async.NextFrame();
            if (!this || !isActiveAndEnabled) return;
            var pos = RevealableText.GetLastRevealedCharPosition();
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y)) return;
            InputIndicator.RectTransform.localPosition = new(pos.x, pos.y, InputIndicator.RectTransform.position.z);
        }

        protected virtual string GetActiveAppearance ()
        {
            if (Appearances is null || Appearances.Count == 0)
                return DefaultAppearanceName;
            foreach (var grp in Appearances)
                if (Mathf.Approximately(grp.alpha, 1f))
                    return grp.gameObject.name;
            return DefaultAppearanceName;
        }

        protected virtual void SetActiveAppearance (string appearance)
        {
            if (Appearances is null || Appearances.Count == 0 || !Appearances.Any(g => g.gameObject.name == appearance))
                return;

            foreach (var grp in Appearances)
                grp.alpha = grp.gameObject.name == appearance ? 1 : 0;
        }

        protected virtual void SetRevealProgress (float value)
        {
            RevealableText.RevealProgress = value;
        }

        protected virtual void SetAuthorNameText (string text)
        {
            if (!AuthorNamePanel) return;

            var isActive = !string.IsNullOrWhiteSpace(text);
            AuthorNamePanel.gameObject.SetActive(isActive);
            if (!isActive) return;

            AuthorNamePanel.Text = text;
        }

        protected virtual void HandleAvatarChanged (CharacterAvatarChangedArgs args)
        {
            if (!AuthorAvatarImage || args.CharacterId != Author.Id) return;

            AuthorAvatarImage.ChangeTexture(args.AvatarTexture).Forget();
        }

        protected virtual void HandleCharRevealed (char character)
        {
            if (AuthorMeta != null && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                PlayAuthorSound();
            if (CharsSfx != null && CharsSfx.Count > 0)
                PlayRevealSfxForChar(character);
        }

        protected virtual void PlayAuthorSound ()
        {
            Audio.PlaySfxFast(AuthorMeta.MessageSound,
                restart: AuthorMeta.MessageSoundPlayback == MessageSoundPlayback.OneShotClipped,
                additive: AuthorMeta.MessageSoundPlayback != MessageSoundPlayback.Looped).Forget();
        }

        protected virtual void PlayRevealSfxForChar (char character)
        {
            foreach (var chars in CharsSfx)
                if (ShouldPlay(chars))
                    Audio.PlaySfxFast(chars.SfxName).Forget();

            bool ShouldPlay (CharsToSfx chars) =>
                !string.IsNullOrEmpty(chars.SfxName) &&
                (string.IsNullOrEmpty(chars.Characters) || chars.Characters.IndexOf(character) >= 0);
        }

        protected virtual string FormatMessage (PrintedMessage message, bool adjacent)
        {
            var text = base.FormatMessage(message);
            if (adjacent)
                for (int i = 0; i < AutoLineBreaks; i++)
                    text = $"<br>{text}";
            return text;
        }
    }
}

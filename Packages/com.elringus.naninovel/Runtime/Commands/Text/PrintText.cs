using System;
using System.Linq;
using JetBrains.Annotations;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Prints (reveals over time) specified text message using a text printer actor.",
        @"
This command is used under the hood when processing generic text lines, eg generic line `Kohaku: Hello World!` will be 
automatically transformed into `@print ""Hello World!"" author:Kohaku` when parsing the scenario scripts.<br/>
Will reset (clear) the printer before printing the new message by default; set `reset` parameter to *false* or disable `Auto Reset` in the printer actor configuration to prevent that and append the text instead.<br/>
Will make the printer default and hide other printers by default; set `default` parameter to *false* or disable `Auto Default` in the printer actor configuration to prevent that.<br/>
Will wait for user input before finishing the task by default; set `waitInput` parameter to *false* or disable `Auto Wait` in the printer actor configuration to return as soon as the text is fully revealed.",
        @"
; Will print the phrase with a default printer.
@print ""Lorem ipsum dolor sit amet.""",
        @"
; To include quotes in the text itself, escape them.
@print ""Shouting \""Stop the car!\"" was a mistake.""",
        @"
; Reveal message with half of the normal speed and
; don't wait for user input to continue.
@print ""Lorem ipsum dolor sit amet."" speed:0.5 !waitInput",
        @"
; Print the line with ""Together"" displayed as author name and
; make all visible characters author of the printed text.
@print ""Hello World!"" author:* as:""Together""",
        @"
; Similar, but make only ""Kohaku"" and ""Yuko"" the authors.
@print ""Hello World!"" author:Kohaku,Yuko as:""Kohaku and Yuko"""
    )]
    [Serializable, Alias("print"), TextGroup, Icon("Comment")]
    public class PrintText : PrinterCommand, Command.IPreloadable, Command.ILocalizable
    {
        [Doc("Text of the message to print. When the text contains spaces, wrap it in double quotes (`\"`). " +
             "In case you wish to include the double quotes in the text itself, escape them.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public LocalizableTextParameter Text;
        [Doc("ID of the printer actor to use. Will use a default one when not specified.")]
        [Alias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        [Doc("ID of the actor, which should be associated with the printed message. Ignored when appending. " +
             "Specify `*` or use `,` to delimit multiple actor IDs to make all/selected characters authors of the text; " +
             "useful when coupled with `as` parameter to represent multiple characters speaking at the same time.")]
        [Alias("author"), ActorContext(CharactersConfiguration.DefaultPathPrefix)]
        public StringParameter AuthorId;
        [Doc("When specified, will use the label instead of author ID (or associated display name) " +
             "to represent author name in the text printer while printing the message. Useful to " +
             "override default name for a few messages or represent multiple authors speaking at the same time " +
             "without triggering author-specific behaviour of the text printer, such as message color or avatar.")]
        [Alias("as")]
        public LocalizableTextParameter AuthorLabel;
        [Doc("Text reveal speed multiplier; should be positive or zero. Setting to one will yield the default speed.")]
        [Alias("speed"), ParameterDefaultValue("1")]
        public DecimalParameter RevealSpeed;
        [Doc("Whether to reset text of the printer before executing the printing task. " +
             "Default value is controlled via `Auto Reset` property in the printer actor configuration menu.")]
        [Alias("reset")]
        public BooleanParameter ResetPrinter;
        [Doc("Whether to make the printer default and hide other printers before executing the printing task. " +
             "Default value is controlled via `Auto Default` property in the printer actor configuration menu.")]
        [Alias("default")]
        public BooleanParameter DefaultPrinter;
        [Doc("Whether to wait for user input after finishing the printing task. " +
             "Default value is controlled via `Auto Wait` property in the printer actor configuration menu.")]
        [Alias("waitInput")]
        public BooleanParameter WaitForInput;
        [Doc("Whether to append the printed text to the last printer message.")]
        public BooleanParameter Append;
        [Doc("Controls duration (in seconds) of the printers show and hide animations associated with this command. " +
             "Default value for each printer is set in the actor configuration.")]
        [Alias("fadeTime")]
        public DecimalParameter ChangeVisibilityDuration;
        [Doc("Whether to await the text reveal and prompt for completion (wait for input) before playing next command.")]
        public BooleanParameter Wait;

        [CanBeNull] protected override string AssignedPrinterId => PrinterId;
        [CanBeNull] protected override string AssignedAuthorId => GetAssignedAuthorId();
        protected virtual float AssignedRevealSpeed => GetAssignedOrDefault(RevealSpeed, 1f);
        protected virtual string AutoVoicePath { get; set; }
        protected virtual IScriptPlayer Player => Engine.GetServiceOrErr<IScriptPlayer>();
        protected virtual IAudioManager Audio => Engine.GetServiceOrErr<IAudioManager>();
        protected virtual ITextLocalizer Localizer => Engine.GetServiceOrErr<ITextLocalizer>();
        protected virtual CharacterMetadata AuthorMeta => Characters.Configuration.GetMetadataOrDefault(AssignedAuthorId);

        public override async Awaitable PreloadResources ()
        {
            await base.PreloadResources();

            await PreloadStaticTextResources(Text);

            if (Audio.Configuration.EnableAutoVoicing && !string.IsNullOrEmpty(AutoVoicePath = BuildAutoVoicePath()))
                await Audio.VoiceLoader.Load(AutoVoicePath, this);

            if (Assigned(AuthorId) && !AuthorId.DynamicValue && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                await Audio.AudioLoader.Load(AuthorMeta.MessageSound, this);
        }

        public override void ReleaseResources ()
        {
            base.ReleaseResources();

            ReleaseStaticTextResources(Text);

            if (!string.IsNullOrEmpty(AutoVoicePath))
                Audio?.VoiceLoader?.Release(AutoVoicePath, this);

            if (Assigned(AuthorId) && !AuthorId.DynamicValue && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                Audio?.AudioLoader?.Release(AuthorMeta.MessageSound, this);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var printer = await GetOrAddPrinter(ctx.Token);
            await WaitOrForget(ctx => Print(printer, ctx), Wait, ctx);
        }

        protected virtual async Awaitable Print (ITextPrinterActor printer, ExecutionContext ctx)
        {
            var meta = Printers.Configuration.GetMetadataOrDefault(printer.Id);
            var resetText = ShouldResetText(meta);
            if (resetText) ResetText(printer);

            var showTask = Async.Completed;
            if (!printer.Visible)
                if (meta.WaitVisibilityBeforePrint)
                    await ShowPrinter(printer, meta, ctx);
                else showTask = ShowPrinter(printer, meta, ctx);

            if (ShouldSetDefaultPrinter(meta))
                SetDefaultPrinter(printer, ctx);

            if (meta.StopVoice) Audio.StopVoice();

            if (ShouldPlayAutoVoice())
                await PlayAutoVoice(printer, ctx);

            // Copy to a temp var to prevent multiple evaluations of dynamic values.
            var printedText = Text.Value;
            if (printedText.IsEmpty) return;

            using var _ = await LoadDynamicTextResources((Text, printedText));

            await Print(printedText, printer, ctx);

            for (int i = 0; i < meta.PrintFrameDelay; i++)
                await Async.NextFrame(ctx.Token);

            await WaitSkipDelay(ctx);

            if (ShouldWaitForInput(meta, ctx))
                await WaitInput(printedText, ctx);
            else
            {
                if (IsPlayingAutoVoice()) await WaitAutoVoice(ctx);
                if (ShouldAllowRollbackWhenInputNotAwaited(ctx))
                    Engine.GetService<IStateManager>()?.PeekRollbackStack()?.AllowPlayerRollback();
            }

            if (meta.AddToBacklog)
                AddBacklog(printedText);

            if (!showTask.IsCompleted)
                await showTask;
        }

        protected virtual bool ShouldResetText (TextPrinterMetadata meta)
        {
            return Assigned(ResetPrinter) && ResetPrinter.Value || !Assigned(ResetPrinter) && meta.AutoReset;
        }

        protected virtual void ResetText (ITextPrinterActor printer)
        {
            printer.ClearMessages();
            printer.RevealProgress = 0f;
        }

        [CanBeNull]
        protected virtual string BuildAutoVoicePath ()
        {
            return AutoVoiceResolver.Resolve(Text);
        }

        protected virtual Awaitable ShowPrinter (ITextPrinterActor printer, TextPrinterMetadata meta, ExecutionContext ctx)
        {
            var showDuration = Assigned(ChangeVisibilityDuration) ? ChangeVisibilityDuration.Value : meta.ChangeVisibilityDuration;
            return printer.ChangeVisibility(true, new(showDuration), token: ctx.Token);
        }

        protected virtual bool ShouldSetDefaultPrinter (TextPrinterMetadata meta)
        {
            return Assigned(DefaultPrinter) && DefaultPrinter.Value || !Assigned(DefaultPrinter) && meta.AutoDefault;
        }

        protected virtual void SetDefaultPrinter (ITextPrinterActor defaultPrinter, ExecutionContext ctx)
        {
            if (Printers.DefaultPrinterId != defaultPrinter.Id)
                Printers.DefaultPrinterId = defaultPrinter.Id;

            using var _ = Printers.RentActors(out var actors);
            foreach (var printer in actors)
                if (printer.Id != defaultPrinter.Id && printer.Visible)
                    HideOtherPrinter(printer);

            void HideOtherPrinter (ITextPrinterActor other)
            {
                var otherMeta = Printers.Configuration.GetMetadataOrDefault(other.Id);
                var otherHideDuration = Assigned(ChangeVisibilityDuration) ? ChangeVisibilityDuration.Value : otherMeta.ChangeVisibilityDuration;
                other.ChangeVisibility(false, new(otherHideDuration), token: ctx.Token).Forget();
            }
        }

        protected virtual bool ShouldPlayAutoVoice ()
        {
            return Audio.Configuration.EnableAutoVoicing &&
                   !string.IsNullOrEmpty(PlaybackSpot.ScriptPath) &&
                   !Player.Skipping;
        }

        protected virtual async Awaitable PlayAutoVoice (ITextPrinterActor printer, ExecutionContext ctx)
        {
            if (string.IsNullOrEmpty(AutoVoicePath)) AutoVoicePath = BuildAutoVoicePath();
            if (string.IsNullOrEmpty(AutoVoicePath)) return;
            if (!Audio.VoiceLoader.Exists(AutoVoicePath)) return;
            var playedVoicePath = Audio.GetPlayedVoice();
            if (Audio.Configuration.VoiceOverlapPolicy == VoiceOverlapPolicy.PreventCharacterOverlap &&
                printer.FinalMessage?.Author?.Id == AssignedAuthorId && !string.IsNullOrEmpty(playedVoicePath))
                Audio.StopVoice();
            await Audio.PlayVoice(AutoVoicePath, authorId: AssignedAuthorId, token: ctx.Token);
        }

        protected virtual Awaitable Print (LocalizableText text, ITextPrinterActor printer, ExecutionContext ctx)
        {
            var message = new PrintedMessage(text, new(AssignedAuthorId, AuthorLabel));
            return Printers.Print(printer.Id, message, Append, AssignedRevealSpeed, ctx.Token);
        }

        protected virtual bool ShouldWaitForInput (TextPrinterMetadata meta, ExecutionContext ctx)
        {
            if (ctx.Token.Completed && !meta.WaitAfterRevealSkip) return false;
            if (Assigned(WaitForInput)) return WaitForInput.Value;
            return meta.AutoWait;
        }

        protected virtual bool ShouldAllowRollbackWhenInputNotAwaited (ExecutionContext ctx)
        {
            // Required for rollback to work when WaitAfterRevealSkip is disabled.
            return !(Assigned(WaitForInput) && !WaitForInput) && ctx.Token.Completed;
        }

        protected virtual async Awaitable WaitInput (LocalizableText text, ExecutionContext ctx)
        {
            if (Player.AutoPlaying)
                await WaitAutoPlayDelay(text, ctx);
            ctx.Track.SetAwaitInput(true);
        }

        protected virtual void AddBacklog (LocalizableText text)
        {
            var backlogUI = Engine.GetServiceOrErr<IUIManager>().GetUI<IBacklogUI>();
            if (backlogUI is null) return;
            var voicePath = !string.IsNullOrEmpty(AutoVoicePath) && Audio.VoiceLoader.IsLoaded(AutoVoicePath) ? AutoVoicePath : null;
            if (Append) backlogUI.AppendMessage(text, voicePath);
            else
                backlogUI.AddMessage(new() {
                    Text = text,
                    AuthorId = string.IsNullOrWhiteSpace(AssignedAuthorId) ? null : AssignedAuthorId,
                    AuthorLabel = Assigned(AuthorLabel) ? AuthorLabel : null,
                    Spot = PlaybackSpot,
                    Voice = voicePath
                });
        }

        protected virtual async Awaitable WaitAutoVoice (ExecutionContext ctx)
        {
            while (IsPlayingAutoVoice() && ctx.Token.EnsureNotCanceledOrCompleted())
                await Async.NextFrame();
        }

        protected virtual async Awaitable WaitAutoPlayDelay (LocalizableText text, ExecutionContext ctx)
        {
            var baseDelay = Configuration.ScaleAutoWait ? Printers.BaseAutoDelay * AssignedRevealSpeed : Printers.BaseAutoDelay;
            var textLength = Localizer.Resolve(text).Count(char.IsLetterOrDigit);
            var autoPlayDelay = Mathf.Lerp(0, Configuration.MaxAutoWaitDelay, baseDelay) * textLength;
            var waitUntilTime = Engine.Time.Time + autoPlayDelay;
            while ((Engine.Time.Time < waitUntilTime || IsPlayingAutoVoice()) && ctx.Token.EnsureNotCanceledOrCompleted())
                await Async.NextFrame();
        }

        protected virtual async Awaitable WaitSkipDelay (ExecutionContext ctx)
        {
            if (!Player.Skipping || Configuration.SkipPrintDelay <= 0) return;

            var startTime = Engine.Time.UnscaledTime;
            var waitTime = Configuration.SkipPrintDelay;
            while (ctx.Token.EnsureNotCanceledOrCompleted())
            {
                await Async.NextFrame(ctx.Token);
                var waitedEnough = Engine.Time.UnscaledTime - startTime >= waitTime;
                if (waitedEnough) break;
            }
        }

        protected virtual bool IsPlayingAutoVoice ()
        {
            return ShouldPlayAutoVoice() && Audio.GetPlayedVoice() == AutoVoicePath;
        }

        [CanBeNull]
        protected virtual string GetAssignedAuthorId ()
        {
            if (Assigned(AuthorId)) return AuthorId;
            if (Characters.Configuration.Metadata.ContainsId(CharactersConfiguration.DefaultAuthorId))
                return CharactersConfiguration.DefaultAuthorId;
            return null;
        }
    }
}

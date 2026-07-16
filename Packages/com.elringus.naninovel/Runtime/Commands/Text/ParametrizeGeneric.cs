using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Used to apply [generic parameters](/guide/scenario-scripting#generic-parameters) via `[< ...]` syntax.",
        null,
        @"
; Following line will be authored by Kohaku and Yuko actors, while
; the display name label on the printer will show ""All Together"".
Kohaku,Yuko: How low hello![< as:""All Together""]",
        @"
; First part of the sentence will be printed with 50% speed,
; while the second one with 250% speed and it won't be awaited.
Lorem ipsum[< speed:0.5] dolor sit amet.[< speed:2.5 nowait!]"
    )]
    [Serializable, Alias("<"), InlineOnly, TextGroup, Icon("TextDuo")]
    public class ParametrizeGeneric : Command, Command.IPreloadable, Command.ILocalizable
    {
        [Doc("ID of the printer actor to use.")]
        [Alias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        [Doc("ID of the actor, which should be associated with the printed message. " +
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
        [Alias("speed")]
        public DecimalParameter RevealSpeed;
        [Doc("Whether to not reset printed text before printing this line effectively appending the text.")]
        [Alias("join")]
        public BooleanParameter Join;
        [Doc("Whether to not wait for user input (aka CTC) after finishing the printing task. Same as appending [>].")]
        [Alias("skip")]
        public BooleanParameter SkipAwaitInput;
        [Doc("Whether to not wait for the printed text to finish printing (revealing) before proceeding with the playback. Disables waiting for input as well.")]
        [Alias("nowait"), MutuallyExclusiveWith(nameof(SkipAwaitInput))]
        public BooleanParameter NoWait;

        public virtual Awaitable PreloadResources () => PreloadStaticTextResources(AuthorLabel);
        public virtual void ReleaseResources () => ReleaseStaticTextResources(AuthorLabel);

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            using var _ = await LoadDynamicTextResources(AuthorLabel);
        }
    }
}

using System;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Adds a [choice](/guide/choices) option to a choice handler with the specified ID (or the default one).  
Use instead of [@choice] to dynamically add choices and have more control over when (or whether) to halt the playback.",
        @"
When nesting commands under the choice, `goto`, `gosub` and `set` parameters are ignored.",
        @"
; A quick-time event: game over unless player selects a choice in 3 seconds.
Decide now![>]
@addChoice ""Turn left"" goto:Left
@addChoice ""Turn Right"" goto:Right
@wait 3
@clearChoice
You crashed!",
        @"
; Add a random choice, then halt the playback until player selects it.
@random
    @addChoice ""Top choice""
        You've selected the top choice!
    @addChoice ""Mediocre choice""
        You've selected a mediocre choice.
    @addChoice ""The worst choice""
        You've selected the worst possible choice...
@stop"
    )]
    [Serializable, BranchingGroup, Icon("Circle"), Branch(BranchTraits.Interactive | BranchTraits.Nest | BranchTraits.Endpoint)]
    public class AddChoice : Command, Command.ILocalizable, Command.IPreloadable, Command.INestedHost, Command.INavigator
    {
        [Doc("Text to show for the choice. When the text contains spaces, wrap it in double quotes (`\"`). " +
             "In case you wish to include the double quotes in the text itself, escape them.")]
        [Alias(NamelessParameterAlias)]
        public LocalizableTextParameter ChoiceSummary;
        [Doc("Unique identifier of the choice. Can be used to remove the choice later with [@clearChoice].")]
        public StringParameter Id;
        [Doc("Whether the choice should be disabled or otherwise not accessible for player to select; " +
             "see [choice docs](/guide/choices#locked-choice) for more info. Disabled by default.")]
        [ConditionContext, ParameterDefaultValue("false")]
        public StringParameter Lock;
        [Doc("Local resource path of the [button prefab](/guide/choices#choice-button) representing the choice. " +
             "The prefab should have a `ChoiceHandlerButton` component attached to the root object. " +
             "Will use a default button when not specified.")]
        [Alias("button"), ResourceContext(ChoiceHandlersConfiguration.DefaultButtonPathPrefix)]
        public StringParameter ButtonPath;
        [Doc("Local position of the choice button inside the choice handler (if supported by the handler implementation).")]
        [Alias("pos"), VectorContext("X,Y")]
        public DecimalListParameter ButtonPosition;
        [Doc("ID of the choice handler to add choice for. Will use a default handler if not specified.")]
        [Alias("handler"), ActorContext(ChoiceHandlersConfiguration.DefaultPathPrefix)]
        public StringParameter HandlerId;
        [Doc("Path to go when the choice is selected by user; see [@goto] command for the path format. " +
             "Ignored when nesting commands under the choice.")]
        [Alias("goto"), EndpointContext]
        public StringParameter GotoPath;
        [Doc("Path to a subroutine to go when the choice is selected by user; see [@gosub] command for the path format. " +
             "When `goto` is assigned this parameter will be ignored. Ignored when nesting commands under the choice.")]
        [Alias("gosub"), EndpointContext, MutuallyExclusiveWith(nameof(GotoPath))]
        public StringParameter GosubPath;
        [Doc("Set expression to execute when the choice is selected by user; see [@set] command for syntax reference. " +
             "Ignored when nesting commands under the choice.")]
        [Alias("set"), AssignmentContext]
        public StringParameter SetExpression;
        [Doc("Whether to also show choice handler the choice is added for; enabled by default.")]
        [Alias("show"), ParameterDefaultValue("true")]
        public BooleanParameter ShowHandler;
        [Doc("Duration (in seconds) of the fade-in (reveal) animation.")]
        [Alias("time"), ParameterDefaultValue("0.35")]
        public DecimalParameter Duration;

        string INavigator.ScriptPath => Assigned(GotoPath) ? GotoPath : GosubPath;
        bool INavigator.HoldResources => Assigned(GosubPath);
        bool INavigator.ReleaseResources => false;

        protected virtual IChoiceHandlerManager Handlers => Engine.GetServiceOrErr<IChoiceHandlerManager>();
        protected virtual IScriptManager Scripts => Engine.GetServiceOrErr<IScriptManager>();

        public virtual async Awaitable PreloadResources ()
        {
            await PreloadStaticTextResources(ChoiceSummary);

            if (Assigned(HandlerId) && !HandlerId.DynamicValue)
            {
                var handlerId = Assigned(HandlerId) ? HandlerId.Value : Handlers.DefaultHandlerId;
                await Handlers.GetOrAddActor(handlerId);
            }

            if (Assigned(ButtonPath) && !ButtonPath.DynamicValue)
                await Handlers.ChoiceButtonLoader.Load(ButtonPath, this);
        }

        public virtual void ReleaseResources ()
        {
            ReleaseStaticTextResources(ChoiceSummary);

            if (Assigned(ButtonPath) && !ButtonPath.DynamicValue)
                Handlers.ChoiceButtonLoader.Release(ButtonPath, this);
        }

        public override int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (!playlist.HasNested(this)) return base.GetNextPlaybackIndex(playlist, playedIndex);

            if (playlist.IsEnteringNestedAt(playedIndex))
                // Always skip the nested callback; it's executed when (if) the choice is picked by the player.
                return playlist.SkipNestedAt(playedIndex, Indent);

            if (!playlist.IsExitingNestedAt(playedIndex, Indent))
                return playedIndex + 1;

            // Exiting the block: navigate to the spot which was assigned to continue playback when the choice was picked.
            var continueAt = Handlers.PopSelectedChoice(PlaybackSpot);
            if (!continueAt.Valid) return -1; // No further navigation required, the choice callback block finishes with nothing.
            if (continueAt.ScriptPath != playlist.ScriptPath)
                throw Fail("Choice callback from another script is not supported.");
            return playlist.IndexOf(continueAt);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            if (Assigned(ButtonPath) && ButtonPath.DynamicValue)
                await Handlers.ChoiceButtonLoader.LoadOrErr(ButtonPath);
            using var _ = await LoadDynamicTextResources(ChoiceSummary);
            var handler = await GetOrAddHandler(ctx.Token);
            handler.AddChoice(CreateChoice(ctx.Track));
            if (ShouldShow(handler)) Show(handler, ctx.Token).Forget();
        }

        protected virtual async Awaitable<IChoiceHandlerActor> GetOrAddHandler (AsyncToken token)
        {
            var handlerId = Assigned(HandlerId) ? HandlerId.Value : Handlers.DefaultHandlerId;
            var handler = await Handlers.GetOrAddActor(handlerId);
            token.ThrowIfCanceled();
            return handler;
        }

        protected virtual Awaitable Show (IChoiceHandlerActor handler, AsyncToken token)
        {
            var duration = Assigned(Duration) ? Duration.Value : Handlers.Configuration.DefaultDuration;
            return handler.ChangeVisibility(true, new(duration), token: token);
        }

        protected virtual Choice CreateChoice (IScriptTrack track)
        {
            var options = new ChoiceOptions {
                Id = GetAssignedOrDefault(Id, default(string)),
                Summary = ChoiceSummary,
                Lock = ShouldLock(),
                ButtonPath = ButtonPath,
                ButtonPosition = Assigned(ButtonPosition) ? ArrayUtils.ToVector2(ButtonPosition) : null
            };
            return track.IsEnteringNested()
                ? new(CreateNestedCallback(track), options)
                : new(CreateDirectiveCallback(track), options);
        }

        protected virtual DirectiveChoiceCallback CreateDirectiveCallback (IScriptTrack track)
        {
            return new() {
                Set = SetExpression,
                Goto = Assigned(GotoPath) ? ToCanonicalPath(GotoPath) : null,
                Gosub = Assigned(GosubPath) ? ToCanonicalPath(GosubPath) : null,
                TrackId = track.Id
            };
        }

        protected virtual NestedChoiceCallback CreateNestedCallback (IScriptTrack track)
        {
            if (Assigned(GotoPath) || Assigned(GosubPath) || Assigned(SetExpression))
                Warn("Using goto, gosub and set parameters with nested choice callback is not supported.");
            return new() { HostedAt = PlaybackSpot, TrackId = track.Id };
        }

        protected virtual string ToCanonicalPath (string syntax)
        {
            if (!Scripts.TryResolveEndpoint(syntax, PlaybackSpot.ScriptPath, out var endpoint))
                Err($"Invalid endpoint syntax: {syntax}");
            return Scripts.SerializeEndpoint(endpoint, PlaybackSpot.ScriptPath);
        }

        protected virtual bool ShouldLock ()
        {
            if (!Assigned(Lock)) return false;
            return ExpressionEvaluator.Evaluate<bool>(Lock, new() { OnError = Err, Parameter = new(Lock, 0) });
        }

        protected virtual bool ShouldShow (IChoiceHandlerActor handler)
        {
            if (handler.Visible) return false;
            return GetAssignedOrDefault(ShowHandler, true);
        }
    }
}

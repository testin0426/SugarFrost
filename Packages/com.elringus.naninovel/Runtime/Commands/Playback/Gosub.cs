using System;
using System.Diagnostics.CodeAnalysis;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Navigates scenario script playback to the specified path and saves that path to global state;
[@return] commands use this info to redirect to command after the last invoked gosub command.",
        @"
While this command can be used as a function (subroutine) to invoke a common set of script lines,
remember that NaniScript is a scenario scripting DSL and is not suited for general programming.
It's strongly recommended to use [custom commands](/guide/custom-commands) instead.",
        @"
; Navigate to 'VictoryScene' label in the currently played script, then
; execute the commands and navigate back to the command after the 'gosub'.
@gosub #VictoryScene
...
@stop
# VictoryScene
@back Victory
@sfx Fireworks
@bgm Fanfares
You are victorious!
@return",
        @"
; Another example with some branching inside the subroutine.
@set time=10
; Here we get one result.
@gosub #Room
...
@set time=3
; And here we get another.
@gosub #Room
@stop
# Room
@print ""It's too early, I should visit after sunset."" if:time<21&time>6
@print ""I can sense an ominous presence!"" if:time>21|time<6
@return"
    )]
    [Serializable, PlaybackGroup, Icon("ArrowsRepeatDuo"), Branch(BranchTraits.Endpoint | BranchTraits.Return)]
    public class Gosub : Command, Command.INavigator
    {
        [Doc("Path to navigate into in the following format: `ScriptPath#Label`. " +
             "When label is omitted, will play specified script from the start. " +
             "When script path is omitted, will attempt to find a label in the currently played script.")]
        [Alias(NamelessParameterAlias), RequiredParameter, EndpointContext]
        public StringParameter Path;

        string INavigator.ScriptPath => Path;
        bool INavigator.HoldResources => true;
        bool INavigator.ReleaseResources => false;

        protected virtual IScriptManager Scripts => Engine.GetServiceOrErr<IScriptManager>();
        protected virtual ResourcePolicy Policy => Engine.GetConfiguration<ResourceProviderConfiguration>().ResourcePolicy;

        public override Awaitable Execute (ExecutionContext ctx)
        {
            PushReturnSpot(ctx.Track);
            if (!TryGetScriptPathAndLabel(ctx.Track, out var scriptPath, out var label)) return Async.Completed;
            if (ShouldNavigatePlayedScript(ctx.Track, scriptPath)) NavigatePlayedScript(ctx.Track, label);
            else if (string.IsNullOrEmpty(label)) return ctx.Track.LoadAndPlay(scriptPath);
            else return ctx.Track.LoadAndPlayAtLabel(scriptPath, label);
            return Async.Completed;
        }

        protected virtual void PushReturnSpot (IScriptTrack track)
        {
            var returnIndex = track.Playlist.MoveAt(track.PlayedIndex);
            if (returnIndex == -1) track.GosubReturnSpots.Push(PlaybackSpot.Invalid);
            else track.GosubReturnSpots.Push(track.Playlist[returnIndex].PlaybackSpot);
        }

        protected virtual bool TryGetScriptPathAndLabel (IScriptTrack track, out string scriptPath, [MaybeNull] out string label)
        {
            if (!Scripts.TryResolveEndpoint(Path.Value, PlaybackSpot.ScriptPath, out var endpoint))
                Err($"Failed to execute '@gosub' command: endpoint syntax is not valid: {Path.Value}");
            scriptPath = endpoint.ScriptPath;
            label = endpoint.Label;
            var valid = !string.IsNullOrWhiteSpace(scriptPath) || track.PlayedScript;
            if (!valid) Err("Failed to execute '@gosub' command: script path is not specified and no script is currently played.");
            return valid;
        }

        protected virtual bool ShouldNavigatePlayedScript (IScriptTrack track, string scriptPath)
        {
            if (Policy == ResourcePolicy.Lazy) return false; // lazy drops inter-script resources, so always load
            return string.IsNullOrWhiteSpace(scriptPath) ||
                   track.PlayedScript && scriptPath.EqualsIgnoreCase(track.PlayedScript.Path);
        }

        protected virtual void NavigatePlayedScript (IScriptTrack track, [MaybeNull] string label)
        {
            if (string.IsNullOrEmpty(label)) track.Resume();
            else if (track.PlayedScript.LabelExists(label)) track.ResumeAtLabel(label);
            else Err($"Failed navigating script playback to '{label}' label: label not found in '{track.PlayedScript.Path}' script.");
        }
    }
}

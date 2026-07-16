using System;
using System.Linq;
using JetBrains.Annotations;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Automatically save the game to the first auto save slot.",
        null,
        @"
; Auto-save at the current position.
@save",
        @"
; Player can choose to either 'rest', which will auto-save the game and
; exit to title or continue to 'NextDay'. When player loads the saved game
; after resting, they're moved to the line after '# Camp' label, with
; 'rested' set to 'true', which forces them to continue to the 'NextDay'.

# Camp

; Notice the variable is set with '?=' – this will only assign the value
; in case it's not already assigned, which won't be the case after player
; loads auto-saved game after the rest.
@set rested?=false

@if rested
    Good morning! We have to go now.
    @goto NextDay

@choice ""No time to rest!"" goto:NextDay
@choice ""Let's rest a bit""
    @set rested=true
    ; Notice the 'at' parameter – it'll redirect the player to the
    ; specified label when the game is loaded.
    @save at:#Camp
    @title"
    )]
    [Serializable, Alias("save"), UIGroup, Icon("FloppyDisk"), Branch(BranchTraits.Endpoint)]
    public class AutoSave : Command, Command.IPreloadable
    {
        [Doc("Playback position of the save in the following format: `ScriptPath#Label`. " +
             "When omitted, uses the current player position. " +
             "Can be used to redirect the player to a specific label or script after the game is loaded.")]
        [EndpointContext]
        public StringParameter At;

        protected virtual IStateManager State => Engine.GetServiceOrErr<IStateManager>();
        protected virtual IScriptManager Scripts => Engine.GetServiceOrErr<IScriptManager>();

        public virtual async Awaitable PreloadResources ()
        {
            if (GetAtScriptPath() is { } script && script != PlaybackSpot.ScriptPath)
                await Scripts.ScriptLoader.Load(script, this);
        }

        public virtual void ReleaseResources ()
        {
            if (GetAtScriptPath() is { } script && script != PlaybackSpot.ScriptPath)
                Scripts.ScriptLoader.Release(script, this);
        }

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var trackId = ctx.Track.Id;

            if (GetAtScriptPath() is { } atScriptPath)
            {
                await Scripts.ScriptLoader.Load(atScriptPath);
                State.AddOnGameSerializeTask(OverridePlaybackSpot);
            }

            await State.AutoSave();

            void OverridePlaybackSpot (GameStateMap map)
            {
                State.RemoveOnGameSerializeTask(OverridePlaybackSpot);
                var end = Scripts.ResolveEndpointOrErr(At, PlaybackSpot.ScriptPath);
                var script = Scripts.ScriptLoader.GetLoadedOrErr(end.ScriptPath);
                var lineIdx = end.HasLabel ? script.GetLineIndexForLabel(end.Label) : 0;
                map.PlaybackSpot = new(end.ScriptPath, lineIdx, 0);
                if (map.GetState<ScriptPlayer.GameState>()?.Tracks?.FirstOrDefault(t => t.Id.EqualsOrdinal(trackId)) is { } trackState)
                    trackState.ExecutedPlayedCommand = false;
            }
        }

        [CanBeNull]
        protected virtual string GetAtScriptPath ()
        {
            if (!Assigned(At)) return null;
            return Scripts.ResolveEndpointOrErr(At, PlaybackSpot.ScriptPath).ScriptPath;
        }
    }
}

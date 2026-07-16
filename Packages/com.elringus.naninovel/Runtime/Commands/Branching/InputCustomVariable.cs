using System;
using UnityEngine;

namespace Naninovel.Commands
{
    [Doc(
        @"
Shows an input field UI where user can enter an arbitrary text.
Upon submit the entered text will be assigned to the specified custom variable.",
        @"
To assign a display name for a character using this command consider [binding the name to a custom variable](/guide/characters#display-names).",
        @"
; Prompt to enter an arbitrary text and assign it to 'name' custom variable.
@input name summary:""Choose your name.""

; You can then inject the assigned 'name' variable in scenario scripts.
Archibald: Greetings, {name}!

; ...or use it inside set and conditional expressions.
@set score++ if:name=""Felix"""
    )]
    [Serializable, Alias("input"), BranchingGroup, Icon("PenFieldDuo")]
    public class InputCustomVariable : Command, Command.IPreloadable, Command.ILocalizable
    {
        [Doc("Name of a custom variable to which the entered text will be assigned.")]
        [Alias(NamelessParameterAlias), RequiredParameter]
        public StringParameter VariableName;
        [Doc("Type of the input content; defaults to the specified variable type. " +
             "Use to change assigned variable type or when assigning to a new variable. " +
             "Supported types: `String`, `Numeric`, `Boolean`.")]
        [Alias("type"), ConstantContext(typeof(CustomVariableValueType))]
        public StringParameter VariableType;
        [Doc("An optional summary text to show along with input field. " +
             "When the text contains spaces, wrap it in double quotes (`\"`). " +
             "In case you wish to include the double quotes in the text itself, escape them.")]
        public LocalizableTextParameter Summary;
        [Doc("A predefined value to set for the input field. When not assigned will pull existing value of the assigned variable (if any).")]
        [Alias("value")]
        public LocalizableTextParameter PredefinedValue;
        [Doc("Whether to not halt script playback until the input is submitted by the player.")]
        [Alias("nostop"), ParameterDefaultValue("false")]
        public BooleanParameter SkipPlayerStop;

        public virtual Awaitable PreloadResources () => PreloadStaticTextResources(Summary, PredefinedValue);
        public virtual void ReleaseResources () => ReleaseStaticTextResources(Summary, PredefinedValue);

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            using var _ = await LoadDynamicTextResources(Summary, PredefinedValue);

            var inputUI = Engine.GetServiceOrErr<IUIManager>().GetUI<UI.IVariableInputUI>();
            var stop = !GetAssignedOrDefault(SkipPlayerStop, false);
            if (stop && !IsNextCommandStop(ctx.Track)) ctx.Track.Stop();

            inputUI?.Show(VariableName, new() {
                Summary = Summary,
                PredefinedValue = Assigned(PredefinedValue) ? PredefinedValue : LocalizableText.Empty,
                ValueType = Assigned(VariableType) ? ParseValueType() : null,
                ResumeTrackId = stop ? ctx.Track.Id : null
            });
        }

        protected virtual CustomVariableValueType ParseValueType ()
        {
            if (!ParseUtils.TryConstantParameter(VariableType, out CustomVariableValueType type))
                Warn($"Failed to parse '{VariableType}' enum.");
            return type;
        }

        protected virtual bool IsNextCommandStop (IScriptTrack track)
        {
            // Required for backward compatibility when @stop was required after @input.
            // In which case don't stop the playback here, as it'll be stopped in the next played command.
            var nextIdx = track.Playlist.MoveAt(track.PlayedIndex);
            return track.Playlist.GetCommandByIndex(nextIdx) is Stop stop && !Assigned(stop.TrackId);
        }
    }
}

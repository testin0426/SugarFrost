// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Naninovel.Metadata;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a <see cref="Script"/> command.
    /// </summary>
    [Serializable]
    public abstract class Command
    {
        /// <summary>
        /// Implementing <see cref="Command"/> will be included in localization scripts.
        /// </summary>
        public interface ILocalizable { }

        /// <summary>
        /// Implementing <see cref="Command"/> is able to preload resources it uses.
        /// </summary>
        public interface IPreloadable
        {
            /// <summary>
            /// Preloads the resources used by the command; invoked by <see cref="IScriptLoader"/>
            /// when preloading the script resources, in accordance with <see cref="ResourcePolicy"/>.
            /// </summary>
            /// <remarks>
            /// Make sure to only preload resources associated with static parameter values, as
            /// dynamic values may change at any point while the preloaded script is playing.
            /// Resources associated with the dynamic values have to be loaded just before executing
            /// the command, using the resolved value, which is propagated to the underlying consumers.
            /// </remarks>
            Awaitable PreloadResources ();
            /// <summary>
            /// Releases the preloaded resources used by the command.
            /// </summary>
            void ReleaseResources ();
        }

        /// <summary>
        /// Implementing <see cref="Command"/> hosts an underlying block of commands associated with it
        /// via indentation. Host command is able to control the execution flow of the nested commands,
        /// ie <see cref="Command.GetNextPlaybackIndex"/> of the host can override children's.
        /// </summary>
        public interface INestedHost { }

        /// <summary>
        /// Implementing <see cref="Command"/> navigates to another scenario script
        /// or to a label in the same script.
        /// </summary>
        /// <remarks>
        /// Implement this interface for all commands that may navigate the playback,
        /// so that <see cref="IScriptLoader"/> can resolve the dependencies.
        /// </remarks>
        public interface INavigator
        {
            /// <summary>
            /// The target script <see cref="Endpoint"/> path syntax.
            /// Null means the command doesn't cause the navigation in its current configuration.
            /// </summary>
            [CanBeNull] string ScriptPath { get; }
            /// <summary>
            /// Whether the resources of the target script have to be preloaded when the host script is loaded.
            /// </summary>
            bool HoldResources { get; }
            /// <summary>
            /// Whether the resources of the host script have to be unloaded on navigation.
            /// </summary>
            bool ReleaseResources { get; }
        }

        /// <summary>
        /// Assigns an alias name for <see cref="Command"/> class or <see cref="CommandParameter"/> field.
        /// Aliases can be used instead of the IDs (command type names or parameter field names)
        /// to reference commands or parameters in the scenario scripts.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, Inherited = false)]
        public sealed class AliasAttribute : Attribute
        {
            public string Alias { get; }

            public AliasAttribute (string alias)
            {
                Alias = alias;
            }
        }

        /// <summary>
        /// Assigns a category for <see cref="Command"/> organization purposes in web editor.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public sealed class GroupAttribute : Attribute
        {
            public string GroupId { get; }

            public GroupAttribute (string groupId)
            {
                GroupId = groupId;
            }
        }

        /// <summary>
        /// Signals that the command can only be inlined inside generic lines and
        /// can't be used as a standalone command line. The command won't appear
        /// in completion lists for command lines in the IDE extension and the standalone editor.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public sealed class InlineOnlyAttribute : Attribute { }

        // @formatter:off
        /// <inheritdoc cref="CommandGroups.Actors"/>
        [Conditional("UNITY_EDITOR")] public class ActorsGroupAttribute : Attribute { }
        /// <inheritdoc cref="CommandGroups.Text"/>
        [Conditional("UNITY_EDITOR")] public class TextGroupAttribute : Attribute { }
        /// <inheritdoc cref="CommandGroups.Playback"/>
        [Conditional("UNITY_EDITOR")] public class PlaybackGroupAttribute : Attribute { }
        /// <inheritdoc cref="CommandGroups.Branching"/>
        [Conditional("UNITY_EDITOR")] public class BranchingGroupAttribute : Attribute { }
        /// <inheritdoc cref="CommandGroups.Visuals"/>
        [Conditional("UNITY_EDITOR")] public class VisualsGroupAttribute : Attribute { }
        /// <inheritdoc cref="CommandGroups.Audio"/>
        [Conditional("UNITY_EDITOR")] public class AudioGroupAttribute : Attribute { }
        /// <inheritdoc cref="CommandGroups.UI"/>
        [Conditional("UNITY_EDITOR")] public class UIGroupAttribute : Attribute { }
        // @formatter:on

        /// <summary>
        /// Assigns an icon for <see cref="Command"/> organization purposes in web editor.
        /// Can optionally end with :color.
        /// </summary>
        /// <example>CameraMovie:Red</example>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Class, Inherited = false)]
        public sealed class IconAttribute : Attribute
        {
            public string Icon { get; }

            public IconAttribute (string icon = null)
            {
                Icon = icon;
            }
        }

        /// <summary>
        /// When applied to a command parameter or command class, Naninovel won't generate metadata for it.
        /// Used to ignore parameters and commands by external tools (IDE extension, web editor),
        /// for example, to prevent them from being added to the auto-complete lists.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
        public sealed class IgnoreAttribute : Attribute
        {
            public readonly string ParameterId;

            /// <param name="paramId">
            /// When the attribute is applied to a class, specifies parameter field name.
            /// When applied to a class and parameter not specified, will ignore the whole command.
            /// </param>
            public IgnoreAttribute (string paramId = null)
            {
                ParameterId = paramId;
            }
        }

        /// <summary>
        /// Registers the field as a required <see cref="ICommandParameter"/> logging error
        /// when it's not supplied in naninovel scripts.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        public sealed class RequiredParameterAttribute : Attribute { }

        /// <summary>
        /// Associates a default value with the <see cref="ICommandParameter"/> field.
        /// Intended for external tools to access metadata; ignored at runtime.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Field)]
        public sealed class ParameterDefaultValueAttribute : Attribute
        {
            public string Value { get; }

            public ParameterDefaultValueAttribute (string value)
            {
                Value = value;
            }
        }

        /// <summary>
        /// Hints the authoring tools that the parameter will be ignored in case
        /// a parameter with the specified ID is also assigned.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
        public sealed class MutuallyExclusiveWithAttribute : Attribute
        {
            /// <summary>
            /// ID of the parameter (C# field name) that is mutually exclusive with the attributed parameter.
            /// </summary>
            public string ParameterId { get; }

            public MutuallyExclusiveWithAttribute (string parameterId)
            {
                ParameterId = parameterId;
            }
        }

        /// <summary>
        /// Namespace for all the built-in commands implementations.
        /// </summary>
        public const string DefaultNamespace = "Naninovel.Commands";
        /// <summary>
        /// Use this alias to specify a nameless command parameter.
        /// </summary>
        public const string NamelessParameterAlias = "";
        /// <summary>
        /// Contains all the available <see cref="Command"/> types in the application domain, 
        /// indexed by command alias (if available) or implementing type name. Keys are case-insensitive.
        /// </summary>
        public static LiteralMap<Type> CommandTypes => commandTypesCache ??= GetCommandTypes();

        /// <summary>
        /// In case the command belongs to a <see cref="Script"/> asset, represents position inside the script.
        /// </summary>
        public PlaybackSpot PlaybackSpot { get => playbackSpot; set => playbackSpot = value; }
        /// <summary>
        /// Indentation level of the line to which this command belongs.
        /// </summary>
        public int Indent { get => indent; set => indent = value; }
        /// <summary>
        /// Whether this command should be executed, as per <see cref="ConditionalExpression"/> or
        /// <see cref="InvertedConditionalExpression"/>.
        /// </summary>
        public virtual bool ShouldExecute =>
            !Assigned(ConditionalExpression) && !Assigned(InvertedConditionalExpression) ||
            Assigned(ConditionalExpression) && ExpressionEvaluator.Evaluate<bool>(ConditionalExpression,
                new() { OnError = Err, Parameter = new(ConditionalExpression, 0) }) ||
            Assigned(InvertedConditionalExpression) && !ExpressionEvaluator.Evaluate<bool>(InvertedConditionalExpression,
                new() { OnError = Err, Parameter = new(InvertedConditionalExpression, 0) });

        [Doc("A boolean [script expression](/guide/script-expressions), controlling whether this command should execute.")]
        [Alias("if"), ConditionContext]
        public StringParameter ConditionalExpression;
        [Doc("A boolean [script expression](/guide/script-expressions), controlling whether this command should NOT execute (the inverse of 'if').")]
        [Alias("unless"), ConditionContext(inverted: true), MutuallyExclusiveWith(nameof(ConditionalExpression))]
        public StringParameter InvertedConditionalExpression;

        [SerializeField] private PlaybackSpot playbackSpot = PlaybackSpot.Invalid;
        [SerializeField] private int indent;

        private static LiteralMap<Type> commandTypesCache;

        /// <summary>
        /// Attempts to find a <see cref="Command"/> type based on the specified command alias or type name.
        /// </summary>
        public static Type ResolveCommandType (string commandId)
        {
            if (string.IsNullOrEmpty(commandId)) return null;
            // First, try to resolve by key.
            CommandTypes.TryGetValue(commandId, out var result);
            // If not found, look by type name (in case a type name was requested for a command with a defined alias).
            return result ?? CommandTypes.Values.FirstOrDefault(cmdType => cmdType.Name.EqualsIgnoreCase(commandId));
        }

        /// <summary>
        /// Given specified playlist contains the command and is being executed at the specified index,
        /// returns the next index inside the list to execute. -1 indicates no further navigation is required,
        /// ie the script playback is finished.
        /// </summary>
        public virtual int GetNextPlaybackIndex (ScriptPlaylist playlist, int playedIndex)
        {
            if (this is INestedHost && playlist.IsExitingNestedAt(playedIndex, Indent))
                return playlist.ExitNestedAt(playedIndex, Indent);
            return playedIndex + 1;
        }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="ctx">Playback context associated with <see cref="IScriptTrack"/> executing the command.</param>
        public virtual Awaitable Execute (ExecutionContext ctx) => Async.Completed;

        /// <summary>
        /// Logs an informational message to the console; will include the script path and line number of the command.
        /// </summary>
        public virtual void Log (string message) => Engine.Log(message, PlaybackSpot);

        /// <summary>
        /// Logs a warning to the console; will include the script path and line number of the command.
        /// </summary>
        public virtual void Warn (string message) => Engine.Warn(message, PlaybackSpot);

        /// <summary>
        /// Logs an error to the console; will include the script path and line number of the command.
        /// </summary>
        public virtual void Err (string message) => Engine.Err(message, PlaybackSpot);

        /// <summary>
        /// Creates an exception with the specified message annotated with the script path and line number of the command.
        /// </summary>
        public virtual Error Fail (string message) => Engine.Fail(message, PlaybackSpot);

        /// <summary>
        /// Whether the specified parameter has a value assigned.
        /// </summary>
        public static bool Assigned (ICommandParameter parameter) => parameter is not null && parameter.HasValue;

        /// <summary>
        /// Whether the specified parameter has a value assigned and is dynamic (value may change at runtime).
        /// </summary>
        /// <remarks>
        /// Resources associated with dynamic parameters can't be resolved when preloading a script, as their value may change
        /// while the script is playing; resources of such parameters have to be loaded just before the command is executed.
        /// </remarks>
        public static bool AssignedDynamic (ICommandParameter parameter) => Assigned(parameter) && parameter.DynamicValue;

        /// <summary>
        /// Whether the specified parameter has a value assigned and is static (value is immutable at runtime).
        /// </summary>
        /// <remarks>
        /// Resources associated with static parameters can be resolved when preloading a script, as their value won't change
        /// while the script is playing.
        /// </remarks>
        public static bool AssignedStatic (ICommandParameter parameter) => Assigned(parameter) && !parameter.DynamicValue;

        /// <summary>
        /// Returns the specified command parameter value if it is assigned; otherwise, returns the specified default value.
        /// </summary>
        public static TValue GetAssignedOrDefault<TParam, TValue> (TParam param, TValue defaultValue)
            where TParam : CommandParameter<TValue> => Assigned(param) ? param : defaultValue;

        /// <summary>
        /// Preloads the resources associated with the specified static localizable text parameters.
        /// </summary>
        protected virtual async Awaitable PreloadStaticTextResources (params LocalizableTextParameter[] text)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var t in text)
                if (AssignedStatic(t))
                    tasks.Add(t.Value.Load(this));
            await Async.All(tasks);
        }

        /// <summary>
        /// Releases the preloaded resources associated with the specified static localizable text parameters.
        /// </summary>
        protected virtual void ReleaseStaticTextResources (params LocalizableTextParameter[] text)
        {
            foreach (var t in text)
                if (AssignedStatic(t))
                    t.Value.Release(this);
        }

        /// <summary>
        /// Loads the resources associated with the specified dynamic localizable text parameters;
        /// dispose returned instance to release the resources.
        /// </summary>
        /// <remarks>
        /// It's safe to dispose the returned text holder on <see cref="Execute"/> context exit,
        /// as at that point the text is expected to be held by the underlying consumers, when necessary.
        /// </remarks>
        protected virtual async Awaitable<IDisposable> LoadDynamicTextResources (params LocalizableTextParameter[] text)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var t in text)
                if (AssignedDynamic(t))
                    tasks.Add(t.Value.Load(this));
            await Async.All(tasks);
            return Defer.With((text, cmd: this), static s => {
                foreach (var t in s.text)
                    if (AssignedDynamic(t))
                        t.Value.Release(s.cmd);
            });
        }

        /// <summary>
        /// Loads the resources associated with the specified dynamic localizable text parameters;
        /// dispose returned instance to release the resources.
        /// </summary>
        protected virtual async Awaitable<IDisposable> LoadDynamicTextResources
            (params (LocalizableTextParameter Parameter, LocalizableText ResolvedValue)[] text)
        {
            using var _ = Async.Rent(out var tasks);
            foreach (var (Parameter, ResolvedValue) in text)
                if (AssignedDynamic(Parameter))
                    tasks.Add(ResolvedValue.Load(this));
            await Async.All(tasks);
            return Defer.With((text, cmd: this), static s => {
                foreach (var t in s.text)
                    if (AssignedDynamic(t.Parameter))
                        t.ResolvedValue.Release(s.cmd);
            });
        }

        /// <summary>
        /// Executes specified function and either awaits or forgets the async result depending on the specified
        /// wait parameter or (when not assigned) <see cref="ScriptPlayerConfiguration.WaitByDefault"/> configuration.
        /// </summary>
        /// <remarks>
        /// This method should be used in most cases to wrap the async command implementation logic, as it handles
        /// all the quirks of the async playback, such as merging continue input events to the completion token in case
        /// <see cref="ScriptPlayerConfiguration.CompleteOnContinue"/> is enabled and registering the task with
        /// <see cref="IScriptPlayer.RegisterCompletion"/> when the command is not awaited.
        /// </remarks>
        /// <param name="fn">The async function that results is the subject of this method.</param>
        /// <param name="wait">Command parameter controlling whether the function result has to be awaited.</param>
        /// <param name="ctx">Command execution context as specified in <see cref="Execute"/>.</param>
        protected virtual async Awaitable WaitOrForget (Func<ExecutionContext, Awaitable> fn,
            BooleanParameter wait, ExecutionContext ctx)
        {
            if (Assigned(wait) ? wait.Value : Engine.GetConfiguration<ScriptPlayerConfiguration>().WaitByDefault)
            {
                using var _ = CompleteOnContinueWhenEnabled(ref ctx);
                await fn(ctx);
            }
            else ctx.Track.RegisterCompletion(this, fn(ctx));
        }

        /// <summary>
        /// Invokes <see cref="CompleteOnContinue"/> when allowed by the script track.
        /// Make sure to dispose of the returned object to prevent memory leaks.
        /// </summary>
        protected virtual IDisposable CompleteOnContinueWhenEnabled (ref ExecutionContext ctx)
        {
            if (ctx.Track.CompleteOnContinue)
                return CompleteOnContinue(ref ctx);
            return new DeferNoop();
        }

        /// <summary>
        /// Merges continue input events to the completion token of the specified async token (in-place),
        /// so that <see cref="AsyncToken.Completed"/> triggers when player activates continue or skip inputs.
        /// Make sure to dispose of the returned object to prevent memory leaks.
        /// </summary>
        protected virtual IDisposable CompleteOnContinue (ref ExecutionContext ctx)
        {
            var input = Engine.GetServiceOrErr<IInputManager>();
            if (input.GetContinue() is not { } continueInput) return new DeferNoop();
            var continueInputCT = continueInput.GetNext();
            var skipInputCT = input.GetSkip()?.GetNext() ?? default;
            var toggleSkipInputCT = input.GetToggleSkip()?.GetNext() ?? default;
            var completionAndContinueCTS = CancellationTokenSource.CreateLinkedTokenSource(
                ctx.Token.CompletionToken, continueInputCT, skipInputCT, toggleSkipInputCT);
            ctx = new(ctx.Track, new(ctx.Token.CancellationToken, completionAndContinueCTS.Token));
            return completionAndContinueCTS;
        }

        private static LiteralMap<Type> GetCommandTypes ()
        {
            var result = new LiteralMap<Type>();
            var commandTypes = Engine.Types.Commands
                // Put built-in commands first, so they're overridden by custom commands with the same aliases.
                .OrderByDescending(type => type.Namespace == DefaultNamespace);
            foreach (var commandType in commandTypes)
            {
                if (Compiler.Commands.TryGetValue(commandType.Name, out var locale) &&
                    !string.IsNullOrWhiteSpace(locale.Alias))
                {
                    result[locale.Alias] = commandType;
                    continue;
                }

                var commandKey = commandType.GetCustomAttributes(typeof(AliasAttribute), false)
                    .FirstOrDefault() is AliasAttribute tagAttribute && !string.IsNullOrEmpty(tagAttribute.Alias)
                    ? tagAttribute.Alias
                    : commandType.Name;
                result[commandKey] = commandType;
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptTrack"/>
    public class ScriptTrack : IScriptTrack, IDisposable
    {
        [Serializable]
        public class GameState
        {
            public string Id;
            public bool Playing;
            public bool ExecutedPlayedCommand;
            public bool AwaitingInput;
            public PlaybackSpot PlaybackSpot;
            public List<PlaybackSpot> GosubReturnSpots;
            public PlaybackOptions Options;
        }

        public class EventHooks
        {
            public Action<IScriptTrack> OnPlay;
            public Action<IScriptTrack> OnStop;
            public Action<IScriptTrack> OnExecute;
            public Action<IScriptTrack> OnExecuted;
            public Action<IScriptTrack> OnAwaitInput;
            public IReadOnlyList<Func<IScriptTrack, Awaitable>> PreExeTasks;
            public IReadOnlyList<Func<IScriptTrack, Awaitable>> PostExeTasks;
        }

        public virtual string Id { get; }
        public virtual bool Playing => !PlayRoutineSrc.Completed;
        public virtual bool Executing => ExecutedCommands.Count > 0;
        public virtual bool Completing => !CompleteSrc.Completed;
        public virtual bool CompleteOnContinue { get; }
        public virtual bool AwaitingInput { get; private set; }
        public virtual PlayerSkipMode SkipMode { get; set; }
        public virtual Script PlayedScript { get; set; }
        public virtual Command PlayedCommand => Playlist?.GetCommandByIndex(PlayedIndex);
        public virtual PlaybackSpot PlaybackSpot => PlayedCommand?.PlaybackSpot ?? default;
        public virtual ScriptPlaylist Playlist => PlayedScript?.Playlist;
        public virtual int PlayedIndex { get; private set; }
        public virtual Stack<PlaybackSpot> GosubReturnSpots { get; private set; } = new();

        protected virtual IStateManager State { get; }
        protected virtual IScriptPlayer Player { get; }
        protected virtual IScriptManager Scripts { get; }
        protected virtual EventHooks Hooks { get; }
        protected virtual PlayedScriptRegister Played { get; }
        protected virtual PlaybackOptions Options { get; }
        protected virtual bool SkipAllowed { get; }
        protected virtual bool AutoPlayAllowed { get; }
        protected virtual bool AwaitInputAllowed { get; }
        protected virtual bool Skipping { get; private set; }
        protected virtual bool AutoPlaying { get; private set; }
        protected virtual bool ExecutedPlayedCommand { get; private set; }
        protected virtual bool ShouldCompletePlayedCommand { get; private set; }
        protected virtual bool Disposing { get; private set; }
        [CanBeNull] protected virtual IInputHandle ContinueInput { get; }

        /// <summary>
        /// Completion CTSs of all the currently executed commands, including un-awaited, mapped by the command's
        /// playback spot. The individual CTS allows requesting ASAP completion of the associated command without
        /// affecting others. 
        /// </summary>
        /// <remarks>
        /// Don't use Command instance as keys, because some commands (eg, <see cref="Commands.SpawnEffect"/>)
        /// create other command instances under the hood and execute them, in which case <see cref="ExecutedCommands"/>
        /// and <see cref="AsyncExecutedCommands"/> won't have matching keys when the other command is using
        /// <see cref="RegisterCompletion"/>.
        /// </remarks>
        protected virtual Dictionary<PlaybackSpot, (Command, CancellationTokenSource)> ExecutedCommands { get; } = new();
        /// <summary>
        /// Execution tasks of all the currently executed un-awaited commands registered with
        /// <see cref="RegisterCompletion"/>, mapped by the command's playback spots. The convention is that the
        /// command will register a task which signals its actual completion at <see cref="Command.Execute"/>
        /// in case it doesn't desire to be awaited.
        /// </summary>
        protected virtual Dictionary<PlaybackSpot, (Command, Awaitable)> AsyncExecutedCommands { get; } = new();
        /// <summary>
        /// Allows halting the playback routine (doesn't affect individual command execution).
        /// </summary>
        protected virtual AsyncSource PlayRoutineSrc { get; } = new(completed: true);
        /// <summary>
        /// Allows cancelling all the executed commands with <see cref="CancelCommands"/>.
        /// </summary>
        protected virtual AsyncSource CommandExecutionSrc { get; } = new();
        /// <summary>
        /// Allows waiting until <see cref="SetAwaitInput"/> is invoked with 'false' argument.
        /// </summary>
        protected virtual AsyncSource AwaitInputDisabledSrc { get; } = new(completed: true);
        /// <summary>
        /// Allows waiting until <see cref="Complete"/> is finished.
        /// </summary>
        protected virtual AsyncSource CompleteSrc { get; } = new(completed: true);
        /// <summary>
        /// The on-complete tasks registered with <see cref="Complete"/>.
        /// </summary>
        protected virtual Queue<Func<Awaitable>> OnCompleteTasks { get; } = new();

        public ScriptTrack (string id, PlaybackOptions options, PlayedScriptRegister played, EventHooks hooks,
            IScriptPlayer player, IInputManager input, IScriptManager scripts, IStateManager state)
        {
            Id = id;
            Options = options;
            Played = played;
            Hooks = hooks;
            CompleteOnContinue = options.CompleteOnContinue.WithDefault(player.Configuration.CompleteOnContinue);
            SkipAllowed = !options.DisableSkip;
            AutoPlayAllowed = !options.DisableAutoPlay;
            AwaitInputAllowed = !options.DisableAwaitInput;
            Scripts = scripts;
            Player = player;
            State = state;
            ContinueInput = input.GetContinue();
        }

        public virtual void Reset ()
        {
            Stop();
            CancelCommands();
            // Playlist?.ReleaseResources(); performed in StateManager; 
            // here it could be invoked after the actors are already destroyed.
            PlayedIndex = -1;
            PlayedScript = Scripts.Juggle(PlayedScript, null, this);
            ExecutedPlayedCommand = false;
            ShouldCompletePlayedCommand = false;
            SetAwaitInput(false);
            SetAutoPlay(false);
            SetSkip(false);
        }

        public virtual void Dispose ()
        {
            Reset();
            CommandExecutionSrc.Dispose();
            foreach (var (_, (__, cts)) in ExecutedCommands)
                cts.Dispose();
            ExecutedCommands.Clear();
            AsyncExecutedCommands.Clear();
        }

        [CanBeNull]
        public virtual GameState Save () => Disposing ? null : new() {
            Id = Id,
            Playing = Playing,
            ExecutedPlayedCommand = ExecutedPlayedCommand,
            AwaitingInput = AwaitingInput,
            PlaybackSpot = PlaybackSpot,
            GosubReturnSpots = GosubReturnSpots.Count > 0 ? GosubReturnSpots.Reverse().ToList() : null,
            Options = Options
        };

        public virtual async Awaitable Load ([NotNull] GameState game)
        {
            // Force stop and cancel all running commands to prevent state mutation while loading other services.
            Stop();
            CancelCommands();

            ExecutedPlayedCommand = game.ExecutedPlayedCommand;

            if (game.Playing) // The playback is resumed (when necessary) after other services are loaded.
            {
                if (State.RollbackInProgress) State.OnRollbackFinished += PlayAfterLoad;
                else State.OnGameLoadFinished += HandleGameLoaded;
            }

            if (game.GosubReturnSpots != null && game.GosubReturnSpots.Count > 0)
                GosubReturnSpots = new(game.GosubReturnSpots);
            else GosubReturnSpots.Clear();

            if (string.IsNullOrEmpty(game.PlaybackSpot.ScriptPath)) LoadStoppedState();
            else await LoadPlayingState(game.PlaybackSpot);

            void HandleGameLoaded (GameSaveLoadArgs _) => PlayAfterLoad();

            void PlayAfterLoad ()
            {
                State.OnGameLoadFinished -= HandleGameLoaded;
                State.OnRollbackFinished -= PlayAfterLoad;
                if (!Engine.Initialized || !Player.HasTrack(Id)) return;
                SetAwaitInput(game!.AwaitingInput);
                if (!ExecutedPlayedCommand) ShouldCompletePlayedCommand = true;
                Resume();
            }

            void LoadStoppedState ()
            {
                Playlist?.ReleaseResources();
                PlayedIndex = 0;
                if (PlayedScript) Scripts.ScriptLoader.Release(PlayedScript.Path, this);
                PlayedScript = null;
            }

            async Awaitable LoadPlayingState (PlaybackSpot spot)
            {
                if (Playlist == null || !PlayedScript || !game.PlaybackSpot.ScriptPath.EqualsOrdinal(PlayedScript.Path))
                {
                    var script = await Scripts.ScriptLoader.LoadOrErr(game.PlaybackSpot.ScriptPath, this);
                    PlayedScript = Scripts.Juggle(PlayedScript, script, this);
                }
                PlayedIndex = ResolvePlayedIndex(game.PlaybackSpot);
            }

            int ResolvePlayedIndex (PlaybackSpot spot)
            {
                if (Playlist?.IndexOf(spot) is { } index && index >= 0) return index;

                if (Playlist?.GetCommandAfterLine(spot.LineIndex, -1) is { } nextCommand)
                {
                    if (!Application.isEditor) // in the editor this is expected on hot reload
                        Engine.Warn($"Failed to play '{spot}': the script has probably changed after the save was made." +
                                    " Will play next command instead; expect undefined behaviour.");
                    ExecutedPlayedCommand = false;
                    return Playlist.IndexOf(nextCommand.PlaybackSpot);
                }

                if (Playlist?.GetCommandBeforeLine(spot.LineIndex, 0) is { } prevCommand)
                {
                    if (!Application.isEditor) // in the editor this is expected on hot reload
                        Engine.Warn($"Failed to play '{spot}': the script has probably changed after the save was made." +
                                    " Will play previous command instead; expect undefined behaviour.");
                    return Playlist.IndexOf(prevCommand.PlaybackSpot);
                }

                return -1; // script has all commands removed; don't throw to allow hot-reload to an empty script
            }
        }

        public virtual void Play (string scriptPath, int playlistIndex = 0)
        {
            var script = Scripts.ScriptLoader.GetLoadedOrErr(scriptPath);
            PlayedScript = Scripts.Juggle(PlayedScript, script, this);
            Resume(playlistIndex);
        }

        public virtual void Resume (int? playlistIndex = null)
        {
            if (!PlayedScript || Playlist is null)
                throw new Error("Failed to resume script playback: no currently played script.");

            PlayRoutineSrc.Complete();

            Hooks.OnPlay?.Invoke(this);

            if (playlistIndex == -1 || Playlist.Count == 0 || !playlistIndex.HasValue && PlayedIndex == -1)
            {
                // By convention, when resolving nested indexes via ScriptPlaylist, it returns -1
                // in case there is nowhere to move (the script has finished). The common usage pattern
                // of such APIs is to Resume(nextIndex). This edge case allows gracefully stopping
                // the playback, w/o having to repeatedly handle this by the callee.
                HandleScriptFinished();
                return;
            }

            if (playlistIndex.HasValue)
            {
                PlayedIndex = playlistIndex.Value;
                ExecutedPlayedCommand = false;
            }

            if (!Playlist.IsIndexValid(PlayedIndex))
                throw new Error($"Failed to resume '{PlayedScript.Path}' script playback " +
                                $"at '{PlayedIndex}' playlist index: invalid index.");

            PlayRoutineSrc.Reset();
            PlayRoutine(PlayRoutineSrc.Token).Forget();
        }

        public virtual void Stop ()
        {
            if (!Playing) return;
            PlayRoutineSrc.Complete();
            Hooks.OnStop?.Invoke(this);
        }

        public virtual async Awaitable<bool> Rewind (int lineIndex)
        {
            var targetCommand = Playlist?.GetCommandAfterLine(lineIndex, 0);
            if (targetCommand is null)
                throw new Error($"Script player failed to rewind: target line index ({lineIndex}) " +
                                $"is not valid for '{PlayedScript?.Path}' script.");

            var targetSpot = targetCommand.PlaybackSpot;
            var targetIdx = Playlist.IndexOf(targetCommand);
            if (targetIdx == PlayedIndex) return true;

            Stop();
            SetAutoPlay(false);
            SetSkip(false);
            SetAwaitInput(false);

            if (targetIdx < PlayedIndex)
                return await State.Rollback(s => s.PlaybackSpot == targetSpot);

            while (!await FastForwardRoutine(targetSpot.ScriptPath, targetIdx))
            while (!Playing)
                await Async.NextFrame(Engine.DestroyToken);
            return true;
        }

        public virtual void SetAutoPlay (bool enabled)
        {
            if (!AutoPlayAllowed) return;
            if (AutoPlaying == enabled) return;

            AutoPlaying = enabled;
            if (enabled && AwaitingInput) SetAwaitInput(false);
        }

        public virtual void SetSkip (bool enabled)
        {
            if (!SkipAllowed) return;
            if (Skipping == enabled) return;
            if (enabled && !IsSkipAllowed()) return;

            Skipping = enabled;
            if (enabled && AwaitingInput)
            {
                State.PeekRollbackStack()?.AllowPlayerRollback();
                SetAwaitInput(false);
            }
            if (enabled && AutoPlaying) SetAutoPlay(false);
        }

        public virtual void SetAwaitInput (bool enabled)
        {
            if (!AwaitInputAllowed) return;
            if (AwaitingInput == enabled) return;

            if (Skipping && enabled || (!enabled && (ContinueInput?.Active ?? AutoPlaying)))
                State.PeekRollbackStack()?.AllowPlayerRollback();

            if (Skipping && enabled) return;

            AwaitingInput = enabled;
            if (!enabled) AwaitInputDisabledSrc.Complete();

            Hooks.OnAwaitInput?.Invoke(this);
        }

        public virtual void CollectExecutedCommands (ICollection<Command> commands)
        {
            foreach (var (command, _) in ExecutedCommands.Values)
                commands.Add(command);
        }

        public virtual void RegisterCompletion (Command command, Awaitable task)
        {
            if (task.IsCompleted)
            {
                ExecutedCommands.Remove(command.PlaybackSpot);
                return;
            }

            AsyncExecutedCommands[command.PlaybackSpot] = (command, task);
            WaitAndComplete().Forget();

            async Awaitable WaitAndComplete ()
            {
                try { await task; }
                finally
                {
                    var registeredTask = AsyncExecutedCommands.GetValueOrDefault(command.PlaybackSpot).Item2;
                    if (registeredTask == task)
                    {
                        AsyncExecutedCommands.Remove(command.PlaybackSpot);
                        ExecutedCommands.Remove(command.PlaybackSpot);
                    }
                }
            }
        }

        public virtual async Awaitable Complete (Predicate<Command> filter = null, Func<Awaitable> onComplete = null)
        {
            if (onComplete != null)
                OnCompleteTasks.Enqueue(onComplete);

            if (!CompleteSrc.Completed)
            {
                await CompleteSrc.WaitCompletion();
                return;
            }

            using (new InteractionBlocker())
            {
                using var _ = SetPool<PlaybackSpot>.Rent(out var completing);
                foreach (var (_, (cmd, cts)) in ExecutedCommands)
                    if (filter == null || filter(cmd))
                    {
                        completing.Add(cmd.PlaybackSpot);
                        cts.Cancel();
                    }
                CompleteSrc.Reset();

                await Async.While(() => completing.Overlaps(ExecutedCommands.Keys));

                while (OnCompleteTasks.Count > 0)
                    await OnCompleteTasks.Dequeue()();

                CompleteSrc.Complete();
            }
        }

        /// <summary>
        /// In case <see cref="Complete"/> request is being handled, will wait until it's finished;
        /// returns true in case specified token has requested cancellation.
        /// </summary>
        /// <remarks>This should be awaited after any async operation in the playback routine.</remarks>
        protected virtual async Awaitable<bool> WaitCompletion (AsyncToken token)
        {
            if (token.Canceled) return true;
            if (!CompleteSrc.Completed) await CompleteSrc.WaitCompletion();
            return token.Canceled;
        }

        protected virtual bool IsSkipAllowed ()
        {
            if (!SkipAllowed) return false;
            if (SkipMode == PlayerSkipMode.Everything) return true;
            if (PlayedScript is null) return false;
            return Played.IsIndexPlayed(PlayedScript.Path, PlayedIndex + 1);
        }

        protected virtual async Awaitable WaitUntilAwaitInputDisabled ()
        {
            if (AwaitInputDisabledSrc.Completed) AwaitInputDisabledSrc.Reset();
            // Wait EOF to sync potential render mutations when the following commands executed in parallel.
            await AwaitInputDisabledSrc.WaitCompletionEndOfFrame();
        }

        protected virtual async Awaitable AwaitInputInAutoPlay ()
        {
            await Async.DelayUnscaled(TimeSpan.FromSeconds(Player.Configuration.MinAutoPlayDelay));
            while (AutoPlaying && AwaitingInput && Engine.GetService<IAudioManager>()?.GetPlayedVoice() != null)
                await Async.NextFrame();
            if (!AutoPlaying) await WaitUntilAwaitInputDisabled(); // in case autoplay was disabled while waiting
        }

        protected virtual async Awaitable ExecutePlayedCommand (AsyncToken token)
        {
            if (PlayedScript is null || PlayedCommand is null || !PlayedCommand.ShouldExecute) return;

            Hooks.OnExecute?.Invoke(this);

            Played.RegisterPlayedIndex(PlayedScript.Path, PlayedIndex);

            for (int i = Hooks.PreExeTasks.Count - 1; i >= 0; i--)
            {
                await Hooks.PreExeTasks[i](this);
                if (await WaitCompletion(token)) return;
            }

            if (await WaitCompletion(token)) return;

            var completeAsap = ShouldCompletePlayedCommand;
            ShouldCompletePlayedCommand = false;
            ExecutedPlayedCommand = true;

            await ExecuteIgnoringCancellation(PlayedCommand, completeAsap);
            if (await WaitCompletion(token)) return;

            for (int i = Hooks.PostExeTasks.Count - 1; i >= 0; i--)
            {
                await Hooks.PostExeTasks[i](this);
                if (await WaitCompletion(token)) return;
            }

            if (await WaitCompletion(token)) return;

            Hooks.OnExecuted?.Invoke(this);
        }

        protected virtual async Awaitable ExecuteIgnoringCancellation (Command command, bool completeAsap)
        {
            var completionCTS = new CancellationTokenSource();
            ExecutedCommands[command.PlaybackSpot] = (command, completionCTS);
            var token = new AsyncToken(CommandExecutionSrc.Token, completeAsap ? new(true) : completionCTS.Token);
            try { await command.Execute(new(this, token)); }
            catch (OperationCanceledException) { }
            finally
            {
                if (!AsyncExecutedCommands.ContainsKey(command.PlaybackSpot))
                    ExecutedCommands.Remove(command.PlaybackSpot);
            }
        }

        protected virtual async Awaitable PlayRoutine (AsyncToken token)
        {
            while (Engine.Initialized && Playing)
            {
                if (!ExecutedPlayedCommand)
                {
                    await ExecutePlayedCommand(token);
                    if (await WaitCompletion(token)) return;
                }

                if (AwaitingInput)
                {
                    if (AutoPlaying)
                    {
                        await Async.Any(AwaitInputInAutoPlay(), WaitUntilAwaitInputDisabled());
                        if (await WaitCompletion(token)) return;
                        SetAwaitInput(false);
                    }
                    else
                    {
                        await WaitUntilAwaitInputDisabled();
                        if (await WaitCompletion(token)) return;
                    }
                }

                var nextActionAvailable = SelectNextCommand();
                if (!nextActionAvailable) break;

                if (Skipping && !IsSkipAllowed()) SetSkip(false);
            }
        }

        protected virtual async Awaitable<bool> FastForwardRoutine (string targetPath, int targetIdx)
        {
            SetSkip(true);

            PlayRoutineSrc.Reset();
            var token = PlayRoutineSrc.Token;

            if (!ExecutedPlayedCommand)
            {
                ShouldCompletePlayedCommand = true;
                await ExecutePlayedCommand(token);
                if (await WaitCompletion(token)) return false;
            }

            var reachedLine = true;
            while (Engine.Initialized && Playing)
            {
                var nextCommandAvailable = SelectNextCommand();
                if (!nextCommandAvailable)
                {
                    reachedLine = false;
                    break;
                }

                if (PlayedScript?.Path == targetPath && PlayedIndex >= targetIdx)
                {
                    reachedLine = true;
                    break;
                }

                ShouldCompletePlayedCommand = true;
                await ExecutePlayedCommand(token);
                if (await WaitCompletion(token)) return false;
                SetSkip(true); // Force skip mode to be always active while fast-forwarding.

                if (token.IsCancellationRequested)
                {
                    reachedLine = false;
                    break;
                }
            }

            SetSkip(false);

            if (reachedLine)
            {
                ShouldCompletePlayedCommand = true;
                Resume();
            }

            return reachedLine;
        }

        /// <summary>
        /// Attempts to select next <see cref="Command"/> in the current <see cref="Playlist"/>.
        /// </summary>
        /// <returns>Whether the next command is available and was selected.</returns>
        protected virtual bool SelectNextCommand ()
        {
            if (Playlist is null) return false;

            var nextIndex = -1;
            if (PlayedCommand is Command.INestedHost && !PlayedCommand.ShouldExecute)
                nextIndex = Playlist.SkipNestedAt(PlayedIndex, PlayedCommand.Indent);
            else nextIndex = Playlist.MoveAt(PlayedIndex);

            if (!Playlist.IsIndexValid(nextIndex)) // No commands left in the played script.
            {
                HandleScriptFinished();
                return false;
            }

            PlayedIndex = nextIndex;
            ExecutedPlayedCommand = false;

            return true;
        }

        /// <summary>
        /// Invoked when the last command in <see cref="PlayedScript"/> is executed and there is nothing else to play.
        /// </summary>
        protected virtual void HandleScriptFinished ()
        {
            Stop();
            if (Options.LoopAt is { } loopIdx) FinishWithLoop().Forget();
            else if (Options.Dispose) FinishWithDispose().Forget();

            async Awaitable FinishWithLoop ()
            {
                await Async.NextFrame(); // wait a frame to prevent stalling the main track on tight loops
                if (Engine.Initialized && Player.HasTrack(Id) && PlayedScript) Resume(loopIdx);
            }

            async Awaitable FinishWithDispose ()
            {
                Disposing = true;

                while (AsyncExecutedCommands.Count > 0)
                    await Async.NextFrame(Engine.DestroyToken);

                if (Player.HasTrack(Id))
                    Player.RemoveTrack(Id);
            }
        }

        /// <summary>
        /// Cancels all the executing commands.
        /// </summary>
        /// <remarks>
        /// Be aware that this could lead to an inconsistent state; only use when the current engine state is
        /// going to be discarded (eg, when preparing to load a game or perform state rollback).
        /// </remarks>
        protected virtual void CancelCommands ()
        {
            CommandExecutionSrc.Reset();
        }
    }
}

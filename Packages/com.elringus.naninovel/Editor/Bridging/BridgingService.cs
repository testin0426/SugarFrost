using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Naninovel.Bridging;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class BridgingService
    {
        [CanBeNull] private static IScriptPlayer player => Engine.GetService<IScriptPlayer>();
        private static readonly JsonSerializer serde = new();
        private static readonly Dictionary<string, Server> serverById = new();
        private static bool running;
        private static string bridgingDir;
        private static string metadataFile;
        private static bool updateMetadataScheduled;

        public static void Restart ()
        {
            Deinitialize();
            if (Configuration.GetOrDefault<EngineConfiguration>() is { EnableBridging: true } cfg)
                Initialize(cfg);

            static void Initialize (EngineConfiguration cfg)
            {
                ResolvePaths();
                ForEach(StartServer);
                running = true;
                if (!serverById.ContainsKey("IO"))
                    AddServer("IO", new(new IOFiles(bridgingDir), serde, new UnityLogger()));
                Engine.OnInitializationFinished += HandleEngineInitialized;
                Engine.OnDestroyed += HandleEngineDeinitialized;
                EditorApplication.quitting += HandleUnityDeinitialized;

                if (cfg.AutoGenerateMetadata)
                {
                    EditorApplication.delayCall += UpdateMetadata;
                    Assets.OnModified -= UpdateMetadataDebounced;
                    Assets.OnModified += UpdateMetadataDebounced;
                    AssetProcessor.OnConfigurationModified -= UpdateMetadataDebounced;
                    AssetProcessor.OnConfigurationModified += UpdateMetadataDebounced;
                }
            }

            static void Deinitialize ()
            {
                running = false;
                Engine.OnInitializationFinished -= HandleEngineInitialized;
                Engine.OnDestroyed -= HandleEngineDeinitialized;
                EditorApplication.quitting -= HandleUnityDeinitialized;
                ForEach(StopServer);
            }

            static async void UpdateMetadataDebounced ()
            {
                if (updateMetadataScheduled) return;
                updateMetadataScheduled = true;
                await Task.Delay(TimeSpan.FromSeconds(0.5)); // don't use Awaitable here — it's unreliable in editor
                updateMetadataScheduled = false;
                UpdateMetadata();
            }
        }

        public static void AddServer (string id, Server server)
        {
            if (!serverById.TryAdd(id, server))
                throw new Error($"Failed to add '{id}' bridging server: already added.");

            server.OnPlayRequested += HandlePlayRequest;
            server.OnSkipRequested += HandleSkipRequest;
            server.OnPauseRequested += HandlePauseRequest;
            server.OnGotoRequested += HandleGotoRequest;

            if (running) StartServer(server);
        }

        public static void RemoveServer (string id)
        {
            if (!serverById.TryGetValue(id, out var server))
                throw new Error($"Failed to remove '{id}' bridging server: not found.");

            server.OnPlayRequested -= HandlePlayRequest;
            server.OnSkipRequested -= HandleSkipRequest;
            server.OnPauseRequested -= HandlePauseRequest;
            server.OnGotoRequested -= HandleGotoRequest;

            StopServer(server);
            serverById.Remove(id);
        }

        [MenuItem(MenuPath.Root + "/Update Metadata %#u", priority = 3)]
        public static void UpdateMetadata ()
        {
            ResolvePaths();
            var meta = MetadataGenerator.GenerateProjectMetadata();
            var json = serde.Serialize(meta);
            File.WriteAllText(metadataFile, json, Encoding.UTF8);
        }

        private static void ResolvePaths ()
        {
            var root = Path.GetFullPath(PackagePath.TransientDataPath);
            bridgingDir = $"{root}/Bridging";
            metadataFile = $"{root}/Metadata.json";
            if (!Directory.Exists(bridgingDir)) Directory.CreateDirectory(bridgingDir);
        }

        private static void StartServer (Server server)
        {
            server.Start(new() {
                Name = $"{Application.productName} (Unity)",
                Version = EngineVersion.LoadFromResources().BuildVersionTag()
            });
            server.NotifyReadyChanged(true);
        }

        private static void StopServer (Server server)
        {
            server.NotifyReadyChanged(false);
            server.Stop();
        }

        private static void HandleEngineInitialized ()
        {
            if (Engine.Behaviour is not RuntimeBehaviour) return;
            var player = Engine.GetServiceOrErr<IScriptPlayer>();
            player.OnExecute += NotifyPlayedCommand;
            player.OnSkip += NotifySkipStatusChanged;
            player.OnAwaitInput += NotifyAwaitInputChanged;
            player.OnPlay += _ => NotifyPauseStatusChanged(false);
            player.OnStop += _ => NotifyPauseStatusChanged(true);
            ForEach(server => server.NotifyPlayChanged(true));
        }

        private static void HandleEngineDeinitialized ()
        {
            ForEach(server => {
                server.NotifySkipChanged(false);
                server.NotifyPauseChanged(false);
                server.NotifyPlayChanged(false);
            });
        }

        private static void HandleUnityDeinitialized ()
        {
            ForEach(server => server.NotifyReadyChanged(false));
        }

        private static void HandleGotoRequest (Bridging.PlaybackSpot spot)
        {
            var scriptPath = spot.ScriptPath;
            var lineIdx = spot.LineIndex;
            if (!Application.isPlaying)
            {
                RuntimeInitializer.RequestFastInitialization();
                EditorApplication.EnterPlaymode();
            }
            if (Engine.Initialized) Goto();
            else Engine.OnInitializationFinished += Goto;

            void Goto ()
            {
                Engine.OnInitializationFinished -= Goto;
                var player = Engine.GetServiceOrErr<IScriptPlayer>();
                if (player.MainTrack.PlaybackSpot.ScriptPath == scriptPath)
                    player.MainTrack.Rewind(lineIdx).Forget();
                else
                    Engine.GetServiceOrErr<IStateManager>().ResetState()
                        .Then(async () => {
                            await player.MainTrack.LoadAndPlay(scriptPath);
                            await player.MainTrack.Rewind(lineIdx);
                        }).Forget();
            }
        }

        private static void HandlePlayRequest (bool enable)
        {
            if (enable == Application.isPlaying) return;
            if (enable) EditorApplication.EnterPlaymode();
            else EditorApplication.ExitPlaymode();
        }

        private static void HandleSkipRequest (bool enable)
        {
            if (Engine.Initialized)
                Engine.GetService<IScriptPlayer>()?.SetSkip(enable);
        }

        private static void HandlePauseRequest (bool enable)
        {
            if (Engine.Initialized)
                Engine.GetService<IScriptPlayer>()?.MainTrack.SetAwaitInput(enable);
        }

        private static void NotifyPlayedCommand ([NotNull] IScriptTrack track)
        {
            if (player?.MainTrack == track && track.PlayedCommand != null)
                ForEach(server =>
                    server.NotifyPositionChanged(new() {
                        ScriptPath = track.PlayedCommand.PlaybackSpot.ScriptPath,
                        LineIndex = track.PlayedCommand.PlaybackSpot.LineIndex,
                        InlineIndex = track.PlayedCommand.PlaybackSpot.InlineIndex
                    }));
        }

        private static void NotifySkipStatusChanged (bool skipping)
        {
            ForEach(server => server.NotifySkipChanged(skipping));
        }

        private static void NotifyPauseStatusChanged (bool paused)
        {
            ForEach(server => server.NotifyPauseChanged(paused));
        }

        private static void NotifyAwaitInputChanged ([NotNull] IScriptTrack track)
        {
            if (player?.MainTrack == track)
                NotifyPauseStatusChanged(track.AwaitingInput);
        }

        private static void ForEach (Action<Server> act)
        {
            foreach (var server in serverById.Values.ToArray())
                act(server);
        }
    }
}

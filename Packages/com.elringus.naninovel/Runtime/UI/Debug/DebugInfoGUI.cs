using Naninovel.Commands;
using UnityEngine;

namespace Naninovel.UI
{
    public class DebugInfoGUI : MonoBehaviour
    {
        private const int windowId = 0;

        private static DebugInfoGUI instance;
        private Rect windowRect = new(20, 20, 300, 100);
        private Vector2 voiceScroll = Vector2.zero;
        private bool show;
        private EngineVersion version;
        private IScriptPlayer player;
        private IAudioManager audioManager;
        private IStateManager state;
        private string lastCommandInfo, lastAutoVoiceName;

        public static void Toggle ()
        {
            if (!instance)
                instance = Engine.CreateObject<DebugInfoGUI>(new() { Name = nameof(DebugInfoGUI) });

            instance.show = !instance.show;

            if (instance.show && instance.player != null)
                instance.HandleCommandExecute(instance.player.MainTrack);
        }

        private void Awake ()
        {
            version = EngineVersion.LoadFromResources();
            player = Engine.GetServiceOrErr<IScriptPlayer>();
            audioManager = Engine.GetServiceOrErr<IAudioManager>();
            state = Engine.GetServiceOrErr<IStateManager>();
        }

        private void OnEnable ()
        {
            player.OnExecute += HandleCommandExecute;
            state.OnRollbackFinished += HandleRollbackFinished;
        }

        private void OnDisable ()
        {
            player.OnExecute -= HandleCommandExecute;
            state.OnRollbackFinished -= HandleRollbackFinished;
        }

        private void OnGUI ()
        {
            if (!show) return;

            windowRect = GUI.Window(windowId, windowRect, DrawWindow,
                string.IsNullOrEmpty(lastCommandInfo) ? $"Naninovel ver. {version.Version}" : lastCommandInfo);
        }

        private void DrawWindow (int windowID)
        {
            if (player.MainTrack.PlayedCommand != null)
            {
                if (!string.IsNullOrEmpty(lastAutoVoiceName))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Auto Voice: ");
                    GUILayout.Label(lastAutoVoiceName, GUILayout.MaxWidth(150));
                    if (GUILayout.Button("COPY"))
                        GUIUtility.systemCopyBuffer = lastAutoVoiceName;
                    GUILayout.EndHorizontal();
                }

                GUILayout.FlexibleSpace();
                GUI.enabled = !player.MainTrack.Playing;
                if (!player.MainTrack.Playing && GUILayout.Button("Play")) player.MainTrack.Resume();
                GUI.enabled = player.MainTrack.Playing;
                if (player.MainTrack.Playing && GUILayout.Button("Stop")) player.MainTrack.Stop();
                GUI.enabled = true;
                if (GUILayout.Button("Close Window")) show = false;
            }

            GUI.DragWindow();
        }

        private void HandleCommandExecute (IScriptTrack track)
        {
            if (player is null || player.MainTrack != track || track.PlayedCommand is not { } command) return;

            lastCommandInfo = player.MainTrack.PlayedCommand.PlaybackSpot.ToString();

            if (audioManager != null && audioManager.Configuration.EnableAutoVoicing && command is PrintText print)
                lastAutoVoiceName = AutoVoiceResolver.Resolve(print.Text);
        }

        private void HandleRollbackFinished () => HandleCommandExecute(player.MainTrack);
    }
}

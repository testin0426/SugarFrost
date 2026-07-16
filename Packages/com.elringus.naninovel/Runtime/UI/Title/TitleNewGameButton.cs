using System;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public class TitleNewGameButton : ScriptableButton
    {
        [Tooltip("Services to exclude from state reset when starting a new game.")]
        [SerializeField] private string[] excludeFromReset = Array.Empty<string>();

        private string startScriptPath;
        private TitleMenu titleMenu;
        private IScriptPlayer player;
        private IStateManager state;
        private IScriptManager scripts;

        protected override void Awake ()
        {
            base.Awake();

            scripts = Engine.GetServiceOrErr<IScriptManager>();
            startScriptPath = ResolveStartScriptPath(scripts);
            titleMenu = GetComponentInParent<TitleMenu>();
            player = Engine.GetServiceOrErr<IScriptPlayer>();
            state = Engine.GetServiceOrErr<IStateManager>();
            this.AssertRequiredObjects(titleMenu);
        }

        protected override void Start ()
        {
            base.Start();

            if (string.IsNullOrEmpty(startScriptPath))
                UIComponent.interactable = false;
        }

        protected override async void OnButtonClick ()
        {
            if (string.IsNullOrEmpty(startScriptPath))
            {
                Engine.Err("Can't start new game: specify start script in scripts configuration.");
                return;
            }

            await TitleScreen.PlayCallback(TitleScreen.NewGameCallbackLabel);
            titleMenu.Hide();

            using (Engine.GetConfiguration<ScriptPlayerConfiguration>().ShowLoadingUI ? await LoadingScreen.Show() : null)
                await state.ResetState(excludeFromReset, () => {
                    if (scripts.ScriptLoader.Exists(startScriptPath))
                        return player.MainTrack.LoadAndPlay(startScriptPath);
                    Engine.Warn($"Assigned start game script '{startScriptPath}' is missing — the game will not start." +
                                " Check the scripts configuration and make sure the assigned 'Start Game Script' exist." +
                                " You can scaffold sample scripts via the 'Samples' tab of the UPM window.");
                    return Async.Completed;
                });
        }

        protected virtual string ResolveStartScriptPath (IScriptManager scripts)
        {
            if (!string.IsNullOrEmpty(scripts.Configuration.StartGameScript))
                return scripts.Configuration.StartGameScript;
            if (!Application.isEditor)
                Engine.Warn("Please specify 'Start Game Script' in the scripts configuration. " +
                            "When not specified, Naninovel will pick first available script, " +
                            "which may differ between the editor and build environments.");
            using var _ = scripts.ScriptLoader.RentPaths(out var paths);
            return paths.OrderBy(p => p).FirstOrDefault();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.UI
{
    public abstract class NavigatorPanel : CustomUI
    {
        protected virtual Transform ButtonsContainer => buttonsContainer;
        protected virtual GameObject PlayButtonPrototype => playButtonPrototype;

        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private GameObject playButtonPrototype;

        protected virtual IScriptPlayer Player { get; private set; }
        protected virtual IScriptManager ScriptManager { get; private set; }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(ButtonsContainer, PlayButtonPrototype);
            Player = Engine.GetServiceOrErr<IScriptPlayer>();
            ScriptManager = Engine.GetServiceOrErr<IScriptManager>();
            GenerateScriptButtons().Forget();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();
            Player.OnPlay += HandlePlay;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            if (Player != null)
                Player.OnPlay -= HandlePlay;
        }

        protected abstract Awaitable LocateAllScriptPaths (ICollection<string> paths);

        protected virtual async Awaitable GenerateScriptButtons ()
        {
            if (ButtonsContainer)
                ObjectUtils.DestroyAllChildren(ButtonsContainer);

            using var _ = ListPool<string>.Rent(out var paths);
            await LocateAllScriptPaths(paths);
            foreach (var path in paths)
            {
                var scriptButton = Instantiate(PlayButtonPrototype, ButtonsContainer, false);
                scriptButton.GetComponent<NavigatorPlayButton>().Initialize(this, path, Player);
            }
        }

        private void HandlePlay (IScriptTrack track)
        {
            if (ScriptManager.Configuration.TitleScript != track.PlayedScript.Path)
                Hide();
        }
    }
}

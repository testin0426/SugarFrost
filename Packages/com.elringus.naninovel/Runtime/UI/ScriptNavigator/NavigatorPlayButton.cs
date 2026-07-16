using System;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel.UI
{
    public class NavigatorPlayButton : ScriptableButton
    {
        [Serializable]
        private class OnLabelChangedEvent : UnityEvent<string> { }

        [SerializeField] private OnLabelChangedEvent onLabelChanged;

        private NavigatorPanel navigator;
        private string scriptPath;
        private IScriptPlayer player;
        private IStateManager state;
        private bool isInitialized;

        public virtual void Initialize (NavigatorPanel navigator, string scriptPath, IScriptPlayer player)
        {
            this.navigator = navigator;
            this.scriptPath = scriptPath;
            this.player = player;
            name = "PlayScript: " + scriptPath;
            SetLabel(scriptPath);
            isInitialized = true;
            UIComponent.interactable = true;
        }

        protected override void Awake ()
        {
            base.Awake();

            SetLabel(null);
            UIComponent.interactable = false;
            state = Engine.GetServiceOrErr<IStateManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            state.GameSlotManager.OnBeforeLoad += ControlInteractability;
            state.GameSlotManager.OnLoaded += ControlInteractability;
            state.GameSlotManager.OnBeforeSave += ControlInteractability;
            state.GameSlotManager.OnSaved += ControlInteractability;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            state.GameSlotManager.OnBeforeLoad -= ControlInteractability;
            state.GameSlotManager.OnLoaded -= ControlInteractability;
            state.GameSlotManager.OnBeforeSave -= ControlInteractability;
            state.GameSlotManager.OnSaved -= ControlInteractability;
        }

        protected override void OnButtonClick ()
        {
            Debug.Assert(isInitialized);
            navigator.Hide();
            Engine.GetService<IUIManager>()?.GetUI<ITitleUI>()?.Hide();
            PlayScript();
        }

        protected virtual void SetLabel (string value)
        {
            onLabelChanged?.Invoke(value);
        }

        protected virtual void PlayScript ()
        {
            state.ResetState(() => player.MainTrack.LoadAndPlay(scriptPath)).Forget();
        }

        protected virtual void ControlInteractability (string _)
        {
            UIComponent.interactable = !state.GameSlotManager.Loading && !state.GameSlotManager.Saving;
        }
    }
}

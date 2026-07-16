using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GameSettingsMenu : CustomUI, ISettingsUI
    {
        protected virtual Toggle[] Tabs => tabs;

        [Tooltip("Toggles representing menu tabs. Expected in left to right order.")]
        [SerializeField] private Toggle[] tabs;
        [SerializeField] private IntUnityEvent onTabChanged;

        private IStateManager state => Engine.GetService<IStateManager>();
        private ITextManager docs => Engine.GetService<ITextManager>();
        private readonly List<TMP_Dropdown> dropdowns = new();
        private int tabIndex;

        public override async Awaitable Initialize ()
        {
            await docs.DocumentLoader.Load(ManagedTextPaths.Locales, this);
            BindInput(Inputs.Tab, HandleTabInput);
            BindInput(Inputs.Cancel, HandleCancelInput, new() { OnEnd = true });
        }

        public virtual async Awaitable SaveSettingsAndHide ()
        {
            using (new InteractionBlocker())
                await state.SaveSettings();
            Hide();
        }

        protected override void Awake ()
        {
            base.Awake();
            dropdowns.AddRange(GetComponentsInChildren<TMP_Dropdown>(true));
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            docs?.DocumentLoader?.ReleaseAll(this);
        }

        protected virtual void HandleCancelInput ()
        {
            foreach (var dropdown in dropdowns)
                if (dropdown.transform.childCount > 3) // A dropdown is open.
                    return;
            SaveSettingsAndHide().Forget();
        }

        protected virtual void HandleTabInput (Vector2 force)
        {
            if (tabs == null || tabs.Length == 0) return;
            if (force.x < 0) tabIndex--;
            if (force.x > 0) tabIndex++;
            tabIndex = Mathf.Clamp(tabIndex, 0, tabs.Length - 1);
            for (int i = 0; i < tabs.Length; i++)
                tabs[i].isOn = i == tabIndex;
            EventUtils.Select(FindFocusObject());
            onTabChanged?.Invoke(tabIndex);
        }
    }
}

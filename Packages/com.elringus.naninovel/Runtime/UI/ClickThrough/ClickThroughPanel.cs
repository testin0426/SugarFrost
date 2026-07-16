using System;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <inheritdoc cref="IClickThroughPanel"/>
    public class ClickThroughPanel : CustomUI, IClickThroughPanel, IPointerClickHandler
    {
        private IInputManager input;
        private Action onClick;
        private bool hideOnClick;

        public virtual void Show (bool hideOnClick, Action onClick, params string[] allowedInputs)
        {
            this.hideOnClick = hideOnClick;
            this.onClick = onClick;
            Show();
            input.AddMuter(this, allowedInputs);
        }

        public override void Hide ()
        {
            onClick = null;
            input.RemoveMuter(this);
            base.Hide();
        }

        public virtual void OnPointerClick (PointerEventData eventData)
        {
            onClick?.Invoke();
            if (hideOnClick) Hide();
        }

        protected override void Awake ()
        {
            base.Awake();

            input = Engine.GetServiceOrErr<IInputManager>();
        }
    }
}

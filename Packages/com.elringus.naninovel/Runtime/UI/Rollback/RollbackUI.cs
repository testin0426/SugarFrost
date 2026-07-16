using UnityEngine;

namespace Naninovel.UI
{
    public class RollbackUI : CustomUI, IRollbackUI
    {
        protected float HideTime => hideTime;

        [SerializeField] private float hideTime = 1f;

        private IStateManager state;
        private Timer hideTimer;

        protected override void Awake ()
        {
            base.Awake();

            state = Engine.GetServiceOrErr<IStateManager>();
            hideTimer = new(onCompleted: Hide);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            state.OnRollbackStarted += HandleRollbackStarted;
            state.OnRollbackFinished += HandleRollbackFinished;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            state.OnRollbackStarted -= HandleRollbackStarted;
            state.OnRollbackFinished -= HandleRollbackFinished;
        }

        protected virtual void HandleRollbackStarted ()
        {
            if (hideTimer.Running)
                hideTimer.Stop();

            Show();
        }

        protected virtual void HandleRollbackFinished ()
        {
            if (!state.RollbackInProgress)
                hideTimer.Run(hideTime);
        }
    }
}

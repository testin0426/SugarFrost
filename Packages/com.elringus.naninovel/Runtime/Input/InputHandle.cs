using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IInputHandle"/>
    public class InputHandle : IInputHandle
    {
        public virtual event Action OnStart;
        public virtual event Action OnEnd;
        public virtual event Action<Vector2> OnChange;

        public virtual string Id { get; }
        public virtual bool Active { get; private set; }
        public virtual Vector2 Force { get; private set; }
        public virtual bool Muted { get; set; }

        protected virtual IInputManager Manager { get; }
        protected virtual Stack<InputInterceptRequest> Intercepts { get; } = new();
        protected virtual CancellationTokenSource OnNextCTS { get; private set; }

        public InputHandle (string id, IInputManager manager)
        {
            Id = id;
            Manager = manager;
        }

        public virtual CancellationToken GetNext ()
        {
            OnNextCTS ??= new();
            return OnNextCTS.Token;
        }

        public virtual CancellationToken InterceptNext (CancellationToken token)
        {
            var cts = new CancellationTokenSource();
            Intercepts.Push(new(cts, token));
            return cts.Token;
        }

        public virtual void Activate (Vector2 force)
        {
            SetForce(force);
        }

        protected virtual void SetForce (Vector2 force)
        {
            const float toleranceSqr = .0001f;
            if ((Force - force).sqrMagnitude < toleranceSqr) return;

            Force = force;
            Active = Force.sqrMagnitude > Mathf.Epsilon;

            if (Manager.IsMuted(Id)) return;

            while (Intercepts.Count > 0)
                if (HandleInterceptRequest(Intercepts.Pop()))
                    return;

            if (Active)
            {
                OnNextCTS?.Cancel();
                OnNextCTS?.Dispose();
                OnNextCTS = null;
            }

            if (Active) OnStart?.Invoke();
            else OnEnd?.Invoke();
            OnChange?.Invoke(force);
        }

        protected virtual bool HandleInterceptRequest (InputInterceptRequest request)
        {
            var ownerStillExpectsInterception = !request.HandlerToken.IsCancellationRequested;
            if (ownerStillExpectsInterception) request.CTS.Cancel();
            request.CTS.Dispose();
            return ownerStillExpectsInterception;
        }
    }
}

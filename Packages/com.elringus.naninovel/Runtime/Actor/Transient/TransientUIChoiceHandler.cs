using System;
using System.Collections.Generic;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A transient <see cref="IChoiceHandlerActor"/> implementation backed by
    /// <see cref="UI.ChoiceHandlerPanel"/> with lifecycle managed outside Naninovel.
    /// </summary>
    [ActorResources(null, false)]
    [DefaultExecutionOrder(-1)] // Otherwise the ChoiceHandlerPanel's Awakes are invoked before this one's and require Naninovel's APIs which is not yet initialized.
    [AddComponentMenu("Naninovel/ Actors/Transient UI Choice Handler")]
    public class TransientUIChoiceHandler : TransientChoiceHandler
    {
        public override event Action<string> OnAppearanceChanged { add => UI.OnAppearanceChanged += value; remove => UI.OnAppearanceChanged -= value; }
        public override event Action<bool> OnVisibilityChanged { add => UI.OnVisibilityChanged += value; remove => UI.OnVisibilityChanged -= value; }
        public override event Action<Vector3> OnPositionChanged { add => UI.OnPositionChanged += value; remove => UI.OnPositionChanged -= value; }
        public override event Action<Quaternion> OnRotationChanged { add => UI.OnRotationChanged += value; remove => UI.OnRotationChanged -= value; }
        public override event Action<Vector3> OnScaleChanged { add => UI.OnScaleChanged += value; remove => UI.OnScaleChanged -= value; }
        public override event Action<Color> OnTintColorChanged { add => UI.OnTintColorChanged += value; remove => UI.OnTintColorChanged -= value; }
        public override event Action<Choice> OnChoiceAdded { add => UI.OnChoiceAdded += value; remove => UI.OnChoiceAdded -= value; }
        public override event Action<Choice> OnChoiceRemoved { add => UI.OnChoiceRemoved += value; remove => UI.OnChoiceRemoved -= value; }
        public override event Action<Choice> OnChoiceHandled { add => UI.OnChoiceHandled += value; remove => UI.OnChoiceHandled -= value; }

        [field: SerializeField, Tooltip("Whether to set the choice handler as default on the transient actor initialization.")]
        public virtual bool MakeDefault { get; private set; } = true;
        [field: SerializeField, Tooltip("Whether to automatically hide text printer when the choice handler is shown.")]
        public virtual bool HidePrinter { get; private set; } = true;
        [field: SerializeField, Tooltip("The UI panel backing the choice handler's implementation.")]
        public virtual ChoiceHandlerPanel ChoiceHandlerPanel { get; private set; }

        public override string Appearance { get => UI.Appearance; set => UI.Appearance = value; }
        public override bool Visible { get => UI.Visible; set => UI.Visible = value; }
        public override Vector3 Position { get => UI.Position; set => UI.Position = value; }
        public override Quaternion Rotation { get => UI.Rotation; set => UI.Rotation = value; }
        public override Vector3 Scale { get => UI.Scale; set => UI.Scale = value; }
        public override Color TintColor { get => UI.TintColor; set => UI.TintColor = value; }

        protected virtual UIHandler UI { get; private set; }

        public override Awaitable ChangeAppearance (string appearance, Tween tween, Transition? transition = default, AsyncToken token = default) => UI.ChangeAppearance(appearance, tween, transition, token);
        public override Awaitable ChangeVisibility (bool visible, Tween tween, AsyncToken token = default) => UI.ChangeVisibility(visible, tween, token);
        public override Awaitable ChangePosition (Vector3 position, Tween tween, AsyncToken token = default) => UI.ChangePosition(position, tween, token);
        public override Awaitable ChangeRotation (Quaternion rotation, Tween tween, AsyncToken token = default) => UI.ChangeRotation(rotation, tween, token);
        public override Awaitable ChangeScale (Vector3 scale, Tween tween, AsyncToken token = default) => UI.ChangeScale(scale, tween, token);
        public override Awaitable ChangeTintColor (Color tintColor, Tween tween, AsyncToken token = default) => UI.ChangeTintColor(tintColor, tween, token);
        public override void AddChoice (Choice choice) => UI.AddChoice(choice);
        public override void RemoveChoice (string id) => UI.RemoveChoice(id);
        public override void HandleChoice (string id) => UI.HandleChoice(id);
        public override void CollectChoices (IList<Choice> choices) => UI.CollectChoices(choices);
        public override Choice? FindChoice (Predicate<Choice> filter) => UI.FindChoice(filter);

        protected override void Awake ()
        {
            base.Awake();
            ObjectUtils.AssertRequiredObjects(ChoiceHandlerPanel);
            ChoiceHandlerPanel.gameObject.SetActive(false);
        }

        public override void InitializeTransientActor ()
        {
            UI = new(ActorId, Metadata, ChoiceHandlerPanel, HidePrinter);
            base.InitializeTransientActor();
            ChoiceHandlerPanel.gameObject.SetActive(true);
            ChoiceHandlerPanel.Initialize().Forget();
            UI.Initialize().Forget();
            if (MakeDefault && Engine.TryGetService<IChoiceHandlerManager>(out var choices))
                choices.DefaultHandlerId = ActorId;
        }

        protected class UIHandler : UIChoiceHandler
        {
            protected override ChoiceHandlerPanel HandlerPanel => panel;

            private readonly ChoiceHandlerPanel panel;
            private readonly bool hidePrinters;

            public UIHandler (string id, ChoiceHandlerMetadata meta, ChoiceHandlerPanel panel, bool hidePrinters) : base(id, meta)
            {
                this.panel = panel;
                this.hidePrinters = hidePrinters;
            }

            public override Awaitable Initialize ()
            {
                HandlerPanel.OnChoice += HandleChoice;
                Visible = false;
                if (hidePrinters)
                    OnVisibilityChanged += visible => {
                        if (visible) Engine.GetService<IScriptPlayer>()?.MainTrack.ExecuteTransientCommand("hidePrinter").Forget();
                    };
                return Async.Completed;
            }

            protected override Vector3 GetBehaviourPosition () => HandlerPanel.transform.localPosition;
            protected override void SetBehaviourPosition (Vector3 position) => HandlerPanel.transform.localPosition = (Vector2)position;
            protected override Quaternion GetBehaviourRotation () => HandlerPanel.transform.localRotation;
            protected override void SetBehaviourRotation (Quaternion rotation) => HandlerPanel.transform.localRotation = rotation;
        }
    }
}

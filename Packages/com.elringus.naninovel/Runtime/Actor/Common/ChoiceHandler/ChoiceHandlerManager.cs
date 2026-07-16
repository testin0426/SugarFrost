using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IChoiceHandlerManager"/>
    [InitializeAtRuntime]
    public class ChoiceHandlerManager : ActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>, IChoiceHandlerManager
    {
        [Serializable]
        public new class GameState
        {
            public PickedChoice[] PickedChoices;
            public string DefaultHandlerId;
        }

        [Serializable]
        public struct PickedChoice
        {
            public PlaybackSpot HostedAt;
            public PlaybackSpot PickedAt;
        }

        public virtual IResourceLoader<GameObject> ChoiceButtonLoader => buttonLoader;
        public virtual string DefaultHandlerId { get; set; }

        protected virtual Dictionary<PlaybackSpot, PlaybackSpot> PickedChoices { get; } = new();

        private readonly IResourceProviderManager resources;
        private readonly ILocalizationManager l10n;

        private LocalizableResourceLoader<GameObject> buttonLoader;

        public ChoiceHandlerManager (ChoiceHandlersConfiguration cfg, IResourceProviderManager resources, ILocalizationManager l10n)
            : base(cfg)
        {
            this.resources = resources;
            this.l10n = l10n;
        }

        public override async Awaitable InitializeService ()
        {
            await base.InitializeService();
            buttonLoader = Configuration.ChoiceButtonLoader.CreateLocalizableFor<GameObject>(resources, l10n);
            DefaultHandlerId = Configuration.DefaultHandlerId;
        }

        public override void ResetService ()
        {
            base.ResetService();
            DefaultHandlerId = Configuration.DefaultHandlerId;
        }

        public override void DestroyService ()
        {
            base.DestroyService();
            ChoiceButtonLoader?.ReleaseAll(this);
        }

        public override void SaveServiceState (GameStateMap stateMap)
        {
            base.SaveServiceState(stateMap);
            var state = new GameState {
                PickedChoices = PickedChoices.Select(kv => new PickedChoice {
                    HostedAt = kv.Key,
                    PickedAt = kv.Value
                }).ToArray(),
                DefaultHandlerId = DefaultHandlerId ?? Configuration.DefaultHandlerId
            };
            stateMap.SetState(state);
        }

        public override Awaitable LoadServiceState (GameStateMap stateMap)
        {
            var task = base.LoadServiceState(stateMap);
            var state = stateMap.GetState<GameState>() ?? new GameState();

            PickedChoices.Clear();
            foreach (var picked in state.PickedChoices)
                PickedChoices[picked.HostedAt] = picked.PickedAt;

            DefaultHandlerId = state.DefaultHandlerId ?? Configuration.DefaultHandlerId;

            return task;
        }

        public virtual void PushSelectedChoice (PlaybackSpot hostedAt, PlaybackSpot continueAt)
        {
            PickedChoices[hostedAt] = continueAt;
        }

        public virtual PlaybackSpot PopSelectedChoice (PlaybackSpot hostedAt)
        {
            return PickedChoices.TryGetValue(hostedAt, out var pickedAt) ? pickedAt :
                throw new Error($"Failed to get picked choice for host at {hostedAt}");
        }
    }
}

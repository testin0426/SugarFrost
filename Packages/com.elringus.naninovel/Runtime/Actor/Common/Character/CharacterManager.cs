using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ICharacterManager"/>
    [InitializeAtRuntime]
    public class CharacterManager : OrthoActorManager<ICharacterActor, CharacterState, CharacterMetadata, CharactersConfiguration>, ICharacterManager
    {
        [Serializable]
        public new class GameState
        {
            public SerializableLiteralStringMap CharIdToAvatarPathMap = new();
        }

        public event Action<CharacterAvatarChangedArgs> OnCharacterAvatarChanged;

        protected virtual SerializableLiteralStringMap AvatarPathByCharId { get; } = new();
        protected virtual ResourceLoader<Texture2D> AvatarTextureLoader { get; private set; }

        private readonly ITextManager docs;
        private readonly ITextPrinterManager printers;

        public CharacterManager (CharactersConfiguration cfg, CameraConfiguration cameraCfg,
            ITextManager docs, ITextPrinterManager printers)
            : base(cfg, cameraCfg)
        {
            this.docs = docs;
            this.printers = printers;
        }

        public override void ResetService ()
        {
            base.ResetService();

            AvatarPathByCharId.Clear();
        }

        public override async Awaitable InitializeService ()
        {
            await base.InitializeService();

            var resources = Engine.GetServiceOrErr<IResourceProviderManager>();
            AvatarTextureLoader = Configuration.AvatarLoader.CreateFor<Texture2D>(resources);
            printers.OnPrintStarted += HandleAuthorHighlighting;
            // Loading only the required avatars not possible, as we can't use async to provide them later.
            // In case of issues, consider using 'render character to texture' feature instead.
            await AvatarTextureLoader.LoadAll(holder: this);
            await docs.DocumentLoader.Load(ManagedTextPaths.DisplayNames, this);
        }

        public override void DestroyService ()
        {
            base.DestroyService();

            if (printers != null)
                printers.OnPrintStarted -= HandleAuthorHighlighting;
            AvatarTextureLoader?.ReleaseAll(this);
            docs?.DocumentLoader?.ReleaseAll(this);
        }

        public override void SaveServiceState (GameStateMap stateMap)
        {
            base.SaveServiceState(stateMap);

            var gameState = new GameState {
                CharIdToAvatarPathMap = new(AvatarPathByCharId)
            };
            stateMap.SetState(gameState);
        }

        public override async Awaitable LoadServiceState (GameStateMap stateMap)
        {
            await base.LoadServiceState(stateMap);

            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                AvatarPathByCharId.Clear();
                return;
            }

            // Remove non-existing avatar mappings.
            if (AvatarPathByCharId.Count > 0)
                foreach (var charId in AvatarPathByCharId.Keys.ToArray())
                    if (!state.CharIdToAvatarPathMap.ContainsKey(charId))
                        AvatarPathByCharId.Remove(charId);
            // Add new or changed avatar mappings.
            foreach (var kv in state.CharIdToAvatarPathMap)
                SetAvatarTexturePathFor(kv.Key, kv.Value);
        }

        public virtual bool AvatarTextureExists (string avatarTexturePath)
        {
            return AvatarTextureLoader.IsLoaded(avatarTexturePath);
        }

        public virtual void RemoveAvatarTextureFor (string characterId)
        {
            // Don't just remove the record here, because printers use
            // GetAvatarTextureFor on each author change, hence we need to
            // know when user explicitly removed/hidden the avatar by setting "none".
            AvatarPathByCharId[characterId] = "none";
            OnCharacterAvatarChanged?.Invoke(new(characterId, null));
        }

        public virtual Texture2D GetAvatarTextureFor (string characterId)
        {
            var avatarTexturePath = GetAvatarTexturePathFor(characterId);
            if (avatarTexturePath is null) return null;
            return AvatarTextureLoader.GetLoaded(avatarTexturePath);
        }

        public virtual string GetAvatarTexturePathFor (string characterId)
        {
            if (!AvatarPathByCharId.TryGetValue(characterId ?? string.Empty, out var avatarTexturePath))
            {
                var defaultPath = $"{characterId}/Default"; // Attempt default path.
                return AvatarTextureExists(defaultPath) ? defaultPath : null;
            }
            if (!AvatarTextureExists(avatarTexturePath)) return null;
            return avatarTexturePath;
        }

        public virtual void SetAvatarTexturePathFor (string characterId, string avatarTexturePath)
        {
            if (!ActorExists(characterId))
            {
                Engine.Warn($"Failed to assign '{avatarTexturePath}' avatar texture to '{characterId}' character: character with the specified ID not found.");
                return;
            }

            if (avatarTexturePath.EqualsIgnoreCase("none"))
            {
                RemoveAvatarTextureFor(characterId);
                return;
            }

            if (!AvatarTextureExists(avatarTexturePath))
            {
                Engine.Warn($"Failed to assign '{avatarTexturePath}' avatar texture to '{characterId}' character: avatar texture with the specified path not found.");
                return;
            }

            AvatarPathByCharId.TryGetValue(characterId ?? string.Empty, out var initialPath);
            AvatarPathByCharId[characterId] = avatarTexturePath;

            if (initialPath != avatarTexturePath)
            {
                var avatarTexture = GetAvatarTextureFor(characterId);
                OnCharacterAvatarChanged?.Invoke(new(characterId, avatarTexture));
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Will first attempt to find a corresponding record in the managed text documents, and, if not found, check the character metadata.
        /// In case the display name is found and is wrapped in curly braces, will attempt to evaluate the value from the expression.
        /// </remarks>
        public virtual string GetAuthorName (string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;

            var meta = this.GetActorMetaOrDefault(characterId);
            if (!meta.HasName) return null;

            var displayName = docs.GetRecordValue(characterId, ManagedTextPaths.DisplayNames);
            if (string.IsNullOrEmpty(displayName)) displayName = meta.DisplayName;

            if (!string.IsNullOrEmpty(displayName) && displayName.StartsWithOrdinal("{") && displayName.EndsWithOrdinal("}"))
            {
                var expression = displayName.GetAfterFirst("{").GetBeforeLast("}");
                displayName = ExpressionEvaluator.Evaluate<string>(expression,
                    new() { OnError = desc => Engine.Err($"Failed to evaluate '{characterId}' character display name: {desc}") });
            }

            return string.IsNullOrWhiteSpace(displayName) ? characterId : displayName;
        }

        public virtual CharacterLookDirection LookAtOriginDirection (float xPos)
        {
            if (Mathf.Approximately(xPos, GlobalSceneOrigin.x)) return CharacterLookDirection.Center;
            return xPos < GlobalSceneOrigin.x ? CharacterLookDirection.Right : CharacterLookDirection.Left;
        }

        public virtual async Awaitable ArrangeCharacters (bool lookAtOrigin, Tween tween, AsyncToken token = default)
        {
            var actors = ManagedActors?.Values
                .Where(c => c.Visible && !Configuration.GetMetadataOrDefault(c.Id).RenderTexture).ToList();
            if (actors is null || actors.Count == 0) return;

            var sceneWidth = CameraConfiguration.SceneRect.width;
            var arrangeRange = Configuration.ArrangeRange;
            var arrangeWidth = sceneWidth * (arrangeRange.y - arrangeRange.x);
            var stepSize = arrangeWidth / actors.Count;
            var xOffset = (sceneWidth * arrangeRange.x - sceneWidth * (1 - arrangeRange.y)) / 2;

            using var _ = Async.Rent(out var tasks);
            var evenCount = 1;
            var unevenCount = 1;
            for (int i = 0; i < actors.Count; i++)
            {
                var isEven = i.IsEven();
                var posX = xOffset;
                if (isEven)
                {
                    var step = (evenCount * stepSize) / 2f;
                    posX += -(arrangeWidth / 2f) + step;
                    evenCount++;
                }
                else
                {
                    var step = (unevenCount * stepSize) / 2f;
                    posX += arrangeWidth / 2f - step;
                    unevenCount++;
                }
                tasks.Add(actors[i].ChangePositionX(posX, tween, token));

                if (lookAtOrigin)
                {
                    var lookDir = LookAtOriginDirection(posX);
                    tasks.Add(actors[i].ChangeLookDirection(lookDir, tween, token));
                }
            }
            await Async.All(tasks);
        }

        protected override async Awaitable<ICharacterActor> ConstructActor (string actorId)
        {
            var actor = await base.ConstructActor(actorId);

            // When adding new character place it at the scene origin by default.
            actor.Position = new(GlobalSceneOrigin.x, GlobalSceneOrigin.y, actor.Position.z);

            var meta = Configuration.GetMetadataOrDefault(actorId);
            if (meta.HighlightWhenSpeaking)
                ApplyPose(actor, meta.NotSpeakingPose, new(0));
            if (meta.Anchors?.Count > 0)
                InitializeAnchors(actor, meta.Anchors);
            return actor;
        }

        protected virtual void HandleAuthorHighlighting (PrintMessageArgs args)
        {
            if (ManagedActors.Count == 0) return;

            var visibleActors = ManagedActors.Count(a => a.Value.Visible);
            var authorIds = (args.Message.Author?.Id ?? "").Split(',');

            foreach (var actor in ManagedActors.Values)
            {
                var actorMeta = Configuration.GetMetadataOrDefault(actor.Id);
                if (!actorMeta.HighlightWhenSpeaking) continue;
                var speaking = actorMeta.HighlightCharacterCount > visibleActors || authorIds.Contains(actor.Id) || args.Message.Author?.Id == "*";
                var poseName = speaking ? actorMeta.SpeakingPose : actorMeta.NotSpeakingPose;
                var tween = new Tween(actorMeta.HighlightDuration, actorMeta.HighlightEasing);
                ApplyPose(actor, poseName, tween);
            }

            if (string.IsNullOrEmpty(args.Message.Author?.Id) || !ActorExists(args.Message.Author?.Id)) return;
            var authorMeta = Configuration.GetMetadataOrDefault(args.Message.Author?.Id);
            if (authorMeta.HighlightWhenSpeaking && authorMeta.HighlightCharacterCount <= visibleActors && authorMeta.PlaceOnTop)
            {
                var topmostChar = ManagedActors.Values.OrderBy(c => c.Position.z).FirstOrDefault();
                if (topmostChar != null && !topmostChar.Id.EqualsOrdinal(args.Message.Author?.Id))
                {
                    var authorChar = this.GetActorOrErr(args.Message.Author?.Id);
                    var authorZPos = authorChar.Position.z;
                    var topmostZPos = topmostChar.Position.z < authorZPos ? topmostChar.Position.z : topmostChar.Position.z - .1f;
                    var tween = new Tween(authorMeta.HighlightDuration, authorMeta.HighlightEasing);
                    authorChar.ChangePositionZ(topmostZPos, tween).Forget();
                    topmostChar.ChangePositionZ(authorZPos, tween).Forget();
                }
            }
        }

        protected virtual void ApplyPose (ICharacterActor actor, string poseName, Tween tween)
        {
            if (string.IsNullOrEmpty(poseName)) return;
            var pose = Configuration.GetActorOrSharedPose<CharacterState>(actor.Id, poseName);
            if (pose is null) return;

            if (pose.IsPropertyOverridden(nameof(CharacterState.Appearance)))
                actor.ChangeAppearance(pose.ActorState.Appearance, tween).Forget();
            if (pose.IsPropertyOverridden(nameof(CharacterState.Position)))
                actor.ChangePosition(pose.ActorState.Position, tween).Forget();
            if (pose.IsPropertyOverridden(nameof(CharacterState.Rotation)))
                actor.ChangeRotation(pose.ActorState.Rotation, tween).Forget();
            if (pose.IsPropertyOverridden(nameof(CharacterState.Scale)))
                actor.ChangeScale(pose.ActorState.Scale, tween).Forget();
            if (pose.IsPropertyOverridden(nameof(CharacterState.Visible)))
                actor.ChangeVisibility(pose.ActorState.Visible, tween).Forget();
            if (pose.IsPropertyOverridden(nameof(CharacterState.LookDirection)))
                actor.ChangeLookDirection(pose.ActorState.LookDirection, tween).Forget();
            if (pose.IsPropertyOverridden(nameof(CharacterState.TintColor)))
                actor.ChangeTintColor(pose.ActorState.TintColor, tween).Forget();
        }

        protected virtual void InitializeAnchors (ICharacterActor chara, IReadOnlyCollection<ActorMetadata.Anchor> anchors)
        {
            if (chara is not MonoBehaviourActor { Transform: { } trs }) return;
            foreach (var anchorMeta in anchors)
            {
                var anchor = Engine.CreateObject<ActorAnchor>(new() { Name = anchorMeta.Id, Parent = trs });
                anchor.ActorId = chara.Id;
                anchor.AnchorId = anchorMeta.Id;
                anchor.transform.localPosition = anchorMeta.Position;
            }
        }
    }
}

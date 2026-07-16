using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Routes essential <see cref="ICharacterManager"/> APIs to <see cref="UnityEngine.Events.UnityEvent"/>.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Character Events")]
    public class CharacterEvents : UnityEvents
    {
        [ActorPopup(CharactersConfiguration.DefaultPathPrefix), CanBeNull]
        [Tooltip("The identifier of the character actor for the events and actions.")]
        public string CharacterId;

        [Space]
        [Tooltip("Occurs when availability of the character manager engine service changes.")]
        public BoolUnityEvent ServiceAvailable;
        [Tooltip("Occurs when a character with ID is added.")]
        public StringUnityEvent CharacterAdded;
        [Tooltip("Occurs when a character with ID is removed")]
        public StringUnityEvent CharacterRemoved;
        [Tooltip("Occurs when availability of a character with the specified ID changes.")]
        public BoolUnityEvent CharacterAvailable;
        [Tooltip("Occurs when appearance of a character with the specified ID changes.")]
        public StringUnityEvent CharacterAppearanceChanged;
        [Tooltip("Occurs when visibility of a character with the specified ID changes.")]
        public BoolUnityEvent CharacterVisibilityChanged;
        [Tooltip("Occurs when position of a character with the specified ID changes.")]
        public Vector3UnityEvent CharacterPositionChanged;
        [Tooltip("Occurs when scale of a character with the specified ID changes.")]
        public Vector3UnityEvent CharacterScaleChanged;
        [Tooltip("Occurs when rotation of a character with the specified ID changes.")]
        public QuaternionUnityEvent CharacterRotationChanged;
        [Tooltip("Occurs when tint color of a character with the specified ID changes.")]
        public ColorUnityEvent CharacterTintColorChanged;
        [Tooltip("Occurs when look direction of a character with the specified ID changes. The integer is mapped as follows: 0 = center, 1 = left, 2 = right.")]
        public IntUnityEvent CharacterLookDirectionChanged;

        public async void ShowCharacter ()
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangeVisibility(true, new(manager.Configuration.DefaultDuration));
        }

        public async void HideCharacter ()
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            if (!manager.ActorExists(CharacterId)) return;
            await manager.GetActorOrErr(CharacterId).ChangeVisibility(false, new(manager.Configuration.DefaultDuration));
        }

        public void HideAllCharacters ()
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            using var _ = manager.RentActors(out var charas);
            foreach (var chara in charas)
                chara.ChangeVisibility(false, new(manager.Configuration.DefaultDuration)).Forget();
        }

        public async void ChangeCharacterAppearance (string appearance)
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangeAppearance(appearance, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeCharacterLookDirection (int direction)
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangeLookDirection((CharacterLookDirection)direction, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeCharacterPositionX (int x)
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangePositionX(x, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeCharacterPositionY (int y)
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangePositionY(y, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeCharacterPositionZ (int z)
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangePositionZ(z, new(manager.Configuration.DefaultDuration));
        }

        public async void ChangeCharacterScale (int scale)
        {
            if (!Engine.TryGetService<ICharacterManager>(out var manager)) return;
            var chara = await manager.GetOrAddActor(CharacterId);
            await chara.ChangeScale(Vector3.one * scale, new(manager.Configuration.DefaultDuration));
        }

        protected override void HandleEngineInitialized ()
        {
            if (Engine.TryGetService<ICharacterManager>(out var chars))
            {
                ServiceAvailable?.Invoke(true);

                chars.OnActorAdded -= HandleActorAdded;
                chars.OnActorAdded += HandleActorAdded;
                if (chars.ActorExists(CharacterId))
                    HandleActorAdded(CharacterId);

                chars.OnActorRemoved -= HandleActorRemoved;
                chars.OnActorRemoved += HandleActorRemoved;

                if (!string.IsNullOrEmpty(CharacterId) && chars.ActorExists(CharacterId))
                    CharacterAvailable?.Invoke(true);
                else CharacterAvailable?.Invoke(false);
            }
            else ServiceAvailable?.Invoke(false);
        }

        protected override void HandleEngineDestroyed ()
        {
            ServiceAvailable?.Invoke(false);
            CharacterAvailable?.Invoke(false);
        }

        protected virtual void HandleActorAdded (string id)
        {
            CharacterAdded?.Invoke(id);
            if (id == CharacterId)
            {
                CharacterAvailable?.Invoke(true);

                var chara = Engine.GetServiceOrErr<ICharacterManager>().GetActorOrErr(id);
                chara.OnAppearanceChanged += CharacterAppearanceChanged.SafeInvoke;
                chara.OnVisibilityChanged += CharacterVisibilityChanged.SafeInvoke;
                chara.OnPositionChanged += CharacterPositionChanged.SafeInvoke;
                chara.OnScaleChanged += CharacterScaleChanged.SafeInvoke;
                chara.OnRotationChanged += CharacterRotationChanged.SafeInvoke;
                chara.OnTintColorChanged += CharacterTintColorChanged.SafeInvoke;
                chara.OnLookDirectionChanged += dir => CharacterLookDirectionChanged?.Invoke((int)dir);
            }
        }

        protected virtual void HandleActorRemoved (string id)
        {
            CharacterRemoved?.Invoke(id);
            if (id == CharacterId) CharacterAvailable?.Invoke(false);
        }
    }
}

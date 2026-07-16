using System;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Allows to listen for events when an unlockable item managed by <see cref="IUnlockableManager"/> is updated.
    /// </summary>
    [AddComponentMenu("Naninovel/ Events/Unlockable Events")]
    public class UnlockableEvents : MonoBehaviour
    {
        [Serializable]
        private class UnlockedStateChangedEvent : UnityEvent<bool> { }

        /// <summary>
        /// Invoked when unlocked state of the listened unlockable item is changed.
        /// </summary>
        public event Action<bool> OnUnlockedStateChanged;

        /// <summary>
        /// ID of the unlockable item to listen for.
        /// </summary>
        public virtual string UnlockableItemId { get => unlockableItemId; set => unlockableItemId = value; }

        [Tooltip("ID of the unlockable item to listen for.")]
        [SerializeField] private string unlockableItemId;
        [Tooltip("Invoked when unlocked state of the listened unlockable item is changed; also invoked when the component is started.")]
        [SerializeField] private UnlockedStateChangedEvent onUnlockedStateChanged;
        [Tooltip("Invoked when the item is unlocked.")]
        [SerializeField] private UnityEvent onUnlocked;
        [Tooltip("Invoked when the item is locked.")]
        [SerializeField] private UnityEvent onLocked;

        public void Lock (string id)
        {
            if (Engine.TryGetService<IUnlockableManager>(out var manager))
                manager.LockItem(id);
        }

        public void LockAll ()
        {
            if (Engine.TryGetService<IUnlockableManager>(out var manager))
                manager.LockAllItems();
        }

        public void Unlock (string id)
        {
            if (Engine.TryGetService<IUnlockableManager>(out var manager))
                manager.UnlockItem(id);
        }

        public void UnlockAll ()
        {
            if (Engine.TryGetService<IUnlockableManager>(out var manager))
                manager.UnlockAllItems();
        }

        protected virtual void OnEnable ()
        {
            Engine.OnInitializationFinished += Initialize;
            if (Engine.Initialized) Initialize();
        }

        protected virtual void OnDisable ()
        {
            Engine.OnInitializationFinished -= Initialize;
            if (Engine.TryGetService<IUnlockableManager>(out var manager))
                manager.OnItemUpdated -= HandleItemUpdated;
        }

        protected virtual void Initialize ()
        {
            var manager = Engine.GetServiceOrErr<IUnlockableManager>();
            manager.OnItemUpdated -= HandleItemUpdated;
            manager.OnItemUpdated += HandleItemUpdated;

            var unlocked = manager.ItemUnlocked(UnlockableItemId);
            InvokeEvents(unlocked);
        }

        protected virtual void HandleItemUpdated (UnlockableItemUpdatedArgs args)
        {
            if (args.Id.EqualsIgnoreCase(UnlockableItemId))
                InvokeEvents(args.Unlocked);
        }

        private void InvokeEvents (bool unlocked)
        {
            OnUnlockedStateChanged?.Invoke(unlocked);
            onUnlockedStateChanged?.Invoke(unlocked);
            if (unlocked) onUnlocked?.Invoke();
            else onLocked.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages serializable data instances (slots) in-memory, w/o actually persisting the data.
    /// Can be useful for automated testing and various integration scenarios.
    /// </summary>
    public abstract class TransientSaveSlotManager : ISaveSlotManager
    {
        public event Action<string> OnBeforeSave;
        public event Action<string> OnSaved;
        public event Action<string> OnBeforeLoad;
        public event Action<string> OnLoaded;
        public event Action<string> OnBeforeDelete;
        public event Action<string> OnDeleted;
        public event Action<string, string> OnBeforeRename;
        public event Action<string, string> OnRenamed;

        public virtual bool Loading { get; private set; }
        public virtual bool Saving { get; private set; }

        public abstract bool SaveSlotExists (string slotId);
        public abstract bool AnySaveExists ();
        public abstract void CollectSlotIds (ICollection<string> ids);
        public abstract void DeleteSaveSlot (string slotId);
        public abstract void RenameSaveSlot (string sourceSlotId, string destSlotId);

        protected virtual void InvokeOnBeforeSave (string slotId)
        {
            Saving = true;
            OnBeforeSave?.Invoke(slotId);
        }

        protected virtual void InvokeOnSaved (string slotId)
        {
            Saving = false;
            OnSaved?.Invoke(slotId);
        }

        protected virtual void InvokeOnBeforeLoad (string slotId)
        {
            Loading = true;
            OnBeforeLoad?.Invoke(slotId);
        }

        protected virtual void InvokeOnLoaded (string slotId)
        {
            Loading = false;
            OnLoaded?.Invoke(slotId);
        }

        protected virtual void InvokeOnBeforeDelete (string slotId)
        {
            OnBeforeDelete?.Invoke(slotId);
        }

        protected virtual void InvokeOnDeleted (string slotId)
        {
            OnDeleted?.Invoke(slotId);
        }

        protected virtual void InvokeOnBeforeRename (string sourceSlotId, string destSlotId)
        {
            OnBeforeRename?.Invoke(sourceSlotId, destSlotId);
        }

        protected virtual void InvokeOnRenamed (string sourceSlotId, string destSlotId)
        {
            OnRenamed?.Invoke(sourceSlotId, destSlotId);
        }
    }

    /// <inheritdoc cref="TransientSaveSlotManager"/>
    public class TransientSaveSlotManager<TData> : TransientSaveSlotManager, ISaveSlotManager<TData> where TData : new()
    {
        public static Func<TData> DefaultFactory { get; set; } = () => new();

        protected virtual Dictionary<string, TData> DataBySlotId { get; } = new();

        public virtual void SaveSync (string slotId, TData data)
        {
            InvokeOnBeforeSave(slotId);
            DataBySlotId[slotId] = data;
            InvokeOnSaved(slotId);
        }

        public virtual TData LoadSync (string slotId)
        {
            InvokeOnBeforeLoad(slotId);
            if (!SaveSlotExists(slotId))
                throw new Error($"Slot '{slotId}' not found when loading '{typeof(TData)}' data.");
            InvokeOnLoaded(slotId);
            return DataBySlotId[slotId];
        }

        public virtual TData LoadOrDefaultSync (string slotId)
        {
            if (!SaveSlotExists(slotId)) Save(slotId, DefaultFactory());
            return LoadSync(slotId);
        }

        public virtual Awaitable Save (string slotId, TData data)
        {
            SaveSync(slotId, data);
            return Async.Completed;
        }

        public virtual Awaitable<TData> Load (string slotId)
        {
            return Async.Result(LoadSync(slotId));
        }

        public virtual Awaitable<TData> LoadOrDefault (string slotId)
        {
            return Async.Result(LoadOrDefaultSync(slotId));
        }

        public override bool SaveSlotExists (string slotId)
        {
            return DataBySlotId.ContainsKey(slotId);
        }

        public override bool AnySaveExists ()
        {
            return DataBySlotId.Count > 0;
        }

        public override void CollectSlotIds (ICollection<string> ids)
        {
            foreach (var id in DataBySlotId.Keys)
                ids.Add(id);
        }

        public override void DeleteSaveSlot (string slotId)
        {
            if (!SaveSlotExists(slotId)) return;
            InvokeOnBeforeDelete(slotId);
            DataBySlotId.Remove(slotId);
            InvokeOnDeleted(slotId);
        }

        public override void RenameSaveSlot (string sourceSlotId, string destSlotId)
        {
            if (!SaveSlotExists(sourceSlotId)) return;
            InvokeOnBeforeRename(sourceSlotId, destSlotId);
            Save(destSlotId, LoadSync(sourceSlotId));
            DeleteSaveSlot(sourceSlotId);
            InvokeOnRenamed(sourceSlotId, destSlotId);
        }
    }
}

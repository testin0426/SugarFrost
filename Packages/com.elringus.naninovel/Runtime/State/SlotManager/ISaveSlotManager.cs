using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage serializable data instances (slots).
    /// </summary>
    public interface ISaveSlotManager
    {
        /// <summary>
        /// Occurs before a save (serialization) operation is started.
        /// Returns ID of the affected save slot.
        /// </summary>
        event Action<string> OnBeforeSave;
        /// <summary>
        /// Occurs after a save (serialization) operation is finished.
        /// Returns ID of the affected save slot.
        /// </summary>
        event Action<string> OnSaved;
        /// <summary>
        /// Occurs before a load (de-serialization) operation is started.
        /// Returns ID of the affected save slot.
        /// </summary>
        event Action<string> OnBeforeLoad;
        /// <summary>
        /// Occurs after a load (de-serialization) operation is finished.
        /// Returns ID of the affected save slot.
        /// </summary>
        event Action<string> OnLoaded;
        /// <summary>
        /// Occurs before a save slot is deleted.
        /// Returns ID of the affected save slot.
        /// </summary>
        event Action<string> OnBeforeDelete;
        /// <summary>
        /// Occurs after a save slot is deleted.
        /// Returns ID of the affected save slot.
        /// </summary>
        event Action<string> OnDeleted;
        /// <summary>
        /// Occurs before a save slot is renamed.
        /// Returns source (old) and destination (new) IDs.
        /// </summary>
        event Action<string, string> OnBeforeRename;
        /// <summary>
        /// Occurs after a save slot is renamed.
        /// Returns source (old) and destination (new) IDs.
        /// </summary>
        event Action<string, string> OnRenamed;

        /// <summary>
        /// Whether a save (serialization) operation is currently running.
        /// </summary>
        bool Loading { get; }
        /// <summary>
        /// Whether a load (de-serialization) operation is currently running.
        /// </summary>
        bool Saving { get; }

        /// <summary>
        /// Checks whether a save slot with the specified ID is available.
        /// </summary>
        /// <param name="slotId">Unique identifier (name) of the save slot.</param>
        bool SaveSlotExists (string slotId);
        /// <summary>
        /// Checks whether any save slot is available.
        /// </summary>
        bool AnySaveExists ();
        /// <summary>
        /// Collects all the existing save slot identifiers to the specified collection.
        /// </summary>
        void CollectSlotIds (ICollection<string> ids);
        /// <summary>
        /// Deletes a save slot with the specified ID.
        /// </summary>
        /// <param name="slotId">Unique identifier (name) of the save slot.</param>
        void DeleteSaveSlot (string slotId);
        /// <summary>
        /// Renames a save slot from <paramref name="sourceSlotId"/> to <paramref name="destSlotId"/>.
        /// Will overwrite <paramref name="destSlotId"/> slot in case it exists.
        /// </summary>
        /// <param name="sourceSlotId">ID of the slot to rename.</param>
        /// <param name="destSlotId">New ID of the slot.</param>
        void RenameSaveSlot (string sourceSlotId, string destSlotId);
    }

    /// <summary>
    /// Implementation is able to manage serializable data instances (slots) of type <typeparamref name="TData"/>.
    /// </summary>
    /// <typeparam name="TData">Type of the managed data; should be serializable via Unity's serialization system.</typeparam>
    public interface ISaveSlotManager<TData> : ISaveSlotManager where TData : new()
    {
        /// <summary>
        /// Saves (serializes) specified data under the specified save slot ID.
        /// </summary>
        /// <param name="slotId">Unique identifier (name) of the save slot.</param>
        /// <param name="data">Data to serialize.</param>
        Awaitable Save (string slotId, TData data);
        /// <summary>
        /// Loads (de-serializes) a save slot with the specified ID;
        /// returns null in case requested save slot doesn't exist.
        /// </summary>
        /// <param name="slotId">Unique identifier (name) of the save slot.</param>
        Awaitable<TData> Load (string slotId);
        /// <summary>
        /// Loads (de-serializes) a save slot with the specified ID; 
        /// will create a new default <typeparamref name="TData"/> and save it under the specified slot ID in case it doesn't exist.
        /// </summary>
        /// <param name="slotId">Unique identifier (name) of the save slot.</param>
        Awaitable<TData> LoadOrDefault (string slotId);
    }
}

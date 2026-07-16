using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages serializable data instances (slots) using <see cref="UnityEngine.PlayerPrefs"/>.
    /// </summary>
    public abstract class PlayerPrefsSaveSlotManager : ISaveSlotManager
    {
        public event Action<string> OnBeforeSave;
        public event Action<string> OnSaved;
        public event Action<string> OnBeforeLoad;
        public event Action<string> OnLoaded;
        public event Action<string> OnBeforeDelete;
        public event Action<string> OnDeleted;
        public event Action<string, string> OnBeforeRename;
        public event Action<string, string> OnRenamed;

        public bool Loading { get; private set; }
        public bool Saving { get; private set; }

        protected abstract bool PrettifyJson { get; }
        protected abstract bool Binary { get; }
        protected virtual string KeyPrefix => GetType().FullName;
        protected virtual string IndexKey => KeyPrefix + "Index";
        protected virtual string IndexDelimiter => "|";

        public virtual bool SaveSlotExists (string slotId)
        {
            return PlayerPrefs.HasKey(SlotIdToKey(slotId));
        }

        public virtual bool AnySaveExists ()
        {
            var prefsValue = PlayerPrefs.GetString(IndexKey);
            if (string.IsNullOrEmpty(prefsValue)) return false;
            return ParseIndexList(prefsValue).Count > 0;
        }

        public void CollectSlotIds (ICollection<string> ids)
        {
            var prefsValue = PlayerPrefs.GetString(IndexKey);
            if (string.IsNullOrEmpty(prefsValue)) return;
            foreach (var key in ParseIndexList(prefsValue))
                ids.Add(KeyToSlotId(key));
        }

        public virtual void DeleteSaveSlot (string slotId)
        {
            if (!SaveSlotExists(slotId)) return;

            InvokeOnBeforeDelete(slotId);
            var slotKey = SlotIdToKey(slotId);
            PlayerPrefs.DeleteKey(slotKey);
            RemoveKeyIndex(slotKey);
            PlayerPrefs.Save();
            InvokeOnDeleted(slotId);
        }

        public virtual void RenameSaveSlot (string sourceSlotId, string destSlotId)
        {
            if (!SaveSlotExists(sourceSlotId)) return;

            var sourceKey = SlotIdToKey(sourceSlotId);
            var destKey = SlotIdToKey(destSlotId);
            var sourceValue = PlayerPrefs.GetString(sourceKey);

            InvokeOnBeforeRename(sourceSlotId, destSlotId);
            DeleteSaveSlot(sourceSlotId);
            PlayerPrefs.SetString(destKey, sourceValue);
            AddKeyIndexIfNotExist(destKey);
            PlayerPrefs.Save();
            InvokeOnRenamed(sourceSlotId, destSlotId);
        }

        protected virtual string SlotIdToKey (string slotId)
        {
            return KeyPrefix + slotId;
        }

        protected virtual string KeyToSlotId (string key)
        {
            return key.GetAfterFirst(KeyPrefix);
        }

        protected virtual void AddKeyIndexIfNotExist (string slotKey)
        {
            if (!PlayerPrefs.HasKey(IndexKey))
                PlayerPrefs.SetString(IndexKey, string.Empty);

            var indexList = ParseIndexList(PlayerPrefs.GetString(IndexKey));
            if (indexList.Exists(i => i == slotKey)) return;

            indexList.Add(slotKey);
            var index = string.Join(IndexDelimiter, indexList);
            PlayerPrefs.SetString(IndexKey, index);
        }

        protected virtual void RemoveKeyIndex (string slotKey)
        {
            if (!PlayerPrefs.HasKey(IndexKey)) return;

            var indexList = ParseIndexList(PlayerPrefs.GetString(IndexKey));
            if (!indexList.Remove(slotKey)) return;

            var index = string.Join(IndexDelimiter, indexList);
            PlayerPrefs.SetString(IndexKey, index);
        }

        private List<string> ParseIndexList (string prefsValue)
        {
            return prefsValue.Split(new[] { IndexDelimiter }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        protected void InvokeOnBeforeSave (string slotId)
        {
            Saving = true;
            OnBeforeSave?.Invoke(slotId);
        }

        protected void InvokeOnSaved (string slotId)
        {
            Saving = false;
            OnSaved?.Invoke(slotId);
        }

        protected void InvokeOnBeforeLoad (string slotId)
        {
            Loading = true;
            OnBeforeLoad?.Invoke(slotId);
        }

        protected void InvokeOnLoaded (string slotId)
        {
            Loading = false;
            OnLoaded?.Invoke(slotId);
        }

        protected void InvokeOnBeforeDelete (string slotId)
        {
            OnBeforeDelete?.Invoke(slotId);
        }

        protected void InvokeOnDeleted (string slotId)
        {
            OnDeleted?.Invoke(slotId);
        }

        protected void InvokeOnBeforeRename (string sourceSlotId, string destSlotId)
        {
            OnBeforeRename?.Invoke(sourceSlotId, destSlotId);
        }

        protected void InvokeOnRenamed (string sourceSlotId, string destSlotId)
        {
            OnRenamed?.Invoke(sourceSlotId, destSlotId);
        }
    }

    /// <summary>
    /// Manages serializable <typeparamref name="TData"/> instances (slots) using <see cref="UnityEngine.PlayerPrefs"/>.
    /// </summary>
    public class PlayerPrefsSaveSlotManager<TData> : PlayerPrefsSaveSlotManager, ISaveSlotManager<TData> where TData : class, new()
    {
        protected override bool PrettifyJson => Debug.isDebugBuild;
        protected override bool Binary => false;

        private bool saveInProgress;

        public virtual async Awaitable Save (string slotId, TData data)
        {
            while (saveInProgress && Application.isPlaying)
                await Async.NextFrame();

            saveInProgress = true;

            InvokeOnBeforeSave(slotId);

            await SerializeData(slotId, data);
            InvokeOnSaved(slotId);

            saveInProgress = false;
        }

        public virtual void SaveSync (string slotId, TData data)
        {
            saveInProgress = true;
            InvokeOnBeforeSave(slotId);
            SerializeDataSync(slotId, data);
            InvokeOnSaved(slotId);
            saveInProgress = false;
        }

        public virtual async Awaitable<TData> Load (string slotId)
        {
            InvokeOnBeforeLoad(slotId);

            if (!SaveSlotExists(slotId))
                throw new Error($"Slot '{slotId}' not found when loading '{typeof(TData)}' data.");

            var data = await DeserializeData(slotId);
            InvokeOnLoaded(slotId);

            return data;
        }

        public virtual async Awaitable<TData> LoadOrDefault (string slotId)
        {
            if (!SaveSlotExists(slotId))
                await SerializeData(slotId, new());

            return await Load(slotId);
        }

        protected virtual async Awaitable SerializeData (string slotId, TData data)
        {
            var jsonData = JsonUtility.ToJson(data, PrettifyJson);
            var slotKey = SlotIdToKey(slotId);

            if (Binary)
            {
                var bytes = await StringUtils.ZipStringAsync(jsonData);
                jsonData = Convert.ToBase64String(bytes);
            }

            PlayerPrefs.SetString(slotKey, jsonData);
            AddKeyIndexIfNotExist(slotKey);
            PlayerPrefs.Save();
        }

        protected virtual void SerializeDataSync (string slotId, TData data)
        {
            var jsonData = JsonUtility.ToJson(data, PrettifyJson);
            var slotKey = SlotIdToKey(slotId);

            if (Binary)
            {
                var bytes = StringUtils.ZipString(jsonData);
                jsonData = Convert.ToBase64String(bytes);
            }

            PlayerPrefs.SetString(slotKey, jsonData);
            AddKeyIndexIfNotExist(slotKey);
            PlayerPrefs.Save();
        }

        protected virtual async Awaitable<TData> DeserializeData (string slotId)
        {
            var slotKey = SlotIdToKey(slotId);
            var jsonData = default(string);

            if (Binary)
            {
                var base64 = PlayerPrefs.GetString(slotKey);
                var bytes = Convert.FromBase64String(base64);
                jsonData = await StringUtils.UnzipStringAsync(bytes);
            }
            else jsonData = PlayerPrefs.GetString(slotKey);

            return JsonUtility.FromJson<TData>(jsonData) ?? new TData();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages serializable data instances (slots) using local file system (<see cref="System.IO.File"/>).
    /// </summary>
    public abstract class IOSaveSlotManager : ISaveSlotManager
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

        protected virtual string GameDataPath => GetGameDataPath();
        protected virtual string SaveDataPath => $"{GameDataPath}/SaveData";
        protected abstract bool PrettifyJson { get; }
        protected abstract bool Binary { get; }
        protected abstract string Extension { get; }

        public virtual bool SaveSlotExists (string slotId)
        {
            return File.Exists(SlotIdToFilePath(slotId));
        }

        public virtual bool AnySaveExists ()
        {
            if (!Directory.Exists(SaveDataPath)) return false;
            return Directory.GetFiles(SaveDataPath, $"*.{Extension}", SearchOption.TopDirectoryOnly).Length > 0;
        }

        public virtual void CollectSlotIds (ICollection<string> ids)
        {
            if (!Directory.Exists(SaveDataPath)) return;
            foreach (var path in Directory.GetFiles(SaveDataPath, $"*.{Extension}", SearchOption.TopDirectoryOnly))
                if (FilePathToSlotId(path) is { } id)
                    ids.Add(id);
        }

        public virtual void DeleteSaveSlot (string slotId)
        {
            if (!SaveSlotExists(slotId)) return;
            InvokeOnBeforeDelete(slotId);
            IOUtils.DeleteFile(SlotIdToFilePath(slotId));
            InvokeOnDeleted(slotId);
        }

        public virtual void RenameSaveSlot (string sourceSlotId, string destSlotId)
        {
            if (!SaveSlotExists(sourceSlotId)) return;

            InvokeOnBeforeRename(sourceSlotId, destSlotId);
            var sourceFilePath = SlotIdToFilePath(sourceSlotId);
            var destFilePath = SlotIdToFilePath(destSlotId);
            IOUtils.MoveFile(sourceFilePath, destFilePath);
            InvokeOnRenamed(sourceSlotId, destSlotId);
        }

        protected virtual string SlotIdToFilePath (string slotId)
        {
            return $"{SaveDataPath}/{slotId}.{Extension}";
        }

        [CanBeNull]
        protected virtual string FilePathToSlotId (string filePath)
        {
            return PathUtils.FormatPath(filePath).GetBetween($"{SaveDataPath}/", $".{Extension}");
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

        protected virtual string GetGameDataPath ()
        {
            #if UNITY_EDITOR
            return PathUtils.FormatPath(Application.dataPath);
            #elif UNITY_WEBGL // on web Unity appends URL hash to the path, which changes when updating builds on itch.io, so use the appid instead
            return $"/idbfs/{CryptoUtils.PersistentHexCode(Application.identifier)}";
            #else
            return PathUtils.FormatPath(Application.persistentDataPath);
            #endif
        }
    }

    /// <summary>
    /// Manages serializable <typeparamref name="TData"/> instances (slots) using local file system (<see cref="System.IO.File"/>).
    /// </summary>
    public class IOSaveSlotManager<TData> : IOSaveSlotManager, ISaveSlotManager<TData> where TData : class, new()
    {
        protected override bool PrettifyJson => Debug.isDebugBuild;
        protected override bool Binary => false;
        protected override string Extension => "json";

        private bool saveInProgress;

        public async Awaitable Save (string slotId, TData data)
        {
            while (saveInProgress && Application.isPlaying)
                await Async.NextFrame();

            saveInProgress = true;

            InvokeOnBeforeSave(slotId);

            await SerializeData(slotId, data);
            InvokeOnSaved(slotId);

            saveInProgress = false;
        }

        public async Awaitable<TData> Load (string slotId)
        {
            InvokeOnBeforeLoad(slotId);

            if (!SaveSlotExists(slotId))
                throw new Error($"Slot '{slotId}' not found when loading '{typeof(TData)}' data.");

            var data = await DeserializeData(slotId);
            InvokeOnLoaded(slotId);

            return data;
        }

        public async Awaitable<TData> LoadOrDefault (string slotId)
        {
            if (!SaveSlotExists(slotId))
                await SerializeData(slotId, new());

            return await Load(slotId);
        }

        protected virtual async Awaitable SerializeData (string slotId, TData data)
        {
            var jsonData = JsonUtility.ToJson(data, PrettifyJson);
            var filePath = SlotIdToFilePath(slotId);
            IOUtils.CreateDirectory(SaveDataPath);

            if (Binary)
            {
                var bytes = await StringUtils.ZipStringAsync(jsonData);
                await IOUtils.WriteFile(filePath, bytes);
            }
            else await IOUtils.WriteTextFile(filePath, jsonData);
        }

        protected virtual async Awaitable<TData> DeserializeData (string slotId)
        {
            var filePath = SlotIdToFilePath(slotId);
            var jsonData = default(string);

            if (Binary)
            {
                var bytes = await IOUtils.ReadFile(filePath);
                jsonData = await StringUtils.UnzipStringAsync(bytes);
            }
            else jsonData = await IOUtils.ReadTextFile(filePath);

            return JsonUtility.FromJson<TData>(jsonData) ?? new TData();
        }
    }
}

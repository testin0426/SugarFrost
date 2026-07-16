#if ADDRESSABLES_AVAILABLE

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Naninovel
{
    /// <summary>
    /// Implementation of the <see cref="IAddressables"/> when the package is installed in the project.
    /// </summary>
    public class UnityAddressables : IAddressables
    {
        private readonly AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        private readonly Dictionary<string, AddressableAssetEntry> byFullPath = new();

        public UnityAddressables ()
        {
            foreach (var group in settings.groups)
            foreach (var entry in group.entries)
                HandleAdded(entry);
            settings.OnModification -= HandleEvent;
            settings.OnModification += HandleEvent;
        }

        public void Register (string guid, string fullPath)
        {
            if (byFullPath.GetValueOrDefault(fullPath) is { } entry)
            {
                if (entry.guid == guid) return;
                entry.SetAddress(AddressableUtils.BuildAddress(fullPath));
                EditorUtility.SetDirty(settings);
                return;
            }
            var group = GetOrCreateGroup();
            entry = settings.CreateOrMoveEntry(guid, group);
            entry.SetAddress(AddressableUtils.BuildAddress(fullPath));
            byFullPath[fullPath] = entry;
            EditorUtility.SetDirty(settings);
        }

        public void UnregisterResource (string fullPath)
        {
            byFullPath.Remove(fullPath);
        }

        public void UnregisterAsset (string guid)
        {
            var any = false;
            foreach (var (fullPath, entry) in byFullPath.ToArray())
                if (guid == entry.guid)
                {
                    any = byFullPath.Remove(fullPath);
                    entry.parentGroup?.RemoveAssetEntry(entry);
                }
            if (any) EditorUtility.SetDirty(settings);
        }

        public void CollectResources (string guid, ICollection<string> fullPaths)
        {
            foreach (var (fullPath, entry) in byFullPath)
                if (entry.guid == guid)
                    fullPaths.Add(fullPath);
        }

        public void CollectAssets (ICollection<string> guids)
        {
            using var _ = SetPool<string>.Rent(out var collectedGuids);
            foreach (var entry in byFullPath.Values)
                if (collectedGuids.Add(entry.guid))
                    guids.Add(entry.guid);
        }

        public void Label ()
        {
            AddressablesLabeler.Label(settings);
        }

        public void Build (IEnumerable<string> excludeGuids = null)
        {
            using var _ = ListPool<(AddressableAssetEntry, AddressableAssetGroup)>.Rent(out var excluded);
            try
            {
                using var __ = SetPool<string>.Rent(out var excludedGuidSet);
                excludedGuidSet.UnionWith(excludeGuids);
                foreach (var entry in byFullPath.Values.ToArray())
                    if (excludedGuidSet.Contains(entry.guid))
                    {
                        entry.parentGroup?.RemoveAssetEntry(entry);
                        excluded.Add((entry, entry.parentGroup));
                    }

                // Ensure all assigned assets are registered with addressables.
                using var ___ = Assets.Rent(out var assets);
                foreach (var asset in assets)
                    if (!excludedGuidSet.Contains(asset.Guid))
                        Register(asset.Guid, asset.FullPath);

                AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
                AddressableAssetSettings.BuildPlayerContent();
            }
            finally
            {
                foreach (var (entry, group) in excluded)
                    settings.MoveEntry(entry, group);
            }
        }

        private void HandleEvent (AddressableAssetSettings _,
            AddressableAssetSettings.ModificationEvent evt, object payload)
        {
            if (payload is AddressableAssetEntry entry) HandleEvent(evt, entry);
            if (payload is IEnumerable<AddressableAssetEntry> entries)
                foreach (var e in entries)
                    HandleEvent(evt, e);
        }

        private void HandleEvent (AddressableAssetSettings.ModificationEvent evt, AddressableAssetEntry entry)
        {
            if (evt == AddressableAssetSettings.ModificationEvent.EntryAdded) HandleAdded(entry);
            if (evt == AddressableAssetSettings.ModificationEvent.EntryRemoved) HandleRemoved(entry);
            if (evt == AddressableAssetSettings.ModificationEvent.EntryMoved ||
                evt == AddressableAssetSettings.ModificationEvent.EntryModified) HandleMoved(entry);
        }

        private void HandleAdded (AddressableAssetEntry entry)
        {
            if (!AddressableUtils.IsResourceAddress(entry.address)) return;
            using var _ = Assets.RentWithGuid(entry.guid, out var assets);
            foreach (var asset in assets)
                byFullPath[asset.FullPath] = entry;
        }

        private void HandleRemoved (AddressableAssetEntry entry)
        {
            foreach (var (fullPath, exEntry) in byFullPath.ToArray())
                if (entry.guid == exEntry.guid)
                    byFullPath.Remove(fullPath);
        }

        private void HandleMoved (AddressableAssetEntry entry)
        {
            HandleRemoved(entry);
            HandleAdded(entry);
        }

        private AddressableAssetGroup GetOrCreateGroup ()
        {
            var group = settings.FindGroup(AddressableUtils.Group);
            if (group) return group;
            group = settings.CreateGroup(AddressableUtils.Group, false, false, true, settings.DefaultGroup.Schemas);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.OnlyHash;
            return group;
        }
    }
}

#endif

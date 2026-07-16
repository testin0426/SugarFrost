using System;
using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Manages reference-based transient input mute state.
    /// </summary>
    public class InputMuteRegistry : IDisposable
    {
        protected virtual Dictionary<object, HashSet<string>> AllowedIdsByMuter { get; } = new();
        protected virtual HashSet<string> AllowedIds { get; } = new();

        public virtual void AddMuter (object muter, IReadOnlyCollection<string> allowedIds = null)
        {
            if (AllowedIdsByMuter.ContainsKey(muter)) return;
            if (allowedIds == null || allowedIds.Count == 0)
                AllowedIdsByMuter[muter] = null;
            else
            {
                SetPool<string>.Rent(out var rentedAllowedIds);
                rentedAllowedIds.UnionWith(allowedIds);
                AllowedIdsByMuter[muter] = rentedAllowedIds;
            }
            UpdateAllowedIds();
        }

        public virtual void RemoveMuter (object muter)
        {
            if (!AllowedIdsByMuter.ContainsKey(muter)) return;
            if (AllowedIdsByMuter[muter] is { } allowedIds)
                SetPool<string>.Return(allowedIds);
            AllowedIdsByMuter.Remove(muter);
            UpdateAllowedIds();
        }

        public virtual bool IsAllowed (string id)
        {
            if (AllowedIdsByMuter.Count == 0) return true;
            return AllowedIds.Contains(id);
        }

        public virtual void Dispose ()
        {
            foreach (var ids in AllowedIdsByMuter.Values)
                if (ids != null)
                    SetPool<string>.Return(ids);
            AllowedIdsByMuter.Clear();
        }

        protected virtual void UpdateAllowedIds ()
        {
            AllowedIds.Clear();
            var united = false;
            foreach (var ids in AllowedIdsByMuter.Values)
            {
                if (ids == null || ids.Count == 0) // a muter muted everything
                {
                    AllowedIds.Clear();
                    return;
                }
                if (!united) // initial allowed subset
                {
                    AllowedIds.UnionWith(ids);
                    united = true;
                }
                else AllowedIds.IntersectWith(ids); // subsequent allowed subset
            }
        }
    }
}

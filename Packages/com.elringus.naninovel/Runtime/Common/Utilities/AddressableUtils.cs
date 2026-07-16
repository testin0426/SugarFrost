#if ADDRESSABLES_AVAILABLE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Naninovel
{
    /// <summary>
    /// Provides utilities and extension methods for the Addressables APIs.
    /// </summary>
    public static class AddressableUtils
    {
        /// <summary>
        /// Identifier of the addressable group managed by Naninovel.
        /// </summary>
        public const string Group = "Naninovel";
        /// <summary>
        /// Prefix of the addresses assigned to the Naninovel assets.
        /// </summary>
        public const string AddressPrefix = "Naninovel/";
        /// <summary>
        /// String used to separate addresses containing multiples resource paths.
        /// </summary>
        public const string JoinSeparator = ", ";

        /// <summary>
        /// Checks whether specified address is a Naninovel resource address.
        /// </summary>
        public static bool IsResourceAddress ([CanBeNull] string address)
        {
            if (address == null) return false;
            return address.StartsWithOrdinal(AddressPrefix);
        }

        /// <summary>
        /// Assuming specified addressable address is a Naninovel resource address,
        /// collects associated full resource paths to the specified collection.
        /// </summary>
        public static void CollectPaths (string address, ICollection<string> fullPaths)
        {
            if (!IsResourceAddress(address)) return;
            if (address.Contains(JoinSeparator))
                foreach (var addr in address.Split(JoinSeparator))
                    fullPaths.Add(addr.GetAfterFirst(AddressPrefix));
            else fullPaths.Add(address.GetAfterFirst(AddressPrefix));
        }

        /// <summary>
        /// Assuming specified addressable address is a Naninovel resource address,
        /// rents a pooled list and collects associated full resource paths.
        /// </summary>
        public static IDisposable RentPaths (string address, out List<string> fullPaths)
        {
            var rent = ListPool<string>.Rent(out fullPaths);
            CollectPaths(address, fullPaths);
            return rent;
        }

        /// <summary>
        /// Builds addressable address from the specified full resource path.
        /// </summary>
        public static string BuildAddress (string fullPath)
        {
            if (Assets.Get(fullPath) is not { } asset || Assets.CountWithGuid(asset.Guid) == 1)
                return $"{AddressPrefix}{fullPath}";
            using var _ = Assets.RentWithGuid(asset.Guid, out var assets);
            return string.Join(JoinSeparator, assets
                .OrderBy(a => a.FullPath)
                .Select(a => $"{AddressPrefix}{a.FullPath}"));
        }

        /// <summary>
        /// Allows awaiting addressable operations.
        /// </summary>
        public static Awaitable<T>.Awaiter GetAwaiter<T> (this AsyncOperationHandle<T> op)
        {
            return WaitCompletion(op).GetAwaiter();

            static async Awaitable<T> WaitCompletion (AsyncOperationHandle<T> op)
            {
                // Awaiting the method directly fails on WebGL (they're using multithreaded Task fot GetAwaiter)
                while (op.IsValid() && !op.IsDone) await Async.NextFrame(Engine.DestroyToken);
                return op.IsValid() ? op.Result : default;
            }
        }
    }
}

#endif

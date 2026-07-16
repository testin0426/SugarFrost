using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.PackageManager;

namespace Naninovel
{
    /// <summary>
    /// Allows installing UPM packages and allows attaching a callback for when the operation finishes.
    /// </summary>
    public class PackageInstaller
    {
        private const string cbKey = "NaninovelPackageInstallerCallbackInfo";

        /// <summary>
        /// Initializes the installer and invokes the callback if pending.
        /// </summary>
        public static void Initialize ()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(cbKey, "")))
                WaitStabilization();
        }

        /// <summary>
        /// Installs packages with the specified names and invokes the specified static callback when finished.
        /// </summary>
        /// <remarks>
        /// Unity's UPM client is extremely flaky, especially when installing multiple packages with dependencies:
        /// it triggers multiple script compilations (domain reloads) and asset imports, chained in an unpredictable
        /// order with random pauses between events. This utility makes the best effort to detect when all
        /// installation phases are complete and invokes the callback only then.
        /// </remarks>
        public static void Install (string[] packages, [CanBeNull] Action callback = null)
        {
            var task = Client.AddAndRemove(packages);
            if (callback == null) return;

            var cbInfo = $"{callback.Method.DeclaringType!.AssemblyQualifiedName}:{callback.Method.Name}";
            SessionState.SetString(cbKey, cbInfo);
            EditorUtils.OnUpdate(() => {
                if (!task.IsCompleted) return false;
                WaitStabilization();
                return true;
            });
        }

        private static void WaitStabilization ()
        {
            var req = Client.List(true); // using list request as a barrier to ensure UPM is fully idle
            EditorUtils.OnUpdate(() => {
                if (!req.IsCompleted || EditorApplication.isCompiling || EditorApplication.isUpdating) return false;
                EditorApplication.delayCall += InvokeCallback; // delay to guard against random phase pauses
                return true;
            });
        }

        private static void InvokeCallback ()
        {
            var cbInfo = SessionState.GetString(cbKey, "");
            SessionState.EraseString(cbKey);
            var splitIdx = cbInfo.LastIndexOf(':');
            if (splitIdx == -1) return;

            var cbTypeName = cbInfo[..splitIdx];
            var cbName = cbInfo[(splitIdx + 1)..];
            var cbType = Type.GetType(cbTypeName);
            var cb = cbType?.GetMethod(cbName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (cb == null) throw new Error("Failed to execute package installer callback: method not found");
            if (!cb.IsStatic) throw new Error("Failed to execute package installer callback: method is not static.");

            cb.Invoke(null, null);
        }
    }
}

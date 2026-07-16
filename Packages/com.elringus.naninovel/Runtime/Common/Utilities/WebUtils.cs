using UnityEngine;

namespace Naninovel
{
    public static class WebUtils
    {
        public static AudioType EvaluateAudioTypeFromMime (string mimeType) => mimeType switch {
            "audio/aiff" => AudioType.AIFF,
            "audio/mpeg" or "audio/mpeg3" or "audio/mp3" => AudioType.MPEG,
            "audio/ogg" or "video/ogg" => AudioType.OGGVORBIS,
            "audio/wav" => AudioType.WAV,
            _ => AudioType.UNKNOWN
        };

        /// <summary>
        /// Navigates to the specified URL using default or current web browser.
        /// </summary>
        /// <remarks>
        /// When used outside WebGL or in editor will use <see cref="Application.OpenURL"/>,
        /// otherwise native window.open() JS function is used.
        /// </remarks>
        /// <param name="url">The URL to navigate to.</param>
        /// <param name="target">Browsing context: _self, _blank, _parent, _top. Not supported outside WebGL.</param>
        public static void OpenURL (string url, string target = "_self")
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            OpenWindow(url, target);
            #else
            Application.OpenURL(url);
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// Calls FS.syncfs in native js.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("__Internal", EntryPoint = "naninovelSyncFs")]
        public static extern void SyncFs ();
        /// <summary>
        /// Invokes window.open() with the specified parameters.
        /// https://developer.mozilla.org/en-US/docs/Web/API/Window/open
        /// </summary>
        [System.Runtime.InteropServices.DllImport("__Internal", EntryPoint = "naninovelOpenWindow")]
        public static extern void OpenWindow (string url, string target);
        #endif
    }
}

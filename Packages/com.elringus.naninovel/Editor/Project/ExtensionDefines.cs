using UnityEditor;

namespace Naninovel
{
    public static class ExtensionDefines
    {
        #if NANINOVEL_ENABLE_LIVE2D
        [MenuItem(MenuPath.Root + "/Extensions/Disable Live2D")]
        private static void DisableLive2D () => EditorUtils.UndefineSymbol("NANINOVEL_ENABLE_LIVE2D");
        #else
        [MenuItem(MenuPath.Root + "/Extensions/Enable Live2D")]
        private static void EnableLive2D () => EditorUtils.DefineSymbol("NANINOVEL_ENABLE_LIVE2D");
        #endif

        #if NANINOVEL_ENABLE_SPINE
        [MenuItem(MenuPath.Root + "/Extensions/Disable Spine")]
        private static void DisableSpine () => EditorUtils.UndefineSymbol("NANINOVEL_ENABLE_SPINE");
        #else
        [MenuItem(MenuPath.Root + "/Extensions/Enable Spine")]
        private static void EnableSpine () => EditorUtils.DefineSymbol("NANINOVEL_ENABLE_SPINE");
        #endif
    }
}

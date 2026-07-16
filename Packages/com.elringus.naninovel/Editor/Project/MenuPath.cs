using UnityEditor;

namespace Naninovel
{
    public static class MenuPath
    {
        #if NANINOVEL_MENU_UNDER_TOOLS
        [MenuItem(Root + "/Move To Top", priority = 9999)]
        public static void ToTop () => EditorUtils.UndefineSymbol("NANINOVEL_MENU_UNDER_TOOLS");
        public const string Root = "Tools/Naninovel";
        #else
        [MenuItem(Root + "/Move Under \"Tools\"", priority = 9999)]
        public static void ToTools () => EditorUtils.DefineSymbol("NANINOVEL_MENU_UNDER_TOOLS");
        public const string Root = "Naninovel";
        #endif
    }
}

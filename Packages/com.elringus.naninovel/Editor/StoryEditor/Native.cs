using System;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.CallingConvention;
using As = System.Runtime.InteropServices.MarshalAsAttribute;
using Fn = System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute;
using Dll = System.Runtime.InteropServices.DllImportAttribute;

namespace Naninovel.StoryEditor
{
    public static class Native
    {
        #if UNITY_EDITOR_OSX
        private const string dll = "Naninovel.StoryEditor.dylib";
        #else
        private const string dll = "Naninovel.StoryEditor";
        #endif
        private const UnmanagedType Utf8 = UnmanagedType.LPUTF8Str;

        [Fn(Cdecl)] public delegate void DirtyCb ();
        [Fn(Cdecl)] public delegate IntPtr ReadFile ([As(Utf8)] string name);
        [Fn(Cdecl)] public delegate void WriteFile ([As(Utf8)] string name, [As(Utf8)] string content);
        [Fn(Cdecl)] public delegate IntPtr ListFiles ();
        [Fn(Cdecl)] public delegate void Reload ([As(Utf8)] string text);
        [Fn(Cdecl)] public delegate byte NavCb ([As(Utf8)] string url);
        [Fn(Cdecl)] public delegate void FocusCb (byte focused);
        [Fn(Cdecl)] public delegate void InitCb ();
        [Fn(Cdecl)] public delegate void DeinitCb ();

        [Dll(dll, EntryPoint = "init", CallingConvention = Cdecl)]
        public static extern void Init (
            [As(Utf8)] string fsUserDir, [As(Utf8)] string fsProjDir, [As(Utf8)] string fsProjFilters,
            DirtyCb fsDirtyCb, ReadFile brRead, WriteFile brWrite, ListFiles brList, Reload brReload,
            IntPtr viewHostWindow, [As(Utf8)] string viewHomeUrl, [As(Utf8)] string viewDataDir,
            NavCb viewNavCb, FocusCb viewFocusCb, InitCb viewInitCb);
        [Dll(dll, EntryPoint = "deinit", CallingConvention = Cdecl)]
        public static extern void Deinit (DeinitCb deinitCb);
        [Dll(dll, EntryPoint = "find_window", CallingConvention = Cdecl)]
        public static extern IntPtr FindWindow ([As(Utf8)] string title);
        [Dll(dll, EntryPoint = "handle_bridging_changes", CallingConvention = Cdecl)]
        public static extern void HandleBridgingChanges ([As(Utf8)] string name);
        [Dll(dll, EntryPoint = "resize_view", CallingConvention = Cdecl)]
        public static extern void ResizeView (double x, double y, double width, double height);
        [Dll(dll, EntryPoint = "set_view_visible", CallingConvention = Cdecl)]
        public static extern void SetViewVisible (byte visible);
        [Dll(dll, EntryPoint = "preview_file", CallingConvention = Cdecl)]
        public static extern void PreviewFile ([As(Utf8)] string uri);
    }
}

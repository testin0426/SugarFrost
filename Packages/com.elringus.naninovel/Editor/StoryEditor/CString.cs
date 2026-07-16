using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Naninovel.StoryEditor
{
    public sealed class CString : IDisposable
    {
        private readonly byte[] buffer;
        private readonly IntPtr ptr;
        private GCHandle handle;

        public CString (int size)
        {
            buffer = new byte[size];
            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            ptr = handle.AddrOfPinnedObject();
        }

        public IntPtr Write (string str)
        {
            var len = Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
            buffer[len] = 0; // append null terminator to the c string
            return ptr;
        }

        public void Dispose ()
        {
            if (handle.IsAllocated)
                handle.Free();
        }

        ~CString () => Dispose();
    }
}

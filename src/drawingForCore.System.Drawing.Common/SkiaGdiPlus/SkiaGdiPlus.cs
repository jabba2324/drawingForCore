// drawingForCore: the GDI+ flat API on SkiaSharp, in place of libgdiplus (tools/lift/lift_drawing.py).
// Licensed under the MIT license (as System.Drawing.Common).
#nullable disable
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Drawing
{
    // Microsoft's System.Drawing calls these functions with the signatures of its libgdiplus P/Invokes. GDI+ objects
    // are managed objects here; the handle the caller keeps (an IntPtr, passed back in a HandleRef) is a number that
    // the handle table maps to the object, until the matching Gdip*Delete/Dispose function removes it (the wrapper's
    // Dispose or finaliser calls it). Results are GDI+ Status codes.
    internal static partial class SafeNativeMethods
    {
    internal static unsafe partial class SkiaGdiPlus
    {
        // GDI+ Status (gdiplustypes.h)
        internal const int Ok = 0;
        internal const int GenericError = 1;
        internal const int InvalidParameter = 2;
        internal const int OutOfMemory = 3;
        internal const int ObjectBusy = 4;
        internal const int InsufficientBuffer = 5;
        internal const int NotImplemented = 6;
        internal const int Win32Error = 7;
        internal const int WrongState = 8;
        internal const int Aborted = 9;
        internal const int FileNotFound = 10;
        internal const int ValueOverflow = 11;
        internal const int AccessDenied = 12;
        internal const int UnknownImageFormat = 13;
        internal const int FontFamilyNotFound = 14;
        internal const int FontStyleNotFound = 15;
        internal const int NotTrueTypeFont = 16;
        internal const int UnsupportedGdiplusVersion = 17;
        internal const int GdiplusNotInitialized = 18;
        internal const int PropertyNotFound = 19;
        internal const int PropertyNotSupported = 20;

        // The handle table: a plain dictionary, not GCHandles, so nothing here steers the GC (CONTRIBUTING.md, rule 9),
        // and an unknown or already freed handle is InvalidParameter instead of a crash.
        static readonly Dictionary<IntPtr, object> s_handles = new Dictionary<IntPtr, object>();
        static long s_lastHandle;

        internal static IntPtr NewHandle(object value)
        {
            var handle = (IntPtr)Interlocked.Increment(ref s_lastHandle);
            lock (s_handles) s_handles.Add(handle, value);
            return handle;
        }

        internal static T Get<T>(IntPtr handle) where T : class
        {
            if (handle == IntPtr.Zero) return null;
            lock (s_handles) return s_handles.TryGetValue(handle, out var value) ? value as T : null;
        }

        // Removes the handle without disposing what it refers to (a font family shared with its collection).
        internal static bool Release(IntPtr handle)
        {
            lock (s_handles) return s_handles.Remove(handle);
        }

        internal static T Get<T>(HandleRef handle) where T : class => Get<T>(handle.Handle);

        internal static int Free(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return InvalidParameter;
            object value;
            lock (s_handles) { if (!s_handles.Remove(handle, out value)) return InvalidParameter; }
            (value as IDisposable)?.Dispose();
            return Ok;
        }

        internal static int Free(HandleRef handle) => Free(handle.Handle);

        // Startup and memory

        internal static int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output)
        {
            token = (IntPtr)1;
            output = default;
            return Ok;
        }

        internal static void GdiplusShutdown(ref ulong token)
        {
        }

        internal static IntPtr GdipAlloc(int size) => Marshal.AllocHGlobal(size);

        internal static void GdipFree(IntPtr ptr) => Marshal.FreeHGlobal(ptr);
    }
    }
}

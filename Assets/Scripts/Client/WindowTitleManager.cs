using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CraftSharp
{
    /// <summary>
    /// Handles window title updates for standalone builds on supported platforms.
    /// </summary>
    internal static class WindowTitleManager
    {
        private static readonly object titleLock = new();
        private static readonly string DefaultTitle = Application.productName;
        private static string currentTitle = DefaultTitle;

        public static void SetDefaultTitle()
        {
            SetTitle(DefaultTitle);
        }

        public static void SetServerTitle(string description)
        {
            var title = $"{Application.productName} {description}";
            SetTitle(title);
        }

        private static void SetTitle(string title)
        {
#if !UNITY_EDITOR
            lock (titleLock)
            {
                if (string.Equals(currentTitle, title, StringComparison.Ordinal))
                    return;

                try
                {
                    ApplyPlatformTitle(title);
                    currentTitle = title;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set window title: {e.Message}");
                }
            }
#endif
        }

        private static void ApplyPlatformTitle(string title)
        {
#if UNITY_STANDALONE_WIN
            WindowsSetTitle(title);
#elif UNITY_STANDALONE_OSX
            MacSetTitle(title);
#elif UNITY_STANDALONE_LINUX
            LinuxSetTitle(title);
#endif
        }

#if UNITY_STANDALONE_WIN
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetWindowTextW(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        private static void WindowsSetTitle(string title)
        {
            var handle = GetActiveWindow();

            if (handle == IntPtr.Zero)
                handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            if (handle != IntPtr.Zero)
                _ = SetWindowTextW(handle, title);
        }
#endif

#if UNITY_STANDALONE_OSX
        private const string OBJC_LIB = "/usr/lib/libobjc.A.dylib";

        [DllImport(OBJC_LIB)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(OBJC_LIB)]
        private static extern IntPtr sel_registerName(string selName);

        [DllImport(OBJC_LIB, EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(OBJC_LIB, EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [DllImport(OBJC_LIB, EntryPoint = "objc_msgSend")]
        private static extern void objc_msgSend_Void(IntPtr receiver, IntPtr selector, IntPtr arg1);

        private static void MacSetTitle(string title)
        {
            var nsAppClass = objc_getClass("NSApplication");
            var sharedApp = objc_msgSend(nsAppClass, sel_registerName("sharedApplication"));
            if (sharedApp == IntPtr.Zero)
                return;

            var window = objc_msgSend(sharedApp, sel_registerName("mainWindow"));
            if (window == IntPtr.Zero)
                window = objc_msgSend(sharedApp, sel_registerName("keyWindow"));

            if (window == IntPtr.Zero)
                return;

            var nsString = CreateNSString(title);
            if (nsString != IntPtr.Zero)
                objc_msgSend_Void(window, sel_registerName("setTitle:"), nsString);
        }

        private static IntPtr CreateNSString(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var buffer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            Marshal.WriteByte(buffer, bytes.Length, 0);

            var nsStringClass = objc_getClass("NSString");
            var nsString = objc_msgSend_IntPtr(nsStringClass, sel_registerName("stringWithUTF8String:"), buffer);
            Marshal.FreeHGlobal(buffer);

            return nsString;
        }
#endif

#if UNITY_STANDALONE_LINUX
        private const string SDL_LIB = "libSDL2-2.0.so.0";

        [DllImport(SDL_LIB, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SDL_GL_GetCurrentWindow();

        [DllImport(SDL_LIB, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_SetWindowTitle(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);

        private static void LinuxSetTitle(string title)
        {
            var window = SDL_GL_GetCurrentWindow();
            if (window != IntPtr.Zero)
                SDL_SetWindowTitle(window, title);
        }
#endif
    }
}

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Applies the Windows 11 window backdrop (Mica / Acrylic) and Fluent window chrome
    /// attributes, and reports whether window translucency is available.
    ///
    /// Per the design system, the backdrop material is applied <b>once per window</b>.
    /// Content above it uses layer-fill / card-fill brushes — individual elements never
    /// get their own blur or translucency.
    ///
    /// Falls back to an opaque solid background when:
    ///   * the OS is older than Windows 11 build 22000 (no Mica),
    ///   * the user turned off "Transparency effects" in Settings,
    ///   * High Contrast is active.
    /// </summary>
    public static class BackdropService
    {
        public enum Backdrop
        {
            Mica,
            MicaAlt,
            Acrylic,
            None
        }

        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwaSystemBackdropType = 38;
        private const int DwmwaMicaEffect = 1029;

        private const int DwmWindowCornerRound = 2;
        private const int DwmSystemBackdropMainWindow = 2;
        private const int DwmSystemBackdropTransientWindow = 3;
        private const int DwmSystemBackdropTabbedWindow = 4;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        public static bool IsWindows11 => Environment.OSVersion.Version.Build >= 22000;

        /// <summary>True when Mica/Acrylic were applied; false means the caller should paint an opaque background.</summary>
        public static bool BackdropApplied { get; private set; }

        /// <summary>
        /// "Transparency effects" master switch (Windows 11 Settings &gt; Personalisation &gt; Colours),
        /// High Contrast, or Remote Desktop force the opaque fallback.
        /// </summary>
        public static bool TranslucencyAllowed()
        {
            if (SystemParameters.HighContrast) return false;
            if (SystemParameters.IsRemoteSession) return false;
            if (!IsWindows11) return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("EnableTransparency") is int enabled && enabled == 0) return false;
            }
            catch
            {
                // Registry unreadable: assume transparency is fine.
            }

            return true;
        }

        /// <summary>
        /// Configures the window: dark/light caption treatment, rounded corners and the backdrop material.
        /// </summary>
        public static bool Apply(Window window, bool isDarkTheme, Backdrop backdrop = Backdrop.Mica)
        {
            BackdropApplied = false;
            if (window == null) return false;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return false;

            SetDarkMode(hwnd, isDarkTheme);
            SetRoundedCorners(hwnd);

            if (backdrop == Backdrop.None || !TranslucencyAllowed()) return false;

            var backdropType = backdrop switch
            {
                Backdrop.MicaAlt => DwmSystemBackdropTabbedWindow,
                Backdrop.Acrylic => DwmSystemBackdropTransientWindow,
                _ => DwmSystemBackdropMainWindow
            };

            int hr = DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref backdropType, sizeof(int));
            if (hr != 0)
            {
                // Windows 11 21H2 used the undocumented Mica flag instead.
                int on = 1;
                hr = DwmSetWindowAttribute(hwnd, DwmwaMicaEffect, ref on, sizeof(int));
            }

            if (hr != 0) return false;

            // Let the backdrop show through the client area.
            if (HwndSource.FromHwnd(hwnd)?.CompositionTarget is HwndTarget target)
            {
                target.BackgroundColor = Colors.Transparent;
            }
            var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            BackdropApplied = true;
            return true;
        }

        /// <summary>Keeps the caption / border in sync with the in-app theme.</summary>
        public static void SetTheme(Window window, bool isDarkTheme)
        {
            if (window == null) return;
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            SetDarkMode(hwnd, isDarkTheme);
        }

        private static void SetDarkMode(IntPtr hwnd, bool isDark)
        {
            int value = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
        }

        private static void SetRoundedCorners(IntPtr hwnd)
        {
            int preference = DwmWindowCornerRound;
            DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
    }
}

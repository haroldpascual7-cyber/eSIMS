using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace SmartSchoolIncidentManagementSystem.Services
{
    /// <summary>
    /// Reads the user's Windows accent colour and projects it onto the design-system accent
    /// ramp, so the application follows the platform accent instead of shipping a hard-coded
    /// brand blue. Called at startup and whenever the theme changes or the user edits the
    /// Windows accent colour.
    ///
    /// Derived steps (hover / pressed / subtle fill) are documented approximations of the
    /// WinUI accent ramp: the platform does not publish the exact per-step formulas.
    /// </summary>
    public static class SystemAccentProvider
    {
        private static readonly Color FallbackLight = Color.FromRgb(0x00, 0x5F, 0xB8);
        private static readonly Color FallbackDark = Color.FromRgb(0x60, 0xCD, 0xFF);
        private static bool _listening;

        public static Color CurrentAccent { get; private set; } = FallbackLight;

        /// <summary>Starts listening for Windows colour changes and applies the accent once.</summary>
        public static void Initialize(Func<bool> isDarkTheme)
        {
            if (!_listening)
            {
                try
                {
                    SystemEvents.UserPreferenceChanged += (_, _) => Refresh(isDarkTheme());
                    _listening = true;
                }
                catch
                {
                    // Session without SystemEvents (rare): accent simply stays at its last value.
                }
            }

            Refresh(isDarkTheme());
        }

        /// <summary>Re-resolves the system accent and rewrites the accent resources.</summary>
        public static void Refresh(bool isDarkTheme)
        {
            try
            {
                Apply(ResolveAccent(isDarkTheme), isDarkTheme);
            }
            catch
            {
                // Never let theming break startup.
            }
        }

        /// <summary>Applies an explicit accent (used when the user picks a custom accent).</summary>
        public static void Apply(Color accent, bool isDarkTheme)
        {
            CurrentAccent = accent;

            var hover = isDarkTheme ? Lighten(accent, 0.12) : Darken(accent, 0.10);
            var pressed = isDarkTheme ? Lighten(accent, 0.24) : Darken(accent, 0.24);
            var onAccent = PickOnAccent(accent);

            Set("AccentColor", accent);
            SetBrush("AccentFillColorDefaultBrush", accent);
            SetBrush("AccentFillColorSecondaryBrush", hover);
            SetBrush("AccentFillColorTertiaryBrush", pressed);
            SetBrush("AccentSubtleFillBrush", WithAlpha(accent, isDarkTheme ? (byte)0x24 : (byte)0x16));
            SetBrush("AccentStrokeBrush", WithAlpha(accent, isDarkTheme ? (byte)0x3D : (byte)0x2E));
            SetBrush("AccentTextFillColorPrimaryBrush", onAccent);
            SetBrush("TextOnAccentFillColorPrimaryBrush", onAccent);
            SetBrush("SystemFillColorAttentionBrush", accent);

            // Legacy aliases used across the pre-refactor views.
            SetBrush("PrimaryBrush", accent);
            SetBrush("PrimaryHoverBrush", hover);
            SetBrush("PrimaryActiveBrush", pressed);
            SetBrush("AccentBrush", isDarkTheme ? hover : accent);
            SetBrush("InfoBrush", accent);
            SetBrush("ButtonTextBrush", onAccent);
        }

        /// <summary>
        /// Resolves the Windows accent: DWM glass colour first, then the registry values the
        /// Personalisation page writes, then the WinUI default for the active appearance.
        /// </summary>
        public static Color ResolveAccent(bool isDarkTheme)
        {
            try
            {
                var glass = SystemParameters.WindowGlassColor;
                if (glass.A != 0 && (glass.R | glass.G | glass.B) != 0)
                {
                    return Color.FromRgb(glass.R, glass.G, glass.B);
                }
            }
            catch
            {
                // Fall through to the registry.
            }

            try
            {
                using var dwm = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                if (dwm?.GetValue("AccentColor") is int abgr && abgr != 0)
                {
                    // Stored as 0xAABBGGRR.
                    return Color.FromRgb((byte)(abgr & 0xFF), (byte)((abgr >> 8) & 0xFF), (byte)((abgr >> 16) & 0xFF));
                }

                if (dwm?.GetValue("ColorizationColor") is int argb)
                {
                    // Stored as 0xAARRGGBB.
                    return Color.FromRgb((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF));
                }
            }
            catch
            {
                // Fall through to the default.
            }

            return isDarkTheme ? FallbackDark : FallbackLight;
        }

        /// <summary>
        /// Picks the on-accent foreground that clears WCAG AA (4.5:1) against the accent fill.
        /// Prevents unreadable white-on-pale-accent buttons when the user picks a light accent.
        /// </summary>
        public static Color PickOnAccent(Color accent)
        {
            var white = Colors.White;
            var nearBlack = Color.FromRgb(0x0A, 0x0A, 0x0A);
            return ContrastRatio(accent, white) >= ContrastRatio(accent, nearBlack) ? white : nearBlack;
        }

        private static double ContrastRatio(Color a, Color b)
        {
            var la = RelativeLuminance(a);
            var lb = RelativeLuminance(b);
            var lighter = Math.Max(la, lb);
            var darker = Math.Min(la, lb);
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double RelativeLuminance(Color c)
        {
            static double Channel(byte value)
            {
                var v = value / 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        private static Color Lighten(Color color, double amount)
        {
            byte Mix(byte channel) => (byte)Math.Round(channel + (255 - channel) * amount);
            return Color.FromRgb(Mix(color.R), Mix(color.G), Mix(color.B));
        }

        private static Color Darken(Color color, double amount)
        {
            byte Mix(byte channel) => (byte)Math.Round(channel * (1 - amount));
            return Color.FromRgb(Mix(color.R), Mix(color.G), Mix(color.B));
        }

        private static Color WithAlpha(Color color, byte alpha)
            => Color.FromArgb(alpha, color.R, color.G, color.B);

        private static void Set(string key, object value)
        {
            if (Application.Current != null) Application.Current.Resources[key] = value;
        }

        private static void SetBrush(string key, Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            Set(key, brush);
        }
    }
}

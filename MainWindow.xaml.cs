using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels;

namespace SmartSchoolIncidentManagementSystem
{
    /// <summary>
    /// Shell window. Hosts the Fluent title bar, the navigation pane and the active page,
    /// and owns the window-level platform integration (Mica backdrop, caption theme,
    /// rounded corners, opaque fallback when translucency is unavailable).
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += OnSourceInitialized;
            Closed += OnWindowClosed;
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var isDark = ViewModel?.IsDarkMode ?? false;

            // Apply DWM dark caption & rounded corners without Mica/DwmExtendFrameIntoClientArea
            // on WindowStyle="None", which otherwise causes a blank white composition surface.
            BackdropService.Apply(this, isDark, BackdropService.Backdrop.None);
            ShellRoot.SetResourceReference(Panel.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");

            // Clamp maximized bounds to the monitor work area so the Windows taskbar never overlaps the bottom.
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

            if (ViewModel != null) ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MainViewModel.IsDarkMode)) return;

            var isDark = ViewModel?.IsDarkMode ?? false;
            BackdropService.SetTheme(this, isDark);

            if (BackdropService.BackdropApplied)
            {
                ShellRoot.Background = Brushes.Transparent;
            }
            else
            {
                ShellRoot.SetResourceReference(Panel.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            }
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (ViewModel != null) ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnMinimizeWindowClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void OnMaximizeWindowClick(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void OnCloseWindowClick(object sender, RoutedEventArgs e) => Close();

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int wmGetMinMaxInfo = 0x0024;
            if (msg == wmGetMinMaxInfo)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            const uint monitorDefaultToNearest = 0x00000002;
            var monitor = MonitorFromWindow(hwnd, monitorDefaultToNearest);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var work = monitorInfo.rcWork;
                    var mon = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.x = Math.Abs(work.left - mon.left);
                    mmi.ptMaxPosition.y = Math.Abs(work.top - mon.top);
                    mmi.ptMaxSize.x = Math.Abs(work.right - work.left);
                    mmi.ptMaxSize.y = Math.Abs(work.bottom - work.top);
                }
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
    }
}

using System.Windows.Controls;

namespace SmartSchoolIncidentManagementSystem.Views
{
    /// <summary>
    /// Interaction logic for TabletDashboardView.xaml
    /// </summary>
    public partial class TabletDashboardView : UserControl
    {
        public TabletDashboardView()
        {
            InitializeComponent();
        }

        private void OnMinimizeWindowClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.Windows.Window.GetWindow(this) is System.Windows.Window win)
            {
                win.WindowState = System.Windows.WindowState.Minimized;
            }
        }

        private void OnCloseWindowClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (System.Windows.Window.GetWindow(this) is System.Windows.Window win)
            {
                win.Close();
            }
        }
    }
}

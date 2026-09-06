using System.Windows;

namespace SmartSchoolIncidentManagementSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnMinimizeWindowClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseWindowClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

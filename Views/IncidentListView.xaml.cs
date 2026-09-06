using System.Windows.Controls;
using System.Windows.Input;
using SmartSchoolIncidentManagementSystem.ViewModels;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class IncidentListView : UserControl
    {
        public IncidentListView()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem != null)
            {
                dg.ScrollIntoView(dg.SelectedItem);
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is IncidentListViewModel vm && vm.SelectedIncident != null)
            {
                vm.ViewIncidentDetailsCommand.Execute(vm.SelectedIncident);
            }
        }
    }
}

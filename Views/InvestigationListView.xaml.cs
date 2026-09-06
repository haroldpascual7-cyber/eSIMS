using System.Windows.Controls;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class InvestigationListView : UserControl
    {
        public InvestigationListView()
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
    }
}
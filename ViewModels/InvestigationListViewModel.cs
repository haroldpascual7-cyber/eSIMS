using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class InvestigationListViewModel : ObservableObject
    {
        private readonly IIncidentService _incidentService;
        private readonly IInvestigationService _investigationService;
        private readonly Action<Incident> _openWorkspaceAction;
        private readonly int? _preselectedIncidentId;

        private string _searchText = string.Empty;
        private Incident? _selectedIncident;
        private bool _isLoading;

        public InvestigationListViewModel(
            IIncidentService incidentService,
            IInvestigationService investigationService,
            Action<Incident> openWorkspaceAction,
            Incident? preselectedIncident = null)
        {
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _investigationService = investigationService ?? throw new ArgumentNullException(nameof(investigationService));
            _openWorkspaceAction = openWorkspaceAction ?? throw new ArgumentNullException(nameof(openWorkspaceAction));
            _preselectedIncidentId = preselectedIncident?.Id;
            _selectedIncident = preselectedIncident;

            Incidents = new ObservableCollection<Incident>();

            OpenWorkspaceCommand = new RelayCommand(() =>
            {
                if (SelectedIncident != null) _openWorkspaceAction(SelectedIncident);
            }, () => SelectedIncident != null);

            OpenIncidentWorkspaceCommand = new RelayCommand<Incident>(inc =>
            {
                if (inc != null)
                {
                    SelectedIncident = inc;
                    _openWorkspaceAction(inc);
                }
            });

            RefreshCommand = new RelayCommand(async () => await LoadIncidentsAsync());

            _ = LoadIncidentsAsync();
        }

        public ObservableCollection<Incident> Incidents { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadIncidentsAsync();
                }
            }
        }

        public Incident? SelectedIncident
        {
            get => _selectedIncident;
            set => SetProperty(ref _selectedIncident, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand OpenWorkspaceCommand { get; }
        public ICommand OpenIncidentWorkspaceCommand { get; }
        public ICommand RefreshCommand { get; }

        public async Task LoadIncidentsAsync()
        {
            IsLoading = true;
            try
            {
                var all = await _incidentService.SearchIncidentsAsync(SearchText);
                Incidents.Clear();
                foreach (var inc in all)
                {
                    Incidents.Add(inc);
                }

                if (_preselectedIncidentId.HasValue)
                {
                    var match = Incidents.FirstOrDefault(i => i.Id == _preselectedIncidentId.Value);
                    if (match != null)
                    {
                        SelectedIncident = match;
                    }
                }
                else if (SelectedIncident != null)
                {
                    SelectedIncident = Incidents.FirstOrDefault(i => i.Id == SelectedIncident.Id);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
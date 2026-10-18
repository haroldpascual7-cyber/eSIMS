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
        private bool _isIncidentModalOpen;
        private Incident? _viewingIncident;

        public bool IsIncidentModalOpen
        {
            get => _isIncidentModalOpen;
            set => SetProperty(ref _isIncidentModalOpen, value);
        }

        public Incident? ViewingIncident
        {
            get => _viewingIncident;
            set => SetProperty(ref _viewingIncident, value);
        }

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

            // Card click -> show detail modal
            OpenIncidentWorkspaceCommand = new RelayCommand<Incident>(inc =>
            {
                if (inc == null) return;
                SelectedIncident    = inc;
                ViewingIncident     = inc;
                IsIncidentModalOpen = true;
            });

            CloseIncidentModalCommand = new RelayCommand(() =>
            {
                IsIncidentModalOpen = false;
                ViewingIncident     = null;
            });

            OpenWorkspaceFromModalCommand = new RelayCommand(() =>
            {
                if (ViewingIncident == null) return;
                IsIncidentModalOpen = false;
                _openWorkspaceAction(ViewingIncident);
            });

            OpenWorkspaceCommand = new RelayCommand(() =>
            {
                if (SelectedIncident != null) _openWorkspaceAction(SelectedIncident);
            }, () => SelectedIncident != null);

            RefreshCommand = new RelayCommand(async () => await ExecuteRefreshAsync(), () => !IsLoading && !IsRefreshing);

            _ = LoadIncidentsAsync();
        }

        public ObservableCollection<Incident> Incidents { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    _ = LoadIncidentsAsync();
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
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonText));
                }
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (SetProperty(ref _isRefreshing, value))
                {
                    OnPropertyChanged(nameof(IsNotRefreshing));
                    OnPropertyChanged(nameof(RefreshButtonText));
                }
            }
        }

        public bool IsNotRefreshing => !IsRefreshing && !IsLoading;
        public string RefreshButtonText => (IsRefreshing || IsLoading) ? "⌛ Refreshing..." : "↻ Refresh";

        public async Task ExecuteRefreshAsync()
        {
            if (IsRefreshing || IsLoading) return;
            try
            {
                IsRefreshing = true;
                IsLoading = true;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
                await Task.Yield();
                var all = await _incidentService.SearchIncidentsAsync(SearchText);
                Incidents.Clear();
                foreach (var inc in all)
                    Incidents.Add(inc);

                if (_preselectedIncidentId.HasValue)
                {
                    var match = Incidents.FirstOrDefault(i => i.Id == _preselectedIncidentId.Value);
                    if (match != null) SelectedIncident = match;
                }
                else if (SelectedIncident != null)
                {
                    SelectedIncident = Incidents.FirstOrDefault(i => i.Id == SelectedIncident.Id);
                }
                await Task.Delay(750);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing investigations: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
                OnPropertyChanged(nameof(IsNotRefreshing));
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }

        public ICommand OpenWorkspaceCommand        { get; }
        public ICommand OpenIncidentWorkspaceCommand{ get; }
        public ICommand CloseIncidentModalCommand   { get; }
        public ICommand OpenWorkspaceFromModalCommand{ get; }
        public ICommand RefreshCommand              { get; }

        public async Task LoadIncidentsAsync()
        {
            IsLoading = true;
            try
            {
                var all = await _incidentService.SearchIncidentsAsync(SearchText);
                Incidents.Clear();
                foreach (var inc in all)
                    Incidents.Add(inc);

                if (_preselectedIncidentId.HasValue)
                {
                    var match = Incidents.FirstOrDefault(i => i.Id == _preselectedIncidentId.Value);
                    if (match != null) SelectedIncident = match;
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

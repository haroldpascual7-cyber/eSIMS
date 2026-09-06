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
    public class AnecdotalRecordsViewModel : ObservableObject
    {
        private readonly IAnecdotalService _anecdotalService;
        private readonly Action _onBack;

        private string _searchText = string.Empty;
        private string _selectedTypeFilter = "All Types";
        private bool _isAddModalOpen;
        private bool _isDetailsModalOpen;
        private AnecdotalRecordItem? _selectedRecord;

        // Modal Form Fields
        private string _newStudentIdNumber = string.Empty;
        private string _newStudentName = string.Empty;
        private string _newGradeSection = "Grade 10 - St. Thomas";
        private string _newSettingOrSubject = "Homeroom & Values Education";
        private string _newObservationType = "Positive Conduct";
        private string _newNarrativeObservation = string.Empty;
        private string _newActionTaken = string.Empty;
        private string _newFollowUpPlan = "Monitor in class";
        private string _newStatus = "Resolved in Class";
        private DateTime _newObservationDate = DateTime.Now;
        private string _modalErrorMessage = string.Empty;

        public AnecdotalRecordsViewModel(IAnecdotalService? anecdotalService = null, Action? onBack = null)
        {
            _anecdotalService = anecdotalService ?? new AnecdotalService();
            _onBack = onBack ?? (() => { });

            Records = new ObservableCollection<AnecdotalRecordItem>();

            TypeFilterOptions = new ObservableCollection<string>
            {
                "All Types",
                "Positive Conduct",
                "Minor Infraction",
                "Academic Focus",
                "Attendance",
                "Social/Peer Interaction"
            };

            ObservationTypeOptions = new ObservableCollection<string>
            {
                "Positive Conduct",
                "Minor Infraction",
                "Academic Focus",
                "Attendance",
                "Social/Peer Interaction"
            };

            GradeSectionOptions = new ObservableCollection<string>
            {
                "Grade 7 - Diamond",
                "Grade 8 - Ruby",
                "Grade 9 - St. Jude",
                "Grade 10 - St. Thomas",
                "Grade 11 - STEM A",
                "Grade 12 - HUMSS A"
            };

            SettingOptions = new ObservableCollection<string>
            {
                "Homeroom & Values Education",
                "Mathematics",
                "English Literature",
                "Science Laboratory",
                "Physical Education / Field",
                "School Canteen / Hallway"
            };

            FollowUpPlanOptions = new ObservableCollection<string>
            {
                "Monitor in class",
                "Notify Homeroom Adviser",
                "Schedule Parent Check-in",
                "Refer to Guidance Counselor",
                "Award Good Conduct Points"
            };

            StatusOptions = new ObservableCollection<string>
            {
                "Resolved in Class",
                "Monitoring",
                "Referred to Guidance"
            };

            BackCommand = new RelayCommand(_ => _onBack());
            SearchCommand = new RelayCommand(_ => _ = LoadRecordsAsync());
            OpenAddModalCommand = new RelayCommand(_ => OpenAddModal());
            CloseAddModalCommand = new RelayCommand(_ => IsAddModalOpen = false);
            SaveNewRecordCommand = new RelayCommand(_ => _ = SaveNewRecordAsync());
            DeleteRecordCommand = new RelayCommand(param => _ = DeleteRecordAsync(param as AnecdotalRecordItem));
            ViewDetailsCommand = new RelayCommand(param => ViewDetails(param as AnecdotalRecordItem));
            CloseDetailsModalCommand = new RelayCommand(_ => IsDetailsModalOpen = false);

            _ = LoadRecordsAsync();
        }

        public ObservableCollection<AnecdotalRecordItem> Records { get; }
        public ObservableCollection<string> TypeFilterOptions { get; }
        public ObservableCollection<string> ObservationTypeOptions { get; }
        public ObservableCollection<string> GradeSectionOptions { get; }
        public ObservableCollection<string> SettingOptions { get; }
        public ObservableCollection<string> FollowUpPlanOptions { get; }
        public ObservableCollection<string> StatusOptions { get; }

        public ICommand BackCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand OpenAddModalCommand { get; }
        public ICommand CloseAddModalCommand { get; }
        public ICommand SaveNewRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand CloseDetailsModalCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value)) _ = LoadRecordsAsync();
            }
        }

        public string SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set
            {
                if (SetProperty(ref _selectedTypeFilter, value)) _ = LoadRecordsAsync();
            }
        }

        public bool IsAddModalOpen
        {
            get => _isAddModalOpen;
            set => SetProperty(ref _isAddModalOpen, value);
        }

        public bool IsDetailsModalOpen
        {
            get => _isDetailsModalOpen;
            set => SetProperty(ref _isDetailsModalOpen, value);
        }

        public AnecdotalRecordItem? SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        public int TotalObservationsCount => Records.Count;
        public int PositiveConductCount => Records.Count(r => r.ObservationType == "Positive Conduct" || r.ObservationType == "Academic Focus");
        public int MinorInfractionsCount => Records.Count(r => r.ObservationType == "Minor Infraction");
        public int PendingFollowUpCount => Records.Count(r => r.Status == "Monitoring" || r.Status == "Referred to Guidance");

        public string NewStudentIdNumber { get => _newStudentIdNumber; set => SetProperty(ref _newStudentIdNumber, value); }
        public string NewStudentName { get => _newStudentName; set => SetProperty(ref _newStudentName, value); }
        public string NewGradeSection { get => _newGradeSection; set => SetProperty(ref _newGradeSection, value); }
        public string NewSettingOrSubject { get => _newSettingOrSubject; set => SetProperty(ref _newSettingOrSubject, value); }
        public string NewObservationType { get => _newObservationType; set => SetProperty(ref _newObservationType, value); }
        public string NewNarrativeObservation { get => _newNarrativeObservation; set => SetProperty(ref _newNarrativeObservation, value); }
        public string NewActionTaken { get => _newActionTaken; set => SetProperty(ref _newActionTaken, value); }
        public string NewFollowUpPlan { get => _newFollowUpPlan; set => SetProperty(ref _newFollowUpPlan, value); }
        public string NewStatus { get => _newStatus; set => SetProperty(ref _newStatus, value); }
        public DateTime NewObservationDate { get => _newObservationDate; set => SetProperty(ref _newObservationDate, value); }
        public string ModalErrorMessage { get => _modalErrorMessage; set => SetProperty(ref _modalErrorMessage, value); }

        public async Task LoadRecordsAsync()
        {
            var list = await _anecdotalService.GetAllAsync(SearchText, SelectedTypeFilter);
            Records.Clear();
            foreach (var item in list) Records.Add(item);

            OnPropertyChanged(nameof(TotalObservationsCount));
            OnPropertyChanged(nameof(PositiveConductCount));
            OnPropertyChanged(nameof(MinorInfractionsCount));
            OnPropertyChanged(nameof(PendingFollowUpCount));
        }

        private void OpenAddModal()
        {
            ModalErrorMessage = string.Empty;
            NewStudentIdNumber = string.Empty;
            NewStudentName = string.Empty;
            NewGradeSection = "Grade 10 - St. Thomas";
            NewSettingOrSubject = "Homeroom & Values Education";
            NewObservationType = "Positive Conduct";
            NewNarrativeObservation = string.Empty;
            NewActionTaken = string.Empty;
            NewFollowUpPlan = "Monitor in class";
            NewStatus = "Resolved in Class";
            NewObservationDate = DateTime.Now;

            IsAddModalOpen = true;
        }

        private void ViewDetails(AnecdotalRecordItem? item)
        {
            if (item == null) return;
            SelectedRecord = item;
            IsDetailsModalOpen = true;
        }

        private async Task SaveNewRecordAsync()
        {
            if (string.IsNullOrWhiteSpace(NewStudentName))
            {
                ModalErrorMessage = "Please enter student name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewNarrativeObservation))
            {
                ModalErrorMessage = "Please describe the observed behavior.";
                return;
            }

            var newItem = new AnecdotalRecordItem
            {
                StudentIdNumber = string.IsNullOrWhiteSpace(NewStudentIdNumber) ? "STD-" + new Random().Next(1000, 9999) : NewStudentIdNumber.Trim(),
                StudentName = NewStudentName.Trim(),
                GradeSection = NewGradeSection.Trim(),
                TeacherName = !string.IsNullOrWhiteSpace(SessionManager.FullName) 
                    ? SessionManager.FullName 
                    : (SessionManager.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ? "Staff Observer" : "Teacher (Classroom Adviser)"),
                ObservationDate = NewObservationDate,
                SettingOrSubject = NewSettingOrSubject,
                ObservationType = NewObservationType,
                NarrativeObservation = NewNarrativeObservation.Trim(),
                ActionTakenByTeacher = string.IsNullOrWhiteSpace(NewActionTaken) ? "Classroom dialogue and positive reinforcement." : NewActionTaken.Trim(),
                FollowUpPlan = NewFollowUpPlan,
                Status = NewStatus
            };

            await _anecdotalService.AddAsync(newItem);
            IsAddModalOpen = false;
            await LoadRecordsAsync();
        }

        private async Task DeleteRecordAsync(AnecdotalRecordItem? item)
        {
            if (item == null) return;
            await _anecdotalService.DeleteAsync(item.Id);
            await LoadRecordsAsync();
        }
    }
}

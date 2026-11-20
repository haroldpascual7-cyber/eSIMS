// ViewModels/StudentListViewModel.cs
// Implements a view model to display and search all students

using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class StudentListViewModel : ObservableObject
    {
        private readonly IStudentService _studentService;
        private readonly Action<int>? _openDetailAction;
        private string _searchText = string.Empty;
        private bool _isLoading;
        private Student? _selectedStudent;

        public StudentListViewModel(IStudentService studentService, Action<int>? openDetailAction = null)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _openDetailAction = openDetailAction;
            Students = new ObservableCollection<Student>();
            SearchCommand = new RelayCommand(async () => await LoadStudentsAsync(), () => !IsLoading);
            RefreshCommand = new RelayCommand(async () => await RefreshStudentsAsync(), () => !IsLoading);
            OpenDetailCommand = new RelayCommand(() => OpenStudentDetail(), () => SelectedStudent != null && !IsLoading);

            // Initial load
            _ = LoadStudentsAsync();
        }

        // --- Collections ---
        public ObservableCollection<Student> Students { get; }

        // --- Commands ---
        public ICommand SearchCommand { get; }
        public ICommand OpenDetailCommand { get; }
        public ICommand RefreshCommand { get; }

        // --- Properties ---
        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (SetProperty(ref _selectedStudent, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Optional: auto-search on text change
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsNotLoading));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsNotLoading => !IsLoading;

        private void OpenStudentDetail()
        {
            if (SelectedStudent != null)
            {
                _openDetailAction?.Invoke(SelectedStudent.Id);
            }
        }

        // --- Data loading ---
        private async Task LoadStudentsAsync()
        {
            IsLoading = true;
            try
            {
                var list = string.IsNullOrWhiteSpace(SearchText)
                    ? await _studentService.GetAllStudentsAsync()
                    : await _studentService.SearchStudentsAsync(SearchText);

                Students.Clear();
                foreach (var s in list)
                    Students.Add(s);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshStudentsAsync()
        {
            SearchText = string.Empty;
            await LoadStudentsAsync();
        }
    }
}

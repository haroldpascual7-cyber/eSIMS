using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class StudentDetailViewModel : ObservableObject
    {
        private readonly IStudentService _studentService;
        private Student? _student;
        private bool _isLoading;

        public StudentDetailViewModel(IStudentService studentService, int studentId)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            LoadStudentCommand = new RelayCommand(async () => await LoadStudentAsync(studentId), () => !IsLoading);
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
            // Initial load
            _ = LoadStudentAsync(studentId);
        }

        public ICommand LoadStudentCommand { get; }
        public ICommand CloseCommand { get; }

        public Student? Student
        {
            get => _student;
            private set => SetProperty(ref _student, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsNotLoading));
                }
            }
        }

        public bool IsNotLoading => !IsLoading;

        public event EventHandler? RequestClose;

        private async System.Threading.Tasks.Task LoadStudentAsync(int id)
        {
            IsLoading = true;
            try
            {
                Student = await _studentService.GetStudentByIdAsync(id);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

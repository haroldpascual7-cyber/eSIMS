using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class DispatchViewModel : ObservableObject
    {
        private readonly ICommunicationService _commService;
        private readonly IIncidentService _incidentService;
        private readonly Action _onBack;

        private ObservableCollection<Student> _registeredStudents = new();
        private ObservableCollection<Student> _filteredStudents = new();
        private Student? _selectedStudent;
        private string _studentSearchText = string.Empty;

        private string _guardianName = string.Empty;
        private string _guardianPhone = string.Empty;
        private string _guardianEmail = string.Empty;

        private string _dispatchChannel = "SMS"; // SMS, Email, Both
        private string _messageBody = string.Empty;
        private string _characterCountText = "0 / 160 Characters";
        private string _characterCountColor = "#64748B";
        private bool _isSending;

        private List<QuickTemplateItem> _quickTemplates = new();
        private QuickTemplateItem? _selectedQuickTemplate;

        private ObservableCollection<DispatchLog> _recentDispatches = new();
        private DispatchLog? _selectedDispatch;

        public DispatchViewModel(
            ICommunicationService commService,
            IIncidentService incidentService,
            Action onBack)
        {
            _commService     = commService     ?? throw new ArgumentNullException(nameof(commService));
            _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
            _onBack          = onBack          ?? throw new ArgumentNullException(nameof(onBack));

            BackToHubCommand       = new RelayCommand(_ => _onBack());
            ClearSearchCommand     = new RelayCommand(_ => StudentSearchText = string.Empty);
            DispatchMessageCommand = new RelayCommand(async _ => await DispatchMessageAsync(), _ => !IsSending);
            ViewMessageCommand     = new RelayCommand(param => ViewMessage(param as DispatchLog ?? SelectedDispatch));

            SetupQuickTemplates();
            _ = InitializeDataAsync();
        }

        public ObservableCollection<Student> RegisteredStudents
        {
            get => _registeredStudents;
            set => SetProperty(ref _registeredStudents, value);
        }

        public ObservableCollection<Student> FilteredStudents
        {
            get => _filteredStudents;
            set => SetProperty(ref _filteredStudents, value);
        }

        public string StudentSearchText
        {
            get => _studentSearchText;
            set
            {
                if (SetProperty(ref _studentSearchText, value))
                {
                    OnPropertyChanged(nameof(HasSearchText));
                    FilterStudents();
                }
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(StudentSearchText);

        public Student? SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (SetProperty(ref _selectedStudent, value) && value != null)
                {
                    GuardianName = string.IsNullOrWhiteSpace(value.GuardianName) ? "Parent / Guardian" : value.GuardianName;
                    
                    // Format phone with +63 prefix if local 09xx
                    string phone = value.GuardianContact ?? string.Empty;
                    if (phone.StartsWith("09"))
                    {
                        phone = "+63 " + phone.Substring(1);
                    }
                    else if (!phone.StartsWith("+") && !string.IsNullOrWhiteSpace(phone))
                    {
                        phone = "+63 " + phone;
                    }
                    GuardianPhone = string.IsNullOrWhiteSpace(phone) ? "+63 917-555-0000" : phone;

                    string email = value.GuardianEmail ?? string.Empty;
                    GuardianEmail = string.IsNullOrWhiteSpace(email) ? $"{value.FirstName.ToLower()}.guardian@gmail.com" : email;

                    ApplyQuickTemplate();
                }
            }
        }

        public string GuardianName  { get => _guardianName; set => SetProperty(ref _guardianName, value); }
        public string GuardianPhone { get => _guardianPhone; set => SetProperty(ref _guardianPhone, value); }
        public string GuardianEmail { get => _guardianEmail; set => SetProperty(ref _guardianEmail, value); }

        public string DispatchChannel
        {
            get => _dispatchChannel;
            set
            {
                if (SetProperty(ref _dispatchChannel, value))
                {
                    OnPropertyChanged(nameof(IsSmsChannel));
                    OnPropertyChanged(nameof(IsEmailChannel));
                    OnPropertyChanged(nameof(IsBothChannel));
                    UpdateCharacterCount();
                }
            }
        }

        public bool IsSmsChannel   { get => DispatchChannel == "SMS";   set { if (value) DispatchChannel = "SMS"; } }
        public bool IsEmailChannel { get => DispatchChannel == "Email"; set { if (value) DispatchChannel = "Email"; } }
        public bool IsBothChannel  { get => DispatchChannel == "Both";  set { if (value) DispatchChannel = "Both"; } }

        public List<QuickTemplateItem> QuickTemplates { get => _quickTemplates; set => SetProperty(ref _quickTemplates, value); }

        public QuickTemplateItem? SelectedQuickTemplate
        {
            get => _selectedQuickTemplate;
            set
            {
                if (SetProperty(ref _selectedQuickTemplate, value))
                {
                    ApplyQuickTemplate();
                }
            }
        }

        public string MessageBody
        {
            get => _messageBody;
            set
            {
                if (SetProperty(ref _messageBody, value))
                {
                    UpdateCharacterCount();
                }
            }
        }

        public string CharacterCountText  { get => _characterCountText; set => SetProperty(ref _characterCountText, value); }
        public string CharacterCountColor { get => _characterCountColor; set => SetProperty(ref _characterCountColor, value); }
        public bool IsSending             { get => _isSending; set => SetProperty(ref _isSending, value); }

        public ObservableCollection<DispatchLog> RecentDispatches
        {
            get => _recentDispatches;
            set => SetProperty(ref _recentDispatches, value);
        }

        public DispatchLog? SelectedDispatch
        {
            get => _selectedDispatch;
            set => SetProperty(ref _selectedDispatch, value);
        }

        // Commands
        public ICommand BackToHubCommand       { get; }
        public ICommand ClearSearchCommand      { get; }
        public ICommand DispatchMessageCommand { get; }
        public ICommand ViewMessageCommand     { get; }

        public async Task InitializeDataAsync()
        {
            var students = await _incidentService.GetAllStudentsAsync();
            RegisteredStudents = new ObservableCollection<Student>(students);
            FilterStudents();

            if (FilteredStudents.Any())
            {
                SelectedStudent = FilteredStudents.First();
            }

            if (QuickTemplates.Any())
            {
                SelectedQuickTemplate = QuickTemplates.First();
            }

            await RefreshHistoryAsync();
        }

        private void FilterStudents()
        {
            if (string.IsNullOrWhiteSpace(StudentSearchText))
            {
                FilteredStudents = new ObservableCollection<Student>(RegisteredStudents);
            }
            else
            {
                string query = StudentSearchText.Trim().ToLower();
                var matches = RegisteredStudents.Where(s =>
                    (!string.IsNullOrEmpty(s.StudentNumber) && s.StudentNumber.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(s.FullName) && s.FullName.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(s.GradeLevel) && s.GradeLevel.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(s.Section) && s.Section.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(s.GuardianName) && s.GuardianName.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(s.GuardianContact) && s.GuardianContact.ToLower().Contains(query)) ||
                    (!string.IsNullOrEmpty(s.GuardianEmail) && s.GuardianEmail.ToLower().Contains(query))
                ).ToList();

                FilteredStudents = new ObservableCollection<Student>(matches);
            }

            if (SelectedStudent == null || !FilteredStudents.Contains(SelectedStudent))
            {
                if (FilteredStudents.Any())
                {
                    SelectedStudent = FilteredStudents.First();
                }
            }
        }

        private async Task RefreshHistoryAsync()
        {
            var logs = await _commService.GetRecentDispatchesAsync(50);
            RecentDispatches = new ObservableCollection<DispatchLog>(logs);
        }

        private void SetupQuickTemplates()
        {
            QuickTemplates = new List<QuickTemplateItem>
            {
                new QuickTemplateItem
                {
                    Title = "Urgent Parent Conference",
                    BodyTemplate = "Smart Academy Alert: Dear {{GuardianName}}, you are requested to attend an urgent Parent-Counselor Conference regarding {{StudentName}} on 2026-08-29 at 10:00 AM."
                },
                new QuickTemplateItem
                {
                    Title = "Behavioral Infraction Disclosure",
                    BodyTemplate = "Smart Academy Advisory: Dear {{GuardianName}}, a behavioral incident involving {{StudentName}} was logged today. Please check your registered email for disclosure details."
                },
                new QuickTemplateItem
                {
                    Title = "Suspension Order Alert",
                    BodyTemplate = "Smart Academy Official Notice: Dear {{GuardianName}}, a Suspension Order has been issued for {{StudentName}}. Please call the Guidance Office immediately."
                },
                new QuickTemplateItem
                {
                    Title = "General Conduct Update",
                    BodyTemplate = "Smart Academy Update: Dear {{GuardianName}}, {{StudentName}} has demonstrated improved conduct score standing this week. Thank you for your continued support."
                }
            };
        }

        private void ApplyQuickTemplate()
        {
            if (SelectedQuickTemplate == null) return;
            string text = SelectedQuickTemplate.BodyTemplate;
            string guardian = string.IsNullOrWhiteSpace(GuardianName) ? "Parent/Guardian" : GuardianName;
            string student = SelectedStudent?.FullName ?? "your child";

            text = text.Replace("{{GuardianName}}", guardian)
                       .Replace("{{StudentName}}", student);

            MessageBody = text;
        }

        private void UpdateCharacterCount()
        {
            int len = MessageBody?.Length ?? 0;
            if (DispatchChannel == "SMS" || DispatchChannel == "Both")
            {
                CharacterCountText = $"{len} / 160 Characters (SMS)";
                CharacterCountColor = len > 160 ? "#DC2626" : "#64748B";
            }
            else
            {
                CharacterCountText = $"{len} Characters (Email)";
                CharacterCountColor = "#059669";
            }
        }

        private async Task DispatchMessageAsync()
        {
            if (SelectedStudent == null)
            {
                MessageBox.Show("Please select a target student/guardian.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(MessageBody))
            {
                MessageBox.Show("Please enter a message body to dispatch.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsSending = true;
            try
            {
                string studentId = SelectedStudent.StudentNumber;
                string guardian = GuardianName;
                string phone = GuardianPhone;
                string email = GuardianEmail;

                if (DispatchChannel == "SMS")
                {
                    await _commService.SendSmsAsync(studentId, guardian, phone, MessageBody);
                }
                else if (DispatchChannel == "Email")
                {
                    await _commService.SendEmailAsync(studentId, guardian, email, SelectedQuickTemplate?.Title ?? "Official Smart Academy Alert", MessageBody);
                }
                else if (DispatchChannel == "Both")
                {
                    await _commService.SendSmsAsync(studentId, guardian, phone, MessageBody);
                    await _commService.SendEmailAsync(studentId, guardian, email, SelectedQuickTemplate?.Title ?? "Official Smart Academy Alert", MessageBody);
                }

                MessageBox.Show($"Notification dispatched successfully to {guardian} ({DispatchChannel})!", "Dispatch Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error dispatching notification:\n{ex.Message}", "Dispatch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSending = false;
            }
        }

        private void ViewMessage(DispatchLog? log)
        {
            if (log == null) return;
            MessageBox.Show($"Recipient: {log.GuardianName}\nDestination: {log.ContactDestination}\nChannel: {log.Channel}\nDate Sent: {log.FormattedDate}\n\nMessage Body:\n{log.MessageBody}",
                            $"Dispatch Log Details ({log.Id})", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
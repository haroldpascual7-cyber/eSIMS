using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class StudentsViewModel
    {
        private bool _isStudentDetailOpen;
        private int _detailIncidentCount;
        private int _detailConductScore = 100;
        private string _detailDisciplinaryStanding = "Clear • Good Standing";
        private string _detailRecentIncident = "None Logged";
        private string _detailLastIncidentDate = "N/A";
        private string _detailStudentAge = "16 Years Old";
        private string? _selectedStudentPhotoPath;

        public bool IsStudentDetailOpen
        {
            get => _isStudentDetailOpen;
            set => SetProperty(ref _isStudentDetailOpen, value);
        }

        public int DetailIncidentCount
        {
            get => _detailIncidentCount;
            set => SetProperty(ref _detailIncidentCount, value);
        }

        public int DetailConductScore
        {
            get => _detailConductScore;
            set => SetProperty(ref _detailConductScore, value);
        }

        public string DetailDisciplinaryStanding
        {
            get => _detailDisciplinaryStanding;
            set => SetProperty(ref _detailDisciplinaryStanding, value);
        }

        public string DetailRecentIncident
        {
            get => _detailRecentIncident;
            set => SetProperty(ref _detailRecentIncident, value);
        }

        public string DetailLastIncidentDate
        {
            get => _detailLastIncidentDate;
            set => SetProperty(ref _detailLastIncidentDate, value);
        }

        public string DetailStudentAge
        {
            get => _detailStudentAge;
            set => SetProperty(ref _detailStudentAge, value);
        }

        public string? SelectedStudentPhotoPath
        {
            get => _selectedStudentPhotoPath;
            set
            {
                if (SetProperty(ref _selectedStudentPhotoPath, value))
                {
                    OnPropertyChanged(nameof(HasSelectedStudentPhoto));
                }
            }
        }

        public bool HasSelectedStudentPhoto => !string.IsNullOrWhiteSpace(SelectedStudentPhotoPath) && File.Exists(SelectedStudentPhotoPath);

        // System School & Category Info
        public string DetailSchoolName => "Matti National High School";
        public string DetailLevelCategory => (SelectedStudent?.GradeLevel?.Contains("11") == true || SelectedStudent?.GradeLevel?.Contains("12") == true)
            ? "Senior High School"
            : "Junior High School";

        public ICommand BackToDirectoryCommand { get; private set; } = null!;
        public ICommand ResetDetailCommand { get; private set; } = null!;
        public ICommand PrintGoodMoralCertCommand { get; private set; } = null!;
        public ICommand ExportStudentDossierCommand { get; private set; } = null!;
        public ICommand FileIncidentForStudentCommand { get; private set; } = null!;
        public ICommand ViewBehavioralProfileCommand { get; private set; } = null!;
        public ICommand ViewIncidentHistoryCommand { get; private set; } = null!;
        public ICommand ViewConsequenceTrackingCommand { get; private set; } = null!;
        public ICommand ViewRestorativeInterventionsCommand { get; private set; } = null!;
        public ICommand ViewSuspensionCalendarCommand { get; private set; } = null!;
        public ICommand UploadStudentPhotoCommand { get; private set; } = null!;
        public ICommand RemoveStudentPhotoCommand { get; private set; } = null!;

        private void InitializeDetailCommands()
        {
            BackToDirectoryCommand = new RelayCommand(() => IsStudentDetailOpen = false);
            ResetDetailCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null) OpenViewStudentModal(SelectedStudent);
            });
            PrintGoodMoralCertCommand = new RelayCommand(ExecutePrintGoodMoralCert);
            ExportStudentDossierCommand = new RelayCommand(ExecuteExportStudentDossier);
            FileIncidentForStudentCommand = new RelayCommand(() =>
            {
                string name = SelectedStudent?.FullName ?? string.Empty;
                IsStudentDetailOpen = false;
                _openCreateIncidentAction(name);
            });
            ViewBehavioralProfileCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null) OpenStudentProfileModal(SelectedStudent);
            });
            ViewIncidentHistoryCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null) OpenStudentProfileModal(SelectedStudent);
            });
            ViewConsequenceTrackingCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null)
                {
                    OpenStudentProfileModal(SelectedStudent);
                }
            });
            ViewRestorativeInterventionsCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null)
                {
                    OpenStudentProfileModal(SelectedStudent);
                }
            });
            ViewSuspensionCalendarCommand = new RelayCommand(() =>
            {
                if (SelectedStudent != null)
                {
                    OpenStudentProfileModal(SelectedStudent);
                }
            });

            UploadStudentPhotoCommand = new RelayCommand(async () =>
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select Student Profile Photo",
                    Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        var avatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StudentAvatars");
                        if (!Directory.Exists(avatarsDir))
                        {
                            Directory.CreateDirectory(avatarsDir);
                        }
                        var ext = Path.GetExtension(dlg.FileName);
                        var id = SelectedStudent?.StudentNumber ?? "student";
                        var destFile = Path.Combine(avatarsDir, $"{id}_{DateTime.Now.Ticks}{ext}");
                        File.Copy(dlg.FileName, destFile, true);

                        SelectedStudentPhotoPath = destFile;
                        if (SelectedStudent != null)
                        {
                            SelectedStudent.ProfilePicturePath = destFile;
                            await _studentService.UpdateStudentAsync(SelectedStudent);
                            await LoadStudentsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving student profile photo: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });

            RemoveStudentPhotoCommand = new RelayCommand(async () =>
            {
                SelectedStudentPhotoPath = null;
                if (SelectedStudent != null)
                {
                    SelectedStudent.ProfilePicturePath = string.Empty;
                    await _studentService.UpdateStudentAsync(SelectedStudent);
                    await LoadStudentsAsync();
                }
            });
        }

        public void LoadStudentDetailStatistics(Student s)
        {
            SelectedStudentPhotoPath = s.ProfilePicturePath;
            OnPropertyChanged(nameof(HasSelectedStudentPhoto));
            OnPropertyChanged(nameof(DetailLevelCategory));

            if (s.DateOfBirth.HasValue)
            {
                int age = DateTime.Today.Year - s.DateOfBirth.Value.Year;
                if (s.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                DetailStudentAge = $"{age}";
            }
            else
            {
                DetailStudentAge = "17";
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var allIncidents = await _incidentService.GetAllIncidentsAsync();
                    var related = allIncidents.Where(i => i.StudentId == s.Id ||
                                                         (!string.IsNullOrEmpty(i.PrimaryInvolvedStudent) &&
                                                          i.PrimaryInvolvedStudent.Contains(s.FullName, StringComparison.OrdinalIgnoreCase)))
                                              .OrderByDescending(i => i.IncidentDate)
                                              .ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DetailIncidentCount = related.Count;
                        DetailConductScore = Math.Max(0, 100 - (related.Count * 10));

                        if (related.Count == 0)
                        {
                            DetailDisciplinaryStanding = "Clear • Good Standing";
                            DetailRecentIncident = "None Logged";
                            DetailLastIncidentDate = "N/A";
                        }
                        else
                        {
                            var latest = related[0];
                            DetailDisciplinaryStanding = $"{related.Count} Incident{(related.Count > 1 ? "s" : "")} on Record";
                            DetailRecentIncident = string.IsNullOrEmpty(latest.Category) ? latest.Title : latest.Category;
                            DetailLastIncidentDate = latest.IncidentDate.ToString("MMM dd, yyyy");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading student stats: {ex.Message}");
                }
            });
        }

        private void ExecutePrintGoodMoralCert()
        {
            if (SelectedStudent == null) return;
            try
            {
                var s = SelectedStudent;
                var transcriptResult = new StudentTranscriptResult
                {
                    StudentId = s.StudentNumber,
                    StudentName = s.FullName,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    GradeLevel = s.GradeLevel,
                    Section = s.Section,
                    Adviser = s.Adviser,
                    GuardianName = s.GuardianName,
                    GuardianContact = s.GuardianContact,
                    DateOfBirth = s.DateOfBirth,
                    ConductScore = DetailConductScore
                };

                var dlg = new SaveFileDialog
                {
                    FileName = $"Good_Moral_Certificate_{s.StudentNumber}_{DateTime.Now:yyyyMMdd}.pdf",
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    Title = "Save Good Moral Certificate"
                };

                if (dlg.ShowDialog() == true)
                {
                    var pdfService = new PdfExportService();
                    pdfService.GenerateGoodMoralCertificate(dlg.FileName, transcriptResult);
                    MessageBox.Show($"Good Moral Certificate successfully exported to:\n{dlg.FileName}", "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating Good Moral Certificate:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportStudentDossier()
        {
            if (SelectedStudent == null) return;
            try
            {
                var s = SelectedStudent;
                var transcriptResult = new StudentTranscriptResult
                {
                    StudentId = s.StudentNumber,
                    StudentName = s.FullName,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    GradeLevel = s.GradeLevel,
                    Section = s.Section,
                    Adviser = s.Adviser,
                    GuardianName = s.GuardianName,
                    GuardianContact = s.GuardianContact,
                    DateOfBirth = s.DateOfBirth,
                    ConductScore = DetailConductScore
                };

                var dlg = new SaveFileDialog
                {
                    FileName = $"Student_Dossier_{s.StudentNumber}_{DateTime.Now:yyyyMMdd}.pdf",
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    Title = "Save Student Conduct Transcript / Dossier"
                };

                if (dlg.ShowDialog() == true)
                {
                    var pdfService = new PdfExportService();
                    pdfService.GenerateConductTranscript(dlg.FileName, transcriptResult);
                    MessageBox.Show($"Student Dossier successfully exported to:\n{dlg.FileName}", "Export Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating Student Dossier:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class IncidentListViewModel
    {
        // --- Collections ---
        public ObservableCollection<InvolvedPersonEntry> InvolvedPersonsList { get; } = new();
        public ObservableCollection<WitnessEntry> WitnessesList { get; } = new();
        public ObservableCollection<IncidentEvidenceEntry> EvidenceList { get; } = new();

        // --- Involved Persons Form ---
        private string _newInvolvedName = string.Empty;
        private string _newInvolvedStudentNumber = string.Empty;
        private string _newInvolvedRole = "Participant";
        private string _newInvolvedGradeSection = string.Empty;
        private string _newInvolvedNotes = string.Empty;
        private bool _isInvolvedStudentSuggestionsOpen;
        private ObservableCollection<Student> _involvedStudentSuggestions = new();

        public string NewInvolvedName
        {
            get => _newInvolvedName;
            set
            {
                if (SetProperty(ref _newInvolvedName, value))
                {
                    UpdateInvolvedStudentSuggestions(value);
                }
            }
        }
        public string NewInvolvedStudentNumber { get => _newInvolvedStudentNumber; set => SetProperty(ref _newInvolvedStudentNumber, value); }
        public string NewInvolvedRole { get => _newInvolvedRole; set => SetProperty(ref _newInvolvedRole, value); }
        public string NewInvolvedGradeSection { get => _newInvolvedGradeSection; set => SetProperty(ref _newInvolvedGradeSection, value); }
        public string NewInvolvedNotes { get => _newInvolvedNotes; set => SetProperty(ref _newInvolvedNotes, value); }
        public bool IsInvolvedStudentSuggestionsOpen { get => _isInvolvedStudentSuggestionsOpen; set => SetProperty(ref _isInvolvedStudentSuggestionsOpen, value); }
        public ObservableCollection<Student> InvolvedStudentSuggestions => _involvedStudentSuggestions;

        // --- Witnesses Form ---
        private string _newWitnessName = string.Empty;
        private string _newWitnessRole = "Student Witness";
        private string _newWitnessStatement = string.Empty;
        private string _newWitnessContact = string.Empty;

        public string NewWitnessName { get => _newWitnessName; set => SetProperty(ref _newWitnessName, value); }
        public string NewWitnessRole { get => _newWitnessRole; set => SetProperty(ref _newWitnessRole, value); }
        public string NewWitnessStatement { get => _newWitnessStatement; set => SetProperty(ref _newWitnessStatement, value); }
        public string NewWitnessContact { get => _newWitnessContact; set => SetProperty(ref _newWitnessContact, value); }

        // --- Evidence Form ---
        private string _newEvidenceTitle = string.Empty;
        private string _newEvidenceDescription = string.Empty;
        private string _newEvidenceType = "File"; // "File" or "Link"
        private string _newEvidenceFilePathOrUrl = string.Empty;
        private string _newEvidenceFileName = string.Empty;
        private long _newEvidenceFileSize = 0;

        public string NewEvidenceTitle { get => _newEvidenceTitle; set => SetProperty(ref _newEvidenceTitle, value); }
        public string NewEvidenceDescription { get => _newEvidenceDescription; set => SetProperty(ref _newEvidenceDescription, value); }
        public string NewEvidenceType
        {
            get => _newEvidenceType;
            set
            {
                if (SetProperty(ref _newEvidenceType, value))
                {
                    OnPropertyChanged(nameof(IsFileEvidenceMode));
                    OnPropertyChanged(nameof(IsLinkEvidenceMode));
                }
            }
        }
        public bool IsFileEvidenceMode => NewEvidenceType == "File";
        public bool IsLinkEvidenceMode => NewEvidenceType == "Link";
        public string NewEvidenceFilePathOrUrl { get => _newEvidenceFilePathOrUrl; set => SetProperty(ref _newEvidenceFilePathOrUrl, value); }
        public string NewEvidenceFileName { get => _newEvidenceFileName; set => SetProperty(ref _newEvidenceFileName, value); }
        public long NewEvidenceFileSize { get => _newEvidenceFileSize; set => SetProperty(ref _newEvidenceFileSize, value); }

        private string _newEvidencePreviewPath = string.Empty;
        public string NewEvidencePreviewPath
        {
            get => _newEvidencePreviewPath;
            set
            {
                if (SetProperty(ref _newEvidencePreviewPath, value))
                {
                    OnPropertyChanged(nameof(HasNewEvidenceImagePreview));
                    OnPropertyChanged(nameof(IsNewEvidenceAnImage));
                }
            }
        }
        public bool HasNewEvidenceImagePreview => !string.IsNullOrWhiteSpace(NewEvidencePreviewPath) && IsNewEvidenceAnImage;
        public bool IsNewEvidenceAnImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NewEvidencePreviewPath)) return false;
                string ext = Path.GetExtension(NewEvidencePreviewPath).ToLowerInvariant();
                return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp" or ".gif";
            }
        }

        // --- Photo Gallery: computed from EvidenceList for prominent display ---
        public List<IncidentEvidenceEntry> EvidenceImageList =>
            EvidenceList.Where(e => e.IsImage).ToList();
        public bool HasAttachedImages => EvidenceList.Any(e => e.IsImage);
        public int AttachedImageCount => EvidenceList.Count(e => e.IsImage);
        public bool HasNoAttachedEvidence => EvidenceList.Count == 0;

        public void NotifyPhotoGalleryChanged()
        {
            OnPropertyChanged(nameof(EvidenceImageList));
            OnPropertyChanged(nameof(HasAttachedImages));
            OnPropertyChanged(nameof(AttachedImageCount));
            OnPropertyChanged(nameof(HasNoAttachedEvidence));
        }

        // --- Primary Student Confirmation ---
        private bool _isPrimaryStudentConfirmed = false;
        public bool IsPrimaryStudentConfirmed
        {
            get => _isPrimaryStudentConfirmed;
            set
            {
                if (SetProperty(ref _isPrimaryStudentConfirmed, value))
                {
                    OnPropertyChanged(nameof(PrimaryStudentConfirmationStatusText));
                }
            }
        }
        public string PrimaryStudentConfirmationStatusText => IsPrimaryStudentConfirmed ? "[OK] Student Record Confirmed" : "[!] Confirmation Required";

        // --- Commands ---
        public ICommand ConfirmPrimaryStudentCommand { get; private set; } = null!;
        public ICommand AddInvolvedPersonCommand { get; private set; } = null!;
        public ICommand RemoveInvolvedPersonCommand { get; private set; } = null!;
        public ICommand SelectInvolvedStudentSuggestionCommand { get; private set; } = null!;

        public ICommand AddWitnessCommand { get; private set; } = null!;
        public ICommand RemoveWitnessCommand { get; private set; } = null!;

        public ICommand BrowseEvidenceFileCommand { get; private set; } = null!;
        public ICommand AddEvidenceCommand { get; private set; } = null!;
        public ICommand RemoveEvidenceCommand { get; private set; } = null!;
        public ICommand OpenEvidenceCommand { get; private set; } = null!;

        private void InitializeInvolvedWitnessEvidence()
        {
            ConfirmPrimaryStudentCommand = new RelayCommand(TogglePrimaryStudentConfirmation);
            AddInvolvedPersonCommand = new RelayCommand(AddInvolvedPerson);
            RemoveInvolvedPersonCommand = new RelayCommand(param => RemoveInvolvedPerson(param as InvolvedPersonEntry));
            SelectInvolvedStudentSuggestionCommand = new RelayCommand(param => SelectInvolvedStudentSuggestion(param as Student));

            AddWitnessCommand = new RelayCommand(AddWitness);
            RemoveWitnessCommand = new RelayCommand(param => RemoveWitness(param as WitnessEntry));

            BrowseEvidenceFileCommand = new RelayCommand(BrowseEvidenceFile);
            AddEvidenceCommand = new RelayCommand(AddEvidence);
            RemoveEvidenceCommand = new RelayCommand(param => RemoveEvidence(param as IncidentEvidenceEntry));
            OpenEvidenceCommand = new RelayCommand(param => OpenEvidence(param as IncidentEvidenceEntry));

            EvidenceList.CollectionChanged += (s, e) => NotifyPhotoGalleryChanged();
        }

        private void TogglePrimaryStudentConfirmation()
        {
            if (string.IsNullOrWhiteSpace(ModalStudentName))
            {
                ModalErrorMessage = "Please select or type a primary involved student first.";
                return;
            }
            IsPrimaryStudentConfirmed = !IsPrimaryStudentConfirmed;
            ModalErrorMessage = string.Empty;
        }

        private void UpdateInvolvedStudentSuggestions(string? input)
        {
            if (string.IsNullOrWhiteSpace(input) || _allEnrolledStudents.Count == 0)
            {
                InvolvedStudentSuggestions.Clear();
                IsInvolvedStudentSuggestionsOpen = false;
                return;
            }

            var matches = _allEnrolledStudents
                .Where(s => s.FullName.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                            s.StudentNumber.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            InvolvedStudentSuggestions.Clear();
            foreach (var m in matches)
            {
                InvolvedStudentSuggestions.Add(m);
            }

            IsInvolvedStudentSuggestionsOpen = matches.Count > 0 && !matches.Any(m => m.FullName.Equals(input.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void SelectInvolvedStudentSuggestion(Student? student)
        {
            if (student == null) return;
            NewInvolvedName = student.FullName;
            NewInvolvedStudentNumber = student.StudentNumber;
            NewInvolvedGradeSection = $"{student.GradeLevel} - {student.Section}";
            InvolvedStudentSuggestions.Clear();
            IsInvolvedStudentSuggestionsOpen = false;
        }

        private void AddInvolvedPerson()
        {
            if (string.IsNullOrWhiteSpace(NewInvolvedName))
            {
                ModalErrorMessage = "Please enter the involved person/student name.";
                return;
            }

            InvolvedPersonsList.Add(new InvolvedPersonEntry
            {
                Name = NewInvolvedName.Trim(),
                StudentNumber = NewInvolvedStudentNumber.Trim(),
                Role = string.IsNullOrWhiteSpace(NewInvolvedRole) ? "Participant" : NewInvolvedRole.Trim(),
                GradeSection = NewInvolvedGradeSection.Trim(),
                Notes = NewInvolvedNotes.Trim()
            });

            NewInvolvedName = string.Empty;
            NewInvolvedStudentNumber = string.Empty;
            NewInvolvedGradeSection = string.Empty;
            NewInvolvedNotes = string.Empty;
            NewInvolvedRole = "Participant";
            ModalErrorMessage = string.Empty;
        }

        private void RemoveInvolvedPerson(InvolvedPersonEntry? entry)
        {
            if (entry != null) InvolvedPersonsList.Remove(entry);
        }

        private void AddWitness()
        {
            if (string.IsNullOrWhiteSpace(NewWitnessName))
            {
                ModalErrorMessage = "Please enter the witness name.";
                return;
            }

            WitnessesList.Add(new WitnessEntry
            {
                Name = NewWitnessName.Trim(),
                RoleOrAffiliation = string.IsNullOrWhiteSpace(NewWitnessRole) ? "Student Witness" : NewWitnessRole.Trim(),
                Statement = NewWitnessStatement.Trim(),
                ContactInfo = NewWitnessContact.Trim(),
                DateRecorded = DateTime.Now
            });

            NewWitnessName = string.Empty;
            NewWitnessStatement = string.Empty;
            NewWitnessContact = string.Empty;
            NewWitnessRole = "Student Witness";
            ModalErrorMessage = string.Empty;
        }

        private void RemoveWitness(WitnessEntry? entry)
        {
            if (entry != null) WitnessesList.Remove(entry);
        }

        private void BrowseEvidenceFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Approved Evidence File",
                Filter = "All Evidence Files (*.png;*.jpg;*.jpeg;*.pdf;*.mp4;*.docx;*.txt)|*.png;*.jpg;*.jpeg;*.pdf;*.mp4;*.docx;*.txt|Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Documents (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt|Videos (*.mp4)|*.mp4|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                NewEvidenceFilePathOrUrl = dlg.FileName;
                NewEvidenceFileName = Path.GetFileName(dlg.FileName);
                NewEvidencePreviewPath = dlg.FileName;
                if (string.IsNullOrWhiteSpace(NewEvidenceTitle))
                {
                    NewEvidenceTitle = Path.GetFileNameWithoutExtension(dlg.FileName);
                }
                try
                {
                    var fileInfo = new FileInfo(dlg.FileName);
                    NewEvidenceFileSize = fileInfo.Length;
                }
                catch
                {
                    NewEvidenceFileSize = 0;
                }
            }
        }

        private void AddEvidence()
        {
            if (string.IsNullOrWhiteSpace(NewEvidenceFilePathOrUrl))
            {
                ModalErrorMessage = IsFileEvidenceMode ? "Please select an evidence file." : "Please enter an evidence link URL.";
                return;
            }

            string finalPathOrUrl = NewEvidenceFilePathOrUrl.Trim();
            string fileName = string.IsNullOrWhiteSpace(NewEvidenceFileName) ? (IsLinkEvidenceMode ? "Web / Cloud Link" : Path.GetFileName(finalPathOrUrl)) : NewEvidenceFileName;

            // If it's a file, securely copy to local app data evidence directory for this incident
            if (IsFileEvidenceMode && File.Exists(finalPathOrUrl))
            {
                try
                {
                    string safeIncNum = string.IsNullOrWhiteSpace(ModalIncidentNumber) ? "INC-PENDING" : ModalIncidentNumber.Replace("/", "_").Replace("\\", "_");
                    string destDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool", "Evidence", safeIncNum);
                    Directory.CreateDirectory(destDir);
                    string destFile = Path.Combine(destDir, $"{Guid.NewGuid().ToString("N")[..8]}_{Path.GetFileName(finalPathOrUrl)}");
                    File.Copy(finalPathOrUrl, destFile, true);
                    finalPathOrUrl = destFile;
                    fileName = Path.GetFileName(destFile);
                }
                catch { /* fallback to original file path if copy fails */ }
            }

            EvidenceList.Add(new IncidentEvidenceEntry
            {
                Title = string.IsNullOrWhiteSpace(NewEvidenceTitle) ? fileName : NewEvidenceTitle.Trim(),
                Description = NewEvidenceDescription.Trim(),
                EvidenceType = NewEvidenceType,
                FilePathOrUrl = finalPathOrUrl,
                FileName = fileName,
                FileSizeBytes = NewEvidenceFileSize,
                DateAttached = DateTime.Now
            });

            NewEvidenceTitle = string.Empty;
            NewEvidenceDescription = string.Empty;
            NewEvidenceFilePathOrUrl = string.Empty;
            NewEvidenceFileName = string.Empty;
            NewEvidenceFileSize = 0;
            NewEvidencePreviewPath = string.Empty;
            ModalErrorMessage = string.Empty;
        }

        private void RemoveEvidence(IncidentEvidenceEntry? entry)
        {
            if (entry != null) EvidenceList.Remove(entry);
        }

        private void OpenEvidence(IncidentEvidenceEntry? entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.FilePathOrUrl)) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = entry.FilePathOrUrl,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open evidence: {ex.Message}", "Open Evidence", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetInvolvedWitnessEvidenceForms()
        {
            InvolvedPersonsList.Clear();
            WitnessesList.Clear();
            EvidenceList.Clear();

            NewInvolvedName = string.Empty;
            NewInvolvedStudentNumber = string.Empty;
            NewInvolvedGradeSection = string.Empty;
            NewInvolvedNotes = string.Empty;
            NewInvolvedRole = "Participant";

            NewWitnessName = string.Empty;
            NewWitnessRole = "Student Witness";
            NewWitnessStatement = string.Empty;
            NewWitnessContact = string.Empty;

            NewEvidenceTitle = string.Empty;
            NewEvidenceDescription = string.Empty;
            NewEvidenceType = "File";
            NewEvidenceFilePathOrUrl = string.Empty;
            NewEvidenceFileName = string.Empty;
            NewEvidenceFileSize = 0;
            NewEvidencePreviewPath = string.Empty;

            IsPrimaryStudentConfirmed = false;
        }

        private void PopulateInvolvedWitnessEvidence(Incident inc)
        {
            ResetInvolvedWitnessEvidenceForms();
            IsPrimaryStudentConfirmed = !string.IsNullOrWhiteSpace(inc.PrimaryInvolvedStudent);

            // Deserialize Involved Persons
            if (!string.IsNullOrWhiteSpace(inc.InvolvedPersons))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<InvolvedPersonEntry>>(inc.InvolvedPersons);
                    if (items != null)
                    {
                        foreach (var item in items) InvolvedPersonsList.Add(item);
                    }
                }
                catch
                {
                    // Fallback if legacy comma/semicolon delimited
                    var parts = inc.InvolvedPersons.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        InvolvedPersonsList.Add(new InvolvedPersonEntry { Name = part.Trim(), Role = "Participant" });
                    }
                }
            }

            // Deserialize Witnesses
            if (!string.IsNullOrWhiteSpace(inc.Witnesses))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<WitnessEntry>>(inc.Witnesses);
                    if (items != null)
                    {
                        foreach (var item in items) WitnessesList.Add(item);
                    }
                }
                catch
                {
                    var parts = inc.Witnesses.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        WitnessesList.Add(new WitnessEntry { Name = part.Trim(), RoleOrAffiliation = "Witness" });
                    }
                }
            }

            // Deserialize Evidence
            if (!string.IsNullOrWhiteSpace(inc.EvidenceData))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<IncidentEvidenceEntry>>(inc.EvidenceData);
                    if (items != null)
                    {
                        foreach (var item in items) EvidenceList.Add(item);
                    }
                }
                catch { }
            }

            NotifyPhotoGalleryChanged();
        }
    }
}

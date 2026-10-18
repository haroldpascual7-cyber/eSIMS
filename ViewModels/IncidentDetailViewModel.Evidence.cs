using Microsoft.Win32;
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class IncidentDetailViewModel
    {
        // ---- Evidence & Photo Attachment Support ----
        public ObservableCollection<IncidentEvidenceEntry> EvidenceList { get; private set; } = null!;

        private string _newEvidenceTitle = string.Empty;
        private string _newEvidenceDescription = string.Empty;
        private string _newEvidenceType = "File";
        private string _newEvidenceFilePathOrUrl = string.Empty;
        private string _newEvidenceFileName = string.Empty;
        private long _newEvidenceFileSize;
        private string _newEvidencePreviewPath = string.Empty;

        public string NewEvidenceTitle { get => _newEvidenceTitle; set => SetProperty(ref _newEvidenceTitle, value); }
        public string NewEvidenceDescription { get => _newEvidenceDescription; set => SetProperty(ref _newEvidenceDescription, value); }
        public string NewEvidenceType
        {
            get => _newEvidenceType;
            set { if (SetProperty(ref _newEvidenceType, value)) { OnPropertyChanged(nameof(IsFileEvidenceMode)); OnPropertyChanged(nameof(IsLinkEvidenceMode)); } }
        }
        public bool IsFileEvidenceMode => NewEvidenceType == "File";
        public bool IsLinkEvidenceMode => NewEvidenceType == "Link";
        public string NewEvidenceFilePathOrUrl { get => _newEvidenceFilePathOrUrl; set => SetProperty(ref _newEvidenceFilePathOrUrl, value); }
        public string NewEvidenceFileName { get => _newEvidenceFileName; set => SetProperty(ref _newEvidenceFileName, value); }
        public long NewEvidenceFileSize { get => _newEvidenceFileSize; set => SetProperty(ref _newEvidenceFileSize, value); }
        public string NewEvidencePreviewPath
        {
            get => _newEvidencePreviewPath;
            set { if (SetProperty(ref _newEvidencePreviewPath, value)) { OnPropertyChanged(nameof(HasNewEvidenceImagePreview)); OnPropertyChanged(nameof(IsNewEvidenceAnImage)); } }
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
        public bool HasEvidence => EvidenceList.Count > 0;

        public ICommand BrowseEvidenceFileCommand { get; private set; } = null!;
        public ICommand AddEvidenceCommand { get; private set; } = null!;
        public ICommand RemoveEvidenceCommand { get; private set; } = null!;
        public ICommand OpenEvidenceCommand { get; private set; } = null!;

        private void InitializeEvidenceSupport()
        {
            EvidenceList = new ObservableCollection<IncidentEvidenceEntry>();
            BrowseEvidenceFileCommand = new RelayCommand(BrowseEvidenceFile);
            AddEvidenceCommand = new RelayCommand(AddEvidence);
            RemoveEvidenceCommand = new RelayCommand(param => RemoveEvidence(param as IncidentEvidenceEntry));
            OpenEvidenceCommand = new RelayCommand(param => OpenEvidence(param as IncidentEvidenceEntry));
        }

        internal void LoadEvidenceFromIncident()
        {
            EvidenceList.Clear();
            if (string.IsNullOrWhiteSpace(_incident.EvidenceData)) return;
            try
            {
                var items = JsonSerializer.Deserialize<List<IncidentEvidenceEntry>>(_incident.EvidenceData);
                if (items != null) foreach (var item in items) EvidenceList.Add(item);
            }
            catch { }
            OnPropertyChanged(nameof(HasEvidence));
        }

        private void BrowseEvidenceFile()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Evidence File / Photo",
                Filter = "All Evidence Files (*.png;*.jpg;*.jpeg;*.pdf;*.mp4;*.docx;*.txt)|*.png;*.jpg;*.jpeg;*.pdf;*.mp4;*.docx;*.txt|Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Documents|*.pdf;*.docx;*.txt|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                NewEvidenceFilePathOrUrl = dlg.FileName;
                NewEvidenceFileName = Path.GetFileName(dlg.FileName);
                NewEvidencePreviewPath = dlg.FileName;
                if (string.IsNullOrWhiteSpace(NewEvidenceTitle))
                    NewEvidenceTitle = Path.GetFileNameWithoutExtension(dlg.FileName);
                try { NewEvidenceFileSize = new FileInfo(dlg.FileName).Length; } catch { NewEvidenceFileSize = 0; }
            }
        }

        private void AddEvidence()
        {
            if (string.IsNullOrWhiteSpace(NewEvidenceFilePathOrUrl)) return;

            string finalPath = NewEvidenceFilePathOrUrl.Trim();
            string fileName = string.IsNullOrWhiteSpace(NewEvidenceFileName)
                ? Path.GetFileName(finalPath) : NewEvidenceFileName;

            if (IsFileEvidenceMode && File.Exists(finalPath))
            {
                try
                {
                    string safeNum = string.IsNullOrWhiteSpace(_incident.IncidentNumber)
                        ? "INC-PENDING"
                        : _incident.IncidentNumber.Replace("/", "_").Replace("\\", "_");
                    string destDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "SmartSchool", "Evidence", safeNum);
                    Directory.CreateDirectory(destDir);
                    string destFile = Path.Combine(destDir, $"{Guid.NewGuid().ToString("N")[..8]}_{Path.GetFileName(finalPath)}");
                    File.Copy(finalPath, destFile, true);
                    finalPath = destFile;
                    fileName = Path.GetFileName(destFile);
                }
                catch { }
            }

            EvidenceList.Add(new IncidentEvidenceEntry
            {
                Title = string.IsNullOrWhiteSpace(NewEvidenceTitle) ? fileName : NewEvidenceTitle.Trim(),
                Description = NewEvidenceDescription.Trim(),
                EvidenceType = NewEvidenceType,
                FilePathOrUrl = finalPath,
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
            OnPropertyChanged(nameof(HasEvidence));
        }

        private void RemoveEvidence(IncidentEvidenceEntry? entry)
        {
            if (entry != null) { EvidenceList.Remove(entry); OnPropertyChanged(nameof(HasEvidence)); }
        }

        private void OpenEvidence(IncidentEvidenceEntry? entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.FilePathOrUrl)) return;
            try { Process.Start(new ProcessStartInfo { FileName = entry.FilePathOrUrl, UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open: {ex.Message}", "Open Evidence",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

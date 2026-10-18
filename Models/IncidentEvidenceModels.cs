using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class InvolvedPersonEntry
    {
        public string Name { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Participant";
        public string GradeSection { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public string RoleBadgeText => !string.IsNullOrWhiteSpace(Role) ? Role : "Involved";
        public string DisplayText => string.IsNullOrWhiteSpace(StudentNumber)
            ? $"{Name} ({Role})"
            : $"{Name} [{StudentNumber}] - {Role}" + (!string.IsNullOrWhiteSpace(GradeSection) ? $" ({GradeSection})" : "");
    }

    public class WitnessEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string RoleOrAffiliation { get; set; } = "Student Witness";
        public string Statement { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public DateTime DateRecorded { get; set; } = DateTime.Now;

        public string FormattedDate => DateRecorded.ToString("MMM dd, yyyy hh:mm tt");
        public string StatementPreview => Statement.Length > 90 ? Statement.Substring(0, 87) + "..." : Statement;
    }

    public class IncidentEvidenceEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EvidenceType { get; set; } = "File";
        public string FilePathOrUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; } = 0;
        public DateTime DateAttached { get; set; } = DateTime.Now;

        public string FormattedDate => DateAttached.ToString("MMM dd, yyyy hh:mm tt");
        public string FormattedFileSize
        {
            get
            {
                if (EvidenceType == "Link") return "External URL / Cloud Link";
                if (FileSizeBytes <= 0) return "File Attachment";
                if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
                return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
            }
        }

        public string TypeIcon => EvidenceType == "Link" ? "🔗" : (IsImage ? "🖼️" : "📎");
        public bool IsLink => EvidenceType == "Link";

        public bool IsImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FilePathOrUrl)) return false;
                string cleanPath = FilePathOrUrl.Split('?')[0].Split('#')[0];
                string ext = System.IO.Path.GetExtension(cleanPath).ToLowerInvariant();
                return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp" or ".gif";
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public object? ImageDisplaySource
        {
            get
            {
                if (!IsImage || string.IsNullOrWhiteSpace(FilePathOrUrl)) return null;
                try
                {
                    if (System.IO.File.Exists(FilePathOrUrl))
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bmp.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                        bmp.UriSource = new Uri(FilePathOrUrl, UriKind.Absolute);
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                    else if (Uri.TryCreate(FilePathOrUrl, UriKind.Absolute, out var uri))
                    {
                        return uri;
                    }
                }
                catch { }
                return FilePathOrUrl;
            }
        }
    }
}

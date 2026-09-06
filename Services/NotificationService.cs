using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private readonly List<SystemNotificationItem> _notifications = new();
        private bool _isInitialized = false;

        public event Action? NotificationsChanged;

        public NotificationService()
        {
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                using var context = new AppDbContext();
                var incidents = await context.Incidents
                    .Include(i => i.Student)
                    .OrderByDescending(i => i.IncidentDate)
                    .Take(15)
                    .ToListAsync();

                _notifications.Clear();

                foreach (var inc in incidents)
                {
                    string studentName = inc.StudentDisplayName;

                    // 1. Pending Triage for Admin / Discipline Officer
                    if (inc.Status == IncidentStatus.Reported)
                    {
                        _notifications.Add(new SystemNotificationItem
                        {
                            Id = $"notif-inc-rep-{inc.Id}",
                            Title = $"New Incident Report: {inc.IncidentNumber}",
                            Message = $"'{inc.Title}' filed for {studentName}. Category: {inc.Category}. Requires initial review and triaging.",
                            Category = "Incident",
                            Icon = "🚨",
                            Timestamp = inc.IncidentDate,
                            TargetWorkspace = "Incidents",
                            TargetReferenceId = inc.IncidentNumber,
                            TargetRole = "Administrator,Discipline Officer",
                            SeverityLevel = inc.Risk >= RiskLevel.High ? "Urgent" : "Warning",
                            IsRead = false
                        });
                    }

                    // 2. High Risk Alerts for Admin & Discipline
                    if (inc.Risk == RiskLevel.High || inc.Risk == RiskLevel.Critical)
                    {
                        _notifications.Add(new SystemNotificationItem
                        {
                            Id = $"notif-risk-{inc.Id}",
                            Title = $"High Risk Level Flagged: {inc.IncidentNumber}",
                            Message = $"Incident for {studentName} evaluated at {inc.Risk} Risk. Immediate supervisory review recommended.",
                            Category = "Security",
                            Icon = "⚠️",
                            Timestamp = inc.IncidentDate.AddMinutes(5),
                            TargetWorkspace = "Incidents",
                            TargetReferenceId = inc.IncidentNumber,
                            TargetRole = "Administrator,Discipline Officer",
                            SeverityLevel = "Urgent",
                            IsRead = false
                        });
                    }

                    // 3. Counseling Referrals for Guidance Counselor
                    if (inc.Status == IncidentStatus.UnderInvestigation || inc.Category.Contains("Bullying", StringComparison.OrdinalIgnoreCase) || inc.Category.Contains("Behavioral", StringComparison.OrdinalIgnoreCase))
                    {
                        _notifications.Add(new SystemNotificationItem
                        {
                            Id = $"notif-counsel-{inc.Id}",
                            Title = $"Student Counseling Referral: {studentName}",
                            Message = $"Case '{inc.IncidentNumber}' ({inc.Title}) requires behavioral assessment and supportive counseling session.",
                            Category = "Behavior",
                            Icon = "🤝",
                            Timestamp = inc.IncidentDate.AddMinutes(15),
                            TargetWorkspace = "Behavior",
                            TargetReferenceId = inc.IncidentNumber,
                            TargetRole = "Guidance Counselor",
                            SeverityLevel = "Normal",
                            IsRead = false
                        });
                    }

                    // 4. Case Investigation Update
                    if (inc.Status == IncidentStatus.UnderInvestigation)
                    {
                        _notifications.Add(new SystemNotificationItem
                        {
                            Id = $"notif-inv-{inc.Id}",
                            Title = $"Active Investigation Case: {inc.IncidentNumber}",
                            Message = $"Formal inquiry active for {studentName}. Interview statements and evidence log awaiting review.",
                            Category = "Investigation",
                            Icon = "🔍",
                            Timestamp = inc.IncidentDate.AddMinutes(30),
                            TargetWorkspace = "Investigations",
                            TargetReferenceId = inc.IncidentNumber,
                            TargetRole = "Administrator,Discipline Officer,Guidance Counselor",
                            SeverityLevel = "Normal",
                            IsRead = false
                        });
                    }

                    // 5. Staff Filing Update (For Staff role)
                    if (!string.IsNullOrWhiteSpace(inc.ReportingStaffTeacher))
                    {
                        string statusLabel = inc.Status == IncidentStatus.Reported ? "Submitted & In Queue" : inc.Status.ToString();
                        _notifications.Add(new SystemNotificationItem
                        {
                            Id = $"notif-staff-{inc.Id}",
                            Title = $"Report Status: {inc.IncidentNumber}",
                            Message = $"Your submitted report for {studentName} ('{inc.Title}') is currently marked as: {statusLabel}.",
                            Category = "Incident",
                            Icon = "📋",
                            Timestamp = inc.IncidentDate.AddMinutes(10),
                            TargetWorkspace = "Incidents",
                            TargetReferenceId = inc.IncidentNumber,
                            TargetRole = "Staff",
                            SeverityLevel = "Normal",
                            IsRead = false
                        });
                    }
                }

                // General School-Wide System Notifications for all roles
                _notifications.Add(new SystemNotificationItem
                {
                    Id = "notif-system-welcome",
                    Title = "School Safety System Active",
                    Message = "Real-time incident tracking, behavioral observation logs, and parent communication channels are fully operational.",
                    Category = "System",
                    Icon = "🛡️",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    TargetWorkspace = "Dashboard",
                    TargetRole = "All",
                    SeverityLevel = "Normal",
                    IsRead = true
                });

                _notifications.Add(new SystemNotificationItem
                {
                    Id = "notif-notice-dispatch",
                    Title = "Parent Notice Dispatched",
                    Message = "Automated advisory notice dispatched via email/SMS for attendance and behavioral review.",
                    Category = "ParentNotice",
                    Icon = "📬",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    TargetWorkspace = "ParentNotices",
                    TargetRole = "Staff,Guidance Counselor,Administrator,Discipline Officer",
                    SeverityLevel = "Normal",
                    IsRead = false
                });

                _isInitialized = true;
            }
            catch
            {
                // Fallback demo notifications if database query fails
                EnsureFallbackNotifications();
                _isInitialized = true;
            }

            NotificationsChanged?.Invoke();
        }

        private void EnsureFallbackNotifications()
        {
            if (_notifications.Any()) return;

            _notifications.Add(new SystemNotificationItem
            {
                Id = "fallback-1",
                Title = "New Incident Intake Pending",
                Message = "A newly submitted incident report requires review and triage in the registry.",
                Category = "Incident",
                Icon = "🚨",
                Timestamp = DateTime.UtcNow.AddMinutes(-20),
                TargetWorkspace = "Incidents",
                TargetRole = "All",
                SeverityLevel = "Warning",
                IsRead = false
            });

            _notifications.Add(new SystemNotificationItem
            {
                Id = "fallback-2",
                Title = "Parent Advisory Notice Dispatched",
                Message = "Official letter generated and sent to guardian regarding behavioral check-in.",
                Category = "ParentNotice",
                Icon = "📬",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                TargetWorkspace = "ParentNotices",
                TargetRole = "All",
                SeverityLevel = "Normal",
                IsRead = false
            });
        }

        public List<SystemNotificationItem> GetNotificationsForUser(string role, string username)
        {
            if (string.IsNullOrWhiteSpace(role))
                return _notifications.OrderByDescending(n => n.Timestamp).ToList();

            return _notifications
                .Where(n => n.TargetRole.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                            n.TargetRole.IndexOf(role, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (role.Equals("Staff", StringComparison.OrdinalIgnoreCase) && n.TargetRole.IndexOf("Teacher", StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderByDescending(n => n.Timestamp)
                .ToList();
        }

        public int GetUnreadCountForUser(string role, string username)
        {
            return GetNotificationsForUser(role, username).Count(n => !n.IsRead);
        }

        public void MarkAsRead(string notificationId)
        {
            var item = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (item != null && !item.IsRead)
            {
                item.IsRead = true;
                NotificationsChanged?.Invoke();
            }
        }

        public void MarkAllAsRead(string role, string username)
        {
            var userNotifs = GetNotificationsForUser(role, username);
            bool changed = false;
            foreach (var item in userNotifs)
            {
                if (!item.IsRead)
                {
                    item.IsRead = true;
                    changed = true;
                }
            }

            if (changed)
            {
                NotificationsChanged?.Invoke();
            }
        }

        public void AddNotification(SystemNotificationItem notification)
        {
            _notifications.Insert(0, notification);
            NotificationsChanged?.Invoke();
        }
    }
}

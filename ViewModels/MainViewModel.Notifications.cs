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
    public partial class MainViewModel
    {
        private bool _isNotificationModalOpen;
        private string _selectedNotificationFilter = "All";
        private int _unreadNotificationCount;

        public ObservableCollection<SystemNotificationItem> Notifications { get; } = new();
        public ObservableCollection<SystemNotificationItem> FilteredNotifications { get; } = new();

        public bool IsNotificationModalOpen
        {
            get => _isNotificationModalOpen;
            set => SetProperty(ref _isNotificationModalOpen, value);
        }

        public string SelectedNotificationFilter
        {
            get => _selectedNotificationFilter;
            set
            {
                if (SetProperty(ref _selectedNotificationFilter, value))
                {
                    ApplyNotificationFilter();
                    OnPropertyChanged(nameof(IsAllFilterActive));
                    OnPropertyChanged(nameof(IsUnreadFilterActive));
                }
            }
        }

        public bool IsAllFilterActive => SelectedNotificationFilter == "All";
        public bool IsUnreadFilterActive => SelectedNotificationFilter == "Unread";

        public int UnreadNotificationCount
        {
            get => _unreadNotificationCount;
            private set
            {
                if (SetProperty(ref _unreadNotificationCount, value))
                {
                    OnPropertyChanged(nameof(HasUnreadNotifications));
                }
            }
        }

        public bool HasUnreadNotifications => UnreadNotificationCount > 0;
        public bool HasFilteredNotifications => FilteredNotifications.Count > 0;

        public ICommand OpenNotificationModalCommand { get; private set; } = null!;
        public ICommand CloseNotificationModalCommand { get; private set; } = null!;
        public ICommand MarkAllNotificationsAsReadCommand { get; private set; } = null!;
        public ICommand NavigateNotificationCommand { get; private set; } = null!;
        public ICommand FilterAllNotificationsCommand { get; private set; } = null!;
        public ICommand FilterUnreadNotificationsCommand { get; private set; } = null!;

        private void InitializeNotificationSystem()
        {
            OpenNotificationModalCommand = new RelayCommand(_ => OpenNotificationModal());
            CloseNotificationModalCommand = new RelayCommand(_ => CloseNotificationModal());
            MarkAllNotificationsAsReadCommand = new RelayCommand(_ => MarkAllNotificationsAsRead());
            NavigateNotificationCommand = new RelayCommand(param =>
            {
                if (param is SystemNotificationItem notif)
                {
                    NavigateFromNotification(notif);
                }
            });
            FilterAllNotificationsCommand = new RelayCommand(_ => SelectedNotificationFilter = "All");
            FilterUnreadNotificationsCommand = new RelayCommand(_ => SelectedNotificationFilter = "Unread");

            NotificationService.Instance.NotificationsChanged += RefreshNotifications;
        }

        public async Task LoadNotificationsAsync()
        {
            await NotificationService.Instance.InitializeAsync();
            RefreshNotifications();
        }

        public void OpenNotificationModal()
        {
            _ = LoadNotificationsAsync();
            IsNotificationModalOpen = true;
        }

        public void CloseNotificationModal()
        {
            IsNotificationModalOpen = false;
        }

        public void RefreshNotifications()
        {
            var items = NotificationService.Instance.GetNotificationsForUser(CurrentUserRole, CurrentUserName);
            Notifications.Clear();
            foreach (var item in items)
            {
                Notifications.Add(item);
            }

            UnreadNotificationCount = items.Count(n => !n.IsRead);
            ApplyNotificationFilter();
        }

        private void ApplyNotificationFilter()
        {
            FilteredNotifications.Clear();
            var source = SelectedNotificationFilter == "Unread"
                ? Notifications.Where(n => !n.IsRead)
                : Notifications;

            foreach (var item in source)
            {
                FilteredNotifications.Add(item);
            }
            OnPropertyChanged(nameof(HasFilteredNotifications));
        }

        public void MarkAllNotificationsAsRead()
        {
            NotificationService.Instance.MarkAllAsRead(CurrentUserRole, CurrentUserName);
            RefreshNotifications();
        }

        public void NavigateFromNotification(SystemNotificationItem notif)
        {
            if (notif == null) return;

            NotificationService.Instance.MarkAsRead(notif.Id);
            RefreshNotifications();
            CloseNotificationModal();

            switch (notif.TargetWorkspace?.Trim())
            {
                case "Incidents":
                    ShowIncidents();
                    break;
                case "Investigations":
                    ShowInvestigations();
                    break;
                case "Behavior":
                    ShowBehaviorWorkspace();
                    break;
                case "ParentNotices":
                    ShowParentNotices();
                    break;
                case "Students":
                    ShowStudents();
                    break;
                case "Reports":
                    ShowReportsAnalytics();
                    break;
                case "Settings":
                    ShowSettingsWorkspace();
                    break;
                case "Dashboard":
                default:
                    ShowDashboard();
                    break;
            }
        }
    }
}

using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class AuditTrailHubViewModel : ObservableObject
    {
        private readonly Action _onActivityLog;
        private readonly Action _onIntegrityAudit;
        private readonly Action _onSecurityAccessLog;

        public AuditTrailHubViewModel(Action onActivityLog, Action onIntegrityAudit, Action onSecurityAccessLog)
        {
            _onActivityLog       = onActivityLog       ?? throw new ArgumentNullException(nameof(onActivityLog));
            _onIntegrityAudit    = onIntegrityAudit    ?? throw new ArgumentNullException(nameof(onIntegrityAudit));
            _onSecurityAccessLog = onSecurityAccessLog ?? throw new ArgumentNullException(nameof(onSecurityAccessLog));

            var actCmd = new RelayCommand(_ => _onActivityLog());
            var intCmd = new RelayCommand(_ => _onIntegrityAudit());
            var secCmd = new RelayCommand(_ => _onSecurityAccessLog());

            NavigateActivityLogCommand    = actCmd;
            OpenActivityLogCommand        = actCmd;

            NavigateIntegrityAuditCommand = intCmd;
            OpenIntegrityAuditCommand     = intCmd;

            NavigateSecurityAccessLogCommand = secCmd;
            OpenSecurityAccessLogCommand     = secCmd;
        }

        public ICommand NavigateActivityLogCommand    { get; }
        public ICommand OpenActivityLogCommand        { get; }

        public ICommand NavigateIntegrityAuditCommand { get; }
        public ICommand OpenIntegrityAuditCommand     { get; }

        public ICommand NavigateSecurityAccessLogCommand { get; }
        public ICommand OpenSecurityAccessLogCommand     { get; }

        public string Title    => "System Audit Trail & Security Logs Hub";
        public string Subtitle => "Enterprise user activity tracking, data modification audit trails, and system access logs.";
    }
}
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class ParentNoticesHubViewModel : ObservableObject
    {
        private readonly Action _onLetterTemplates;
        private readonly Action _onDispatch;
        private readonly Action _onAcknowledgment;

        public ParentNoticesHubViewModel(Action onLetterTemplates, Action onDispatch, Action onAcknowledgment)
        {
            _onLetterTemplates  = onLetterTemplates  ?? throw new ArgumentNullException(nameof(onLetterTemplates));
            _onDispatch         = onDispatch         ?? throw new ArgumentNullException(nameof(onDispatch));
            _onAcknowledgment   = onAcknowledgment   ?? throw new ArgumentNullException(nameof(onAcknowledgment));

            var letterCmd   = new RelayCommand(_ => _onLetterTemplates());
            var dispatchCmd = new RelayCommand(_ => _onDispatch());
            var ackCmd      = new RelayCommand(_ => _onAcknowledgment());

            NavigateLetterTemplatesCommand = letterCmd;
            OpenLetterTemplatesCommand     = letterCmd;

            NavigateDispatchCommand        = dispatchCmd;
            OpenDispatchCommand            = dispatchCmd;

            NavigateAcknowledgmentCommand  = ackCmd;
            OpenAcknowledgmentCommand      = ackCmd;
        }

        public ICommand NavigateLetterTemplatesCommand { get; }
        public ICommand OpenLetterTemplatesCommand     { get; }

        public ICommand NavigateDispatchCommand        { get; }
        public ICommand OpenDispatchCommand            { get; }

        public ICommand NavigateAcknowledgmentCommand  { get; }
        public ICommand OpenAcknowledgmentCommand      { get; }

        public string Title    => "Parent Notices & Official Disclosures";
        public string Subtitle => "Automated letter templates, guardian SMS/email notifications, and disclosure slips.";
    }
}
using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class ParentNoticesViewModel
    {
        // 1. Notice Card Details Modal
        private bool _isViewNoticeModalOpen;
        private AcknowledgmentLog? _viewingNotice;

        public bool IsViewNoticeModalOpen
        {
            get => _isViewNoticeModalOpen;
            set => SetProperty(ref _isViewNoticeModalOpen, value);
        }

        public AcknowledgmentLog? ViewingNotice
        {
            get => _viewingNotice;
            set => SetProperty(ref _viewingNotice, value);
        }

        // 2. Dispatch Card Details Modal
        private bool _isViewDispatchModalOpen;
        private DispatchLog? _viewingDispatch;

        public bool IsViewDispatchModalOpen
        {
            get => _isViewDispatchModalOpen;
            set => SetProperty(ref _isViewDispatchModalOpen, value);
        }

        public DispatchLog? ViewingDispatch
        {
            get => _viewingDispatch;
            set => SetProperty(ref _viewingDispatch, value);
        }

        // 3. Template Card Details Modal
        private bool _isViewTemplateModalOpen;
        private LetterTemplate? _viewingTemplate;

        public bool IsViewTemplateModalOpen
        {
            get => _isViewTemplateModalOpen;
            set => SetProperty(ref _isViewTemplateModalOpen, value);
        }

        public LetterTemplate? ViewingTemplate
        {
            get => _viewingTemplate;
            set => SetProperty(ref _viewingTemplate, value);
        }

        // Commands
        public ICommand OpenNoticeCardCommand => new RelayCommand<AcknowledgmentLog>(notice =>
        {
            if (notice == null) return;
            ViewingNotice = notice;
            SelectedNotice = notice;
            IsViewNoticeModalOpen = true;
        });

        public ICommand CloseNoticeCardCommand => new RelayCommand(() =>
        {
            IsViewNoticeModalOpen = false;
            ViewingNotice = null;
        });

        public ICommand DeleteNoticeCommand => new RelayCommand(async () =>
        {
            if (ViewingNotice == null) return;
            var confirm = MessageBox.Show(
                $"Are you sure you want to delete notice reference {ViewingNotice.NoticeId} for student {ViewingNotice.StudentName}?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await _ackService.DeleteLogAsync(ViewingNotice.Id);
                IsViewNoticeModalOpen = false;
                ViewingNotice = null;
                await LoadDataAsync();
            }
        });

        public ICommand InModalLogReceiptCommand => new RelayCommand(() =>
        {
            if (ViewingNotice == null) return;
            var target = ViewingNotice;
            IsViewNoticeModalOpen = false;
            OpenLogReceipt(target);
        });

        public ICommand InModalExportPdfCommand => new RelayCommand(() =>
        {
            if (ViewingNotice == null) return;
            ExportNoticePdf(ViewingNotice);
        });

        public ICommand OpenDispatchCardCommand => new RelayCommand<DispatchLog>(dispatch =>
        {
            if (dispatch == null) return;
            ViewingDispatch = dispatch;
            IsViewDispatchModalOpen = true;
        });

        public ICommand CloseDispatchCardCommand => new RelayCommand(() =>
        {
            IsViewDispatchModalOpen = false;
            ViewingDispatch = null;
        });

        public ICommand OpenTemplateCardCommand => new RelayCommand<LetterTemplate>(template =>
        {
            if (template == null) return;
            ViewingTemplate = template;
            IsViewTemplateModalOpen = true;
        });

        public ICommand CloseTemplateCardCommand => new RelayCommand(() =>
        {
            IsViewTemplateModalOpen = false;
            ViewingTemplate = null;
        });
    }
}

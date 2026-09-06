using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class BehaviorWorkspaceViewModel
    {
        // 1. Timeline Entry Modal State
        private bool _isViewTimelineModalOpen;
        private TranscriptEntryModel? _viewingTimelineEntry;

        public bool IsViewTimelineModalOpen
        {
            get => _isViewTimelineModalOpen;
            set => SetProperty(ref _isViewTimelineModalOpen, value);
        }

        public TranscriptEntryModel? ViewingTimelineEntry
        {
            get => _viewingTimelineEntry;
            set => SetProperty(ref _viewingTimelineEntry, value);
        }

        // 2. Anecdotal Record Modal State
        private bool _isViewAnecdotalModalOpen;
        private AnecdotalRecordItem? _viewingAnecdotalRecord;

        public bool IsViewAnecdotalModalOpen
        {
            get => _isViewAnecdotalModalOpen;
            set => SetProperty(ref _isViewAnecdotalModalOpen, value);
        }

        public AnecdotalRecordItem? ViewingAnecdotalRecord
        {
            get => _viewingAnecdotalRecord;
            set => SetProperty(ref _viewingAnecdotalRecord, value);
        }

        // 3. Restorative Plan Modal State
        private bool _isViewRestorativeModalOpen;
        private RestorativePlan? _viewingRestorativePlan;

        public bool IsViewRestorativeModalOpen
        {
            get => _isViewRestorativeModalOpen;
            set => SetProperty(ref _isViewRestorativeModalOpen, value);
        }

        public RestorativePlan? ViewingRestorativePlan
        {
            get => _viewingRestorativePlan;
            set => SetProperty(ref _viewingRestorativePlan, value);
        }

        // 4. Action Plan Modal State
        private bool _isViewActionPlanModalOpen;
        private IndividualActionPlan? _viewingActionPlan;

        public bool IsViewActionPlanModalOpen
        {
            get => _isViewActionPlanModalOpen;
            set => SetProperty(ref _isViewActionPlanModalOpen, value);
        }

        public IndividualActionPlan? ViewingActionPlan
        {
            get => _viewingActionPlan;
            set => SetProperty(ref _viewingActionPlan, value);
        }

        // Commands
        public ICommand OpenTimelineCardCommand => new RelayCommand<TranscriptEntryModel>(entry =>
        {
            if (entry == null) return;
            ViewingTimelineEntry = entry;
            IsViewTimelineModalOpen = true;
        });

        public ICommand CloseTimelineCardCommand => new RelayCommand(() =>
        {
            IsViewTimelineModalOpen = false;
            ViewingTimelineEntry = null;
        });

        public ICommand OpenAnecdotalCardCommand => new RelayCommand<AnecdotalRecordItem>(record =>
        {
            if (record == null) return;
            ViewingAnecdotalRecord = record;
            IsViewAnecdotalModalOpen = true;
        });

        public ICommand CloseAnecdotalCardCommand => new RelayCommand(() =>
        {
            IsViewAnecdotalModalOpen = false;
            ViewingAnecdotalRecord = null;
        });

        public ICommand DeleteAnecdotalRecordCommand => new RelayCommand(async () =>
        {
            if (ViewingAnecdotalRecord == null) return;
            var confirm = MessageBox.Show(
                $"Are you sure you want to delete this anecdotal observation?",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                await _anecdotalService.DeleteAsync(ViewingAnecdotalRecord.Id);
                IsViewAnecdotalModalOpen = false;
                ViewingAnecdotalRecord = null;
                if (SelectedStudent != null) await LoadStudentBehaviorDataAsync(SelectedStudent);
            }
        });

        public ICommand OpenRestorativeCardCommand => new RelayCommand<RestorativePlan>(plan =>
        {
            if (plan == null) return;
            ViewingRestorativePlan = plan;
            IsViewRestorativeModalOpen = true;
        });

        public ICommand CloseRestorativeCardCommand => new RelayCommand(() =>
        {
            IsViewRestorativeModalOpen = false;
            ViewingRestorativePlan = null;
        });

        public ICommand MarkRestorativeCompletedCommand => new RelayCommand(async () =>
        {
            if (ViewingRestorativePlan == null) return;
            await _restorativeService.UpdatePlanStatusAsync(ViewingRestorativePlan.Id, "Completed", true);
            IsViewRestorativeModalOpen = false;
            ViewingRestorativePlan = null;
            if (SelectedStudent != null) await LoadStudentBehaviorDataAsync(SelectedStudent);
        });

        public ICommand OpenActionPlanCardCommand => new RelayCommand<IndividualActionPlan>(iap =>
        {
            if (iap == null) return;
            ViewingActionPlan = iap;
            IsViewActionPlanModalOpen = true;
        });

        public ICommand CloseActionPlanCardCommand => new RelayCommand(() =>
        {
            IsViewActionPlanModalOpen = false;
            ViewingActionPlan = null;
        });
    }
}

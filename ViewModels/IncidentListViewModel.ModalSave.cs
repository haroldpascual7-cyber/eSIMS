using SmartSchoolIncidentManagementSystem.Models;
using SmartSchoolIncidentManagementSystem.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public partial class IncidentListViewModel
    {
        private async Task SaveModalAsync()
        {
            ModalErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ModalTitleField))
            {
                ModalErrorMessage = "Incident Title is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ModalStudentName))
            {
                ModalErrorMessage = "Primary involved student is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ModalDescription))
            {
                ModalErrorMessage = "Incident narrative description is required.";
                return;
            }

            try
            {
                var matchedStudent = _allEnrolledStudents.FirstOrDefault(s =>
                    s.FullName.Equals(ModalStudentName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    s.StudentNumber.Equals(ModalStudentNumber.Trim(), StringComparison.OrdinalIgnoreCase));

                DateTime incidentDateTime = ModalDate.Date;
                if (TimeSpan.TryParse(ModalTime, out var timeOfDay))
                {
                    incidentDateTime = incidentDateTime.Add(timeOfDay);
                }

                // Auto-attach any pending browsed/selected evidence if user forgot to click "+ Attach"
                if (!string.IsNullOrWhiteSpace(NewEvidenceFilePathOrUrl))
                {
                    AddEvidence();
                }

                string involvedJson = JsonSerializer.Serialize(InvolvedPersonsList.ToList());
                string witnessJson = JsonSerializer.Serialize(WitnessesList.ToList());
                string evidenceJson = JsonSerializer.Serialize(EvidenceList.ToList());
                bool isLimitedReporter = IsLimitedIncidentReporterRole();
                string currentReporterName = !string.IsNullOrWhiteSpace(SessionManager.FullName)
                    ? SessionManager.FullName
                    : SessionManager.Username;
                var savedStatus = isLimitedReporter ? IncidentStatus.Reported : ModalStatus;
                var savedSeverity = isLimitedReporter ? LiveSuggestedSeverity : ModalSeverity;
                var savedRisk = isLimitedReporter ? LiveSuggestedRisk : ModalRisk;
                string savedActionTaken = ModalActionTaken?.Trim() ?? string.Empty;

                if (IsModalNewRecord)
                {
                    var newInc = new Incident
                    {
                        IncidentNumber = ModalIncidentNumber,
                        Title = ModalTitleField.Trim(),
                        Category = ModalCategory,
                        Description = ModalDescription.Trim(),
                        Location = ModalLocation.Trim(),
                        IncidentDate = incidentDateTime,
                        PrimaryInvolvedStudent = ModalStudentName.Trim(),
                        StudentId = matchedStudent?.Id,
                        ReportingStaffTeacher = isLimitedReporter ? currentReporterName : ModalReporter.Trim(),
                        CreatedBy = SessionManager.Username,
                        Status = savedStatus,
                        Severity = savedSeverity,
                        Risk = savedRisk,
                        ActionTaken = savedActionTaken,
                        Notes = ModalNotes?.Trim() ?? string.Empty,
                        RecommendedAction = LiveRecommendedAction,
                        IsRepeatIncident = IsRepeatOffenderAlert,
                        InvolvedPersons = involvedJson,
                        Witnesses = witnessJson,
                        EvidenceData = evidenceJson
                    };

                    await _incidentService.AddIncidentAsync(newInc);
                    _modalIncidentPk = newInc.Id;

                    if (!string.IsNullOrWhiteSpace(CounselingDetails))
                    {
                        var action = new DisciplinaryAction
                        {
                            IncidentId = newInc.Id,
                            StudentId = matchedStudent?.Id,
                            ActionType = CounselingActionType,
                            ScheduledDate = CounselingDate,
                            Status = CounselingStatus,
                            ActionDetails = CounselingDetails.Trim(),
                            FollowUpNotes = CounselingNotes?.Trim() ?? string.Empty,
                            CompletedDate = CounselingStatus == "Completed" ? DateTime.Now : null
                        };

                        if (_investigationService != null)
                        {
                            await _investigationService.AddDisciplinaryActionAsync(action);
                        }

                        newInc.Status = IncidentStatus.ActionTaken;
                        newInc.ActionTaken = $"[{CounselingActionType} - {CounselingStatus}] Scheduled: {CounselingDate:yyyy-MM-dd}. {CounselingDetails.Trim()}";
                        await _incidentService.UpdateIncidentAsync(newInc);
                        CounselingDetails = string.Empty;
                        CounselingNotes = string.Empty;
                    }

                    if (SendParentNotification && CanSendParentNotification && !string.IsNullOrWhiteSpace(ModalParentEmail))
                    {
                        string emailSubject = $"Official Incident Notification: [{newInc.IncidentNumber}] {newInc.Title}";
                        string emailBody = $"Dear {ModalParentName},\n\nPlease be informed that an incident concerning {ModalStudentName} was recorded today ({incidentDateTime:yyyy-MM-dd HH:mm}).\nCategory: {ModalCategory}\nSeverity: {savedSeverity}\nDescription: {ModalDescription}\n\nPlease contact the School Discipline Office for further inquiry.\n\nSincerely,\n{newInc.ReportingStaffTeacher}\nMatti National High School";

                        await _commService.SendEmailAsync(matchedStudent?.StudentNumber ?? "N/A", ModalParentName, ModalParentEmail, emailSubject, emailBody);
                    }
                }
                else
                {
                    var existing = await _incidentService.GetIncidentByIdAsync(_modalIncidentPk);
                    if (existing != null)
                    {
                        if (isLimitedReporter)
                        {
                            bool isOriginalReporter = TextEquals(existing.CreatedBy, SessionManager.Username)
                                || TextEquals(existing.ReportingStaffTeacher, SessionManager.Username)
                                || TextEquals(existing.ReportingStaffTeacher, SessionManager.FullName);

                            if (!isOriginalReporter || existing.Status != IncidentStatus.Reported)
                            {
                                ModalErrorMessage = "Staff/Teacher accounts can only update their own incident while it remains Reported.";
                                return;
                            }
                        }

                        existing.Title = ModalTitleField.Trim();
                        existing.Category = ModalCategory;
                        existing.Description = ModalDescription.Trim();
                        existing.Location = ModalLocation.Trim();
                        existing.IncidentDate = incidentDateTime;
                        existing.PrimaryInvolvedStudent = ModalStudentName.Trim();
                        if (matchedStudent != null) existing.StudentId = matchedStudent.Id;
                        existing.ReportingStaffTeacher = isLimitedReporter ? existing.ReportingStaffTeacher : ModalReporter.Trim();
                        existing.Status = isLimitedReporter ? existing.Status : ModalStatus;
                        existing.Severity = isLimitedReporter ? LiveSuggestedSeverity : ModalSeverity;
                        existing.Risk = isLimitedReporter ? LiveSuggestedRisk : ModalRisk;
                        existing.ActionTaken = savedActionTaken;
                        existing.Notes = ModalNotes?.Trim() ?? string.Empty;
                        existing.RecommendedAction = LiveRecommendedAction;
                        existing.IsRepeatIncident = IsRepeatOffenderAlert;
                        existing.InvolvedPersons = involvedJson;
                        existing.Witnesses = witnessJson;
                        existing.EvidenceData = evidenceJson;

                        if (!string.IsNullOrWhiteSpace(CounselingDetails))
                        {
                            var action = new DisciplinaryAction
                            {
                                IncidentId = existing.Id,
                                StudentId = matchedStudent?.Id ?? existing.StudentId,
                                ActionType = CounselingActionType,
                                ScheduledDate = CounselingDate,
                                Status = CounselingStatus,
                                ActionDetails = CounselingDetails.Trim(),
                                FollowUpNotes = CounselingNotes?.Trim() ?? string.Empty,
                                CompletedDate = CounselingStatus == "Completed" ? DateTime.Now : null
                            };

                            if (_investigationService != null)
                            {
                                await _investigationService.AddDisciplinaryActionAsync(action);
                            }

                            existing.Status = IncidentStatus.ActionTaken;
                            existing.ActionTaken = $"[{CounselingActionType} - {CounselingStatus}] Scheduled: {CounselingDate:yyyy-MM-dd}. {CounselingDetails.Trim()}";
                            CounselingDetails = string.Empty;
                            CounselingNotes = string.Empty;
                        }

                        await _incidentService.UpdateIncidentAsync(existing);
                    }
                }

                IsIncidentModalOpen = false;
                await LoadIncidentsAsync();
            }
            catch (Exception ex)
            {
                ModalErrorMessage = ex.Message;
            }
        }

        private async Task DeleteCurrentModalIncidentAsync()
        {
            if (_modalIncidentPk <= 0) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete incident '{ModalIncidentNumber}' ({ModalTitleField})?\nThis action cannot be undone.",
                "Confirm Delete Incident",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var inc = await _incidentService.GetIncidentByIdAsync(_modalIncidentPk);
                    if (inc != null)
                    {
                        await _incidentService.DeleteIncidentAsync(inc);
                    }
                    IsIncidentModalOpen = false;
                    SelectedIncident = null;
                    await LoadIncidentsAsync();
                }
                catch (Exception ex)
                {
                    ModalErrorMessage = $"Error deleting incident: {ex.Message}";
                }
            }
        }

        private async Task InvestigateCurrentModalIncidentAsync()
        {
            if (_modalIncidentPk <= 0) return;
            try
            {
                var target = await _incidentService.GetIncidentByIdAsync(_modalIncidentPk);
                if (target != null)
                {
                    IsIncidentModalOpen = false;
                    _openInvestigationAction?.Invoke(target);
                }
            }
            catch (Exception ex)
            {
                ModalErrorMessage = $"Error opening investigation: {ex.Message}";
            }
        }
    }
}

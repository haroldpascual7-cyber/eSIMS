using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class AnecdotalService : IAnecdotalService
    {
        private readonly List<AnecdotalRecordItem> _records = new()
        {
            new AnecdotalRecordItem
            {
                Id = 1,
                StudentIdNumber = "STD-1042",
                StudentName = "Juan Dela Cruz",
                GradeSection = "Grade 10 - St. Thomas",
                TeacherName = "Maria Santos (Homeroom Adviser)",
                ObservationDate = DateTime.Now.AddDays(-2),
                SettingOrSubject = "Homeroom & Values Education",
                ObservationType = "Positive Conduct",
                NarrativeObservation = "Voluntarily assisted classmates during group project research and led the morning flag ceremony lineup with discipline.",
                ActionTakenByTeacher = "Commended student in front of class and awarded +5 Good Conduct merit points.",
                FollowUpPlan = "Monitor in class",
                Status = "Resolved in Class"
            },
            new AnecdotalRecordItem
            {
                Id = 2,
                StudentIdNumber = "STD-1005",
                StudentName = "Michael Brown",
                GradeSection = "Grade 9 - St. Jude",
                TeacherName = "Carlos Reyes (Science)",
                ObservationDate = DateTime.Now.AddDays(-4),
                SettingOrSubject = "Science Laboratory",
                ObservationType = "Minor Infraction",
                NarrativeObservation = "Repeatedly distracted seatmates during chemistry lab experiment by throwing paper wads.",
                ActionTakenByTeacher = "Given a verbal reminder and reassigned to front lab table. Stayed 10 minutes after class for cleanup.",
                FollowUpPlan = "Monitor in class",
                Status = "Monitoring"
            },
            new AnecdotalRecordItem
            {
                Id = 3,
                StudentIdNumber = "STD-1012",
                StudentName = "Robert Chen",
                GradeSection = "Grade 11 - STEM A",
                TeacherName = "Elena Gomez (Mathematics)",
                ObservationDate = DateTime.Now.AddDays(-6),
                SettingOrSubject = "General Mathematics",
                ObservationType = "Academic Focus",
                NarrativeObservation = "Demonstrated exemplary problem-solving skills on polynomial calculus and tutored struggling peers during study hall.",
                ActionTakenByTeacher = "Recorded academic excellence citation in subject logbook.",
                FollowUpPlan = "Nominate for Math Olympiad",
                Status = "Resolved in Class"
            },
            new AnecdotalRecordItem
            {
                Id = 4,
                StudentIdNumber = "STD-1028",
                StudentName = "Angela Santos",
                GradeSection = "Grade 10 - St. Thomas",
                TeacherName = "Maria Santos (Homeroom Adviser)",
                ObservationDate = DateTime.Now.AddDays(-8),
                SettingOrSubject = "English Literature",
                ObservationType = "Attendance",
                NarrativeObservation = "Arrived 15 minutes late for 3 consecutive morning periods citing transportation delays.",
                ActionTakenByTeacher = "Held private 1-on-1 dialogue with student and scheduled parent check-in conference.",
                FollowUpPlan = "Parent Check-in",
                Status = "Monitoring"
            }
        };

        private int _nextId = 5;

        public Task<List<AnecdotalRecordItem>> GetAllAsync(string? searchTerm = null, string? typeFilter = null)
        {
            var q = _records.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLowerInvariant();
                q = q.Where(r => (r.StudentName?.ToLowerInvariant().Contains(term) ?? false) ||
                                 (r.StudentIdNumber?.ToLowerInvariant().Contains(term) ?? false) ||
                                 (r.GradeSection?.ToLowerInvariant().Contains(term) ?? false) ||
                                 (r.SettingOrSubject?.ToLowerInvariant().Contains(term) ?? false) ||
                                 (r.NarrativeObservation?.ToLowerInvariant().Contains(term) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter) && typeFilter != "All Types")
            {
                q = q.Where(r => r.ObservationType.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(q.OrderByDescending(r => r.ObservationDate).ToList());
        }

        public Task<AnecdotalRecordItem> AddAsync(AnecdotalRecordItem item)
        {
            item.Id = _nextId++;
            _records.Insert(0, item);
            return Task.FromResult(item);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var existing = _records.FirstOrDefault(r => r.Id == id);
            if (existing != null)
            {
                _records.Remove(existing);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}

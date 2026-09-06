using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext _context;

        public StudentService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<Student>> GetAllStudentsAsync()
        {
            return await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<List<Student>> SearchStudentsAsync(string? searchTerm = null, string? gradeLevel = null, string? section = null)
        {
            var query = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    s.StudentNumber.ToLower().Contains(term) ||
                    s.FirstName.ToLower().Contains(term) ||
                    s.LastName.ToLower().Contains(term) ||
                    (s.MiddleName != null && s.MiddleName.ToLower().Contains(term)) ||
                    (s.GuardianName != null && s.GuardianName.ToLower().Contains(term)) ||
                    (s.GuardianEmail != null && s.GuardianEmail.ToLower().Contains(term)) ||
                    (s.Email != null && s.Email.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(gradeLevel) && !gradeLevel.Equals("All Grades", StringComparison.OrdinalIgnoreCase) && !gradeLevel.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.GradeLevel == gradeLevel);
            }

            if (!string.IsNullOrWhiteSpace(section) && !section.Equals("All Sections", StringComparison.OrdinalIgnoreCase) && !section.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(s => s.Section == section);
            }

            return await query
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student?> GetStudentByNumberAsync(string studentNumber)
        {
            if (string.IsNullOrWhiteSpace(studentNumber)) return null;
            string num = studentNumber.Trim().ToLowerInvariant();
            return await _context.Students.FirstOrDefaultAsync(s => s.StudentNumber.ToLower() == num);
        }

        public async Task<bool> AddStudentAsync(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            bool exists = await _context.Students.AnyAsync(s => s.StudentNumber.ToLower() == student.StudentNumber.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException($"A student with ID '{student.StudentNumber}' already exists.");
            }

            student.StudentNumber = student.StudentNumber.Trim();
            student.FirstName = student.FirstName.Trim();
            student.LastName = student.LastName.Trim();
            student.CreatedAt = DateTime.UtcNow;
            student.UpdatedAt = DateTime.UtcNow;
            student.IsSynced = false;

            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStudentAsync(Student student)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));

            var existing = await _context.Students.FirstOrDefaultAsync(s => s.Id == student.Id);
            if (existing == null) return false;

            // Check if student number changed and conflicts
            if (!existing.StudentNumber.Equals(student.StudentNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                bool duplicate = await _context.Students.AnyAsync(s => s.Id != student.Id && s.StudentNumber.ToLower() == student.StudentNumber.Trim().ToLower());
                if (duplicate)
                {
                    throw new InvalidOperationException($"Another student already has ID '{student.StudentNumber}'.");
                }
            }

            existing.StudentNumber = student.StudentNumber.Trim();
            existing.FirstName = student.FirstName.Trim();
            existing.MiddleName = student.MiddleName?.Trim() ?? string.Empty;
            existing.LastName = student.LastName.Trim();
            existing.Gender = student.Gender;
            existing.DateOfBirth = student.DateOfBirth;
            existing.Email = student.Email?.Trim() ?? string.Empty;
            existing.Phone = student.Phone?.Trim() ?? string.Empty;
            existing.Address = student.Address?.Trim() ?? string.Empty;
            existing.GradeLevel = student.GradeLevel;
            existing.Section = student.Section?.Trim() ?? string.Empty;
            existing.TrackStrand = student.TrackStrand;
            existing.Adviser = student.Adviser?.Trim() ?? string.Empty;
            existing.GuardianName = student.GuardianName?.Trim() ?? string.Empty;
            existing.GuardianRelation = student.GuardianRelation;
            existing.GuardianContact = student.GuardianContact?.Trim() ?? string.Empty;
            existing.GuardianEmail = student.GuardianEmail?.Trim() ?? string.Empty;
            existing.MedicalNotes = student.MedicalNotes?.Trim() ?? string.Empty;
            existing.ProfilePicturePath = student.ProfilePicturePath ?? string.Empty;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.IsSynced = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(int successCount, int failureCount, List<string> errors)> ImportStudentsBatchAsync(List<Student> students)
        {
            int success = 0;
            int failed = 0;
            var errors = new List<string>();

            if (students == null || students.Count == 0)
                return (0, 0, errors);

            var existingIds = (await _context.Students.Select(s => s.StudentNumber.ToLower()).ToListAsync()).ToHashSet();

            foreach (var student in students)
            {
                try
                {
                    string idKey = student.StudentNumber.Trim().ToLower();
                    if (existingIds.Contains(idKey))
                    {
                        failed++;
                        errors.Add($"Duplicate ID '{student.StudentNumber}': already registered in the system.");
                        continue;
                    }

                    student.CreatedAt = DateTime.UtcNow;
                    student.UpdatedAt = DateTime.UtcNow;
                    student.IsSynced = false;

                    await _context.Students.AddAsync(student);
                    existingIds.Add(idKey);
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Failed to import student '{student.StudentNumber}': {ex.Message}");
                }
            }

            if (success > 0)
            {
                await _context.SaveChangesAsync();
            }

            return (success, failed, errors);
        }
    }
}

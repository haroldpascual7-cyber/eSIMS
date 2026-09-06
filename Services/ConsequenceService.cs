using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class ConsequenceService : IConsequenceService
    {
        private readonly AppDbContext _context;

        public ConsequenceService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<ConsequenceLog>> GetAllConsequenceLogsAsync()
        {
            return await _context.ConsequenceLogs
                .OrderByDescending(c => c.DateIssued)
                .ToListAsync();
        }

        public async Task<IEnumerable<ConsequenceLog>> SearchAndFilterLogsAsync(string? query, string? categoryFilter, string? statusFilter)
        {
            var q = _context.ConsequenceLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(c => c.StudentName.ToLower().Contains(term) ||
                                 c.StudentIdNumber.ToLower().Contains(term) ||
                                 c.IncidentNumber.ToLower().Contains(term) ||
                                 c.LogId.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "All")
            {
                q = q.Where(c => c.ConsequenceType.ToLower() == categoryFilter.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                string st = statusFilter.Trim().ToLower();
                if (st == "overdue/no-show" || st == "overdue")
                {
                    q = q.Where(c => c.Status.ToLower() == "overdue" || c.Status.ToLower() == "overdue/no-show");
                }
                else
                {
                    q = q.Where(c => c.Status.ToLower() == st);
                }
            }

            return await q.OrderByDescending(c => c.DateIssued).ToListAsync();
        }

        public async Task AddConsequenceLogAsync(ConsequenceLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (string.IsNullOrWhiteSpace(log.LogId))
            {
                log.LogId = await GenerateLogIdAsync();
            }

            log.CreatedAt = DateTime.UtcNow;
            log.UpdatedAt = DateTime.UtcNow;
            log.IsSynced = false;

            await _context.ConsequenceLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateConsequenceStatusAsync(int id, string newStatus)
        {
            var log = await _context.ConsequenceLogs.FindAsync(id);
            if (log != null)
            {
                log.Status = newStatus;
                log.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateLogIdAsync()
        {
            int count = await _context.ConsequenceLogs.CountAsync() + 1;
            return $"CSQ-{DateTime.Now.Year}-{count:D3}";
        }
    }
}
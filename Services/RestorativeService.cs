using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class RestorativeService : IRestorativeService
    {
        private readonly AppDbContext _context;

        public RestorativeService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<RestorativePlan>> GetAllInterventionPlansAsync()
        {
            return await _context.RestorativePlans
                .Include(rp => rp.ProgressLogs)
                .OrderByDescending(rp => rp.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RestorativePlan>> SearchAndFilterPlansAsync(string? query, string? planType, string? statusFilter)
        {
            var q = _context.RestorativePlans.Include(rp => rp.ProgressLogs).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(p => p.StudentName.ToLower().Contains(term) ||
                                 p.StudentIdNumber.ToLower().Contains(term) ||
                                 p.IncidentNumber.ToLower().Contains(term) ||
                                 p.PlanId.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(planType) && planType != "All" && planType != "All Types")
            {
                q = q.Where(p => p.PlanType.ToLower() == planType.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All" && statusFilter != "All Statuses")
            {
                string st = statusFilter.Trim().ToLower();
                if (st == "active / in-progress" || st == "in-progress")
                {
                    q = q.Where(p => p.Status.ToLower() == "active" || p.Status.ToLower() == "in-progress");
                }
                else
                {
                    q = q.Where(p => p.Status.ToLower() == st);
                }
            }

            return await q.OrderByDescending(p => p.StartDate).ToListAsync();
        }

        public async Task AddInterventionPlanAsync(RestorativePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (string.IsNullOrWhiteSpace(plan.PlanId))
            {
                plan.PlanId = await GeneratePlanIdAsync();
            }

            plan.CreatedAt = DateTime.UtcNow;
            plan.UpdatedAt = DateTime.UtcNow;
            plan.IsSynced = false;

            await _context.RestorativePlans.AddAsync(plan);
            await _context.SaveChangesAsync();
        }

        public async Task LogProgressAsync(int planId, int addedUnits, string notes, string staff, bool markCompleted)
        {
            var plan = await _context.RestorativePlans.FindAsync(planId);
            if (plan != null)
            {
                plan.CompletedMetric += addedUnits;
                if (plan.CompletedMetric >= plan.TargetMetric || markCompleted)
                {
                    plan.Status = "Completed";
                    plan.IsCleared = true;
                }
                plan.MarkUpdated();

                int logCount = await _context.InterventionProgressLogs.CountAsync() + 1;
                var progressLog = new InterventionProgressLog
                {
                    LogId = $"LOG-{DateTime.Now.Year}-{logCount:D3}",
                    RestorativePlanId = plan.Id,
                    LoggedUnits = addedUnits,
                    LogDate = DateTime.Now,
                    StaffName = staff,
                    Notes = notes
                };

                await _context.InterventionProgressLogs.AddAsync(progressLog);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePlanStatusAsync(int planId, string status, bool isCleared)
        {
            var plan = await _context.RestorativePlans.FindAsync(planId);
            if (plan != null)
            {
                plan.Status = status;
                plan.IsCleared = isCleared;
                plan.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GeneratePlanIdAsync()
        {
            int count = await _context.RestorativePlans.CountAsync() + 1;
            return $"INT-{DateTime.Now.Year}-{count:D3}";
        }
    }
}
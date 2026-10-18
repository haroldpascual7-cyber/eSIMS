using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class SuspensionService : ISuspensionService
    {
        private readonly AppDbContext _context;

        public SuspensionService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<SuspensionOrder>> GetAllSuspensionOrdersAsync()
        {
            return await _context.SuspensionOrders
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SuspensionOrder>> SearchAndFilterOrdersAsync(string? query, string? typeFilter, string? statusFilter)
        {
            var q = _context.SuspensionOrders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                string term = query.Trim().ToLower();
                q = q.Where(s => s.StudentName.ToLower().Contains(term) ||
                                 s.StudentIdNumber.ToLower().Contains(term) ||
                                 s.IncidentNumber.ToLower().Contains(term) ||
                                 s.OrderId.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter) && typeFilter != "All" && typeFilter != "All Types")
            {
                string tf = typeFilter.Trim().ToLower();
                if (tf.Contains("iss")) q = q.Where(s => s.SuspensionType.ToLower().Contains("iss"));
                else if (tf.Contains("oss")) q = q.Where(s => s.SuspensionType.ToLower().Contains("oss"));
                else q = q.Where(s => s.SuspensionType.ToLower() == tf);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All" && statusFilter != "All Statuses")
            {
                string st = statusFilter.Trim().ToLower();
                q = q.Where(s => s.Status.ToLower() == st);
            }

            return await q.OrderByDescending(s => s.StartDate).ToListAsync();
        }

        public async Task AddSuspensionOrderAsync(SuspensionOrder order)
        {
            DisciplinaryAuthorization.DemandManagePermission();
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.IncidentId == null || order.StudentId == null)
                throw new InvalidOperationException("A verified incident and student are required.");
            if (string.IsNullOrWhiteSpace(order.OrderId))
            {
                order.OrderId = await GenerateOrderIdAsync();
            }

            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.IsSynced = false;
            order.IssuedBy = SessionManager.CurrentUsername;

            await _context.SuspensionOrders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClearanceStatusAsync(int orderId, bool parentCheck, bool packetCheck, bool counselorCheck, string notes, bool isCleared)
        {
            DisciplinaryAuthorization.DemandManagePermission();
            var order = await _context.SuspensionOrders.FindAsync(orderId);
            if (order != null)
            {
                order.ParentConferencePassed = parentCheck;
                order.AcademicPacketPassed = packetCheck;
                order.CounselorIntakePassed = counselorCheck;
                order.ClearanceNotes = notes;

                if (isCleared || (order.ParentConferencePassed && order.AcademicPacketPassed && order.CounselorIntakePassed))
                {
                    order.IsCleared = true;
                    order.Status = "Readmitted";
                }
                else
                {
                    order.Status = "Pending Review";
                }

                order.MarkUpdated();
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateOrderIdAsync()
        {
            int count = await _context.SuspensionOrders.CountAsync() + 1;
            return $"SUS-{DateTime.Now.Year}-{count:D3}";
        }
    }
}

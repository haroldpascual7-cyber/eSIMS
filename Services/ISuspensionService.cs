using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface ISuspensionService
    {
        Task<IEnumerable<SuspensionOrder>> GetAllSuspensionOrdersAsync();
        Task<IEnumerable<SuspensionOrder>> SearchAndFilterOrdersAsync(string? query, string? typeFilter, string? statusFilter);
        Task AddSuspensionOrderAsync(SuspensionOrder order);
        Task UpdateClearanceStatusAsync(int orderId, bool parentCheck, bool packetCheck, bool counselorCheck, string notes, bool isCleared);
        Task<string> GenerateOrderIdAsync();
    }
}
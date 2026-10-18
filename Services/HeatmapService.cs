using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public class HeatmapFilterParams
    {
        public string TimeframeMode { get; set; } = "This Month";
        public DateTime? SpecificDate { get; set; }
        public int? SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
        public string CategoryFilter { get; set; } = "All Categories";
    }

    public interface IHeatmapService
    {
        Task<List<HeatmapNodeRaw>> GetCampusLocationDataAsync(string dateRange, string categoryFilter);
        Task<List<HeatmapNodeRaw>> GetCampusLocationDataAsync(HeatmapFilterParams filters);
    }

    public class HeatmapNodeRaw
    {
        public string Location { get; set; } = string.Empty;
        public int TotalIncidents { get; set; }
        public string TopCategory { get; set; } = "Behavioral Misconduct";
        public List<Incident> Incidents { get; set; } = new();
    }

    public class HeatmapService : IHeatmapService
    {
        private readonly AppDbContext _context;

        public HeatmapService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<List<HeatmapNodeRaw>> GetCampusLocationDataAsync(string dateRange, string categoryFilter)
        {
            return GetCampusLocationDataAsync(new HeatmapFilterParams
            {
                TimeframeMode = dateRange,
                CategoryFilter = categoryFilter
            });
        }

        public async Task<List<HeatmapNodeRaw>> GetCampusLocationDataAsync(HeatmapFilterParams filters)
        {
            using var db = new AppDbContext();
            var query = db.Incidents.AsNoTracking().Include(i => i.Student).Include(i => i.Reporter).AsQueryable();

            if (filters != null)
            {
                var mode = filters.TimeframeMode ?? "This Month";
                if (mode.Equals("Specific Date (Day)", StringComparison.OrdinalIgnoreCase) ||
                    mode.Equals("Specific Date", StringComparison.OrdinalIgnoreCase) ||
                    mode.Equals("Day", StringComparison.OrdinalIgnoreCase))
                {
                    if (filters.SpecificDate.HasValue)
                    {
                        var start = filters.SpecificDate.Value.Date;
                        var end = start.AddDays(1);
                        query = query.Where(i => i.IncidentDate >= start && i.IncidentDate < end);
                    }
                }
                else if (mode.Equals("This Week", StringComparison.OrdinalIgnoreCase))
                {
                    var today = DateTime.Today;
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    var startOfWeek = today.AddDays(-diff);
                    var endOfWeek = startOfWeek.AddDays(7);
                    query = query.Where(i => i.IncidentDate >= startOfWeek && i.IncidentDate < endOfWeek);
                }
                else if (mode.Equals("Specific Month & Year", StringComparison.OrdinalIgnoreCase) ||
                         mode.Equals("Month & Year", StringComparison.OrdinalIgnoreCase))
                {
                    if (filters.SelectedYear.HasValue && filters.SelectedMonth.HasValue)
                    {
                        var start = new DateTime(filters.SelectedYear.Value, filters.SelectedMonth.Value, 1);
                        var end = start.AddMonths(1);
                        query = query.Where(i => i.IncidentDate >= start && i.IncidentDate < end);
                    }
                }
                else if (mode.Equals("Specific Year", StringComparison.OrdinalIgnoreCase) ||
                         mode.Equals("Year", StringComparison.OrdinalIgnoreCase))
                {
                    if (filters.SelectedYear.HasValue)
                    {
                        var start = new DateTime(filters.SelectedYear.Value, 1, 1);
                        var end = start.AddYears(1);
                        query = query.Where(i => i.IncidentDate >= start && i.IncidentDate < end);
                    }
                }
                else if (mode.Equals("This Month", StringComparison.OrdinalIgnoreCase))
                {
                    var start = DateTime.Now.AddDays(-30);
                    query = query.Where(i => i.IncidentDate >= start);
                }
                else if (mode.Equals("This Semester", StringComparison.OrdinalIgnoreCase))
                {
                    var start = DateTime.Now.AddMonths(-6);
                    query = query.Where(i => i.IncidentDate >= start);
                }

                if (!string.IsNullOrWhiteSpace(filters.CategoryFilter) && !filters.CategoryFilter.Equals("All Categories", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(i => i.Category.Equals(filters.CategoryFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            var incidents = await query.ToListAsync();

            var canonicalZones = new List<string>
            {
                "Gymnasium",
                "Classrooms (Bldg A)",
                "Science Labs",
                "Cafeteria",
                "Covered Court",
                "Main Hallway",
                "Library & Study",
                "East Parking Lot"
            };

            var zoneGroups = canonicalZones.ToDictionary(z => z, _ => new List<Incident>());

            foreach (var inc in incidents)
            {
                string zone = MapToCanonicalZone(inc.Location);
                if (zoneGroups.TryGetValue(zone, out var list))
                {
                    list.Add(inc);
                }
                else
                {
                    zoneGroups["Classrooms (Bldg A)"].Add(inc);
                }
            }

            var result = new List<HeatmapNodeRaw>();
            foreach (var zone in canonicalZones)
            {
                var list = zoneGroups[zone];
                string topCategory = list.Any()
                    ? list.GroupBy(i => i.Category).OrderByDescending(g => g.Count()).First().Key
                    : "Stable Zone";

                result.Add(new HeatmapNodeRaw
                {
                    Location = zone,
                    TotalIncidents = list.Count,
                    TopCategory = topCategory,
                    Incidents = list.OrderByDescending(i => i.IncidentDate).ToList()
                });
            }

            return result.OrderByDescending(x => x.TotalIncidents).ToList();
        }

        public static string MapToCanonicalZone(string? rawLocation)
        {
            if (string.IsNullOrWhiteSpace(rawLocation)) return "Classrooms (Bldg A)";
            string loc = rawLocation.Trim().ToLowerInvariant();

            if (loc.Contains("gym") || loc.Contains("sports") || loc.Contains("arena"))
                return "Gymnasium";
            if (loc.Contains("science") || loc.Contains("lab") || loc.Contains("chem") || loc.Contains("bio") || loc.Contains("physics") || loc.Contains("auditorium"))
                return "Science Labs";
            if (loc.Contains("cafeteria") || loc.Contains("canteen") || loc.Contains("dining") || loc.Contains("commons") || loc.Contains("food"))
                return "Cafeteria";
            if (loc.Contains("court") || loc.Contains("field") || loc.Contains("playground") || loc.Contains("outdoor") || loc.Contains("play"))
                return "Covered Court";
            if (loc.Contains("hallway") || loc.Contains("hall") || loc.Contains("corridor") || loc.Contains("admin") || loc.Contains("office") || loc.Contains("detention") || loc.Contains("faculty"))
                return "Main Hallway";
            if (loc.Contains("library") || loc.Contains("learning") || loc.Contains("resource") || loc.Contains("book"))
                return "Library & Study";
            if (loc.Contains("parking") || loc.Contains("gate") || loc.Contains("exit") || loc.Contains("perimeter") || loc.Contains("vehicle"))
                return "East Parking Lot";

            return "Classrooms (Bldg A)";
        }
    }
}
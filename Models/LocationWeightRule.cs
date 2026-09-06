using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class LocationWeightRule : EntityBase
    {
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string Category { get; set; } = "Classroom"; // Classroom, Hallway/Stairwell, Restroom/Locker, Gym/Field, Secluded/Perimeter
        public string SupervisionLevel { get; set; } = "High Constant Supervision"; // High Constant Supervision, Periodic Patrol, Unmonitored Blind Spot
        public bool HasCCTV { get; set; } = true;
        public double RiskMultiplier { get; set; } = 1.0; // 1.0 to 2.5
        public bool IsRestricted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Display Helpers
        public string LocationNameAndBuilding => string.IsNullOrWhiteSpace(Building) ? LocationName : $"{LocationName} ({Building})";
        public string MultiplierText => $"{RiskMultiplier:F1}x Multiplier";
    }
}
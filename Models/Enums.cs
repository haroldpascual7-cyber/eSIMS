namespace SmartSchoolIncidentManagementSystem.Models
{
    public enum IncidentStatus
    {
        Reported,
        UnderInvestigation,
        ActionTaken,
        Resolved,
        Closed
    }

    public enum IncidentSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}
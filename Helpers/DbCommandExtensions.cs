using System.Data.Common;

namespace SmartSchoolIncidentManagementSystem.Helpers
{
    public static class DbCommandExtensions
    {
        public static DbParameter AddParam(this DbCommand command, string name, object? value)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? System.DBNull.Value;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}

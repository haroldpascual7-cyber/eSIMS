using System;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public static class DisciplinaryAuthorization
    {
        private const string AuthorizedRoles = "System Administrator,Discipline Officer,Guidance Counselor";

        public static bool CanManage =>
            SessionManager.HasPermission(AuthorizedRoles) &&
            SessionManager.HasModuleAccess("Disciplinary Actions & Sanctions", AuthorizedRoles);

        public static void DemandManagePermission()
        {
            if (!CanManage)
            {
                throw new UnauthorizedAccessException(
                    "Only authorized school disciplinary personnel can modify suspension or probation records.");
            }
        }
    }
}

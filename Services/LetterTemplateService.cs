using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Data;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSchoolIncidentManagementSystem.Services
{
    public interface ILetterTemplateService
    {
        Task EnsureDatabaseCreatedAsync();
        Task<List<LetterTemplate>> GetAllTemplatesAsync();
        Task<LetterTemplate?> GetTemplateByIdAsync(string id);
        Task SaveTemplateAsync(LetterTemplate template);
        Task DeleteTemplateAsync(string id);
    }

    public class LetterTemplateService : ILetterTemplateService
    {
        private readonly AppDbContext _context;

        public LetterTemplateService(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            EnsureTables();
        }

        private void EnsureTables()
        {
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS LetterTemplates (
                        Id TEXT PRIMARY KEY,
                        TemplateName TEXT NOT NULL,
                        TemplateBody TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ");

                int count = _context.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM LetterTemplates;").AsEnumerable().FirstOrDefault();
                if (count == 0)
                {
                    _context.Database.ExecuteSqlRaw(@"
                        INSERT INTO LetterTemplates (Id, TemplateName, TemplateBody) VALUES
                        (
                            'tpl-001',
                            'Parent Conference Summons',
                            'OFFICIAL NOTICE OF PARENT / GUARDIAN CONFERENCE

Date: {{ActionDate}}
To the Parent/Guardian of {{StudentName}} (ID: {{StudentId}}):

This is an official communication from Smart Academy regarding an incident involving your child, {{StudentName}}, currently enrolled in {{GradeSection}}.

INCIDENT DETAILS:
• Date of Incident: {{IncidentDate}}
• Location: {{Location}}
• Infraction / Category: {{Infraction}}

Due to the nature of this matter, you are required to attend a mandatory Parent-Counselor Conference at the Smart Academy Guidance Office on {{ActionDate}}.

Please present this official summons upon arrival. Your presence and cooperation are essential as we work together to support {{StudentName}}''s educational and behavioral growth.

Issued by:
{{OfficerName}}
Office of the Guidance Counselor & Student Affairs
Smart Academy'
                        ),
                        (
                            'tpl-002',
                            'Notice of Suspension Order',
                            'OFFICIAL DISCIPLINARY NOTICE: SUSPENSION ORDER

Date: {{ActionDate}}
To the Parent/Guardian of {{StudentName}} (ID: {{StudentId}}):

Please be advised that following a formal disciplinary review regarding the incident on {{IncidentDate}} (Category: {{Infraction}}), Smart Academy has issued a formal Suspension Order for {{StudentName}}.

SUMMARY OF INFRACTION:
{{StudentName}} was involved in a {{Infraction}} incident at {{Location}} on {{IncidentDate}}.

REQUIRED ACTION & SCHEDULE:
The student is suspended from attending regular classes starting on {{ActionDate}}. Re-entry counseling must be scheduled prior to class reinstatement.

Issued by:
{{OfficerName}}
Disciplinary Board & School Principal
Smart Academy'
                        ),
                        (
                            'tpl-003',
                            'Warning & Behavioral Conduct Notice',
                            'FORMAL ADVISORY: BEHAVIORAL WARNING NOTICE

Date: {{ActionDate}}
To {{StudentName}} (ID: {{StudentId}}) and Parent/Guardian:

This letter serves as a formal behavioral warning regarding the infraction reported on {{IncidentDate}} involving {{Infraction}} at {{Location}}.

Smart Academy emphasizes character development, safety, and mutual respect. Further violations of school regulations may result in formal disciplinary action or suspension.

Please review this notice with {{StudentName}}. We appreciate your support in fostering a positive learning environment.

Issued by:
{{OfficerName}}
Class Adviser / Guidance Counselor
Smart Academy'
                        );
                    ");
                }
            }
            catch { }
        }

        public Task EnsureDatabaseCreatedAsync()
        {
            EnsureTables();
            return Task.CompletedTask;
        }

        public async Task<List<LetterTemplate>> GetAllTemplatesAsync()
        {
            EnsureTables();
            var list = new List<LetterTemplate>();
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, TemplateName, TemplateBody, CreatedAt FROM LetterTemplates ORDER BY TemplateName ASC;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new LetterTemplate
                {
                    Id = reader.GetString(0),
                    TemplateName = reader.GetString(1),
                    TemplateBody = reader.GetString(2),
                    CreatedAt = reader.IsDBNull(3) ? DateTime.Now : reader.GetDateTime(3)
                });
            }

            return list;
        }

        public async Task<LetterTemplate?> GetTemplateByIdAsync(string id)
        {
            var all = await GetAllTemplatesAsync();
            return all.FirstOrDefault(t => t.Id == id);
        }

        public async Task SaveTemplateAsync(LetterTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            EnsureTables();

            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO LetterTemplates (Id, TemplateName, TemplateBody, CreatedAt)
                VALUES (@Id, @TemplateName, @TemplateBody, @CreatedAt);";

            AddParam(cmd, "@Id", template.Id);
            AddParam(cmd, "@TemplateName", template.TemplateName);
            AddParam(cmd, "@TemplateBody", template.TemplateBody);
            AddParam(cmd, "@CreatedAt", template.CreatedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteTemplateAsync(string id)
        {
            using var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM LetterTemplates WHERE Id = @Id;";
            AddParam(cmd, "@Id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        private static void AddParam(System.Data.Common.DbCommand cmd, string name, object val)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = val ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
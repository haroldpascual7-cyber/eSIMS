using Microsoft.EntityFrameworkCore;
using SmartSchoolIncidentManagementSystem.Models;
using System;
using System.IO;

namespace SmartSchoolIncidentManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Staff> StaffMembers { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Incident> Incidents { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
        public DbSet<Investigation> Investigations { get; set; } = null!;
        public DbSet<DisciplinaryAction> DisciplinaryActions { get; set; } = null!;
        public DbSet<ConsequenceLog> ConsequenceLogs { get; set; } = null!;
        public DbSet<RestorativePlan> RestorativePlans { get; set; } = null!;
        public DbSet<InterventionProgressLog> InterventionProgressLogs { get; set; } = null!;
        public DbSet<SuspensionOrder> SuspensionOrders { get; set; } = null!;
        public DbSet<ProbationRecord> ProbationRecords { get; set; } = null!;
        public DbSet<ProbationProgressNote> ProbationProgressNotes { get; set; } = null!;
        public DbSet<KeywordSensitivityRule> KeywordSensitivityRules { get; set; } = null!;
        public DbSet<LocationWeightRule> LocationWeightRules { get; set; } = null!;
        public DbSet<RepeatOffenderRule> RepeatOffenderRules { get; set; } = null!;
        public DbSet<StudentMeritLog> StudentMeritLogs { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSchool");
                Directory.CreateDirectory(dbFolder);
                string dbPath = Path.Combine(dbFolder, "incidents.db");
                
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Student>().HasIndex(s => s.SyncId).IsUnique();
            modelBuilder.Entity<Staff>().HasIndex(s => s.SyncId).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.SyncId).IsUnique();
            modelBuilder.Entity<Incident>().HasIndex(i => i.SyncId).IsUnique();
            modelBuilder.Entity<SystemSetting>().HasIndex(s => s.Key).IsUnique();
            modelBuilder.Entity<Investigation>().HasIndex(inv => inv.SyncId).IsUnique();
            modelBuilder.Entity<DisciplinaryAction>().HasIndex(d => d.SyncId).IsUnique();
            modelBuilder.Entity<ConsequenceLog>().HasIndex(c => c.SyncId).IsUnique();
            modelBuilder.Entity<RestorativePlan>().HasIndex(rp => rp.SyncId).IsUnique();
            modelBuilder.Entity<InterventionProgressLog>().HasIndex(ipl => ipl.SyncId).IsUnique();
            modelBuilder.Entity<SuspensionOrder>().HasIndex(so => so.SyncId).IsUnique();
            modelBuilder.Entity<ProbationRecord>().HasIndex(p => p.SyncId).IsUnique();
            modelBuilder.Entity<ProbationProgressNote>().HasIndex(p => p.SyncId).IsUnique();
            modelBuilder.Entity<ProbationRecord>()
                .HasMany(p => p.ProgressNotes)
                .WithOne(n => n.ProbationRecord)
                .HasForeignKey(n => n.ProbationRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KeywordSensitivityRule>().HasIndex(ksr => ksr.SyncId).IsUnique();
            modelBuilder.Entity<LocationWeightRule>().HasIndex(lwr => lwr.SyncId).IsUnique();
            modelBuilder.Entity<RepeatOffenderRule>().HasIndex(ror => ror.SyncId).IsUnique();
            modelBuilder.Entity<StudentMeritLog>().HasIndex(sml => sml.SyncId).IsUnique();
        }
    }
}

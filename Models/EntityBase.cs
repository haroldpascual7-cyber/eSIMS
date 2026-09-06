using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public abstract class EntityBase
    {
        [Key]
        public int Id { get; set; }

        public Guid SyncId { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSynced { get; set; } = false;

        public void MarkUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
            IsSynced = false;
        }
    }
}

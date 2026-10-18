using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public abstract class EntityBase : INotifyPropertyChanged
    {
        [Key]
        public int Id { get; set; }

        public Guid SyncId { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSynced { get; set; } = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void MarkUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
            IsSynced = false;
            OnPropertyChanged(nameof(UpdatedAt));
            OnPropertyChanged(nameof(IsSynced));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}


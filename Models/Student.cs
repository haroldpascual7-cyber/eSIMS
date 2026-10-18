using System;

namespace SmartSchoolIncidentManagementSystem.Models
{
    public class Student : EntityBase
    {
        private string _studentNumber = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private string _lastName = string.Empty;
        private string _gender = "Male";
        private DateTime? _dateOfBirth;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;
        private string _gradeLevel = "Grade 10";
        private string _section = string.Empty;
        private string _trackStrand = "Academic";
        private string _adviser = string.Empty;
        private string _guardianName = string.Empty;
        private string _guardianRelation = "Parent";
        private string _guardianContact = string.Empty;
        private string _guardianEmail = string.Empty;
        private string _medicalNotes = string.Empty;
        private string _profilePicturePath = string.Empty;

        public string StudentNumber { get => _studentNumber; set => SetProperty(ref _studentNumber, value); }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                {
                    OnPropertyChanged(nameof(FullName));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string MiddleName
        {
            get => _middleName;
            set
            {
                if (SetProperty(ref _middleName, value))
                {
                    OnPropertyChanged(nameof(FullName));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                {
                    OnPropertyChanged(nameof(FullName));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string Gender { get => _gender; set => SetProperty(ref _gender, value); }
        public DateTime? DateOfBirth { get => _dateOfBirth; set => SetProperty(ref _dateOfBirth, value); }
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
        public string Address { get => _address; set => SetProperty(ref _address, value); }

        public string GradeLevel { get => _gradeLevel; set => SetProperty(ref _gradeLevel, value); }
        public string Section { get => _section; set => SetProperty(ref _section, value); }
        public string TrackStrand { get => _trackStrand; set => SetProperty(ref _trackStrand, value); }
        public string Adviser { get => _adviser; set => SetProperty(ref _adviser, value); }

        public string GuardianName { get => _guardianName; set => SetProperty(ref _guardianName, value); }
        public string GuardianRelation { get => _guardianRelation; set => SetProperty(ref _guardianRelation, value); }
        public string GuardianContact { get => _guardianContact; set => SetProperty(ref _guardianContact, value); }
        public string GuardianEmail { get => _guardianEmail; set => SetProperty(ref _guardianEmail, value); }

        public string MedicalNotes { get => _medicalNotes; set => SetProperty(ref _medicalNotes, value); }
        public string ProfilePicturePath { get => _profilePicturePath; set => SetProperty(ref _profilePicturePath, value); }

        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}".Trim()
            : $"{FirstName} {MiddleName[0]}. {LastName}".Trim();

        public string DisplayName => $"{FullName} ({StudentNumber})";
    }
}
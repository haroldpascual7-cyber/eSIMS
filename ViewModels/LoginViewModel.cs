using SmartSchoolIncidentManagementSystem.Services;
using SmartSchoolIncidentManagementSystem.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IThemeService _themeService;
        private readonly ISettingsService _settingsService;
        private readonly Action _onLoginSuccess;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _isPasswordVisible;

        // Sign Up Properties
        private bool _isSignUpMode;
        private string _signUpFullName = string.Empty;
        private string _signUpEmail = string.Empty;
        private string _signUpUsername = string.Empty;
        private string _signUpPassword = string.Empty;
        private string _signUpConfirmPassword = string.Empty;
        private string _signUpRole = "Staff";
        private bool _signUpAgreeTerms = true;
        private bool _signUpIsPasswordVisible;
        private bool _signUpIsConfirmPasswordVisible;
        private string _signUpErrorMessage = string.Empty;
        private string _signUpSuccessMessage = string.Empty;

        public event Action<bool>? RequestCardFlip;

        public LoginViewModel(IAuthService authService, IThemeService themeService, ISettingsService settingsService, Action onLoginSuccess)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _onLoginSuccess = onLoginSuccess ?? throw new ArgumentNullException(nameof(onLoginSuccess));

            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), () => !IsLoading);
            SignUpCommand = new RelayCommand(async () => await ExecuteSignUpAsync(), () => !IsLoading);
            FlipToSignUpCommand = new RelayCommand(() => FlipCard(true));
            FlipToLoginCommand = new RelayCommand(() => FlipCard(false));
            ToggleThemeCommand = new RelayCommand(() => _themeService.ToggleTheme());
            ExitCommand = new RelayCommand(() => Application.Current?.Shutdown());
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);
            ToggleSignUpPasswordVisibilityCommand = new RelayCommand(() => SignUpIsPasswordVisible = !SignUpIsPasswordVisible);
            ToggleSignUpConfirmPasswordVisibilityCommand = new RelayCommand(() => SignUpIsConfirmPasswordVisible = !SignUpIsConfirmPasswordVisible);

            _settingsService.SettingsChanged += RefreshBranding;
            _ = InitializeSettingsAsync();
        }

        // Role Selection Dropdown Properties
        private string _selectedRole = "Select Role";
        private string _roleIndicatorMessage = string.Empty;

        public ObservableCollection<string> RoleOptions { get; } = new()
        {
            "Select Role",
            "Administrator",
            "Guidance Counselor",
            "Discipline Officer",
            "Staff"
        };

        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    OnRoleSelectionChanged(value);
                }
            }
        }

        public string RoleIndicatorMessage
        {
            get => _roleIndicatorMessage;
            set
            {
                if (SetProperty(ref _roleIndicatorMessage, value))
                {
                    OnPropertyChanged(nameof(HasRoleIndicator));
                }
            }
        }

        public bool HasRoleIndicator => !string.IsNullOrWhiteSpace(RoleIndicatorMessage);

        private void OnRoleSelectionChanged(string? role)
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(role) || role.Equals("Select Role", StringComparison.OrdinalIgnoreCase))
            {
                Username = string.Empty;
                Password = string.Empty;
                RoleIndicatorMessage = string.Empty;
                return;
            }

            switch (role.Trim())
            {
                case "Administrator":
                    Username = "admin";
                    Password = "admin123";
                    RoleIndicatorMessage = "Administrator account selected";
                    break;

                case "Guidance Counselor":
                    Username = "guidance";
                    Password = "guidance123";
                    RoleIndicatorMessage = "Guidance Counselor account selected";
                    break;

                case "Discipline Officer":
                    Username = "discipline";
                    Password = "discipline123";
                    RoleIndicatorMessage = "Discipline Officer account selected";
                    break;

                case "Staff":
                case "Teacher":
                    Username = "staff";
                    Password = "staff123";
                    RoleIndicatorMessage = "Staff account selected";
                    break;

                default:
                    Username = string.Empty;
                    Password = string.Empty;
                    RoleIndicatorMessage = string.Empty;
                    break;
            }
        }

        public ObservableCollection<string> AvailableRoles { get; } = new()
        {
            "Administrator",
            "Guidance Counselor",
            "Discipline Officer",
            "Staff"
        };

        public bool IsSignUpMode
        {
            get => _isSignUpMode;
            set => SetProperty(ref _isSignUpMode, value);
        }

        public string SignUpFullName
        {
            get => _signUpFullName;
            set { if (SetProperty(ref _signUpFullName, value)) SignUpErrorMessage = string.Empty; }
        }

        public string SignUpEmail
        {
            get => _signUpEmail;
            set { if (SetProperty(ref _signUpEmail, value)) SignUpErrorMessage = string.Empty; }
        }

        public string SignUpUsername
        {
            get => _signUpUsername;
            set { if (SetProperty(ref _signUpUsername, value)) SignUpErrorMessage = string.Empty; }
        }

        public string SignUpPassword
        {
            get => _signUpPassword;
            set { if (SetProperty(ref _signUpPassword, value)) SignUpErrorMessage = string.Empty; }
        }

        public string SignUpConfirmPassword
        {
            get => _signUpConfirmPassword;
            set { if (SetProperty(ref _signUpConfirmPassword, value)) SignUpErrorMessage = string.Empty; }
        }

        public string SignUpRole
        {
            get => _signUpRole;
            set => SetProperty(ref _signUpRole, value);
        }

        public bool SignUpAgreeTerms
        {
            get => _signUpAgreeTerms;
            set => SetProperty(ref _signUpAgreeTerms, value);
        }

        public bool SignUpIsPasswordVisible
        {
            get => _signUpIsPasswordVisible;
            set
            {
                if (SetProperty(ref _signUpIsPasswordVisible, value))
                    OnPropertyChanged(nameof(SignUpPasswordEyeIcon));
            }
        }

        public string SignUpPasswordEyeIcon => SignUpIsPasswordVisible ? "🙈" : "👁";

        public bool SignUpIsConfirmPasswordVisible
        {
            get => _signUpIsConfirmPasswordVisible;
            set
            {
                if (SetProperty(ref _signUpIsConfirmPasswordVisible, value))
                    OnPropertyChanged(nameof(SignUpConfirmPasswordEyeIcon));
            }
        }

        public string SignUpConfirmPasswordEyeIcon => SignUpIsConfirmPasswordVisible ? "🙈" : "👁";

        public string SignUpErrorMessage
        {
            get => _signUpErrorMessage;
            set => SetProperty(ref _signUpErrorMessage, value);
        }

        public string SignUpSuccessMessage
        {
            get => _signUpSuccessMessage;
            set => SetProperty(ref _signUpSuccessMessage, value);
        }

        public ICommand SignUpCommand { get; }
        public ICommand FlipToSignUpCommand { get; }
        public ICommand FlipToLoginCommand { get; }
        public ICommand ToggleSignUpPasswordVisibilityCommand { get; }
        public ICommand ToggleSignUpConfirmPasswordVisibilityCommand { get; }

        public void FlipCard(bool toSignUp)
        {
            ErrorMessage = string.Empty;
            SignUpErrorMessage = string.Empty;
            SignUpSuccessMessage = string.Empty;
            if (RequestCardFlip != null)
            {
                RequestCardFlip.Invoke(toSignUp);
            }
            else
            {
                IsSignUpMode = toSignUp;
            }
        }

        public string SchoolName => _settingsService.SchoolName;
        public string SchoolAddress => _settingsService.SchoolAddress;
        public string SystemTitle => _settingsService.SystemTitle;
        public string SystemAcronym => _settingsService.SystemAcronym;
        public string Tagline => _settingsService.Tagline;
        public string Motto => _settingsService.Motto;
        public string LogoIcon => _settingsService.LogoIcon;
        public object? LoginLogoSource => _settingsService.LoginLogoSource;

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    ErrorMessage = string.Empty;
                }
            }
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                if (SetProperty(ref _isPasswordVisible, value))
                {
                    OnPropertyChanged(nameof(PasswordEyeIcon));
                }
            }
        }

        public string PasswordEyeIcon => IsPasswordVisible ? "🙈" : "👁";

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand ExitCommand { get; }

        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private async Task InitializeSettingsAsync()
        {
            await _settingsService.LoadSettingsAsync();
            RefreshBranding();
        }

        private void RefreshBranding()
        {
            OnPropertyChanged(nameof(SchoolName));
            OnPropertyChanged(nameof(SchoolAddress));
            OnPropertyChanged(nameof(SystemTitle));
            OnPropertyChanged(nameof(SystemAcronym));
            OnPropertyChanged(nameof(Tagline));
            OnPropertyChanged(nameof(Motto));
            OnPropertyChanged(nameof(LogoIcon));
            OnPropertyChanged(nameof(LoginLogoSource));
        }

        public async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password.";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _authService.LoginAsync(Username, Password);
                if (result.Success)
                {
                    _onLoginSuccess();
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An unexpected error occurred: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExecuteSignUpAsync()
        {
            SignUpErrorMessage = string.Empty;
            SignUpSuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SignUpFullName))
            {
                SignUpErrorMessage = "Please enter your full name.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SignUpEmail) || !SignUpEmail.Contains("@"))
            {
                SignUpErrorMessage = "Please enter a valid email address.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SignUpUsername))
            {
                SignUpErrorMessage = "Please enter a username.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SignUpPassword))
            {
                SignUpErrorMessage = "Please enter a password.";
                return;
            }
            if (SignUpPassword.Length < 6)
            {
                SignUpErrorMessage = "Password must be at least 6 characters long.";
                return;
            }
            if (SignUpPassword != SignUpConfirmPassword)
            {
                SignUpErrorMessage = "Passwords do not match.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SignUpRole))
            {
                SignUpErrorMessage = "Please select your system role.";
                return;
            }
            if (!SignUpAgreeTerms)
            {
                SignUpErrorMessage = "Please agree to the Terms and Conditions and Privacy Policy.";
                return;
            }

            IsLoading = true;
            try
            {
                var (success, message) = await _authService.RegisterAsync(SignUpUsername, SignUpFullName, SignUpEmail, SignUpPassword, SignUpRole);
                if (success)
                {
                    SignUpSuccessMessage = message;
                    Username = SignUpUsername;
                    // Reset sign-up fields
                    SignUpFullName = string.Empty;
                    SignUpEmail = string.Empty;
                    SignUpPassword = string.Empty;
                    SignUpConfirmPassword = string.Empty;
                    // Delay slightly so user sees success and flip to login
                    await Task.Delay(1200);
                    FlipCard(false);
                }
                else
                {
                    SignUpErrorMessage = message;
                }
            }
            catch (Exception ex)
            {
                SignUpErrorMessage = $"Registration error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

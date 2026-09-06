using SmartSchoolIncidentManagementSystem.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class LoginCardView : UserControl
    {
        private bool _isSyncing;

        public LoginCardView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (s, e) =>
            {
                if (UsernameInput != null)
                {
                    UsernameInput.KeyDown -= OnUsernameKeyDown;
                    UsernameInput.KeyDown += OnUsernameKeyDown;
                }
                if (UserPasswordBox != null)
                {
                    UserPasswordBox.KeyDown -= OnPasswordKeyDown;
                    UserPasswordBox.KeyDown += OnPasswordKeyDown;
                }
                if (VisiblePasswordTextBox != null)
                {
                    VisiblePasswordTextBox.KeyDown -= OnPasswordKeyDown;
                    VisiblePasswordTextBox.KeyDown += OnPasswordKeyDown;
                }
                if (DataContext is LoginViewModel vm)
                {
                    SyncPasswordFromViewModel(vm);
                }
            };
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LoginViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }
            if (e.NewValue is LoginViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                SyncPasswordFromViewModel(newVm);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.Password) && sender is LoginViewModel vm)
            {
                SyncPasswordFromViewModel(vm);
            }
        }

        private void SyncPasswordFromViewModel(LoginViewModel vm)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                string pwd = vm.Password ?? string.Empty;
                if (UserPasswordBox != null && UserPasswordBox.Password != pwd)
                {
                    UserPasswordBox.Password = pwd;
                }
                if (VisiblePasswordTextBox != null && VisiblePasswordTextBox.Text != pwd)
                {
                    VisiblePasswordTextBox.Text = pwd;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void OnUsernameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (UserPasswordBox != null && UserPasswordBox.IsVisible)
                    UserPasswordBox.Focus();
                else if (VisiblePasswordTextBox != null && VisiblePasswordTextBox.IsVisible)
                    VisiblePasswordTextBox.Focus();
            }
        }

        private void OnPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
                {
                    vm.LoginCommand.Execute(null);
                }
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            if (DataContext is LoginViewModel vm && sender is PasswordBox pbox)
            {
                _isSyncing = true;
                vm.Password = pbox.Password;
                if (VisiblePasswordTextBox != null) VisiblePasswordTextBox.Text = pbox.Password;
                _isSyncing = false;
            }
        }

        private void OnVisiblePasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (DataContext is LoginViewModel vm && sender is TextBox txt)
            {
                _isSyncing = true;
                vm.Password = txt.Text;
                if (UserPasswordBox != null) UserPasswordBox.Password = txt.Text;
                _isSyncing = false;
            }
        }
    }
}

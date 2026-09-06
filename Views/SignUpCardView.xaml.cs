using SmartSchoolIncidentManagementSystem.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class SignUpCardView : UserControl
    {
        private bool _isSyncingPass;
        private bool _isSyncingConfirm;

        public SignUpCardView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (SignUpFullNameInput != null)
                {
                    SignUpFullNameInput.KeyDown -= OnFullNameKeyDown;
                    SignUpFullNameInput.KeyDown += OnFullNameKeyDown;
                }
                if (SignUpEmailInput != null)
                {
                    SignUpEmailInput.KeyDown -= OnEmailKeyDown;
                    SignUpEmailInput.KeyDown += OnEmailKeyDown;
                }
                if (SignUpUsernameInput != null)
                {
                    SignUpUsernameInput.KeyDown -= OnUsernameKeyDown;
                    SignUpUsernameInput.KeyDown += OnUsernameKeyDown;
                }
                if (SignUpPasswordBox != null)
                {
                    SignUpPasswordBox.KeyDown -= OnPasswordKeyDown;
                    SignUpPasswordBox.KeyDown += OnPasswordKeyDown;
                }
                if (SignUpVisiblePasswordTextBox != null)
                {
                    SignUpVisiblePasswordTextBox.KeyDown -= OnPasswordKeyDown;
                    SignUpVisiblePasswordTextBox.KeyDown += OnPasswordKeyDown;
                }
                if (SignUpConfirmPasswordBox != null)
                {
                    SignUpConfirmPasswordBox.KeyDown -= OnConfirmPasswordKeyDown;
                    SignUpConfirmPasswordBox.KeyDown += OnConfirmPasswordKeyDown;
                }
                if (SignUpVisibleConfirmPasswordTextBox != null)
                {
                    SignUpVisibleConfirmPasswordTextBox.KeyDown -= OnConfirmPasswordKeyDown;
                    SignUpVisibleConfirmPasswordTextBox.KeyDown += OnConfirmPasswordKeyDown;
                }
            };
        }

        private void OnFullNameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SignUpEmailInput?.Focus();
            }
        }

        private void OnEmailKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SignUpUsernameInput?.Focus();
            }
        }

        private void OnUsernameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (SignUpPasswordBox != null && SignUpPasswordBox.IsVisible)
                    SignUpPasswordBox.Focus();
                else if (SignUpVisiblePasswordTextBox != null && SignUpVisiblePasswordTextBox.IsVisible)
                    SignUpVisiblePasswordTextBox.Focus();
            }
        }

        private void OnPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (SignUpConfirmPasswordBox != null && SignUpConfirmPasswordBox.IsVisible)
                    SignUpConfirmPasswordBox.Focus();
                else if (SignUpVisibleConfirmPasswordTextBox != null && SignUpVisibleConfirmPasswordTextBox.IsVisible)
                    SignUpVisibleConfirmPasswordTextBox.Focus();
            }
        }

        private void OnConfirmPasswordKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (DataContext is LoginViewModel vm && vm.SignUpCommand.CanExecute(null))
                {
                    vm.SignUpCommand.Execute(null);
                }
            }
        }

        private void OnSignUpPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPass) return;
            if (DataContext is LoginViewModel vm && sender is PasswordBox pbox)
            {
                _isSyncingPass = true;
                vm.SignUpPassword = pbox.Password;
                if (SignUpVisiblePasswordTextBox != null) SignUpVisiblePasswordTextBox.Text = pbox.Password;
                _isSyncingPass = false;
            }
        }

        private void OnSignUpVisiblePasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPass) return;
            if (DataContext is LoginViewModel vm && sender is TextBox txt)
            {
                _isSyncingPass = true;
                vm.SignUpPassword = txt.Text;
                if (SignUpPasswordBox != null) SignUpPasswordBox.Password = txt.Text;
                _isSyncingPass = false;
            }
        }

        private void OnSignUpConfirmPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingConfirm) return;
            if (DataContext is LoginViewModel vm && sender is PasswordBox pbox)
            {
                _isSyncingConfirm = true;
                vm.SignUpConfirmPassword = pbox.Password;
                if (SignUpVisibleConfirmPasswordTextBox != null) SignUpVisibleConfirmPasswordTextBox.Text = pbox.Password;
                _isSyncingConfirm = false;
            }
        }

        private void OnSignUpVisibleConfirmPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingConfirm) return;
            if (DataContext is LoginViewModel vm && sender is TextBox txt)
            {
                _isSyncingConfirm = true;
                vm.SignUpConfirmPassword = txt.Text;
                if (SignUpConfirmPasswordBox != null) SignUpConfirmPasswordBox.Password = txt.Text;
                _isSyncingConfirm = false;
            }
        }
    }
}

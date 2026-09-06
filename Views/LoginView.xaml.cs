using SmartSchoolIncidentManagementSystem.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SmartSchoolIncidentManagementSystem.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LoginViewModel oldVm)
            {
                oldVm.RequestCardFlip -= OnRequestCardFlip;
            }
            if (e.NewValue is LoginViewModel newVm)
            {
                newVm.RequestCardFlip += OnRequestCardFlip;
                LoginCardPanel.Visibility = newVm.IsSignUpMode ? Visibility.Collapsed : Visibility.Visible;
                SignUpCardPanel.Visibility = newVm.IsSignUpMode ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnRequestCardFlip(bool toSignUp)
        {
            if (CardScaleTransform == null)
            {
                if (DataContext is LoginViewModel vm) vm.IsSignUpMode = toSignUp;
                LoginCardPanel.Visibility = toSignUp ? Visibility.Collapsed : Visibility.Visible;
                SignUpCardPanel.Visibility = toSignUp ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            var animOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            var animIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            animOut.Completed += (s, e) =>
            {
                if (DataContext is LoginViewModel vm)
                {
                    vm.IsSignUpMode = toSignUp;
                }
                LoginCardPanel.Visibility = toSignUp ? Visibility.Collapsed : Visibility.Visible;
                SignUpCardPanel.Visibility = toSignUp ? Visibility.Visible : Visibility.Collapsed;
                CardScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animIn);
            };

            CardScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animOut);
        }
    }
}

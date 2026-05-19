using Hiemdall_bridge.Interface;
using Hiemdall_bridge.ViewModels;
using System;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static Hiemdall_bridge.MessageBoxCustom;

namespace Hiemdall_bridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Login : Window
    {    

        public Login()
        {
            InitializeComponent();
            Loaded += Login_Loaded;            
        }
        private void Login_Loaded(object sender, RoutedEventArgs e)
        {
            var data = this.Tag as Tuple<string, string>;

            if (data == null)
                return; // normal manual login

            string username = data.Item1;
            string password = data.Item2;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return;

            var vm = (LoginViewModel)this.DataContext;

            vm.Username = username;

            PasswordHidden.Password = password;
            PasswordVisible.Text = password;

            vm.Login(password);
        }
        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordVisible.Visibility == Visibility.Collapsed)
            {
                PasswordVisible.Text = PasswordHidden.Password;
                PasswordVisible.Visibility = Visibility.Visible;
                PasswordHidden.Visibility = Visibility.Collapsed;
                EyeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOff; // Changes icon
            }
            else
            {
                PasswordHidden.Password = PasswordVisible.Text;
                PasswordHidden.Visibility = Visibility.Visible;
                PasswordVisible.Visibility = Visibility.Collapsed;
                EyeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Eye;    // Changes icon back
            }
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = PasswordHidden.Visibility == Visibility.Visible
                          ? PasswordHidden.Password
                          : PasswordVisible.Text;

                var vm = (LoginViewModel)this.DataContext;
                vm.Login(password);
                
            }
            catch (System.Exception ex )
            {

               // _logger.Error("LoginButton_Click Method: " + ex.Message);
            }
        
        }

    }


}


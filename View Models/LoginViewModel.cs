using Hiemdall_bridge.Helpers;
using Hiemdall_bridge.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;


namespace Hiemdall_bridge.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigation;
        private readonly IMessageBoxService _messageBoxService;
        public Action? RequestClose { get; set; }
        public LoginViewModel(
            IAuthenticationService authService,
            INavigationService navigation,
            IMessageBoxService messageBoxService)
        {
            _authService = authService;
            _navigation = navigation;
            _messageBoxService = messageBoxService;
            LoginCommand = new RelayCommand(Login);
            CloseCommand = new RelayCommand(_ => Application.Current.Shutdown());
        }

        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }

        public void Login(object parameter)
        {
            try
            {

                string password = parameter as string;

                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
                {
                    _messageBoxService.Show("Please enter username and password", MessageBoxCustom.MessageType.Error,
                          MessageBoxCustom.MessageButtons.Ok);// new MessageBoxCustom("Please enter username and password" , MessageType.Warning, MessageButtons.Ok).ShowDialog();
                    return;
                }

                var user = _authService.Authenticate(Username, password);

                if (user != null)
                {
                    _navigation.OpenMainWindow(user);
                    // This line tells the Login.xaml.cs to close the window
                    RequestClose?.Invoke();
                }
                else
                    _messageBoxService.Show("Please enter correct username and password", MessageBoxCustom.MessageType.Error,
                        MessageBoxCustom.MessageButtons.Ok);// new MessageBoxCustom("Please enter username and password" , MessageType.Warning, MessageButtons.Ok).ShowDialog();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show(ex.Message, MessageBoxCustom.MessageType.Error,MessageBoxCustom.MessageButtons.Ok);

            }
        }

    }
}

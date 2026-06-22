

using Hiemdall_bridge.Interface;
using Hiemdall_bridge.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Hiemdall_bridge
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }
        private static Mutex _mutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            string instanceId = "1";
            try
            {
                instanceId = ConfigurationManager.AppSettings["IconNumber"];
                // int iconNumber = int.TryParse(iconNoStr, out int no) ? no : 1;
            }
            catch (Exception)
            {
            }
            bool createdNew;

            if (e.Args.Length > 0)
                instanceId = e.Args[0];

            // Store globally
            AppDomain.CurrentDomain.SetData("InstanceId", instanceId);

            // 2. Mutex MUST use same instanceId
            _mutex = new Mutex(true, $"Global_Hiemdall_bridge_{instanceId}", out createdNew);

            if (!createdNew)
            {
                //MessageBox.Show("Application already running for this instance");
                Shutdown();
                return;
            }
            if (!Help.Help.Validate())
            {
                MessageBox.Show("License Invalid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }
            base.OnStartup(e);
            string[] args = e.Args;
            string batUser = "";
            string batPass = "";

            if (e.Args.Length >= 3)
            {
                batUser = e.Args[1];
                batPass = e.Args[2];
            }
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show(((Exception)ex.ExceptionObject).Message);
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                MessageBox.Show(ex.Exception.Message);
                ex.SetObserved();
            };
            ServiceProvider = ServiceConfigurator.Configure();

            var loginVM = ServiceProvider.GetRequiredService<LoginViewModel>();
            // 3. IMMEDIATELY load the config so it's ready for all ViewModels
            var configService = ServiceProvider.GetRequiredService<IAppConfiguration>();
            configService.ReadConfig(); // This fills the 'Current' object before any UI opens
            loginVM.Username = batUser; // if exists
            //var loginWindow = new Login
            //{
            //    DataContext = loginVM
            //};
            //if (MainWindow != null)
            //{
            //    SetApplicationIcon(MainWindow);
            //}
            //loginWindow.Show();
            var loginWindow = new Login
            {
                DataContext = loginVM
            };

            loginWindow.Tag = new Tuple<string, string>(batUser, batPass);
            loginWindow.Show();
        }
        private void App_DispatcherUnhandledException(object sender,
    System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.Exception.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true; // ✅ Prevent app crash
            Application.Current.Shutdown();
        }
        public static void SetApplicationIcon(Window window)
        {
            try
            {
                // Get icon number from app.config
                string iconNumberStr = ConfigurationManager.AppSettings["IconNumber"];

                if (int.TryParse(iconNumberStr, out int iconNumber))
                {
                    // Build the icon path (icons are in the root folder)
                    // string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"S{iconNumber}.ico");
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"S{GetCurrentInstanceNumber()}.ico");
                    // Check if icon file exists
                    if (File.Exists(iconPath))
                    {
                        window.Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                        window.Title = $"{window.Title} (Instance {iconNumber})"; // Optional: show instance in title
                    }
                    else
                    {
                        MessageBox.Show($"Icon file not found: {iconPath}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Icon number not found in app.config", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("SetApplication Icon Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Optional: Get current instance number
        public static int GetCurrentInstanceNumber()
        {

            var instanceId = AppDomain.CurrentDomain.GetData("InstanceId")?.ToString();

            if (int.TryParse(instanceId, out int id))
            {
                return id;
            }

            string iconNumberStr = ConfigurationManager.AppSettings["IconNumber"];
            if (int.TryParse(iconNumberStr, out int iconNumber))
            {
                return iconNumber;
            }

            return 1;
        }
    }
}


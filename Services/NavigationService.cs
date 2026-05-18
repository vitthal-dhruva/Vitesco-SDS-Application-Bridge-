//using Hiemdall_bridge;
//using Hiemdall_bridge.Interface;
//using Hiemdall_bridge.Interfaces;
//using Hiemdall_bridge.Models;
//using Hiemdall_bridge.ViewModels;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Configuration;
//using System.IO;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Media.Imaging;

//public class NavigationService : INavigationService
//{
//    private readonly IServiceProvider _provider;

//    public NavigationService(IServiceProvider provider)
//    {
//        _provider = provider;
//    }

//    public void OpenMainWindow(UserModel user)
//    {
//        var vm = _provider.GetRequiredService<Form2ViewModel>();

//        vm.SetUser(user);

//        var mainWindow = new Form2(vm)
//        {
//            DataContext = vm
//        };

//        // Store reference to login before replacing MainWindow
//        var loginWindow = Application.Current.MainWindow;

//        mainWindow.Show();

//        // Set new MainWindow
//        Application.Current.MainWindow = mainWindow;
//        if(mainWindow!= null)
//        {
//            SetApplicationIcon(mainWindow);
//        }
//        // Close login window
//        loginWindow?.Close();
//    }

//    public void OpenLogin()
//    {
//        // 1. Get hardware services to shut them down
//        var tcp = _provider.GetRequiredService<ITcpClient>();
//        var opc = _provider.GetRequiredService<IOpcClient>();

//        // 2. Fire and forget the disconnection (or make the method async)
//        // This ensures background loops in Singletons are killed
//        Task.Run(async () => {
//            await tcp.DisconnectAsync();
//            await opc.DisconnectOPCSessionAsync();
//        });

//        // 3. Open Login Window
//        var vm = _provider.GetRequiredService<LoginViewModel>();
//        var login = new Login { DataContext = vm };

//        login.Show();

//        // 4. Close Form2 and set Login as the new MainWindow
//        var currentWindow = Application.Current.MainWindow;
//        Application.Current.MainWindow = login;
//        if (mainWindow != null)
//        {
//            SetApplicationIcon(mainWindow);
//        }
//        currentWindow?.Close();
//    }
//    public static void SetApplicationIcon(Window window)
//    {
//        try
//        {
//            // Get icon number from app.config
//            string iconNumberStr = ConfigurationManager.AppSettings["IconNumber"];

//            if (int.TryParse(iconNumberStr, out int iconNumber))
//            {
//                // Build the icon path (icons are in the root folder)
//                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"S{iconNumber}.ico");

//                // Check if icon file exists
//                if (File.Exists(iconPath))
//                {
//                    window.Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
//                    window.Title = $"{window.Title} (Instance {iconNumber})"; // Optional: show instance in title
//                }
//                else
//                {
//                    System.Diagnostics.Debug.WriteLine($"Icon file not found: {iconPath}");
//                }
//            }
//            else
//            {
//                System.Diagnostics.Debug.WriteLine("Invalid IconNumber in config");
//            }
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine($"Error setting icon: {ex.Message}");
//        }
//    }

//    // Optional: Get current instance number
//    public static int GetCurrentInstanceNumber()
//    {
//        string iconNumberStr = ConfigurationManager.AppSettings["IconNumber"];
//        if (int.TryParse(iconNumberStr, out int iconNumber))
//        {
//            return iconNumber;
//        }
//        return 1;
//    }
//}
using Hiemdall_bridge;
using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Interfaces;
using Hiemdall_bridge.Models;
using Hiemdall_bridge.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;

    public NavigationService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void OpenMainWindow(UserModel user)
    {
        var vm = _provider.GetRequiredService<Form2ViewModel>();

        vm.SetUser(user);

        var mainWindow = new Form2(vm)
        {
            DataContext = vm
        };

        // Store reference to login before replacing MainWindow
        var loginWindow = Application.Current.MainWindow;

        SetApplicationIcon(mainWindow);
        mainWindow.Show();

        // Set new MainWindow
        Application.Current.MainWindow = mainWindow;

        // Close login window
        loginWindow?.Close();
    }

    public void OpenLogin()
    {
        // 1. Get hardware services to shut them down
        var tcp = _provider.GetRequiredService<ITcpClient>();
        var opc = _provider.GetRequiredService<IOpcClient>();

        // 2. Fire and forget the disconnection (or make the method async)
        // This ensures background loops in Singletons are killed
        Task.Run(async () => {
            await tcp.DisconnectAsync();
            await opc.DisconnectOPCSessionAsync();
        });

        // 3. Open Login Window
        var vm = _provider.GetRequiredService<LoginViewModel>();
        var login = new Login { DataContext = vm };

        SetApplicationIcon(login);
        login.Show();

        // 4. Close Form2 and set Login as the new MainWindow
        var currentWindow = Application.Current.MainWindow;
        Application.Current.MainWindow = login;

        currentWindow?.Close();
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
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"S{iconNumber}.ico");

                // Check if icon file exists
                if (File.Exists(iconPath))
                {
                    window.Icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                    window.Title = $"{window.Title}  (Instance {iconNumber})";// (Instance {iconNumber})
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Icon file not found: {iconPath}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Invalid IconNumber in config");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting icon: {ex.Message}");
        }
    }

    // Optional: Get current instance number
}
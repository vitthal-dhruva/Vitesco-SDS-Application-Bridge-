using Hiemdall_bridge;
using Hiemdall_bridge.Interface;
using Hiemdall_bridge.Interfaces;
using Hiemdall_bridge.Services;
using Hiemdall_bridge.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Sockets;

public static class ServiceConfigurator
{
    public static ServiceProvider Configure()
    {
        var services = new ServiceCollection();

        // ===============================
        // SINGLETON SERVICES
        // ===============================
     
        services.AddSingleton<IBusinessLayer, BusinessLayer>();
        services.AddSingleton<IOpcClient, OPCUA>();
        services.AddSingleton<ITcpClient, TCPIP>();
        services.AddSingleton<ILogger,ManagedLogger>();       
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IMessageBoxService, MessageBoxService>();
        services.AddSingleton<IAppConfiguration, AppConfiguration>();
        // In your App.xaml.cs or Service Registration
        services.AddTransient<TagConfigViewModel>();
        services.AddTransient<SequenceLogViewModel>();
        services.AddTransient<UserAuthViewModel>();
        // ===============================
        // TRANSIENT SERVICES
        // ===============================

        services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<Form2ViewModel>();

        return services.BuildServiceProvider();
    }
}

using System;
using System.Threading.Tasks;

namespace Hiemdall_bridge.Interface
{
    public interface ITcpClient
    {
        bool IsConnected { get; }
        event Action<string>? OnMessageReceived;
        event Action? OnDisconnected;
        event Action? OnReconnected;
        Task ConnectAsync(string host, int port);
        Task SendAsync(string message);
        Task DisconnectAsync();
    }
}
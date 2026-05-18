
using Hiemdall_bridge.Interface;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hiemdall_bridge
{
    public class TCPIP:ITcpClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;

        private CancellationTokenSource? _connectionCts;

        private readonly int _reconnectDelay = 3000;

        private string _host = "";
        private int _port;

        public bool IsConnected { get; private set; }

        public event Action<string>? OnMessageReceived;
        public event Action? OnDisconnected;
        public event Action? OnReconnected;

        #region CONNECT

        public async Task ConnectAsync(string host, int port)
        {
            if (_host == host && _port == port)
                return;
            _host = host;
            _port = port;

            await DisconnectAsync();

            _connectionCts = new CancellationTokenSource();

            await ConnectInternalAsync(_connectionCts.Token);
        }

        private async Task ConnectInternalAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_host, _port);

                    _client.SendTimeout = 60000;
                    _client.ReceiveTimeout = 60000;

                    _stream = _client.GetStream();
                    IsConnected = true;

                    OnReconnected?.Invoke();

                    _ = Task.Run(() => ReceiveLoop(token), token);
                    return;
                }
                catch
                {
                    await Task.Delay(_reconnectDelay, token);
                }
            }
        }

        #endregion

        #region RECEIVE

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[4096];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_stream == null)
                        throw new Exception("Stream is null");

                    int count = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (count == 0)
                        throw new SocketException();

                    byte STX = 0x02;
                    byte ETX = 0x03;

                    int start = Array.IndexOf(buffer, STX, 0, count);
                    int end = Array.IndexOf(buffer, ETX, 0, count);

                    if (start >= 0 && end > start)
                    {
                        int len = end - start + 1;
                        byte[] extracted = new byte[len];
                        Array.Copy(buffer, start, extracted, 0, len);

                        string msg = Encoding.ASCII.GetString(extracted);

                        if (!string.IsNullOrWhiteSpace(msg))
                            OnMessageReceived?.Invoke(msg);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal cancel
            }
            catch
            {
                HandleDisconnect();

                if (!token.IsCancellationRequested)
                    await ReconnectAsync();
            }
        }

        #endregion

        #region SEND

        public async Task SendAsync(string message)
        {
            try
            {
                if (!IsConnected || _stream == null)
                    return;

                string framed = ((char)2) + message + ((char)3);

                byte[] lengthBytes = BitConverter.GetBytes(framed.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                byte[] payload = Encoding.ASCII.GetBytes(framed);
                byte[] final = lengthBytes.Concat(payload).ToArray();

                await _stream.WriteAsync(final, 0, final.Length);
            }
            catch
            {
                HandleDisconnect();
                await ReconnectAsync();
            }
        }

        #endregion

        #region RECONNECT

        private async Task ReconnectAsync()
        {
            if (_connectionCts == null)
                return;

            while (!_connectionCts.IsCancellationRequested)
            {
                try
                {
                    _client?.Close();
                    _client?.Dispose();

                    _client = new TcpClient();
                    await _client.ConnectAsync(_host, _port);

                    _stream = _client.GetStream();
                    IsConnected = true;

                    OnReconnected?.Invoke();

                    _ = Task.Run(() => ReceiveLoop(_connectionCts.Token));
                    return;
                }
                catch
                {
                    await Task.Delay(_reconnectDelay);
                }
            }
        }

        #endregion

        #region DISCONNECT

        private void HandleDisconnect()
        {
            if (!IsConnected)
                return;

            IsConnected = false;
            OnDisconnected?.Invoke();

            try { _client?.Close(); } catch { }
            try { _client?.Dispose(); } catch { }
        }

        public async Task DisconnectAsync()
        {
            if (_connectionCts != null)
            {
                _connectionCts.Cancel();
                _connectionCts.Dispose();
                _connectionCts = null;
                _port=0;
                _host="";
            }

            HandleDisconnect();
            await Task.Delay(100);
        }

        #endregion
    }
}







using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient?.Connected == true && _stream != null;

        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected)
            {
                Console.WriteLine($"Already connected to {_host}:{_port}");
                return;
            }

            try
            {
                _tcpClient = new TcpClient();
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();

                Console.WriteLine($"Connected to {_host}:{_port}");
                _ = StartListeningAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (!Connected)
            {
                Console.WriteLine("No active connection to disconnect.");
                return;
            }

            _cts?.Cancel();
            _stream?.Close();
            _tcpClient?.Close();

            _cts = null;
            _tcpClient = null;
            _stream = null;

            Console.WriteLine("Disconnected.");
        }

        public Task SendMessageAsync(string message) =>
            SendMessageAsync(Encoding.UTF8.GetBytes(message));

        public async Task SendMessageAsync(byte[] data)
        {
            if (!Connected || _stream is not { CanWrite: true })
                throw new InvalidOperationException("Not connected to a server.");

            Console.WriteLine($"Message sent: {BitConverter.ToString(data).Replace("-", " ")}");
            await _stream.WriteAsync(data, 0, data.Length);
        }

        private async Task StartListeningAsync()
        {
            if (!Connected || _stream is not { CanRead: true })
                throw new InvalidOperationException("Not connected to a server.");

            Console.WriteLine("Starting listening for incoming messages.");

            try
            {
                var buffer = new byte[8192];
                while (!_cts!.Token.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                    if (bytesRead > 0)
                    {
                        MessageReceived?.Invoke(this, buffer[..bytesRead]);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disconnecting
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in listening loop: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Listener stopped.");
            }
        }
    }
}

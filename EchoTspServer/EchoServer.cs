using EchoTspServer.Handlers;
using EchoTspServer.Networking;
using System.Net.Sockets;

namespace EchoTspServer
{
    public class EchoServer
    {
        private readonly ITcpListenerWrapper _listener;
        private readonly IConnectionHandler _handler;
        private readonly CancellationTokenSource _cts;

        public EchoServer(ITcpListenerWrapper listener, IConnectionHandler handler)
        {
            _listener = listener;
            _handler = handler;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener.Start();

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = ProcessClientAsync(client, _cts.Token);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private async Task ProcessClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    await _handler.HandleClientAsync(stream, token);
                }
                catch (OperationCanceledException) { }
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
        }
    }
}
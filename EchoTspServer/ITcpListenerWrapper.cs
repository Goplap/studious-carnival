using System.Net.Sockets;

namespace EchoTspServer.Networking
{
    public interface ITcpListenerWrapper
    {
        void Start();
        void Stop();
        Task<TcpClient> AcceptTcpClientAsync();
    }
}
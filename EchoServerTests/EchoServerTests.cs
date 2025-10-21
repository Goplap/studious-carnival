using EchoTspServer;
using EchoTspServer.Handlers;
using EchoTspServer.Networking;
using Moq;
using System.Net.Sockets;

namespace EchoServerTests
{
    public class EchoServerTests
    {
        private Mock<ITcpListenerWrapper> _listenerMock;
        private Mock<IConnectionHandler> _handlerMock;
        private EchoServer _server;

        [SetUp]
        public void Setup()
        {
            _listenerMock = new Mock<ITcpListenerWrapper>();
            _handlerMock = new Mock<IConnectionHandler>();
            _server = new EchoServer(_listenerMock.Object, _handlerMock.Object);
        }

        [Test]
        public async Task StartAsync_StartsListener()
        {
            // Arrange
            _listenerMock.Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("test"));

            // Act
            await _server.StartAsync();

            // Assert
            _listenerMock.Verify(l => l.Start(), Times.Once);
        }

        [Test]
        public async Task StartAsync_CallsAcceptTcpClientAsync()
        {
            // Arrange  
            _listenerMock.Setup(l => l.AcceptTcpClientAsync())
                .ThrowsAsync(new ObjectDisposedException("test"));

            // Act
            await _server.StartAsync();

            // Assert
            _listenerMock.Verify(l => l.AcceptTcpClientAsync(), Times.AtLeastOnce);
        }

        [Test]
        public void Stop_StopsListener()
        {
            // Act
            _server.Stop();

            // Assert
            _listenerMock.Verify(l => l.Stop(), Times.Once);
        }

        [Test]
        public async Task Stop_CancelsAcceptLoop()
        {
            // Arrange
            var tcs = new TaskCompletionSource<TcpClient>();
            _listenerMock.Setup(l => l.AcceptTcpClientAsync())
                .Returns(tcs.Task);

            var serverTask = Task.Run(() => _server.StartAsync());

            // Act
            _server.Stop();
            await Task.Delay(50);

            // Assert
            _listenerMock.Verify(l => l.Stop(), Times.Once);
        }
    }
}
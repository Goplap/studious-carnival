using NUnit.Framework;
using Moq;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EchoTspServer;
using EchoTspServer.Networking;
using EchoTspServer.Handlers;

namespace EchoTspServer.Tests
{
    [TestFixture]
    public class EchoServerTests
    {
        private Mock<ITcpListenerWrapper> _listenerMock = null!;
        private Mock<IConnectionHandler> _handlerMock = null!;
        private EchoServer _server = null!;

        [SetUp]
        public void SetUp()
        {
            _listenerMock = new Mock<ITcpListenerWrapper>(MockBehavior.Strict);
            _handlerMock = new Mock<IConnectionHandler>(MockBehavior.Strict);

            // by default, Stop() нічого не робить
            _listenerMock.Setup(l => l.Stop());
            _server = new EchoServer(_listenerMock.Object, _handlerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            try { _server.Stop(); } catch { }
        }

        [Test]
        public void Constructor_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => new EchoServer(_listenerMock.Object, _handlerMock.Object));
        }

        [Test]
        public async Task StartAsync_ShouldExit_WhenListenerThrowsObjectDisposedException()
        {
            // Arrange
            _listenerMock.Setup(l => l.Start());
            _listenerMock.Setup(l => l.AcceptTcpClientAsync())
                         .ThrowsAsync(new ObjectDisposedException("listener"));

            // Act
            var task = _server.StartAsync();

            var finished = await Task.WhenAny(task, Task.Delay(1000));

            // Assert (новий стиль)
            Assert.That(finished, Is.EqualTo(task), "StartAsync не завершився при ObjectDisposedException");
            _listenerMock.Verify(l => l.Start(), Times.Once);
        }

        [Test]
        public void Stop_CanBeCalledMultipleTimesWithoutError()
        {
            // Arrange
            _listenerMock.Setup(l => l.Stop());

            // Act & Assert
            Assert.DoesNotThrow(() => _server.Stop());
            Assert.DoesNotThrow(() => _server.Stop());

            _listenerMock.Verify(l => l.Stop(), Times.AtLeastOnce);
        }
    }
}

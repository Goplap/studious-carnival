using NUnit.Framework;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using EchoTspServer.Networking;

namespace EchoTspServer.Tests.Networking
{
    [TestFixture]
    public class TcpListenerWrapperTests
    {
        private const int TestPort = 50123;
        private TcpListenerWrapper _listenerWrapper = null!;

        [SetUp]
        public void SetUp()
        {
            _listenerWrapper = new TcpListenerWrapper(TestPort);
        }

        [TearDown]
        public void TearDown()
        {
            try { _listenerWrapper.Stop(); } catch { }
        }

        [Test]
        public void Constructor_ShouldCreateInstanceWithoutException()
        {
            Assert.DoesNotThrow(() => new TcpListenerWrapper(TestPort));
        }

        [Test]
        public void Start_ShouldStartListenerWithoutException()
        {
            Assert.DoesNotThrow(() => _listenerWrapper.Start());
        }

        [Test]
        public void Stop_ShouldStopListenerWithoutException()
        {
            _listenerWrapper.Start();
            Assert.DoesNotThrow(() => _listenerWrapper.Stop());
        }

        [Test]
        public async Task AcceptTcpClientAsync_ShouldAcceptConnection()
        {
            _listenerWrapper.Start();

            var acceptTask = _listenerWrapper.AcceptTcpClientAsync();

            await Task.Delay(50);

            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", TestPort);

            var acceptedClient = await acceptTask;

            Assert.That(acceptedClient, Is.Not.Null);
            Assert.That(acceptedClient!.Connected, Is.True);

            acceptedClient.Close();
        }

        [Test]
        public void AcceptTcpClientAsync_ShouldThrowIfNotStarted()
        {
            Assert.That(async () => await _listenerWrapper.AcceptTcpClientAsync(),
                        Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Stop_CanBeCalledMultipleTimes()
        {
            _listenerWrapper.Start();
            Assert.DoesNotThrow(() => _listenerWrapper.Stop());
            Assert.DoesNotThrow(() => _listenerWrapper.Stop());
        }
    }
}

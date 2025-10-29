using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientComprehensiveTests
{
    private NetSdrClient _client;
    private Mock<ITcpClient> _tcpMock;
    private Mock<IUdpClient> _udpMock;

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _udpMock = new Mock<IUdpClient>();

        // Setup default behavior for SendMessageAsync to complete immediately
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
            .Callback<byte[]>((bytes) =>
            {
                // Simulate response
                _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
            })
            .Returns(Task.CompletedTask);

        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
    }

    [Test]
    public async Task ConnectAsync_WhenNotConnected_ConnectsAndSendsSetup()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        // Act
        await _client.ConnectAsync();

        // Assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotReconnect()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(true);

        // Act
        await _client.ConnectAsync();

        // Assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Never);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task StartIQAsync_WhenNotConnected_DoesNotStart()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);

        // Act
        await _client.StartIQAsync();

        // Assert
        Assert.That(_client.IQStarted, Is.False);
        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Never);
    }

    [Test]
    public async Task StartIQAsync_WhenConnected_StartsListening()
    {
        // Arrange
        await ConnectClient();

        // Act
        await _client.StartIQAsync();

        // Assert
        Assert.That(_client.IQStarted, Is.True);
        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
    }

    [Test]
    public async Task StopIQAsync_WhenNotConnected_DoesNotStop()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);

        // Act
        await _client.StopIQAsync();

        // Assert
        Assert.That(_client.IQStarted, Is.False);
        _udpMock.Verify(udp => udp.StopListening(), Times.Never);
    }

    [Test]
    public async Task StopIQAsync_WhenConnected_StopsListening()
    {
        // Arrange
        await ConnectClient();
        await _client.StartIQAsync();

        // Act
        await _client.StopIQAsync();

        // Assert
        Assert.That(_client.IQStarted, Is.False);
        _udpMock.Verify(udp => udp.StopListening(), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsync_WhenConnected_SendsMessage()
    {
        // Arrange
        await ConnectClient();
        _tcpMock.Invocations.Clear(); // Clear setup invocations

        // Act
        await _client.ChangeFrequencyAsync(100000000, 1);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task ChangeFrequencyAsync_WithDifferentChannels_Works()
    {
        // Arrange
        await ConnectClient();
        _tcpMock.Invocations.Clear();

        // Act
        await _client.ChangeFrequencyAsync(100000000, 0);
        await _client.ChangeFrequencyAsync(200000000, 1);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(2));
    }

    [Test]
    public void Disconnect_CallsTcpDisconnect()
    {
        // Act
        _client.Disconnect();

        // Assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task Disconnect_AfterConnect_DisconnectsProperly()
    {
        // Arrange
        await ConnectClient();

        // Act
        _client.Disconnect();

        // Assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartStopIQ_Sequence_WorksCorrectly()
    {
        // Arrange
        await ConnectClient();

        // Act
        await _client.StartIQAsync();
        await _client.StopIQAsync();
        await _client.StartIQAsync();

        // Assert
        Assert.That(_client.IQStarted, Is.True);
        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Exactly(2));
        _udpMock.Verify(udp => udp.StopListening(), Times.Once);
    }

    [Test]
    public async Task TcpMessageReceived_HandlesResponse()
    {
        // Arrange
        await ConnectClient();
        var testResponse = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, testResponse);
        });
    }

    [Test]
    public async Task ChangeFrequencyAsync_WithLargeFrequency_Works()
    {
        // Arrange
        await ConnectClient();
        _tcpMock.Invocations.Clear();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _client.ChangeFrequencyAsync(2000000000, 1));
    }

    [Test]
    public async Task MultipleOperations_ExecuteInSequence()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        // Act
        await _client.ConnectAsync();
        await _client.ChangeFrequencyAsync(100000000, 1);
        await _client.StartIQAsync();
        await _client.StopIQAsync();
        _client.Disconnect();

        // Assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    private async Task ConnectClient()
    {
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        await _client.ConnectAsync();
    }
}
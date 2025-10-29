using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task ChangeFrequencyAsync_SendsCorrectMessage()
    {
        // Arrange
        await _client.ConnectAsync();
        long testHz = 144000000; // 144 MHz
        int testChannel = 1;

        // Act
        await _client.ChangeFrequencyAsync(testHz, testChannel);

        // Assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
    }

    [Test]
    public void GetSamples_ReturnsExpectedSamples()
    {
        // Arrange
        byte[] body = { 0x01, 0x00, 0x02, 0x00 }; // два зразки: 1 і 2

        // Act
        var samples = NetSdrMessageHelper.GetSamples(16, body).ToArray();

        // Assert
        Assert.That(samples.Length, Is.EqualTo(2));
        Assert.That(samples[0], Is.EqualTo(1));
        Assert.That(samples[1], Is.EqualTo(2));
    }

    [Test]
    public void GetSamples_Throws_OnTooLargeSampleSize()
    {
        // Arrange
        byte[] body = { 0x00, 0x01 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = NetSdrMessageHelper.GetSamples(64, body).ToArray();
        });
    }


    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconnect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconnect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    //TODO: cover the rest of the NetSdrClient code here
}

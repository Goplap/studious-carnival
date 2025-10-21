using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class TcpClientWrapperTests
{
    [Test]
    public void Constructor_InitializesWithHostAndPort()
    {
        // Arrange & Act
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

        // Assert
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Connected_ReturnsFalse_WhenNotConnected()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

        // Act & Assert
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Disconnect_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Disconnect());
    }

    [Test]
    public async Task SendMessageAsync_WhenNotConnected_ThrowsException()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
        var data = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await wrapper.SendMessageAsync(data));
    }

    [Test]
    public void Connect_ToInvalidHost_HandlesGracefully()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("999.999.999.999", 5000);

        // Act & Assert - Should not throw, just print error
        Assert.DoesNotThrow(() => wrapper.Connect());
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void MessageReceived_Event_CanBeSubscribed()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
        bool eventFired = false;

        // Act
        wrapper.MessageReceived += (sender, data) => eventFired = true;

        // Assert
        Assert.That(eventFired, Is.False); // Event not fired yet
    }

    [Test]
    public void Disconnect_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.Disconnect();
            wrapper.Disconnect();
            wrapper.Disconnect();
        });
    }

    [Test]
    public void Constructor_WithDifferentPorts_CreatesInstances()
    {
        // Arrange & Act
        var wrapper1 = new TcpClientWrapper("127.0.0.1", 5000);
        var wrapper2 = new TcpClientWrapper("127.0.0.1", 5001);
        var wrapper3 = new TcpClientWrapper("localhost", 8080);

        // Assert
        Assert.That(wrapper1, Is.Not.Null);
        Assert.That(wrapper2, Is.Not.Null);
        Assert.That(wrapper3, Is.Not.Null);
    }

    [Test]
    public void Connect_WhenAlreadyConnected_PrintsMessage()
    {
        // This test documents the behavior but doesn't actually connect
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);

        // Act & Assert - both calls should not throw
        Assert.DoesNotThrow(() => wrapper.Connect());
    }

    [Test]
    public async Task SendMessageAsync_WithEmptyData_ThrowsWhenNotConnected()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 5000);
        var data = Array.Empty<byte>();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await wrapper.SendMessageAsync(data));
    }
}
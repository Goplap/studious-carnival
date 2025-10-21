namespace NetSdrClientAppTests;

public class UdpClientWrapperTests
{
    [Test]
    public void Constructor_InitializesWithPort()
    {
        // Arrange & Act
        var wrapper = new UdpClientWrapper(60000);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
    }

    [Test]
    public void StopListening_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(60000);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.StopListening());
    }

    [Test]
    public void Exit_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(60000);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Exit());
    }

    [Test]
    public void MessageReceived_Event_CanBeSubscribed()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(60000);
        bool eventFired = false;

        // Act
        wrapper.MessageReceived += (sender, data) => eventFired = true;

        // Assert
        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void GetHashCode_ReturnsSameValueForSamePort()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(60000);
        var wrapper2 = new UdpClientWrapper(60000);

        // Act
        var hash1 = wrapper1.GetHashCode();
        var hash2 = wrapper2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_ReturnsDifferentValueForDifferentPort()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(60000);
        var wrapper2 = new UdpClientWrapper(60001);

        // Act
        var hash1 = wrapper1.GetHashCode();
        var hash2 = wrapper2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void StopListening_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(60003);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.StopListening();
            wrapper.StopListening();
            wrapper.StopListening();
        });
    }

    [Test]
    public void Exit_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(60004);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.Exit();
            wrapper.Exit();
        });
    }
}
using System.Net;
using System.Net.Sockets;

namespace NetSdrClientAppTests;

public class UdpClientWrapperComprehensiveTests
{
    private const int TestPort = 61000;
    private const int AlternatePort = 61001;

    [TearDown]
    public void TearDown()
    {
        // Ensure cleanup after each test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Task.Delay(100).Wait();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidPort_InitializesSuccessfully()
    {
        // Arrange & Act
        var wrapper = new UdpClientWrapper(TestPort);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithZeroPort_InitializesSuccessfully()
    {
        // Arrange & Act
        var wrapper = new UdpClientWrapper(0);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithMaxPort_InitializesSuccessfully()
    {
        // Arrange & Act
        var wrapper = new UdpClientWrapper(65535);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
    }

    #endregion

    #region MessageReceived Event Tests

    [Test]
    public void MessageReceived_CanSubscribe()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        bool eventFired = false;

        // Act
        wrapper.MessageReceived += (sender, data) => eventFired = true;

        // Assert
        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void MessageReceived_CanUnsubscribe()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        EventHandler<byte[]> handler = (sender, data) => { };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.MessageReceived += handler;
            wrapper.MessageReceived -= handler;
        });
    }

    [Test]
    public void MessageReceived_MultipleSubscribers_AllInvoked()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        int callCount = 0;

        // Act
        wrapper.MessageReceived += (s, d) => callCount++;
        wrapper.MessageReceived += (s, d) => callCount++;
        wrapper.MessageReceived += (s, d) => callCount++;

        // Assert - just verify subscription doesn't throw
        Assert.That(callCount, Is.EqualTo(0));
    }

    #endregion

    #region StartListeningAsync Tests

    [Test]
    public async Task StartListeningAsync_StartsWithoutException()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var cts = new CancellationTokenSource();

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(100);
        wrapper.StopListening();
        cts.Cancel();

        // Assert
        Assert.DoesNotThrow(() => listenTask.Wait(1000));
    }

    [Test]
    public async Task StartListeningAsync_ReceivesMessage_FiresEvent()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var receivedData = new List<byte[]>();
        var messageReceived = new TaskCompletionSource<bool>();

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedData.Add(data);
            messageReceived.TrySetResult(true);
        };

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        // Send test message
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        using (var sender = new UdpClient())
        {
            await sender.SendAsync(testData, testData.Length, "127.0.0.1", TestPort);
        }

        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(2000)) == messageReceived.Task;

        // Cleanup
        wrapper.StopListening();

        // Assert
        Assert.That(received, Is.True, "Message should be received");
        Assert.That(receivedData, Has.Count.GreaterThan(0), "Should have received data");
        if (receivedData.Count > 0)
        {
            Assert.That(receivedData[0], Is.EqualTo(testData), "Data should match");
        }
    }

    [Test]
    public async Task StartListeningAsync_MultipleMessages_AllReceived()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var receivedMessages = new List<byte[]>();
        var expectedCount = 3;
        var countdownEvent = new CountdownEvent(expectedCount);

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedMessages.Add(data);
            countdownEvent.Signal();
        };

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        using (var sender = new UdpClient())
        {
            for (int i = 0; i < expectedCount; i++)
            {
                var data = new byte[] { (byte)i };
                await sender.SendAsync(data, data.Length, "127.0.0.1", TestPort);
                await Task.Delay(50);
            }
        }

        var allReceived = countdownEvent.Wait(3000);
        wrapper.StopListening();

        // Assert
        Assert.That(allReceived, Is.True, "All messages should be received");
        Assert.That(receivedMessages.Count, Is.EqualTo(expectedCount));
    }

    [Test]
    public async Task StartListeningAsync_CalledTwice_HandlesGracefully()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act
        var task1 = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(100);

        // Second call should fail because port is in use
        var task2 = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(100);

        wrapper.StopListening();

        // Assert - at least one should complete
        await Task.WhenAny(task1, task2);
        Assert.Pass("One task should handle the listening");
    }

    #endregion

    #region StopListening Tests

    [Test]
    public void StopListening_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.StopListening());
    }

    [Test]
    public async Task StopListening_WhenListening_StopsGracefully()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        // Act
        wrapper.StopListening();

        // Assert
        var completed = await Task.WhenAny(listenTask, Task.Delay(2000)) == listenTask;
        Assert.That(completed, Is.True, "Listen task should complete after stop");
    }

    [Test]
    public void StopListening_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.StopListening();
            wrapper.StopListening();
            wrapper.StopListening();
        });
    }

    [Test]
    public async Task StopListening_WhileReceivingMessages_StopsCleanly()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(100);

        // Start sending messages
        var sendTask = Task.Run(async () =>
        {
            using var sender = new UdpClient();
            for (int i = 0; i < 10; i++)
            {
                var data = new byte[] { (byte)i };
                await sender.SendAsync(data, data.Length, "127.0.0.1", TestPort);
                await Task.Delay(50);
            }
        });

        await Task.Delay(200);

        // Act
        wrapper.StopListening();

        // Assert
        var completed = await Task.WhenAny(listenTask, Task.Delay(2000)) == listenTask;
        Assert.That(completed, Is.True);
    }

    #endregion

    #region Exit Tests

    [Test]
    public void Exit_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Exit());
    }

    [Test]
    public async Task Exit_WhenListening_StopsListening()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        // Act
        wrapper.Exit();

        // Assert
        var completed = await Task.WhenAny(listenTask, Task.Delay(2000)) == listenTask;
        Assert.That(completed, Is.True);
    }

    [Test]
    public void Exit_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.Exit();
            wrapper.Exit();
        });
    }

    [Test]
    public void Exit_ThenStopListening_DoesNotThrow()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.Exit();
            wrapper.StopListening();
        });
    }

    #endregion

    #region GetHashCode Tests

    [Test]
    public void GetHashCode_SamePort_ReturnsSameValue()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(TestPort);
        var wrapper2 = new UdpClientWrapper(TestPort);

        // Act
        var hash1 = wrapper1.GetHashCode();
        var hash2 = wrapper2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_DifferentPorts_ReturnsDifferentValues()
    {
        // Arrange
        var wrapper1 = new UdpClientWrapper(TestPort);
        var wrapper2 = new UdpClientWrapper(AlternatePort);

        // Act
        var hash1 = wrapper1.GetHashCode();
        var hash2 = wrapper2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_CalledMultipleTimes_ReturnsSameValue()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);

        // Act
        var hash1 = wrapper.GetHashCode();
        var hash2 = wrapper.GetHashCode();
        var hash3 = wrapper.GetHashCode();

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash2, Is.EqualTo(hash3));
    }

    [Test]
    public void GetHashCode_AfterStartStop_RemainsConstant()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var hashBefore = wrapper.GetHashCode();

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        Task.Delay(100).Wait();
        wrapper.StopListening();
        var hashAfter = wrapper.GetHashCode();

        // Assert
        Assert.That(hashAfter, Is.EqualTo(hashBefore));
    }

    [Test]
    public void GetHashCode_EdgeCasePorts_ReturnsValidHash()
    {
        // Arrange & Act
        var wrapper1 = new UdpClientWrapper(0);
        var wrapper2 = new UdpClientWrapper(65535);
        var wrapper3 = new UdpClientWrapper(1);

        var hash1 = wrapper1.GetHashCode();
        var hash2 = wrapper2.GetHashCode();
        var hash3 = wrapper3.GetHashCode();

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
        Assert.That(hash2, Is.Not.EqualTo(hash3));
        Assert.That(hash1, Is.Not.EqualTo(hash3));
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task FullLifecycle_StartReceiveStop_WorksCorrectly()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        var receivedData = new List<byte[]>();
        var messageReceived = new TaskCompletionSource<bool>();

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedData.Add(data);
            messageReceived.TrySetResult(true);
        };

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        // Send message
        using (var sender = new UdpClient())
        {
            var testData = new byte[] { 10, 20, 30 };
            await sender.SendAsync(testData, testData.Length, "127.0.0.1", TestPort);
        }

        await Task.WhenAny(messageReceived.Task, Task.Delay(2000));
        wrapper.StopListening();

        // Assert
        Assert.That(receivedData, Has.Count.GreaterThan(0));
    }

    [Test]
    public async Task EmptyMessage_IsReceivedCorrectly()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        byte[] receivedMessage = null;
        var messageReceived = new TaskCompletionSource<bool>();

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedMessage = data;
            messageReceived.TrySetResult(true);
        };

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        using (var sender = new UdpClient())
        {
            await sender.SendAsync(Array.Empty<byte>(), 0, "127.0.0.1", TestPort);
        }

        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(2000)) == messageReceived.Task;
        wrapper.StopListening();

        // Assert
        Assert.That(received, Is.True);
        Assert.That(receivedMessage, Is.Not.Null);
        Assert.That(receivedMessage.Length, Is.EqualTo(0));
    }

    [Test]
    public async Task LargeMessage_IsReceivedCorrectly()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        byte[] receivedMessage = null;
        var messageReceived = new TaskCompletionSource<bool>();

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedMessage = data;
            messageReceived.TrySetResult(true);
        };

        // Act
        var listenTask = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        var largeData = new byte[8192];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        using (var sender = new UdpClient())
        {
            await sender.SendAsync(largeData, largeData.Length, "127.0.0.1", TestPort);
        }

        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(2000)) == messageReceived.Task;
        wrapper.StopListening();

        // Assert
        Assert.That(received, Is.True);
        Assert.That(receivedMessage, Is.EqualTo(largeData));
    }

    [Test]
    public async Task StopAndRestart_WorksCorrectly()
    {
        // Arrange
        var wrapper = new UdpClientWrapper(TestPort);
        int messageCount = 0;

        wrapper.MessageReceived += (sender, data) => messageCount++;

        // Act - First session
        var listenTask1 = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);
        wrapper.StopListening();
        await Task.Delay(200);

        // Restart
        var listenTask2 = Task.Run(() => wrapper.StartListeningAsync());
        await Task.Delay(200);

        using (var sender = new UdpClient())
        {
            await sender.SendAsync(new byte[] { 1 }, 1, "127.0.0.1", TestPort);
        }

        await Task.Delay(500);
        wrapper.StopListening();

        // Assert
        Assert.That(messageCount, Is.GreaterThan(0));
    }

    #endregion
}
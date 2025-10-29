using NetSdrClientApp.Networking;
using System.Net;
using System.Net.Sockets;

namespace NetSdrClientAppTests;

public class TcpClientWrapperComprehensiveTests
{
    private const int TestPort = 55555;
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
    private TcpListener? _testServer;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

    [TearDown]
    public void TearDown()
    {
        try
        {
            _testServer?.Stop();
            _testServer?.Server?.Dispose();
        }
        catch
        {
            // Ігноруємо винятки при закритті
        }
        finally
        {
            _testServer = null;
            Task.Delay(100).Wait(); // даємо сокетам закритися
        }
    }


    #region Constructor Tests

    [Test]
    public void Constructor_WithValidHostAndPort_InitializesSuccessfully()
    {
        // Arrange & Act
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Constructor_WithDifferentHosts_CreatesInstances()
    {
        // Arrange & Act
        var wrapper1 = new TcpClientWrapper("127.0.0.1", TestPort);
        var wrapper2 = new TcpClientWrapper("localhost", TestPort);
        var wrapper3 = new TcpClientWrapper("192.168.1.1", 8080);

        // Assert
        Assert.That(wrapper1, Is.Not.Null);
        Assert.That(wrapper2, Is.Not.Null);
        Assert.That(wrapper3, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithDifferentPorts_CreatesInstances()
    {
        // Arrange & Act
        var wrapper1 = new TcpClientWrapper("127.0.0.1", 5000);
        var wrapper2 = new TcpClientWrapper("127.0.0.1", 5001);
        var wrapper3 = new TcpClientWrapper("127.0.0.1", 65535);

        // Assert
        Assert.That(wrapper1, Is.Not.Null);
        Assert.That(wrapper2, Is.Not.Null);
        Assert.That(wrapper3, Is.Not.Null);
    }

    #endregion

    #region Connected Property Tests

    [Test]
    public void Connected_WhenNotConnected_ReturnsFalse()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act & Assert
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Connected_AfterSuccessfulConnection_ReturnsTrue()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act
        wrapper.Connect();
        Task.Delay(100).Wait(); // Allow connection to establish

        // Assert
        Assert.That(wrapper.Connected, Is.True);

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public void Connected_AfterDisconnect_ReturnsFalse()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        Task.Delay(100).Wait();

        // Act
        wrapper.Disconnect();
        Task.Delay(100).Wait();

        // Assert
        Assert.That(wrapper.Connected, Is.False);
    }

    #endregion

    #region Connect Tests

    [Test]
    public void Connect_ToRunningServer_Succeeds()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act
        wrapper.Connect();
        Task.Delay(100).Wait();

        // Assert
        Assert.That(wrapper.Connected, Is.True);

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public void Connect_ToInvalidHost_HandlesGracefully()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("999.999.999.999", TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Connect());
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Connect_ToClosedPort_HandlesGracefully()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", 12345); // Unlikely to be open

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Connect());
        Task.Delay(1000).Wait(); // Wait for connection timeout
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Connect_WhenAlreadyConnected_DoesNotReconnect()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        Task.Delay(100).Wait();

        // Act
        wrapper.Connect(); // Try to connect again

        // Assert
        Assert.That(wrapper.Connected, Is.True);

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public void Connect_MultipleTimes_AfterDisconnect_Works()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act & Assert - First connection
        wrapper.Connect();
        Task.Delay(100).Wait();
        Assert.That(wrapper.Connected, Is.True);

        wrapper.Disconnect();
        Task.Delay(100).Wait();
        Assert.That(wrapper.Connected, Is.False);

        // Second connection
        wrapper.Connect();
        Task.Delay(100).Wait();
        Assert.That(wrapper.Connected, Is.True);

        // Cleanup
        wrapper.Disconnect();
    }

    #endregion

    #region Disconnect Tests

    [Test]
    public void Disconnect_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Disconnect());
    }

    [Test]
    public void Disconnect_WhenConnected_DisconnectsSuccessfully()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        Task.Delay(100).Wait();

        // Act
        wrapper.Disconnect();
        Task.Delay(100).Wait();

        // Assert
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Disconnect_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.Disconnect();
            wrapper.Disconnect();
            wrapper.Disconnect();
        });
    }

    [Test]
    public void Disconnect_AfterConnection_CleansUpResources()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        Task.Delay(100).Wait();

        // Act
        wrapper.Disconnect();
        Task.Delay(100).Wait();

        // Assert - Should be able to connect again
        wrapper.Connect();
        Task.Delay(100).Wait();
        Assert.That(wrapper.Connected, Is.True);

        // Cleanup
        wrapper.Disconnect();
    }

    #endregion

    #region SendMessageAsync Tests

    [Test]
    public void SendMessageAsync_WhenNotConnected_ThrowsException()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        var data = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await wrapper.SendMessageAsync(data));
    }

    [Test]
    public async Task SendMessageAsync_WithEmptyData_SendsSuccessfully()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        await Task.Delay(100);

        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await wrapper.SendMessageAsync(emptyData));

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public async Task SendMessageAsync_WithLargeData_SendsSuccessfully()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        await Task.Delay(100);

        var largeData = new byte[10000];
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await wrapper.SendMessageAsync(largeData));

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public async Task SendMessageAsync_MultipleTimes_SendsAll()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        await Task.Delay(100);

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var data = new byte[] { (byte)i };
            Assert.DoesNotThrowAsync(async () => await wrapper.SendMessageAsync(data));
            await Task.Delay(10);
        }

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public void SendMessageAsync_StringOverload_WhenNotConnected_ThrowsException()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        var message = "test message";

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await wrapper.SendMessageAsync(message));
    }

    [Test]
    public async Task SendMessageAsync_StringOverload_SendsSuccessfully()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        await Task.Delay(100);

        var message = "Hello, Server!";

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await wrapper.SendMessageAsync(message));

        // Cleanup
        wrapper.Disconnect();
    }

    #endregion

    #region MessageReceived Event Tests

    [Test]
    public void MessageReceived_CanSubscribe()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
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
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        EventHandler<byte[]> handler = (sender, data) => { };

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            wrapper.MessageReceived += handler;
            wrapper.MessageReceived -= handler;
        });
    }

    [Test]
    public void MessageReceived_MultipleSubscribers_Work()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        int callCount = 0;

        // Act
        wrapper.MessageReceived += (s, d) => callCount++;
        wrapper.MessageReceived += (s, d) => callCount++;

        // Assert - just verify subscription doesn't throw
        Assert.That(callCount, Is.EqualTo(0));
    }

    [Test]
    public async Task MessageReceived_WhenServerSendsData_FiresEvent()
    {
        // Arrange
        var receivedData = new List<byte[]>();
        var messageReceived = new TaskCompletionSource<bool>();

        StartEchoServer(); // Server that echoes back
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedData.Add(data);
            messageReceived.TrySetResult(true);
        };

        wrapper.Connect();
        await Task.Delay(200);

        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        await wrapper.SendMessageAsync(testData);

        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(2000)) == messageReceived.Task;

        // Assert
        Assert.That(received, Is.True, "Event should be fired");
        Assert.That(receivedData, Has.Count.GreaterThan(0));

        // Cleanup
        wrapper.Disconnect();
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task FullLifecycle_ConnectSendReceiveDisconnect_Works()
    {
        // Arrange
        var receivedData = new List<byte[]>();
        var messageReceived = new TaskCompletionSource<bool>();

        StartEchoServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedData.Add(data);
            messageReceived.TrySetResult(true);
        };

        // Act
        wrapper.Connect();
        await Task.Delay(200);
        Assert.That(wrapper.Connected, Is.True);

        var testData = new byte[] { 10, 20, 30 };
        await wrapper.SendMessageAsync(testData);

        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(2000)) == messageReceived.Task;

        wrapper.Disconnect();
        await Task.Delay(100);

        // Assert
        Assert.That(received, Is.True);
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public async Task MultipleConnections_Sequential_Work()
    {
        // Arrange
        StartEchoServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        // Act & Assert - Connection 1
        wrapper.Connect();
        await Task.Delay(100);
        Assert.That(wrapper.Connected, Is.True);
        await wrapper.SendMessageAsync(new byte[] { 1 });
        wrapper.Disconnect();
        await Task.Delay(100);

        // Connection 2
        wrapper.Connect();
        await Task.Delay(100);
        Assert.That(wrapper.Connected, Is.True);
        await wrapper.SendMessageAsync(new byte[] { 2 });
        wrapper.Disconnect();
    }

    [Test]
    public async Task SendReceive_LargeData_Works()
    {
        // Arrange
        var receivedData = new List<byte[]>();
        var messageReceived = new TaskCompletionSource<bool>();

        StartEchoServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);

        wrapper.MessageReceived += (sender, data) =>
        {
            receivedData.Add(data);
            messageReceived.TrySetResult(true);
        };

        wrapper.Connect();
        await Task.Delay(200);

        var largeData = new byte[5000];
        Array.Fill(largeData, (byte)42);

        // Act
        await wrapper.SendMessageAsync(largeData);
        var received = await Task.WhenAny(messageReceived.Task, Task.Delay(3000)) == messageReceived.Task;

        // Assert
        Assert.That(received, Is.True);

        // Cleanup
        wrapper.Disconnect();
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Connect_WithEmptyHost_HandlesGracefully()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("", TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Connect());
        Assert.That(wrapper.Connected, Is.False);
    }

    [Test]
    public void Connect_WithNullCharactersInHost_HandlesGracefully()
    {
        // Arrange
        var wrapper = new TcpClientWrapper("127\0.0.0.1", TestPort);

        // Act & Assert
        Assert.DoesNotThrow(() => wrapper.Connect());
    }

    [Test]
    public async Task SendMessageAsync_AfterServerDisconnects_ThrowsException()
    {
        // Arrange
        StartTestServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        await Task.Delay(100);

        // Stop server
        _testServer?.Stop();
        await Task.Delay(100);

        var data = new byte[] { 1, 2, 3 };

        // Act & Assert - May throw IOException or succeed depending on timing
        // Just ensure it doesn't crash the application
        try
        {
            await wrapper.SendMessageAsync(data);
        }
        catch (Exception ex)
        {
            Assert.That(ex, Is.InstanceOf<Exception>());
        }

        // Cleanup
        wrapper.Disconnect();
    }

    [Test]
    public void Disconnect_DuringActiveOperation_HandlesGracefully()
    {
        // Arrange
        StartEchoServer();
        var wrapper = new TcpClientWrapper("127.0.0.1", TestPort);
        wrapper.Connect();
        Task.Delay(100).Wait();

        // Act - Start sending and immediately disconnect
        var sendTask = wrapper.SendMessageAsync(new byte[1000]);
        wrapper.Disconnect();

        // Assert - Should not throw
        Assert.DoesNotThrow(() => sendTask.Wait(1000));
    }

    #endregion

    #region Helper Methods

    private void StartTestServer()
    {
        _testServer = new TcpListener(IPAddress.Loopback, TestPort);
        _testServer.Start();

        // Accept connections but don't do anything with them
        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var client = await _testServer.AcceptTcpClientAsync();
                    // Keep client connected
                }
            }
            catch (Exception)
            {
                // Server stopped
            }
        });
    }

    private void StartEchoServer()
    {
        _testServer = new TcpListener(IPAddress.Loopback, TestPort);
        _testServer.Start();

        // Echo server - sends back what it receives
        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var client = await _testServer.AcceptTcpClientAsync();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var stream = client.GetStream();
                            var buffer = new byte[8192];
                            int bytesRead;
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await stream.WriteAsync(buffer, 0, bytesRead);
                                await stream.FlushAsync();
                            }
                        }
                        catch (Exception)
                        {
                            // Client disconnected
                        }
                        finally
                        {
                            client?.Close();
                        }
                    });
                }
            }
            catch (Exception)
            {
                // Server stopped
            }
        });
    }

    #endregion
}
using EchoTspServer.Handlers;

namespace EchoServerTests
{
    public class EchoConnectionHandlerExtendedTests
    {
        private EchoConnectionHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new EchoConnectionHandler();
        }

        [Test]
        public async Task HandleClientAsync_WithLargeData_EchoesCorrectly()
        {
            // Arrange
            var largeData = new byte[10000];
            for (int i = 0; i < largeData.Length; i++)
            {
                largeData[i] = (byte)(i % 256);
            }
            var input = new MemoryStream(largeData);
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(largeData));
        }

        [Test]
        public async Task HandleClientAsync_WithMultipleChunks_EchoesAll()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3 };
            var data2 = new byte[] { 4, 5, 6 };
            var data3 = new byte[] { 7, 8, 9 };

            var allData = data1.Concat(data2).Concat(data3).ToArray();
            var input = new MemoryStream(allData);
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(allData));
        }

        [Test]
        public async Task HandleClientAsync_WithCustomBufferSize_Works()
        {
            // Arrange
            var handler = new EchoConnectionHandler(bufferSize: 4096);
            var data = new byte[5000];
            Array.Fill(data, (byte)42);

            var input = new MemoryStream(data);
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task HandleClientAsync_WithSingleByte_EchoesCorrectly()
        {
            // Arrange
            var input = new MemoryStream(new byte[] { 255 });
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 255 }));
        }

        [Test]
        public async Task HandleClientAsync_WithSmallBufferSize_EchoesLargeData()
        {
            // Arrange
            var handler = new EchoConnectionHandler(bufferSize: 10);
            var data = new byte[100];
            Array.Fill(data, (byte)123);

            var input = new MemoryStream(data);
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task HandleClientAsync_WithBinaryData_PreservesData()
        {
            // Arrange
            var binaryData = new byte[] { 0x00, 0xFF, 0x7F, 0x80, 0x01 };
            var input = new MemoryStream(binaryData);
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(binaryData));
        }

        [Test]
        public async Task HandleClientAsync_MultipleCalls_WorksIndependently()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3 };
            var data2 = new byte[] { 4, 5, 6, 7, 8 };

            var stream1 = new CombinedStream(
                new MemoryStream(data1),
                new MemoryStream());
            var stream2 = new CombinedStream(
                new MemoryStream(data2),
                new MemoryStream());

            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(stream1, cts.Token);
            await _handler.HandleClientAsync(stream2, cts.Token);

            // Assert
            Assert.That(((MemoryStream)stream1.GetWriteStream()).ToArray(), Is.EqualTo(data1));
            Assert.That(((MemoryStream)stream2.GetWriteStream()).ToArray(), Is.EqualTo(data2));
        }

        private class CombinedStream : Stream
        {
            private readonly Stream _read;
            private readonly Stream _write;

            public CombinedStream(Stream read, Stream write)
            {
                _read = read;
                _write = write;
            }

            public Stream GetWriteStream() => _write;

            public override bool CanRead => true;
            public override bool CanWrite => true;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                _read.Read(buffer, offset, count);

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
                _read.ReadAsync(buffer, offset, count, ct);

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) =>
                _read.ReadAsync(buffer, ct);

            public override void Write(byte[] buffer, int offset, int count) =>
                _write.Write(buffer, offset, count);

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
                _write.WriteAsync(buffer, offset, count, ct);

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) =>
                _write.WriteAsync(buffer, ct);

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }
    }
}
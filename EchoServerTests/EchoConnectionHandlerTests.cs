using EchoTspServer.Handlers;

namespace EchoServerTests
{
    public class EchoConnectionHandlerTests
    {
        private EchoConnectionHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new EchoConnectionHandler();
        }

        [Test]
        public async Task HandleClientAsync_EchoesDataCorrectly()
        {
            // Arrange
            var input = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var output = new MemoryStream();
            var combinedStream = new CombinedStream(input, output);
            var cts = new CancellationTokenSource();

            // Act
            await _handler.HandleClientAsync(combinedStream, cts.Token);

            // Assert
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public async Task HandleClientAsync_HandlesEmptyStream()
        {
            // Arrange
            var stream = new MemoryStream();
            var cts = new CancellationTokenSource();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _handler.HandleClientAsync(stream, cts.Token));
        }

        [Test]
        public async Task HandleClientAsync_RespectsCancellation()
        {
            // Arrange
            var stream = new MemoryStream(new byte[100]);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            var ex = Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _handler.HandleClientAsync(stream, cts.Token));

            Assert.That(ex, Is.Not.Null);
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

            public override bool CanRead => true;
            public override bool CanWrite => true;
            public override bool CanSeek => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

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

        private class SlowMemoryStream : MemoryStream
        {
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
            {
                await Task.Delay(10000, ct);
                return 0;
            }
        }
    }
}
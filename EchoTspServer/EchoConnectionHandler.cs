using System.Buffers;

namespace EchoTspServer.Handlers
{
    public class EchoConnectionHandler : IConnectionHandler
    {
        private readonly int _bufferSize;

        public EchoConnectionHandler(int bufferSize = 8192)
        {
            _bufferSize = bufferSize;
        }

        public async Task HandleClientAsync(Stream stream, CancellationToken token)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            try
            {
                int bytesRead;
                while (!token.IsCancellationRequested &&
                       (bytesRead = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize), token)) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
                }

                token.ThrowIfCancellationRequested();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
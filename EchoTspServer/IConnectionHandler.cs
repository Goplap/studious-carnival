namespace EchoTspServer.Handlers
{
    public interface IConnectionHandler
    {
        Task HandleClientAsync(Stream stream, CancellationToken token);
    }
}
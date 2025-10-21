using EchoTspServer;
using EchoTspServer.Handlers;
using EchoTspServer.Networking;

var listener = new TcpListenerWrapper(5000);
var handler = new EchoConnectionHandler();
var server = new EchoServer(listener, handler);

_ = Task.Run(() => server.StartAsync());

Console.WriteLine("Echo server started. Press 'q' to quit...");
while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q) { }

server.Stop();
Console.WriteLine("Server stopped.");
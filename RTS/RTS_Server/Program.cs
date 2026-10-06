using RTS;

internal class Program
{
	public static async Task Main(string[] args)
	{
		Server server = new();

		server.onConnected += c =>
		{
			Console.WriteLine("Connected");
			Console.WriteLine($"TcpClient connected '{c.Client.RemoteEndPoint}'");
		};
		
		server.Start(6000, new ConsoleLogger());

		while (true)
		{
			await Task.Delay(100);
		}
		
		server.Stop();
	}
}
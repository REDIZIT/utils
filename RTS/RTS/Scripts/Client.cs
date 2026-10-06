using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RTS
{
	public class Client
	{
		public Action onConnected;
		public Action onDisconnected;
		public Action<IMessage> onMessageReceived;
		
		private TcpClient tcp = new();
		
		public ClientSession session;

		private ILogger logger;

		public Client(ILogger logger)
		{
			this.logger = logger;
		}
		
		public async Task Connect(string host, int port)
		{
			try
			{
				tcp.SendBufferSize = 0;

				await tcp.ConnectAsync(host, port);
				
				session = new(null, tcp, _ => onDisconnected?.Invoke(), logger, OnMessageReceived);
				session.Start();
				
				onConnected?.Invoke();
			}
			catch
			{
			}
		}

		public void Disconnect()
		{
			session?.Disconnect();
			tcp?.Dispose();
		}

		public Bucket Send(IMessage message)
		{
			return session.Send(message);
		}

		private void OnMessageReceived(ClientSession session, IMessage message)
		{
			logger.LogDebug("Message received");
			onMessageReceived?.Invoke(message);
		}
	}
}
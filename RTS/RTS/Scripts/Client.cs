using System;
using System.Diagnostics;
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
		
		public ClientSession session;

		private TcpClient tcp = new();
		private ILogger logger;
		private RequestResponseTable table = new();

		public Client(ILogger logger)
		{
			this.logger = logger;
		}
		
		public async Task Connect(string host, int port)
		{
			try
			{
				// tcp.SendBufferSize = 0;
				tcp.NoDelay = true;

				await tcp.ConnectAsync(host, port).ConfigureAwait(false);;
				
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

		public async Task<TResponse> Send<TResponse, TRequest>(TRequest request) where TRequest : IMessage, ITrackableMessage where TResponse : ITrackableMessage
		{
			Task<ITrackableMessage> responseAwaitTask = table.RegisterRequest(request);
			Stopwatch w1 = Stopwatch.StartNew();
			Send(request);
			w1.Stop();
			logger.LogDebug($"Send.Send in {w1.ElapsedMilliseconds} ms");

			Stopwatch w2 = Stopwatch.StartNew();
			ITrackableMessage response = await responseAwaitTask.ConfigureAwait(false);
			logger.LogDebug($"Awaited in {w2.ElapsedMilliseconds} ms");
			
			return (TResponse)response;
		}

		private void OnMessageReceived(ClientSession session, IMessage message)
		{
			logger.LogDebug("Message received");
			onMessageReceived?.Invoke(message);

			if (message is ITrackableMessage trackableMessage)
			{
				table.TryFireResponse(trackableMessage);
			}
		}
	}
}
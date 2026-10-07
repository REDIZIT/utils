using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RTS
{
	public class Client
	{
		public Action onConnected;
		public Action onDisconnected;
		public Action<object> onMessageReceived;
		
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

				await tcp.ConnectAsync(host, port).ConfigureAwait(false);
				
				session = new(null, tcp, _ => onDisconnected?.Invoke(), logger, OnMessageReceived, false);
				session.Start();
				
				onConnected?.Invoke();
			}
			catch
			{
			}
		}

		public void Disconnect()
		{
			if (tcp != null)
			{
				session?.Disconnect();
				tcp?.Dispose();
				tcp = null;
			}
		}

		public Bucket Send(object message, int requestID)
		{
			return session.Send(message, requestID);
		}

		public async Task<TResponse> Send<TRequest, TResponse>(TRequest request)
		{
			table.RegisterRequest(out Task<object> task, out int requestID);
			
			Send(request!, requestID);
			
			object response = await task.ConfigureAwait(false);
			return (TResponse)response;
		}

		private void OnMessageReceived(MessageContext ctx)
		{
			onMessageReceived?.Invoke(ctx.message);
			table.TryFireResponse(ctx.message, ctx.requestID);
		}
	}
}
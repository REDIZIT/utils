using System;
using System.Collections.Generic;
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

		public Bucket Send(IMessage message, Dictionary<string, string> meta)
		{
			return session.Send(message, meta);
		}

		public async Task<TResponse> Send<TRequest, TResponse>(TRequest request) where TRequest : IMessage where TResponse : IMessage
		{
			table.RegisterRequest(out Task<IMessage> task, out int requestID);

			Dictionary<string, string> meta = new();
			meta["REQUEST_ID"] = requestID.ToString();
			
			Send(request, meta);
			
			IMessage response = await task.ConfigureAwait(false);
			return (TResponse)response;
		}

		private void OnMessageReceived(ClientSession session, IMessage message, Dictionary<string, string> meta)
		{
			onMessageReceived?.Invoke(message);

			if (meta.TryGetValue("REQUEST_ID", out string requestID))
			{
				table.TryFireResponse(message, int.Parse(requestID));
			}
		}
	}
}
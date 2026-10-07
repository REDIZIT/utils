using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RTS
{
	public class Server
	{
		public Action<TcpClient> onConnected;
		
		private TcpListener listener;
		private Task mainTask;
		private CancellationTokenSource cts;

		private List<ClientSession> sessions = new();
		private ILogger logger;

		public void Start(int port, ILogger logger)
		{
			this.logger = logger;
			
			cts = new();
			
			listener = new(IPAddress.Any, port);
			listener.Start();

			mainTask = Main();
		}

		public void Stop()
		{
			cts.Cancel();
			listener.Stop();
		}

		private async Task Main()
		{
			while (cts.IsCancellationRequested == false)
			{
				TcpClient tcp = await listener.AcceptTcpClientAsync();
				// tcp.SendBufferSize = 0;
				tcp.NoDelay = true;
				
				onConnected?.Invoke(tcp);

				string id = Guid.NewGuid().ToString();
				ClientSession session = new(id, tcp, OnSessionDisconnected, logger, OnMessageReceived);
				sessions.Add(session);

				session.Start();
			}
		}

		private void OnSessionDisconnected(ClientSession session)
		{
			sessions.Remove(session);
		}

		private void OnMessageReceived(ClientSession session, IMessage m)
		{
			if (m is TestMessage testReq)
			{
				logger.LogDebug("Sending test response");
				Bucket bucket = session.Send(new TestResponse()
				{
					response = "Tuntuntun",
					RequestID = testReq.RequestID
				});
				logger.LogDebug($"Response bucket id: {bucket.id}");
			}
		}
	}
}
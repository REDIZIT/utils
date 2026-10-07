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
				ClientSession session = new(id, tcp, OnSessionDisconnected, logger, OnMessageReceived, true);
				sessions.Add(session);

				session.Start();
			}
		}

		private void OnSessionDisconnected(ClientSession session)
		{
			sessions.Remove(session);
		}

		private void OnMessageReceived(ClientSession session, IMessage m, Dictionary<string, string> meta)
		{
			int requestID = meta.TryGetValue("REQUEST_ID", out string str) ? int.Parse(str) : 0;
			Dictionary<string, string> respondMeta = new();
			respondMeta["REQUEST_ID"] = requestID.ToString();
			
			if (m is TestMessage testReq)
			{
				logger.LogDebug($"[ROUTER] Return TestResponse");
				session.Send(new TestResponse()
				{
					response = "Tuntuntun",
				}, respondMeta);
			}
			else if (m is TestHeavyRequest heavyRequest)
			{
				logger.LogDebug($"[ROUTER] Return HeavyResponse");
				session.Send(new TestHeavyResponse()
				{
					shortResponse = "Short response",
				}, respondMeta);
			}
			else
			{
				logger.LogError($"[ROUTER] Unknown message '{m.GetType().Name}'");
			}
		}
	}
}
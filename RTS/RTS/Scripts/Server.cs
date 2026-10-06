using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public class Server
	{
		public Action<TcpClient> onConnected;
		
		private TcpListener listener;
		private Task mainTask;
		private CancellationTokenSource cts;

		private List<ClientSession> sessions = new();

		public void Start(int port)
		{
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
				onConnected?.Invoke(tcp);

				string id = Guid.NewGuid().ToString();
				ClientSession session = new(id, tcp, OnSessionDisconnected);
				sessions.Add(session);

				session.Start();
			}
		}

		private void OnSessionDisconnected(string id)
		{
			sessions.RemoveAll(s => s.id == id);
		}
	}
}
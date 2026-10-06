using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public class Client
	{
		public Action onConnected;
		public Action onDisconnected;
		
		private TcpClient tcp = new();
		
		private CancellationTokenSource cts;
		private ClientSession session;
		
		public async Task Connect(string host, int port)
		{
			try
			{
				cts = new();
				tcp.SendBufferSize = 0;

				await tcp.ConnectAsync(host, port);
				
				session = new(null, tcp, _ => onDisconnected());
				session.Start();
				
				onConnected?.Invoke();
			}
			catch
			{
			}
		}

		public void Disconnect()
		{
			cts?.Cancel();
			tcp?.Close();
		}

		public Bucket Send(IMessage message)
		{
			return session.Send(message);
		}
		
		private void SetDirty()
		{
			session.SetDirty();
		}
	}
}
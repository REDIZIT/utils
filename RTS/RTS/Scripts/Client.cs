using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public class Client
	{
		public ConnectionStatus ConnectionStatus { get; private set; }

		public Action onConnected;
		public Action onDisconnected;
		
		private TcpClient tcp = new();
		private Buckets buckets = new(5 * 1024 * 1024); // 5 MB
		private SemaphoreSlim dirtySemaphore = new(0);
		private CancellationTokenSource cts;
		private Serializer serializer = new(registry);

		private static TypesRegistry registry = new();
		
		public async Task Connect(string host, int port)
		{
			try
			{
				ConnectionStatus = ConnectionStatus.Connecting;
				cts = new();
				tcp.SendBufferSize = 0;
				await tcp.ConnectAsync(host, port);
				ConnectionStatus = ConnectionStatus.Connected;
				onConnected?.Invoke();

				_ = Run(cts.Token);
			}
			catch
			{
				ConnectionStatus = ConnectionStatus.ConnectionFailed;
			}
		}

		public void Disconnect()
		{
			cts?.Cancel();
			dirtySemaphore.Release();
			tcp?.Close();
			ConnectionStatus = ConnectionStatus.Disconnected;
		}

		public Bucket Send(IMessage message)
		{
			byte[] bytes = serializer.Serialize(message);
			if (buckets.TryAllocateFrom(bytes, out Bucket bucket) == false) throw new("Failed to allocate bucket");
			SetDirty();
			return bucket;
		}

		private async Task Run(CancellationToken token)
		{
			int maxPartSize = 5 * 1024; // 5 KB
			while (token.IsCancellationRequested == false)
			{
				await dirtySemaphore.WaitAsync(token);
				
				NetworkStream stream = tcp.GetStream();
				BinaryWriter w = new(stream);
				
				while (buckets.pendingBuckets.Any())
				{
					Bucket bucket = NextBucket();

					if (bucket.isRegistered == false)
					{
						bucket.isRegistered = true;
						
						w.Write((byte)MessageType.BucketCreate);
						w.Write((int)bucket.id);
						w.Write((int)bucket.bytes.Length);
					}
					else
					{
						BucketPart part = bucket.Next(maxPartSize);
						
						w.Write((byte)MessageType.BucketPart);
						w.Write((int)part.bucketID);
						w.Write((int)part.bytes.Length);
						await stream.WriteAsync(part.bytes, token);
					}
				}
			}
		}
		
		private void SetDirty()
		{
			if (dirtySemaphore.CurrentCount == 0) dirtySemaphore.Release();
		}

		private Bucket NextBucket()
		{
			return buckets.pendingBuckets.First();
		}
	}

	public enum MessageType : byte
	{
		Invalid,
		BucketCreate,
		BucketPart,
		BucketDrop
	}
	
	public struct Message
	{
		
	}
}
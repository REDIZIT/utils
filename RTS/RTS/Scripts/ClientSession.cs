using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RTS
{
	public class ClientSession
	{
		public string id;
		
		private TcpClient tcp;
		private Action<string> onDisconnected;
		private CancellationTokenSource cts = new();

		private Buckets buckets = new(5 * 1024 * 1024); // 5 MB
		private Serializer serializer = new(registry);

		private static TypesRegistry registry = new();

		public ClientSession(string id, TcpClient tcp, Action<string> onDisconnected)
		{
			this.id = id;
			this.tcp = tcp;
			this.onDisconnected = onDisconnected;
		}

		public void Start()
		{
			_ = Run(cts.Token);
		}
		
		private async Task Run(CancellationToken token)
		{
			NetworkStream stream = tcp.GetStream();
			BinaryReader r = new(stream);
			
			while (tcp.Connected)
			{
				try
				{
					while (!token.IsCancellationRequested)
					{
						Console.WriteLine("\nWaiting message...");

						MessageType type = (MessageType)r.ReadByte();
						Console.WriteLine($"Message type: {type}");

						int bucketID = r.ReadInt32();

						if (type == MessageType.BucketCreate)
						{
							int bucketSize = r.ReadInt32();
							if (buckets.TryAllocate(bucketID, bucketSize, out _) == false)
							{
								Disconnect();
							}
							else
							{
								Console.WriteLine($"Bucket {bucketID} with size {bucketSize} created");
							}
						}
						else if (type == MessageType.BucketPart)
						{
							int partLength = r.ReadInt32();
							byte[] bytes = r.ReadBytes(partLength);

							Bucket bucket = buckets.Write(bucketID, bytes);

							Console.WriteLine($"Bucket {bucketID} part with size {partLength} written");

							if (bucket.IsCompleted)
							{
								Console.WriteLine($"Bucket completed with {bucket.bytes.Length} bytes: {string.Join(" ", bucket.bytes.Select(b => b.ToString("x2")))}");

								IMessage message = serializer.Deserialize(bucket.bytes);
								Console.WriteLine($"IMessage type: {message.GetType().Name}");
							}
						}
						else
						{
							Disconnect();
						}
					}
				}
				catch (OperationCanceledException)
				{
					// CancellationToken normally fired
				}
				catch (EndOfStreamException)
				{
					// BinaryReader throws exception if client is disconnected (attempt to read data beyond the end of stream)
					// Explicit call Disconnect()
					Disconnect();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Message read exception: {ex.Message} at {ex.StackTrace}");
					Disconnect();
				}
			}

			Disconnect();
		}

		public void Disconnect()
		{
			Console.WriteLine("Session disconnected");
			
			cts?.Cancel();
			tcp?.Dispose();
			onDisconnected?.Invoke(id);
		}
	}
}
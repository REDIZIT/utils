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
		private SemaphoreSlim dirtySemaphore = new(0);

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
			_ = RunSending(cts.Token);
			_ = RunReceiving(cts.Token);
		}
		
		public Bucket Send(IMessage message)
		{
			byte[] bytes = serializer.Serialize(message);
			if (buckets.TryAllocateFrom(bytes, out Bucket bucket) == false) throw new("Failed to allocate bucket");
			SetDirty();
			return bucket;
		}

		public void SetDirty()
		{
			if (dirtySemaphore.CurrentCount == 0) dirtySemaphore.Release();
		}

		private async Task RunSending(CancellationToken token)
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
		
		private async Task RunReceiving(CancellationToken token)
		{
		    NetworkStream stream = tcp.GetStream();

		    try
		    {
			    AsyncBinaryReader r = new(stream, token);
			    
		        while (!token.IsCancellationRequested && tcp.Connected)
		        {
		            MessageType type = (MessageType)await r.ReadByte();
		            Console.WriteLine($"Message type: {type}");

		            int bucketID = await r.ReadInt();

		            if (type == MessageType.BucketCreate)
		            {
		                int bucketSize = await r.ReadInt();

		                if (buckets.TryAllocate(bucketID, bucketSize, out _) == false)
		                {
		                    Disconnect();
		                    break;
		                }
		                Console.WriteLine($"Bucket {bucketID} with size {bucketSize} created");
		            }
		            else if (type == MessageType.BucketPart)
		            {
		                int partLength = await r.ReadInt();

		                byte[] bytes = new byte[partLength];
		                await stream.ReadExactlySafe(bytes, bytes.Length, token);

		                Bucket bucket = buckets.Write(bucketID, bytes);
		                Console.WriteLine($"Bucket {bucketID} part with size {partLength} written");

		                if (bucket.IsCompleted)
		                {
		                    Console.WriteLine($"Bucket completed");
		                    IMessage message = serializer.Deserialize(bucket.bytes);
		                    Console.WriteLine($"IMessage type: {message.GetType().Name}");
		                    
		                    
		                }
		            }
		            else
		            {
		                Disconnect();
		                break;
		            }
		        }
		    }
		    catch (OperationCanceledException)
		    {
		        // Штатная отмена токена
		    }
		    catch (Exception ex)
		    {
		        Console.WriteLine($"Message read exception: {ex.Message} at {ex.StackTrace}");
		    }
		    finally
		    {
		        Disconnect();
		    }
		}

		public void Disconnect()
		{
			Console.WriteLine("Session disconnected");
			
			cts?.Cancel();
			tcp?.Dispose();
			onDisconnected?.Invoke(id);
		}
		
		private Bucket NextBucket()
		{
			return buckets.pendingBuckets.First();
		}
	}
}
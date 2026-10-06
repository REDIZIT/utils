using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RTS
{
	public class ClientSession
	{
		public string id;
		
		private TcpClient tcp;
		private Action<string> onDisconnected;
		private CancellationTokenSource cts = new();
		private SemaphoreSlim dirtySemaphore = new(0);

		public Buckets buckets = new(5 * 1024 * 1024); // 5 MB
		private Serializer serializer = new(registry);
		private ILogger logger;

		private static TypesRegistry registry = new();

		public ClientSession(string id, TcpClient tcp, Action<string> onDisconnected, ILogger logger)
		{
			this.id = id;
			this.tcp = tcp;
			this.onDisconnected = onDisconnected;
			this.logger = logger;
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
			bucket.state = Bucket.State.QueuedToSend;
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
				AsyncBinaryWriter w = new(stream, token);
				
				while (buckets.pendingBuckets.Any())
				{
					Bucket bucket = NextBucket();

					if (bucket.state == Bucket.State.QueuedToSend)
					{
						bucket.state = Bucket.State.Sending;
						
						await w.Write((byte)MessageType.BucketCreate);
						await w.Write((int)bucket.id);
						await w.Write((int)bucket.bytes.Length);
					}
					else if (bucket.state == Bucket.State.Sending && bucket.BytesToEnd > 0)
					{
						BucketPart part = bucket.Next(maxPartSize);
						
						await w.Write((byte)MessageType.BucketPart);
						await w.Write((int)part.bucketID);
						await w.Write((int)part.bytes.Length);
						await stream.WriteAsync(part.bytes, token);

						if (bucket.BytesToEnd <= 0) bucket.state = Bucket.State.Sent;
					}
					else if (bucket.state == Bucket.State.Received)
					{
						logger.LogDebug("Bucket received, free local bucket");
						bucket.state = Bucket.State.ReceivedReported;
						
						await w.Write((byte)MessageType.BucketReceived);
						await w.Write((int)bucket.id);
						
						if (buckets.TryFree(bucket.id) == false)
						{
							throw new($"Failed to free received bucket {bucket.id}");
						}
					}
				}
			}
		}
		
		private async Task RunReceiving(CancellationToken token)
		{
		    try
		    {
			    NetworkStream stream = tcp.GetStream();
			    AsyncBinaryReader r = new(stream, token);
			    
		        while (!token.IsCancellationRequested && tcp.Connected)
		        {
		            MessageType type = (MessageType)await r.ReadByte();
		            logger.LogDebug($"Message type: {type}");

		            int bucketID = await r.ReadInt();

		            if (type == MessageType.BucketCreate)
		            {
		                int bucketSize = await r.ReadInt();

		                if (buckets.TryAllocate(bucketID, bucketSize, out Bucket bucket) == false)
		                {
		                    Disconnect();
		                    break;
		                }
		                bucket.state = Bucket.State.Receiving;
		                
		                logger.LogDebug($"Bucket {bucketID} with size {bucketSize} created");
		            }
		            else if (type == MessageType.BucketPart)
		            {
		                int partLength = await r.ReadInt();

		                byte[] bytes = new byte[partLength];
		                await stream.ReadExactlySafe(bytes, bytes.Length, token);

		                Bucket bucket = buckets.Write(bucketID, bytes);
		                logger.LogDebug($"Bucket {bucketID} part with size {partLength} written");

		                if (bucket.BytesToEnd <= 0)
		                {
			                bucket.state = Bucket.State.Received;
			                SetDirty();
			                
			                logger.LogDebug($"Bucket completed");
		                    IMessage message = serializer.Deserialize(bucket.bytes);
		                    logger.LogDebug($"IMessage type: {message.GetType().Name}");
		                }
		            }
		            else if (type == MessageType.BucketReceived)
		            {
			            logger.LogDebug($"BucketReceived received");
			            
			            if (buckets.TryFree(bucketID) == false)
			            {
				            throw new($"Failed to free remotely received bucket {bucketID}");
			            }
		            }
		            else
		            {
			            throw new($"Unknown MessageType: {type} ({(byte)type})");
		            }
		        }
		    }
		    catch (OperationCanceledException)
		    {
		        // Штатная отмена токена
		    }
		    catch (Exception e)
		    {
			    logger.LogError($"Message read exception: {e.StackTrace}");
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
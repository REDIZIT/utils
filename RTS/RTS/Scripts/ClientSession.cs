using System;
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
		private Action<ClientSession> onDisconnected;
		private CancellationTokenSource cts = new();

		private Buckets buckets = new(5 * 1024 * 1024); // 5 MB
		private Serializer serializer = new(registry);
		private ILogger logger;
		private Action<ClientSession, IMessage> onMessageReceived;
		
		private volatile TaskCompletionSource<bool> dirtyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		
		private static TypesRegistry registry = new();

		public ClientSession(string id, TcpClient tcp, Action<ClientSession> onDisconnected, ILogger logger, Action<ClientSession, IMessage> onMessageReceived)
		{
			this.id = id;
			this.tcp = tcp;
			this.onDisconnected = onDisconnected;
			this.logger = logger;
			this.onMessageReceived = onMessageReceived;
		}

		public void Start()
		{
			_ = RunSending(cts.Token);
			_ = RunReceiving(cts.Token);
		}
		
		public Bucket Send(IMessage message)
		{
			byte[] bytes = serializer.Serialize(message);
			Bucket bucket = buckets.AllocateFrom(bytes);
			bucket.state = Bucket.State.QueuedToSend;
			SetDirty();
			return bucket;
		}

		public void SetDirty()
		{
			dirtyTcs.TrySetResult(true);
		}

		private async Task RunSending(CancellationToken token)
		{
			try
			{
				NetworkStream stream = tcp.GetStream();
				AsyncBinaryWriter w = new(stream, token);
		        int maxPartSize = 5 * 1024; // 5 KB

		        while (!token.IsCancellationRequested)
		        {
		            TaskCompletionSource<bool> tcs = dirtyTcs;
		            if (!tcs.Task.IsCompleted)
		            {
		                await tcs.Task.ConfigureAwait(false);
		            }
		            Interlocked.CompareExchange(ref dirtyTcs, new(TaskCreationOptions.RunContinuationsAsynchronously), tcs);

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
		                    await w.WriteBytes(part.bytes);

		                    if (bucket.BytesToEnd <= 0) bucket.state = Bucket.State.Sent;
		                }
		                else if (bucket.state == Bucket.State.Received)
		                {
		                    bucket.state = Bucket.State.ReceivedReported;
		                    
		                    await w.Write((byte)MessageType.BucketReceived);
		                    await w.Write((int)bucket.id);

		                    if (!buckets.TryFree(bucket.id))
		                    {
		                        throw new($"Failed to free received bucket {bucket.id}");
		                    }
		                }
		            }
		        }
			}
			catch (Exception e)
			{
				logger.LogError($"Sending run failed. {e.GetType().Name}: '{e.Message}': {e.StackTrace}");
				Disconnect();
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
			        int bucketID = await r.ReadInt();

			        if (type == MessageType.BucketCreate)
			        {
				        int bucketSize = await r.ReadInt();
				        Bucket bucket = buckets.Allocate(bucketID, bucketSize);
				        bucket.state = Bucket.State.Receiving;
			        }
			        else if (type == MessageType.BucketPart)
			        {
				        int partSize = await r.ReadInt();
				        byte[] partBytes = await r.ReadBytes(partSize);
				        Bucket bucket = buckets.Write(bucketID, partBytes);
				        if (bucket.BytesToEnd <= 0)
				        {
					        bucket.state = Bucket.State.Received;
					        SetDirty();
    
					        IMessage message = serializer.Deserialize(bucket.bytes);
					        onMessageReceived?.Invoke(this, message);
				        }
			        }
			        else if (type == MessageType.BucketReceived)
			        {
				        if (buckets.TryFree(bucketID) == false)
				        {
					        throw new($"Failed to free remotely received bucket {bucketID}");
				        }
			        }
			        else
			        {
				        throw new($"Unknown MessageType: {type}");
			        }
		        }
		    }
		    catch (OperationCanceledException)
		    {
		        // Штатная отмена токена
		    }
		    catch (Exception e)
		    {
			    logger.LogError($"Receive run failed. {e.GetType().Name}: '{e.Message}': {e.StackTrace}");
		    }
		    finally
		    {
		        Disconnect();
		    }
		}

		public void Disconnect()
		{
			logger.LogDebug("Session disconnected");
			
			cts?.Cancel();
			tcp?.Dispose();
			onDisconnected?.Invoke(this);
		}
		
		private Bucket NextBucket()
		{
			return buckets.pendingBuckets.First();
		}
	}
}
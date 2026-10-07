using System;
using System.Collections.Generic;
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
		
		private TcpClient? tcp;
		private Action<ClientSession> onDisconnected;
		private CancellationTokenSource cts = new();

		private Buckets buckets;
		private Serializer serializer = new(registry);
		private ILogger logger;
		private Action<ClientSession, IMessage> onMessageReceived;
		
		private volatile TaskCompletionSource<bool> dirtyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		
		private static TypesRegistry registry = new();

		public ClientSession(string id, TcpClient tcp, Action<ClientSession> onDisconnected, ILogger logger, Action<ClientSession, IMessage> onMessageReceived, bool isServer)
		{
			this.id = id;
			this.tcp = tcp;
			this.onDisconnected = onDisconnected;
			this.logger = logger;
			this.onMessageReceived = onMessageReceived;
			buckets = new(isServer);
		}

		public void Start()
		{
			_ = RunSending(cts.Token);
			_ = RunReceiving(cts.Token);
		}
		
		public Bucket Send(IMessage message)
		{
			(byte[] masterBytes, List<IPayload> slaves) = serializer.Serialize(message);
    
			Bucket master = buckets.AllocateSending(0, masterBytes.Length, new MemoryPayload(masterBytes));
			master.expectedSlavesCount = slaves.Count;
    
			foreach (IPayload slavePayload in slaves)
			{
				buckets.AllocateSending(master.id, slavePayload.Length, slavePayload);
			}
			
			logger.LogDebug($"Sending bucket {master.id} ({masterBytes.Length} bytes) with {slaves.Count} slaves");

			SetDirty();
			return master;
		}
		
		private async Task RunSending(CancellationToken token)
		{
		    try
		    {
		        NetworkStream stream = tcp!.GetStream();
		        AsyncBinaryWriter w = new(stream, token);
		        
		        byte[] sharedBuffer = new byte[10 * 1024];

		        while (!token.IsCancellationRequested)
		        {
		            TaskCompletionSource<bool> tcs = dirtyTcs;
		            if (!tcs.Task.IsCompleted) await tcs.Task.ConfigureAwait(false);
		            Interlocked.CompareExchange(ref dirtyTcs, new(TaskCreationOptions.RunContinuationsAsynchronously), tcs);

		            while (TryNextBucket(out Bucket bucket))
		            {
		                if (bucket.state == Bucket.State.QueuedToSend)
		                {
		                    bucket.state = Bucket.State.Sending;
		                    
		                    logger.LogDebug("[SEND] Start bucket sending");

		                    await w.Write((byte)MessageType.BucketCreate);
		                    await w.Write((int)bucket.id);
		                    await w.Write((int)bucket.masterBucketID);
		                    await w.Write((long)bucket.length);
		                    if (bucket.IsMaster) await w.Write((int)bucket.expectedSlavesCount);
		                }
		                else if (bucket.state == Bucket.State.Sending && bucket.BytesToEnd > 0)
		                {
		                    int toRead = (int)Math.Min(sharedBuffer.Length, bucket.BytesToEnd);
		                    int read = await bucket.Stream.ReadAsync(sharedBuffer, 0, toRead, token);
		                    
			                logger.LogDebug($"[SEND] Bucket {bucket.id} part {bucket.bytesTransferred}+{read} / {bucket.length} ({(bucket.bytesTransferred + read) / (decimal)bucket.length:P1})");
		                    bucket.bytesTransferred += read;
		                    
		                    await w.Write((byte)MessageType.BucketPart);
		                    await w.Write((int)bucket.id);
		                    await w.Write((int)read);
		                    await stream.WriteAsync(sharedBuffer, 0, read, token);

		                    if (bucket.BytesToEnd <= 0) bucket.state = Bucket.State.Sent;
		                }
		                else if (bucket.state == Bucket.State.Received && bucket.isReceiveReported == false)
		                {
			                logger.LogDebug("[SEND] Report bucket received");

			                bucket.isReceiveReported = true;
		                    await w.Write((byte)MessageType.BucketReceived);
		                    await w.Write((int)bucket.id);
		                }
		            }
		        }
		    }
		    catch (IOException)
		    {
			    Disconnect();
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
		        NetworkStream stream = tcp!.GetStream();
		        AsyncBinaryReader r = new(stream, token);
		        
		        byte[] sharedBuffer = new byte[10 * 1024]; // Один буфер на сессию

		        while (!token.IsCancellationRequested && tcp.Connected)
		        {
		            MessageType type = (MessageType)await r.ReadByte();
		            int bucketID = await r.ReadInt();

		            if (type == MessageType.BucketCreate)
		            {
		                int masterBucketID = await r.ReadInt();
		                long bucketSize = await r.ReadLong(); // Используем long для тяжелых файлов!
		                int slavesCount = masterBucketID == 0 ? await r.ReadInt() : 0;
		                
		                Bucket bucket = buckets.AllocateReceiving(bucketID, masterBucketID, bucketSize, slavesCount);
		                bucket.state = Bucket.State.Receiving;
		                
		                logger.LogDebug($"[RECV] Create bucket {bucketID}");
		            }
		            else if (type == MessageType.BucketPart)
		            {
		                int partSize = await r.ReadInt();
		                
		                Bucket bucket = buckets.Get(bucketID);
		                logger.LogDebug($"[RECV] Bucket {bucketID} part {bucket.bytesTransferred}+{partSize} / {bucket.length} ({(bucket.bytesTransferred + partSize) / (decimal)bucket.length:P1})");
		                
		                int remaining = partSize;
		                while (remaining > 0)
		                {
		                    int toRead = Math.Min(remaining, sharedBuffer.Length);
		                    int read = await stream.ReadAsync(sharedBuffer, 0, toRead, token);
		                    if (read == 0) throw new EndOfStreamException();
		                    
		                    await bucket.Stream.WriteAsync(sharedBuffer, 0, read, token);
		                    remaining -= read;
		                }
		                bucket.bytesTransferred += partSize;

		                if (bucket.BytesToEnd <= 0)
		                {
		                    bucket.state = Bucket.State.Received;
		                    bucket.Stream.Position = 0;
		                    
		                    logger.LogDebug($"[RECV] Bucket {bucketID} collected");

		                    if (bucket.Payload == null && bucket.Stream is MemoryStream ms)
		                    {
		                        bucket.Payload = new MemoryPayload(ms.ToArray());
		                    }

		                    SetDirty();
		                    TickReadyMasters();
		                }
		            }
		            else if (type == MessageType.BucketReceived)
		            {
			            logger.LogDebug($"[RECV] Bucket {bucketID} receive reported");
			            buckets.Free(bucketID);
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
		    catch (IOException)
		    {
			    Disconnect();
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
			if (tcp != null)
			{
				logger.LogDebug("Session disconnected");
			
				cts?.Cancel();
				tcp?.Dispose();
				tcp = null;

				onDisconnected?.Invoke(this);
			}
		}

		private bool TryNextBucket(out Bucket bucket)
		{
			Bucket? b = buckets.readyToSend.FirstOrDefault();
			if (b == null)
			{
				bucket = null;
				return false;
			}
			else
			{
				bucket = b;
				return true;
			}
		}
		
		private void SetDirty() => dirtyTcs.TrySetResult(true);

		private void TickReadyMasters()
		{
			Bucket[] readyMasters = buckets.active.Values.Where(b => 
				b.IsMaster && 
				b.state == Bucket.State.Received &&
				b.isHandled == false &&
				buckets.active.Values.Count(s => s.masterBucketID == b.id && s.state == Bucket.State.Received) == b.expectedSlavesCount
			).ToArray();
			
			logger.LogDebug($"Tick {readyMasters.Length} ready masters / {buckets.active.Count} active buckets");

			foreach (Bucket master in readyMasters)
			{
				// Собираем все Payload от слейвов в правильном порядке
				IPayload[] slavePayloads = buckets.active.Values
					.Where(s => s.masterBucketID == master.id)
					.OrderBy(s => s.id) // Важно для порядка при десериализации
					.Select(s => s.Payload)
					.ToArray();
				
				byte[] masterBytes = ((MemoryStream)master.Stream).ToArray();
				IMessage message = serializer.Deserialize(masterBytes, slavePayloads);

				Bucket[] slaveBuckets = buckets.active.Values.Where(s => s.masterBucketID == master.id).ToArray();
				
				master.isHandled = true;
				foreach (Bucket slave in slaveBuckets) slave.isHandled = true;

				onMessageReceived?.Invoke(this, message);

				buckets.Free(master.id);
				foreach (Bucket slave in slaveBuckets) buckets.Free(slave.id);
			}
		}
	}
}
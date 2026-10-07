using System;
using System.Diagnostics;
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
		private Action<ClientSession> onDisconnected;
		private CancellationTokenSource cts = new();
		// private SemaphoreSlim dirtySemaphore = new(0);

		public Buckets buckets = new(5 * 1024 * 1024); // 5 MB
		private Serializer serializer = new(registry);
		private ILogger logger;
		private Action<ClientSession, IMessage> onMessageReceived;
		private DateTime startTimeUTC;
		
		private volatile TaskCompletionSource<bool> dirtyTcs = 
			new(TaskCreationOptions.RunContinuationsAsynchronously);
		
		private readonly Stopwatch _sw = Stopwatch.StartNew();
		private double NowMs => _sw.Elapsed.TotalMilliseconds;


		private static TypesRegistry registry = new();

		public ClientSession(string id, TcpClient tcp, Action<ClientSession> onDisconnected, ILogger logger, Action<ClientSession, IMessage> onMessageReceived)
		{
			this.id = id;
			this.tcp = tcp;
			this.onDisconnected = onDisconnected;
			this.logger = logger;
			this.onMessageReceived = onMessageReceived;
			startTimeUTC = DateTime.UtcNow;
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
			logger.LogDebug($"SetDirty at {(DateTime.UtcNow - startTimeUTC).TotalMilliseconds} ms");
			// if (dirtySemaphore.CurrentCount == 0) dirtySemaphore.Release();
			dirtyTcs.TrySetResult(true);
		}

		private async Task RunSending(CancellationToken token)
		{
			try
			{
				NetworkStream stream = tcp.GetStream();
		        using var sendBuffer = new MemoryStream(64 * 1024);
		        int maxPartSize = 5 * 1024; // 5 KB

		        while (!token.IsCancellationRequested)
		        {
		            TaskCompletionSource<bool> tcs = dirtyTcs;
		            if (!tcs.Task.IsCompleted)
		            {
		                await tcs.Task.ConfigureAwait(false);
		            }
		            Interlocked.CompareExchange(ref dirtyTcs, new(TaskCreationOptions.RunContinuationsAsynchronously), tcs);

		            sendBuffer.SetLength(0); // Очищаем буфер перед пачкой

		            while (buckets.pendingBuckets.Any())
		            {
		                Bucket bucket = NextBucket();

		                if (bucket.state == Bucket.State.QueuedToSend)
		                {
		                    bucket.state = Bucket.State.Sending;
		                    
		                    // Записываем заголовок создания в локальный буфер памяти
		                    sendBuffer.WriteByte((byte)MessageType.BucketCreate);
		                    sendBuffer.Write(BitConverter.GetBytes(bucket.id), 0, 4);
		                    sendBuffer.Write(BitConverter.GetBytes(bucket.bytes.Length), 0, 4);
		                }
		                else if (bucket.state == Bucket.State.Sending && bucket.BytesToEnd > 0)
		                {
		                    BucketPart part = bucket.Next(maxPartSize);
		                    
		                    // Записываем заголовок куска и тело в буфер памяти
		                    sendBuffer.WriteByte((byte)MessageType.BucketPart);
		                    sendBuffer.Write(BitConverter.GetBytes(part.bucketID), 0, 4);
		                    sendBuffer.Write(BitConverter.GetBytes(part.bytes.Length), 0, 4);
		                    sendBuffer.Write(part.bytes, 0, part.bytes.Length);

		                    if (bucket.BytesToEnd <= 0) bucket.state = Bucket.State.Sent;
		                    
		                    // Если накопили достаточно данных (например, больше 16 КБ), можно прерваться и сбросить в сокет
		                    if (sendBuffer.Length >= 16 * 1024) break; 
		                }
		                else if (bucket.state == Bucket.State.Received)
		                {
		                    bucket.state = Bucket.State.ReceivedReported;
		                    
		                    sendBuffer.WriteByte((byte)MessageType.BucketReceived);
		                    sendBuffer.Write(BitConverter.GetBytes(bucket.id), 0, 4);

		                    if (!buckets.TryFree(bucket.id))
		                    {
		                        throw new($"Failed to free received bucket {bucket.id}");
		                    }
		                }
		            }

		            // ЕСЛИ ЕСТЬ ЧТО ОТПРАВИТЬ — ДЕЛАЕМ ВСЕГО ОДИН ASYNC ВЫЗОВ В СЕТЬ НА ВСЮ ПАЧКУ!
		            if (sendBuffer.Length > 0)
		            {
		                await stream.WriteAsync(sendBuffer.GetBuffer(), 0, (int)sendBuffer.Length, token).ConfigureAwait(false);
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
			    
			    byte[] buffer = new byte[64 * 1024]; // 64 КБ буфер чтения
			    int bufferOffset = 0;
			    
		        while (!token.IsCancellationRequested && tcp.Connected)
		        {
			        int bytesRead = await stream.ReadAsync(buffer, bufferOffset, buffer.Length - bufferOffset, token).ConfigureAwait(false);;
			        if (bytesRead == 0) break;

			        bufferOffset += bytesRead;
			        int processedOffset = 0;

			        // 2. СИНХРОННО В ПАМЯТИ (ЗА МИКРОСЕКУНДЫ) парсим столько сообщений, сколько успело прилететь
		            while (true)
		            {
		                int available = bufferOffset - processedOffset;
		                if (available < 5) break; // Даже на заголовок не хватает, ждем следующего ReadAsync

		                MessageType type = (MessageType)buffer[processedOffset];
		                int bucketID = BitConverter.ToInt32(buffer, processedOffset + 1);

		                if (type == MessageType.BucketCreate)
		                {
		                    if (available < 9) break; // 5 байт заголовка + 4 байта размера
		                    
		                    int bucketSize = BitConverter.ToInt32(buffer, processedOffset + 5);
		                    processedOffset += 9;

		                    Bucket bucket = buckets.Allocate(bucketID, bucketSize);
		                    bucket.state = Bucket.State.Receiving;
		                }
		                else if (type == MessageType.BucketPart)
		                {
		                    if (available < 9) break; // 5 байт заголовка + 4 байта длины
		                    
		                    int partLength = BitConverter.ToInt32(buffer, processedOffset + 5);
		                    if (available < 9 + partLength) break; // Тело еще не доехало целиком, ждем

		                    byte[] payload = new byte[partLength];
		                    Array.Copy(buffer, processedOffset + 9, payload, 0, partLength);
		                    processedOffset += 9 + partLength;

		                    Bucket bucket = buckets.Write(bucketID, payload);

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
		                    processedOffset += 5; // Заголовок 5 байт
		                    
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

		            // Сдвигаем «хвост» неразобранных байт в начало буфера (если сообщение пришло не целиком)
		            if (processedOffset > 0)
		            {
		                int remaining = bufferOffset - processedOffset;
		                if (remaining > 0)
		                {
		                    Array.Copy(buffer, processedOffset, buffer, 0, remaining);
		                }
		                bufferOffset = remaining;
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
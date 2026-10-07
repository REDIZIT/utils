using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RTS
{
	public class Buckets
	{
		public IEnumerable<Bucket> readyToSend => active.Values.Where(b => b.state == Bucket.State.QueuedToSend || b.state == Bucket.State.Sending || b.state == Bucket.State.Received);
		
		public Dictionary<int, Bucket> active = new();
		
		private readonly BucketIDGenerator idGenerator;
		private readonly object syncLock = new();

		private readonly ILogger logger;

		public Buckets(bool isServer, ILogger logger)
		{
			idGenerator = new(isServer);
			this.logger = logger;
		}

		public Bucket Get(int id)
		{
			lock (syncLock)
			{
				if (active.TryGetValue(id, out Bucket? bucket))  return bucket;
				throw new($"Bucket {id} not found");
			}
		}

		public Bucket[] GetReadyToSendBuckets()
		{
			lock (syncLock)
			{
				return readyToSend.ToArray();
			}
		}
		
		public Bucket AllocateSending(int masterID, long length, IPayload payload)
		{
			lock (syncLock)
			{
				int bucketID = idGenerator.Next(id => active.ContainsKey(id));

				Bucket bucket = new(bucketID, masterID, length)
				{
					state = Bucket.State.QueuedToSend,
					Payload = payload,
					Stream = payload.OpenRead()
				};

				active[bucketID] = bucket;
				
				logger.LogDebug($"Bucket #{bucketID} allocated (total: {active.Count})");
				
				return bucket;
			}
		}
		
		public Bucket AllocateReceiving(int id, int masterID, long size, int expectedSlaves)
		{
			lock (syncLock)
			{
				if (active.ContainsKey(id)) throw new($"Bucket with id {id} already exists in active pool.");

				Bucket bucket = new(id, masterID, size)
				{
					state = Bucket.State.Receiving,
					expectedSlavesCount = expectedSlaves
				};

				// Если бакет больше 5 МБ — сразу пишем на диск во временный файл
				if (size > 5 * 1024 * 1024)
				{
					TempFilePayload temp = new();
					bucket.Payload = temp;
					bucket.Stream = temp.OpenWrite();
				}
				else
				{
					// MemoryStream без ограничений по размеру
					bucket.Stream = new MemoryStream();
				}

				active[id] = bucket;
				
				logger.LogDebug($"Bucket #{id} allocated (total: {active.Count})");
				
				return bucket;
			}
		}

		public void Free(int bucketID)
		{
			lock (syncLock)
			{
				if (!active.TryGetValue(bucketID, out Bucket? bucket)) throw new($"Bucket {bucketID} not found");

				bucket.Dispose();
				active.Remove(bucketID);
				
				logger.LogDebug($"Bucket #{bucketID} freed (total: {active.Count})");
			}
		}
	}
}
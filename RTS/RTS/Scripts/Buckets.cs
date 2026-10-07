using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTS
{
	public class Buckets
	{
		public IEnumerable<Bucket> readyToSend => active.Values.Where(b => b.state == Bucket.State.QueuedToSend || b.state == Bucket.State.Sending || b.state == Bucket.State.Received);
		
		public Dictionary<int, Bucket> active = new();
		
		private readonly BucketIDGenerator idGenerator;
		private readonly object syncLock = new();

		public Buckets(bool isServer)
		{
			idGenerator = new(isServer);
		}

		public Bucket Get(int id)
		{
			lock (syncLock)
			{
				if (active.TryGetValue(id, out Bucket? bucket))  return bucket;
				throw new($"Bucket {id} not found");
			}
		}
		
		public Bucket AllocateSending(int masterBucketID, long length, IPayload payload)
		{
			lock (syncLock)
			{
				int bucketID = idGenerator.Next(id => active.ContainsKey(id));

				Bucket bucket = new(bucketID, masterBucketID, length)
				{
					state = Bucket.State.QueuedToSend,
					Payload = payload,
					Stream = payload.OpenRead()
				};

				active[bucketID] = bucket;
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
			}
		}
	}
}
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RTS
{
	public class Buckets
	{
		public IEnumerable<Bucket> readyToSend => active.Where(b => b.state == Bucket.State.QueuedToSend || b.state == Bucket.State.Sending || b.state == Bucket.State.Received);
		
		public List<Bucket> active = new();
		private int maxBucketSize;
		private int nextBucketID = 1;

		public Buckets(int maxBucketSize)
		{
			this.maxBucketSize = maxBucketSize;
		}

		public Bucket Get(int id)
		{
			return active.First(b => b.id == id);
		}
		
		public Bucket AllocateSending(int masterBucketID, long length, IPayload payload)
		{
			int bucketID = NextID();
			
			Bucket bucket = new(bucketID, masterBucketID, length)
			{
				state = Bucket.State.QueuedToSend,
				Payload = payload,
				Stream = payload.OpenRead() // Открываем поток для чтения отправляемых данных
			};

			active.Add(bucket);
			return bucket;
		}
		
		public Bucket AllocateReceiving(int id, int masterID, long size, int expectedSlaves)
		{
			Bucket b = new(id, masterID, size);
			b.expectedSlavesCount = expectedSlaves;

			if (size > 5 * 1024 * 1024) // Больше 5 МБ -> на диск
			{
				var tempFile = new TempFilePayload();
				b.Stream = tempFile.OpenWrite();
				b.Payload = tempFile;
			}
			else // Меньше 5 МБ -> в память
			{
				b.Stream = new MemoryStream((int)size);
			}
    
			active.Add(b);
			return b;
		}

		public void Free(int bucketID)
		{
			Bucket? bucket = active.FirstOrDefault(b => b.id == bucketID);
			if (bucket == null) throw new($"Bucket {bucketID} not found");

			bucket.Dispose();
			active.Remove(bucket);
		}

		private int NextID()
		{
			int guard = 0;
			while (active.Any(b => b.id == nextBucketID))
			{
				if (guard++ > 1_000_000) throw new("NextID too many iterations");
				nextBucketID++;
			}
			return nextBucketID;
		}
	}
}
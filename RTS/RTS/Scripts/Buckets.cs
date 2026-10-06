using System.Collections.Generic;
using System.Linq;

namespace RTS
{
	public class Buckets
	{
		public IEnumerable<Bucket> pendingBuckets => active.Where(b => b.state == Bucket.State.QueuedToSend || b.state == Bucket.State.Sending || b.state == Bucket.State.Received);
		
		public List<Bucket> active = new();
		private int maxBucketSize;
		private int nextBucketID = 1;

		public Buckets(int maxBucketSize)
		{
			this.maxBucketSize = maxBucketSize;
		}
		
		public Bucket Allocate(int bucketID, int bucketSize)
		{
			if (active.Any(b => b.id == bucketID)) throw new($"Bucket with same id ({bucketID}) already exists"); 
			if (bucketSize > maxBucketSize) throw new($"Bucket size is too large ({bucketSize} / {bucketSize} bytes)");

			Bucket bucket = new(bucketID, bucketSize);
			active.Add(bucket);
			return bucket;
		}
		
		public Bucket AllocateFrom(byte[] bucketBytes)
		{
			Bucket bucket = Allocate(NextID(), bucketBytes.Length);
			bucket.bytes = bucketBytes;
			return bucket;
		}

		public Bucket Write(int bucketID, byte[] bytes)
		{
			Bucket bucket = active.First(b => b.id == bucketID);
			bucket.Write(bytes);
			return bucket;
		}

		public bool TryFree(int bucketID)
		{
			Bucket? bucket = active.FirstOrDefault(b => b.id == bucketID);
			if (bucket == null) return false;

			if (bucket.state != Bucket.State.Sent && bucket.state != Bucket.State.ReceivedReported) return false;

			bucket.Dispose();
			active.Remove(bucket);
			return true;
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
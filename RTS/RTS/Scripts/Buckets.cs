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
		
		public bool TryAllocate(int bucketID, int bucketSize, out Bucket bucket)
		{
			bucket = null;
			if (active.Any(b => b.id == bucketID)) return false;
			if (bucketSize > maxBucketSize) return false;

			bucket = new(bucketID, bucketSize);
			active.Add(bucket);
			return true;
		}
		
		public bool TryAllocateFrom(byte[] bucketBytes, out Bucket bucket)
		{
			int bucketID = NextID();
			if (TryAllocate(bucketID, bucketBytes.Length, out bucket) == false) return false;
			bucket.bytes = bucketBytes;
			return true;
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

			if (bucket.state != Bucket.State.Sent && bucket.state != Bucket.State.Received) return false;

			bucket.Dispose();
			active.Remove(bucket);
			return true;
		}

		private int NextID()
		{
			return nextBucketID++;
		}
	}
}
using System;

namespace RTS
{
	public class BucketIDGenerator
	{
		private readonly int minId;
		private readonly int maxId;
		private int currentId;
		private readonly object syncRoot = new();

		public BucketIDGenerator(bool isServer)
		{
			minId = isServer ? 2 : 1;
			maxId = isServer ? (int.MaxValue - 1) : int.MaxValue;
			currentId = minId;
		}

		public int Next(Func<int, bool> isOccupied)
		{
			lock (syncRoot)
			{
				int iterations = 0;
				const int maxSearchIterations = 1_000_000; 

				while (iterations++ < maxSearchIterations)
				{
					int candidate = currentId;

					if (currentId >= maxId)
					{
						currentId = minId;
					}
					else
					{
						currentId += 2;
					}

					if (!isOccupied(candidate))
					{
						return candidate;
					}
				}

				throw new("Bucket ID space exhausted: too many active buckets.");
			}
		}
	}
}
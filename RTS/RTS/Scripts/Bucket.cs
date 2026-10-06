using System;

namespace RTS
{
	public class Bucket : IDisposable
	{
		public int id;
		public byte[] bytes;
		public int bytesCaret;
		public State state;

		public int BytesToEnd => bytes.Length - bytesCaret;

		public enum State
		{
			Unregistered,
			QueuedToSend,
			Sending,
			Sent,
			Receiving,
			Received,
			ReceivedReported,
			Released,
		}

		public Bucket(int id, int length)
		{
			this.id = id;
			bytes = new byte[length];
		}

		public BucketPart Next(int maxBytes)
		{
			int bytesToTake = Math.Min(maxBytes, BytesToEnd);
			BucketPart part = new()
			{
				bucketID = id,
				bytes = new byte[bytesToTake]
			};
			Array.Copy(bytes, bytesCaret, part.bytes, 0, bytesToTake);
			bytesCaret += bytesToTake;
			return part;
		}

		public void Write(byte[] partBytes)
		{
			Array.Copy(partBytes, 0, bytes, bytesCaret, partBytes.Length);
			bytesCaret += partBytes.Length;
		}

		public void Dispose()
		{
			
		}
	}
}
using System;
using System.IO;

namespace RTS
{
	public class Bucket : IDisposable
	{
		public int id;
		public int masterBucketID;
		public int expectedSlavesCount;
		public State state;
		public bool isHandled;
		public bool isReceiveReported;
        
		public long length;
		public long bytesTransferred;

		public PriorityState priority;

		public Stream Stream { get; set; } 
		public IPayload Payload { get; set; } 

		public long BytesToEnd => length - bytesTransferred;
		public bool IsMaster => masterBucketID == 0;

		public enum State
		{
			Unregistered,
			QueuedToSend,
			Sending,
			Sent,
			Receiving,
			Received
		}

		public Bucket(int id, int masterBucketID, long length)
		{
			this.id = id;
			this.masterBucketID = masterBucketID;
			this.length = length;
		}

		public void Dispose()
		{
			Stream?.Dispose();
			Payload?.Dispose();
		}
	}
}
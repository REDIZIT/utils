using System;
using System.IO;

namespace RTS
{
	public class Bucket : IDisposable
	{
		public int id;
		public int masterBucketID; // Если 0 — это мастер
		public int expectedSlavesCount; // Сколько слейвов ждет мастер
		public State state;
		public bool isHandled;
		public bool isReceiveReported;
        
		public long length;
		public long bytesTransferred;

		// Поток, из которого мы читаем (отправка) или в который пишем (прием)
		public Stream Stream { get; set; } 
		public IPayload Payload { get; set; } // Для готового результата при приеме

		public long BytesToEnd => length - bytesTransferred;
		public bool IsMaster => masterBucketID == 0;

		public enum State
		{
			Unregistered,
			QueuedToSend,
			Sending,
			Sent,
			Receiving,
			Received,
			Released,
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
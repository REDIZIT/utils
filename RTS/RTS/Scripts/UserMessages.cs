using System.IO;

namespace RTS
{
	public interface IMessage
	{
		void Write(MessageWriter w);
		void Read(MessageReader r);
	}

	public class TestMessage : IMessage, ITrackableMessage
	{
		public string message;
		public int RequestID { get; set; }

		public void Write(MessageWriter w)
		{
			w.Write(message);
			w.Write(RequestID);
		}

		public void Read(MessageReader r)
		{
			message = r.ReadString();
			RequestID = r.ReadInt32();
		}
	}

	public class TestResponse : IMessage, ITrackableMessage
	{
		public string response;
		public int RequestID { get; set; }
		
		public void Write(MessageWriter w)
		{
			w.Write(response);
			w.Write(RequestID);
		}

		public void Read(MessageReader r)
		{
			response = r.ReadString();
			RequestID = r.ReadInt32();
		}
	}
	
	public class TestHeavyRequest : IMessage, ITrackableMessage
	{
		public int RequestID { get; set; }
		public string shortMessage;
		public IPayload Archive;

		public void Write(MessageWriter w)
		{
			w.Write(RequestID);
			w.Write(shortMessage);
			w.WritePayload(Archive);
		}

		public void Read(MessageReader r)
		{
			RequestID = r.ReadInt32();
			shortMessage = r.ReadString();
			Archive = r.ReadPayload();
		}
	}

	public class TestHeavyResponse : IMessage, ITrackableMessage
	{
		public int RequestID { get; set; }
		public string shortResponse;

		public void Write(MessageWriter w)
		{
			w.Write(RequestID);
			w.Write(shortResponse);
		}

		public void Read(MessageReader r)
		{
			RequestID = r.ReadInt32();
			shortResponse = r.ReadString();
		}
	}

	public interface ITrackableMessage
	{
		int RequestID { get; }
	}
}
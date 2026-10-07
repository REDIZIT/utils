namespace RTS
{
	public interface IMessage
	{
		void Write(MessageWriter w);
		void Read(MessageReader r);
	}

	public class TestMessage : IMessage
	{
		public string message;

		public void Write(MessageWriter w)
		{
			w.Write(message);
		}

		public void Read(MessageReader r)
		{
			message = r.ReadString();
		}
	}

	public class TestResponse : IMessage
	{
		public string response;
		
		public void Write(MessageWriter w)
		{
			w.Write(response);
		}

		public void Read(MessageReader r)
		{
			response = r.ReadString();
		}
	}
	
	public class TestHeavyRequest : IMessage
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

	public class TestHeavyResponse : IMessage
	{
		public string shortResponse;

		public void Write(MessageWriter w)
		{
			w.Write(shortResponse);
		}

		public void Read(MessageReader r)
		{
			shortResponse = r.ReadString();
		}
	}
}
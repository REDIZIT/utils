using System.IO;

namespace RTS
{
	public interface IMessage
	{
		void Write(BinaryWriter w);
		void Read(BinaryReader r);
	}
	
	public class TestMessage : IMessage, ITrackableMessage
	{
		public string message;
		public int RequestID { get; set; }

		public void Write(BinaryWriter w)
		{
			w.Write(message);
			w.Write(RequestID);
		}

		public void Read(BinaryReader r)
		{
			message = r.ReadString();
			RequestID = r.ReadInt32();
		}
	}

	public class TestResponse : IMessage, ITrackableMessage
	{
		public string response;
		public int RequestID { get; set; }
		
		public void Write(BinaryWriter w)
		{
			w.Write(response);
			w.Write(RequestID);
		}

		public void Read(BinaryReader r)
		{
			response = r.ReadString();
			RequestID = r.ReadInt32();
		}
	}

	public interface ITrackableMessage
	{
		int RequestID { get; }
	}
}
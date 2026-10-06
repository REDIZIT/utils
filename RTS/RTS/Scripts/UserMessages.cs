using System.IO;

namespace RTS
{
	public interface IMessage
	{
		void Write(BinaryWriter w);
		void Read(BinaryReader r);
	}
	
	public class TestMessage : IMessage
	{
		public string message;

		public void Write(BinaryWriter w)
		{
			w.Write(message);
		}

		public void Read(BinaryReader r)
		{
			message = r.ReadString();
		}
	}
}
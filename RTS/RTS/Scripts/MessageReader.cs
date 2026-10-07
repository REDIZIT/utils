using System.IO;

namespace RTS
{
	public class MessageReader : BinaryReader
	{
		private IPayload[] slaves;

		public MessageReader(Stream input, IPayload[] slaves) : base(input)
		{
			this.slaves = slaves;
		}

		public IPayload ReadPayload()
		{
			int index = ReadInt32();
			return slaves[index];
		}
	}
}
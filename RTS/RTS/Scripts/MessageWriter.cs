using System.Collections.Generic;
using System.IO;

namespace RTS
{
	public class MessageWriter : BinaryWriter
	{
		public List<IPayload> Slaves { get; } = new();

		public MessageWriter(Stream output) : base(output) { }

		public void WritePayload(IPayload payload)
		{
			Slaves.Add(payload);
			Write(Slaves.Count - 1); // Записываем локальный индекс слейва
		}
	}
}
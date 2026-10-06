using System;
using System.IO;
using System.Reflection;

namespace RTS
{
	public class Serializer
	{
		private byte[] buffer = new byte[5 * 1024]; // 5 KB

		private TypesRegistry registry;

		public Serializer(TypesRegistry registry)
		{
			this.registry = registry;
		}
		
		public byte[] Serialize(IMessage message)
		{
			MemoryStream stream = new(buffer);
			BinaryWriter w = new(stream);

			string typeID = registry.GetID(message.GetType());
			w.Write(typeID);
			
			message.Write(w);
			
			byte[] bytes = new byte[stream.Position];
			Array.Copy(buffer, 0, bytes, 0, bytes.Length);
			
			w.Dispose();
			stream.Dispose();
			
			return bytes;
		}

		public IMessage Deserialize(byte[] bytes)
		{
			MemoryStream stream = new(bytes);
			BinaryReader r = new(stream);

			string typeID = r.ReadString();
			Type type = registry.GetType(typeID);
			IMessage message = (IMessage)Activator.CreateInstance(type)!;
			
			message.Read(r);
			
			r.Dispose();
			stream.Dispose();

			return message;
		}
	}
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace RTS
{
	public class Serializer
	{
		private TypesRegistry registry;

		public Serializer(TypesRegistry registry)
		{
			this.registry = registry;
		}
		
		public (byte[] masterBytes, List<IPayload> slaves) Serialize(IMessage message)
		{
			using MemoryStream stream = new();
			using MessageWriter w = new(stream);

			string typeID = registry.GetID(message.GetType());
			w.Write(typeID);
			message.Write(w);
            
			return (stream.ToArray(), w.Slaves);
		}

		public IMessage Deserialize(byte[] masterBytes, IPayload[] slaves)
		{
			using MemoryStream stream = new(masterBytes);
			using MessageReader r = new(stream, slaves);

			string typeID = r.ReadString();
			Type type = registry.GetType(typeID);
			IMessage message = (IMessage)Activator.CreateInstance(type)!;
            
			message.Read(r);
			return message;
		}
	}
}
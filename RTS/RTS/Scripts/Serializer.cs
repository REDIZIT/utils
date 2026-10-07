using System;
using System.Collections.Generic;
using System.IO;

namespace RTS
{
	public class Serializer
	{
		private TypesRegistry registry;

		public Serializer(TypesRegistry registry)
		{
			this.registry = registry;
		}
		
		public (byte[] masterBytes, List<IPayload> slaves) Serialize(IMessage message, Dictionary<string, string> meta)
		{
			using MemoryStream stream = new();
			using MessageWriter w = new(stream);

			w.Write((int)meta.Count);
			foreach (var kv in meta)
			{
				w.Write(kv.Key);
				w.Write(kv.Value);
			}

			string typeID = registry.GetID(message.GetType());
			w.Write(typeID);
			
			message.Write(w);
            
			return (stream.ToArray(), w.Slaves);
		}

		public (IMessage message, Dictionary<string, string> meta) Deserialize(byte[] masterBytes, IPayload[] slaves)
		{
			using MemoryStream stream = new(masterBytes);
			using MessageReader r = new(stream, slaves);

			int metaCount = r.ReadInt32();
			Dictionary<string, string> meta = new(metaCount);
			for (int i = 0; i < metaCount; i++)
			{
				string key = r.ReadString();
				string value = r.ReadString();
				meta[key] = value;
			}
			
			string typeID = r.ReadString();
			Type type = registry.GetType(typeID);
			IMessage message = (IMessage)Activator.CreateInstance(type)!;
            
			message.Read(r);
			
			return (message, meta);
		}
	}
}
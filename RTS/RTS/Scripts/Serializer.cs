using System;
using System.Collections.Generic;
using System.IO;

namespace RTS
{
	public class Serializer
	{
		private readonly TypesRegistry registry;

		public Serializer(TypesRegistry registry)
		{
			this.registry = registry;
		}
		
		public (byte[] masterBytes, List<IPayload> slaves) Serialize(object message, Dictionary<string, string> meta)
		{
			using MemoryStream stream = new();
			using MessageWriter w = new(stream);

			// 1. Метаданные (RequestID и т.д.)
			w.Write(meta.Count);
			foreach (var kv in meta)
			{
				w.Write(kv.Key);
				w.Write(kv.Value);
			}

			// 2. Идентификатор типа и поля объекта
			TypeSchema schema = registry.GetSchema(message.GetType());
			w.Write(schema.TypeID);
			
			foreach (FieldAccessor field in schema.Fields)
			{
				field.Write(message, w);
			}
            
			return (stream.ToArray(), w.Slaves);
		}

		public (object message, Dictionary<string, string> meta) Deserialize(byte[] masterBytes, IPayload[] slaves)
		{
			using MemoryStream stream = new(masterBytes);
			using MessageReader r = new(stream, slaves);

			// 1. Читаем метаданные
			int metaCount = r.ReadInt32();
			Dictionary<string, string> meta = new(metaCount);
			for (int i = 0; i < metaCount; i++)
			{
				meta[r.ReadString()] = r.ReadString();
			}
			
			// 2. Читаем тип и восстанавливаем поля
			string typeID = r.ReadString();
			TypeSchema schema = registry.GetSchema(typeID);
			object message = schema.CreateInstance();
            
			foreach (FieldAccessor field in schema.Fields)
			{
				field.Read(message, r);
			}
			
			return (message, meta);
		}
	}
}
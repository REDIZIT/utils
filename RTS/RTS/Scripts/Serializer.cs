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
		
		public (byte[] masterBytes, List<IPayload> slaves) Serialize(object message, int requestID)
		{
			using MemoryStream stream = new();
			using MessageWriter w = new(stream);

			w.Write(requestID);
			
			TypeSchema schema = registry.GetSchema(message.GetType());
			w.Write(schema.TypeID);
			
			foreach (FieldAccessor field in schema.Fields)
			{
				field.Write(message, w);
			}
            
			return (stream.ToArray(), w.Slaves);
		}

		public (object message, int requestID) Deserialize(byte[] masterBytes, IPayload[] slaves)
		{
			using MemoryStream stream = new(masterBytes);
			using MessageReader r = new(stream, slaves);

			int requestID = r.ReadInt32();
			
			// 2. Читаем тип и восстанавливаем поля
			string typeID = r.ReadString();
			TypeSchema schema = registry.GetSchema(typeID);
			object message = schema.CreateInstance();
            
			foreach (FieldAccessor field in schema.Fields)
			{
				field.Read(message, r);
			}
			
			return (message, requestID);
		}
	}
}
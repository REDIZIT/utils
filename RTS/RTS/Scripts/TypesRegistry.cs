using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RTS
{
	public class TypesRegistry
	{
		private readonly ConcurrentDictionary<Type, TypeSchema> schemaByType = new();
		private readonly ConcurrentDictionary<string, TypeSchema> schemaById = new();

		public TypeSchema GetSchema(Type type)
		{
			return schemaByType.GetOrAdd(type, t =>
			{
				TypeSchema schema = new(t);
				schemaById[schema.TypeID] = schema;
				return schema;
			});
		}

		public TypeSchema GetSchema(string typeID)
		{
			return schemaById.GetOrAdd(typeID, id =>
			{
				// Ищем тип во всех загруженных сборках (только при первом появлении неизвестного ID)
				Type? resolvedType = null;
				foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					resolvedType = asm.GetTypes().FirstOrDefault(t => t.Name == id);
					if (resolvedType != null) break;
				}

				if (resolvedType == null)
				{
					throw new InvalidOperationException($"Type with name '{id}' was not found in any loaded assembly.");
				}

				TypeSchema schema = new(resolvedType);
				schemaByType[resolvedType] = schema;
				return schema;
			});
		}
	}
}
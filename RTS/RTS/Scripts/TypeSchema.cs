using System;
using System.Linq;
using System.Reflection;

namespace RTS
{
	public class TypeSchema
	{
		public Type Type { get; }
		public string TypeID { get; }
		public FieldAccessor[] Fields { get; }

		public TypeSchema(Type type)
		{
			Type = type;
			TypeID = type.Name;

			// Берем только публичные поля экземпляра и обязательно сортируем по имени
			FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.OrderBy(f => f.Name, StringComparer.Ordinal)
				.ToArray();

			Fields = new FieldAccessor[fields.Length];
			for (int i = 0; i < fields.Length; i++)
			{
				Fields[i] = new(fields[i]);
			}
		}

		public object CreateInstance() => Activator.CreateInstance(Type)!;
	}
}
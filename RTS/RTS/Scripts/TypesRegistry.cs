using System;
using System.Collections.Generic;
using System.Reflection;

namespace RTS
{
	public class TypesRegistry
	{
		private Dictionary<string, Type> typeByID = new();
		private Dictionary<Type, string> idByType = new();

		public TypesRegistry()
		{
			Collect();
		}
		
		public void Collect()
		{
			Clear();
			foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (type.IsAbstract == false && type.IsInterface == false && typeof(IMessage).IsAssignableFrom(type))
				{
					Register(type);
				}
			}
		}

		public string GetID(Type type) => idByType[type];
		public Type GetType(string id) => typeByID[id];

		private void Clear()
		{
			typeByID.Clear();
			idByType.Clear();
		}

		private void Register(Type type)
		{
			string id = type.Name;
			typeByID[id] = type;
			idByType[type] = id;
		}
	}
}
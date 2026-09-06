using System;
using System.Collections.Generic;
using System.Reflection;

namespace REDIZIT.RUI
{
	public class Module
	{
		public Dictionary<string, Type> componentTypes = new();
		public Dictionary<string, Type> composerTypes = new();
		public Dictionary<Type, CanvasTemplate> templates = new();

		public void SetTemplate(Type compType, Node_Element templateNode)
		{
			templates[compType] = new(compType, templateNode);
		}

		public void RegisterAllComponentsFromAssembly(Assembly assembly)
		{
			Type componentBaseType = typeof(CanvasComponent);
			Type composerInterfaceType = typeof(IComposer);
            
			Type[] types = assembly.GetTypes();
            
			for (int i = 0; i < types.Length; i++)
			{
				Type t = types[i];
				if (t.IsAbstract || t.IsInterface) continue;
                
				if (componentBaseType.IsAssignableFrom(t)) RegisterComponent(t);
				else if (t.IsImplementInterface(composerInterfaceType)) RegisterComposer(t);
			}
		}
		
		private void RegisterComponent(Type componentType, string alias = null)
		{
			string name = string.IsNullOrEmpty(alias) ? componentType.Name : alias;
			componentTypes[name] = componentType;
		}

		private void RegisterComposer(Type composerType, string alias = null)
		{
			string name = string.IsNullOrEmpty(alias) ? composerType.Name.Split('_')[0] : alias;
			composerTypes[name] = composerType;
		}
	}
}
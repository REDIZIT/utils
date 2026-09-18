using System;
using System.Collections.Generic;

namespace REDIZIT.RUI
{
	public class CanvasStyle
	{
		public readonly string name;
		public readonly Node_Element styleNode;
		// Словарь: имя типа компонента ("Image", "Label") -> узел компонента со свойствами
		public readonly Dictionary<string, Node_Component> components = new(StringComparer.OrdinalIgnoreCase);

		public CanvasStyle(string name, Node_Element styleNode)
		{
			this.name = name;
			this.styleNode = styleNode;
			foreach (Node_Component comp in styleNode.components)
			{
				components[comp.typeName] = comp;
			}
		}
	}
}
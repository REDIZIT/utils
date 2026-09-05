using System;

namespace InGame.UI
{
	public class CanvasTemplate
	{
		public Type componentType;
		public Node_Element templateAst;

		public CanvasTemplate(Type componentType, Node_Element templateAst)
		{
			this.componentType = componentType;
			this.templateAst = templateAst;
		}
	}
}
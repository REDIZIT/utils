using System;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class Resolver
	{
		private readonly ILogger<Resolver> logger;
		
		public Resolver(ILogger<Resolver> logger)
		{
			this.logger = logger;
		}
		
		public void Resolve(Node_Root root, Module module)
		{
			CollectTemplates(root, module);
			CollectStyles(root, module);
		}
		
		private void CollectTemplates(Node_Root rootNode, Module module)
		{
			foreach (Node_Element element in rootNode.elements)
			{
				CollectTemplatesRecursive(element, module);
			}
		}

        private void CollectTemplatesRecursive(Node_Element node, Module module)
        {
	        if (node == null) throw new ResolveException("Node can not be null");

	        // 1. Проверяем, является ли сам узел шаблоном (для случая, когда весь файл — один шаблон)
	        if (node.isTemplate)
	        {
		        ProcessTemplateNode(node, module);
		        // Обычно шаблон — это конечная ветка для поиска других шаблонов, 
		        // но на всякий случай можно выйти, так как мы его зарегистрировали.
		        return; 
	        }

	        // 2. Ищем шаблоны среди дочерних элементов
	        for (int i = node.children.Count - 1; i >= 0; i--)
	        {
		        Node_Element child = node.children[i];

		        if (child.isTemplate)
		        {
			        ProcessTemplateNode(child, module);
			        // Удаляем узел шаблона из списка детей, чтобы он не попал в обычный рендеринг
			        node.children.RemoveAt(i);
		        }
		        else
		        {
			        CollectTemplatesRecursive(child, module);
		        }
	        }
        }

        private void ProcessTemplateNode(Node_Element templateNode, Module module)
        {
	        // Ищем тип компонента (например, HierarchyLot) прямо в этом узле или его детях
	        Type compType = FindFirstComponentType(templateNode, module);
    
	        if (compType == null)
	        {
		        throw new ResolveException($"Template with key '{templateNode.key}' has no any component");
	        }

	        module.SetTemplate(compType, templateNode);
	        
	        logger.LogDebug($"Template registered for component type '{compType.Name}'");
        }
        
        private void CollectStyles(Node_Root rootNode, Module module)
        {
	        for (int i = rootNode.elements.Count - 1; i >= 0; i--)
	        {
		        Node_Element element = rootNode.elements[i];
		        if (element.isStyle)
		        {
			        ProcessStyleNode(element, module);
			        rootNode.elements.RemoveAt(i);
		        }
		        else
		        {
			        CollectStylesRecursive(element, module);
		        }
	        }
        }

        private void CollectStylesRecursive(Node_Element node, Module module)
        {
	        for (int i = node.children.Count - 1; i >= 0; i--)
	        {
		        Node_Element child = node.children[i];
		        if (child.isStyle)
		        {
			        ProcessStyleNode(child, module);
			        node.children.RemoveAt(i);
		        }
		        else
		        {
			        CollectStylesRecursive(child, module);
		        }
	        }
        }

        private void ProcessStyleNode(Node_Element styleNode, Module module)
        {
	        if (string.IsNullOrEmpty(styleNode.key))
		        throw new ResolveException("Style must have a name");

	        module.SetStyle(styleNode.key, styleNode);
	        logger.LogDebug($"Style registered: '{styleNode.key}' with {styleNode.components.Count} component rules");
        }

		private Type FindFirstComponentType(Node_Element node, Module module)
		{
		    if (node.components.Count > 0)
		    {
		        if (module.componentTypes.TryGetValue(node.components[0].typeName, out Type t))
		            return t;
		    }

		    foreach (var child in node.children)
		    {
		        var found = FindFirstComponentType(child, module);
		        if (found != null) return found;
		    }
		    return null;
		}
	}
}
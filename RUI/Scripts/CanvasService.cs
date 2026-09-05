using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace InGame.UI
{
    public class CanvasService
    {
        public DiContainer container;
        public Dictionary<string, Type> componentTypes = new Dictionary<string, Type>();
        
        // Хранилище зарегистрированных шаблонов (Key: тип первого компонента)
        public Dictionary<Type, CanvasTemplate> templates = new Dictionary<Type, CanvasTemplate>();
        
        public Material defaultCombinedMaterial;
        public Material defaultTextMaterial;
        public TMPro.TMP_FontAsset defaultFont;

        public CanvasService(DiContainer container)
        {
            this.container = container;
            
            RegisterAllComponentsFromAssembly(typeof(CanvasComponent).Assembly);
        }

        public void RegisterComponent(Type componentType, string alias = null)
        {
            if (!typeof(CanvasComponent).IsAssignableFrom(componentType) || componentType.IsAbstract) return;

            string name = string.IsNullOrEmpty(alias) ? componentType.Name : alias;
            componentTypes[name] = componentType;
        }

        public void RegisterAllComponentsFromAssembly(Assembly assembly)
        {
            Type baseType = typeof(CanvasComponent);
            Type[] types = assembly.GetTypes();
            
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                {
                    RegisterComponent(t);
                }
            }
        }

		public void CollectTemplates(Node_Element rootNode)
		{
		    CollectTemplatesRecursive(rootNode);
		}

		private void CollectTemplatesRecursive(Node_Element node)
		{
		    // 1. Проверяем сам текущий узел (вдруг весь файл — это один большой template)
		    if (node != null && string.Equals(node.key, "template", StringComparison.OrdinalIgnoreCase))
		    {
		        ProcessTemplateNode(node);
		        return; // Если это шаблон, внутрь него как в обычный DOM идти не нужно
		    }

		    // 2. Ищем template среди дочерних элементов
		    for (int i = node.children.Count - 1; i >= 0; i--)
		    {
		        Node_Element child = node.children[i];

		        if (string.Equals(child.key, "template", StringComparison.OrdinalIgnoreCase))
		        {
		            ProcessTemplateNode(child);
		            // Удаляем узел template из дерева обычных элементов
		            node.children.RemoveAt(i);
		        }
		        else
		        {
		            CollectTemplatesRecursive(child);
		        }
		    }
		}

		private void ProcessTemplateNode(Node_Element templateNode)
		{
			if (templateNode.components.Count == 0)
			{
				throw new InvalidOperationException("Блок template { ... } должен содержать хотя бы один компонент для определения типа!");
			}

			string firstCompName = templateNode.components[0].typeName;
			if (!componentTypes.TryGetValue(firstCompName, out Type compType))
			{
				throw new InvalidOperationException($"Неизвестный компонент '{firstCompName}' в определении template!");
			}

			if (templates.ContainsKey(compType))
			{
				throw new InvalidOperationException($"Обнаружено дублирование шаблона для типа '{compType.Name}'! Шаблон этого типа уже объявлен.");
			}

			// Чертеж шаблона строки — это ВСЕГДА сам templateNode!
			templates[compType] = new CanvasTemplate(compType, templateNode);
		}
        
        public void SetDefaultResources(Material combinedMat, Material textMat, TMPro.TMP_FontAsset font)
        {
	        defaultCombinedMaterial = combinedMat;
	        defaultTextMaterial = textMat;
	        defaultFont = font;
        }
    }
}
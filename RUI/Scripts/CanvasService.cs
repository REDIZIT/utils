using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace InGame.UI
{
    public class CanvasService
    {
        public DiContainer container;
        public Dictionary<string, Type> componentTypes = new();
        public Dictionary<string, Type> composerTypes = new();
        public Dictionary<Type, CanvasTemplate> templates = new();
        
        public readonly Dictionary<string, SubpixelFont> subpixelFontCache = new();
        public Material defaultSubpixelMaterial;
        public byte[] defaultFontBytes;
        
        public Material defaultCombinedMaterial;
        public UIAssetDatabase assetDatabase;
        
        public CanvasService(DiContainer container)
        {
            this.container = container;
            
            RegisterAllComponentsFromAssembly(typeof(CanvasComponent).Assembly);
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

        public void CollectTemplates(Node_Element rootNode)
        {
	        CollectTemplatesRecursive(rootNode);
        }

        private void CollectTemplatesRecursive(Node_Element node)
        {
	        if (node == null) return;

	        // 1. Проверяем, является ли сам узел шаблоном (для случая, когда весь файл — один шаблон)
	        if (node.isTemplate)
	        {
		        ProcessTemplateNode(node);
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
			        ProcessTemplateNode(child);
			        // Удаляем узел шаблона из списка детей, чтобы он не попал в обычный рендеринг
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
	        // Ищем тип компонента (например, HierarchyLot) прямо в этом узле или его детях
	        Type compType = FindFirstComponentType(templateNode);
    
	        if (compType == null)
	        {
		        Debug.LogError($"[RUI] Ошибка в шаблоне: не найден компонент для определения типа!");
		        return;
	        }

	        if (templates.ContainsKey(compType))
	        {
		        // Вместо исключения просто перезаписываем (полезно для Hot Reload)
		        templates[compType] = new CanvasTemplate(compType, templateNode);
	        }
	        else
	        {
		        templates[compType] = new CanvasTemplate(compType, templateNode);
	        }
    
	        Debug.Log($"<color=lime>[RUI]</color> Зарегистрирован шаблон для типа: <b>{compType.Name}</b>");
        }

		// Рекурсивный поиск первого попавшегося компонента в AST узле
		private Type FindFirstComponentType(Node_Element node)
		{
		    if (node.components.Count > 0)
		    {
		        if (componentTypes.TryGetValue(node.components[0].typeName, out Type t))
		            return t;
		    }

		    foreach (var child in node.children)
		    {
		        var found = FindFirstComponentType(child);
		        if (found != null) return found;
		    }
		    return null;
		}
        
        public void SetDefaultResources(Material combinedMat)
        {
	        defaultCombinedMaterial = combinedMat;
        }

        public void ClearSubpixelCache()
        {
	        foreach (var font in subpixelFontCache.Values)
	        {
		        font.Dispose();
	        }
	        subpixelFontCache.Clear();
        }

		// Получить или создать шрифт заданного имени и размера
        public SubpixelFont GetOrCreateSubpixelFont(string fontName, int fontSize, UIAssetDatabase assetDb)
        {
	        string normName = UIAssetDatabase.NormalizeKey(fontName);
	        string cacheKey = $"{normName}:{fontSize}";

	        if (subpixelFontCache.TryGetValue(cacheKey, out SubpixelFont font))
		        return font;

	        // Ищем байты шрифта в UIAssetDatabase, если имя указано
	        byte[] targetBytes = null;
	        if (!string.IsNullOrEmpty(normName) && assetDb != null)
	        {
		        targetBytes = assetDb.GetFontBytes(normName);
	        }

	        // Если по имени не нашли — берем дефолтный шрифт
	        if (targetBytes == null)
	        {
		        targetBytes = defaultFontBytes;
	        }

	        if (targetBytes == null)
	        {
		        Debug.LogError($"[CanvasService] Не найдены байты для шрифта '{fontName}' и нет дефолтного шрифта!");
		        return null;
	        }

	        SubpixelFont newFont = new SubpixelFont(targetBytes, fontSize);
	        subpixelFontCache[cacheKey] = newFont;
	        return newFont;
        }

		// Перегрузка для дефолтного шрифта
        public SubpixelFont GetOrCreateSubpixelFont(int fontSize)
        {
	        return GetOrCreateSubpixelFont(string.Empty, fontSize, null);
        }
        
        public void SetDefaultSubpixelResources(byte[] fontBytes, Material subpixelMaterial)
        {
	        defaultFontBytes = fontBytes;
	        defaultSubpixelMaterial = subpixelMaterial;
	        ClearSubpixelCache();
        }
    }
}
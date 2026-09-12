using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
    public class CanvasService
    {
        public readonly Dictionary<string, SubpixelFont> subpixelFontCache = new();
        public Material defaultSubpixelMaterial;
        public byte[] defaultFontBytes;
        
        public Material defaultCombinedMaterial;
        public Assets assetDatabase;

        public Module module = new();
        
        private readonly Dictionary<Type, Func<CanvasComponent>> fastFactories = new();
        
        // Кэш материалов: ключ = (BaseMaterial.GetInstanceID(), Texture.GetInstanceID())
        private readonly Dictionary<(EntityId, EntityId), Material> materialCache = new();
        
        public CanvasService()
        {
            RegisterAssembly(typeof(CanvasComponent).Assembly);
        }

        public void RegisterAssembly(Assembly assembly)
        {
	        module.RegisterAllComponentsFromAssembly(assembly);
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
        public SubpixelFont GetOrCreateSubpixelFont(string fontName, int fontSize, Assets assetDb)
        {
	        string normName = Assets.NormalizeKey(fontName);
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
        
        public void SetDefaultSubpixelResources(byte[] fontBytes, Material subpixelMaterial)
        {
	        defaultFontBytes = fontBytes;
	        defaultSubpixelMaterial = subpixelMaterial;
	        ClearSubpixelCache();
        }
        
        public CanvasComponent InstantiateComponent(Type type, DiContainer diContainer)
        {
	        // Стандартные встроенные компоненты RUI создаем мгновенно через new()!
	        if (type == typeof(Image)) 
	        {
		        var img = new Image();
		        img.canvasService = this;
		        return img;
	        }
	        if (type == typeof(Button)) 
	        {
		        return new Button();
	        }
	        if (type == typeof(Label)) 
	        {
		        var lbl = new Label();
		        lbl.canvasService = this;
		        lbl.assetDatabase = this.assetDatabase;
		        return lbl;
	        }
	        if (type == typeof(TextBox))
	        {
		        var tb = new TextBox();
		        tb.canvasService = this;
		        tb.assetDatabase = this.assetDatabase;
		        return tb;
	        }

	        // Для остальных типов кэшируем делегат Activator
	        if (!fastFactories.TryGetValue(type, out var factory))
	        {
		        // Проверяем: есть ли у класса конструктор с параметрами или [Inject] методы
		        // Если это кастомный скрипт с инъекциями — вызываем Zenject
		        // Иначе — быстрый Activator
		        factory = () => (CanvasComponent)diContainer.Instantiate(type);
		        fastFactories[type] = factory;
	        }

	        return factory();
        }
        
        public Material GetOrCreateMaterial(Material baseMaterial, Texture texture)
        {
	        if (baseMaterial == null) return null;
	        if (texture == null) return baseMaterial;

	        var key = (baseMaterial.GetEntityId(), texture.GetEntityId());
	        if (materialCache.TryGetValue(key, out var cachedMat) && cachedMat != null)
		        return cachedMat;

	        var newMat = new Material(baseMaterial)
	        {
		        name = $"{baseMaterial.name}_{texture.name}",
		        mainTexture = texture
	        };
	        materialCache[key] = newMat;
	        return newMat;
        }

        public CanvasTemplate GetTemplate(Type compType)
        {
	        if (TryGetTemplate(compType, out CanvasTemplate template)) return template;
	        else throw new($"Template of type '{compType.Name}' not found");
        }
        
        public bool TryGetTemplate(Type compType, out CanvasTemplate template)
        {
	        return module.templates.TryGetValue(compType, out template);
        }
    }
}
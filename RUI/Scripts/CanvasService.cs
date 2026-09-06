using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace REDIZIT.RUI
{
    public class CanvasService
    {
        public DiContainer container;
       
        public readonly Dictionary<string, SubpixelFont> subpixelFontCache = new();
        public Material defaultSubpixelMaterial;
        public byte[] defaultFontBytes;
        
        public Material defaultCombinedMaterial;
        public UIAssetDatabase assetDatabase;

        public Module module = new();
        
        public CanvasService(DiContainer container)
        {
            this.container = container;
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
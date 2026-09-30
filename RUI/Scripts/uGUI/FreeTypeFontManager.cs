using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace REDIZIT.RUI
{
    [ExecuteAlways]
    public class FreeTypeFontManager : MonoBehaviour
    {
        [Serializable]
        public class FontDefinition
        {
            public string fontName = "Main";
            public UnityEngine.Object fontAsset; // Перетаскивайте .ttf, .otf или TextAsset
        }

        [SerializeField] private List<FontDefinition> fonts = new();

        private static FreeTypeFontManager _instance;
        public static FreeTypeFontManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindFirstObjectByType<FreeTypeFontManager>();
                }
                return _instance;
            }
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public readonly string fontName;
            public readonly int fontSize;
            public readonly int hinting;

            public CacheKey(string fontName, int fontSize, int hinting)
            {
                this.fontName = fontName;
                this.fontSize = fontSize;
                this.hinting = hinting;
            }

            public bool Equals(CacheKey other) =>
                fontName == other.fontName && fontSize == other.fontSize && hinting == other.hinting;

            public override bool Equals(object obj) => obj is CacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (fontName != null ? fontName.GetHashCode() : 0);
                    hash = (hash * 397) ^ fontSize;
                    hash = (hash * 397) ^ hinting;
                    return hash;
                }
            }
        }

        private readonly Dictionary<CacheKey, FreeTypeFont> _runtimeFonts = new();

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            ClearCache();
        }

        public void ClearCache()
        {
            foreach (var f in _runtimeFonts.Values) f.Dispose();
            _runtimeFonts.Clear();
        }

        public string[] GetAvailableFontNames()
        {
            if (fonts == null || fonts.Count == 0) return Array.Empty<string>();
            string[] names = new string[fonts.Count];
            for (int i = 0; i < fonts.Count; i++)
            {
                names[i] = string.IsNullOrEmpty(fonts[i].fontName) ? $"Font_{i}" : fonts[i].fontName;
            }
            return names;
        }

        public FreeTypeFont GetFont(string fontName, int fontSize, FreeTypeHinting hinting)
        {
            if (string.IsNullOrEmpty(fontName)) return null;

            var key = new CacheKey(fontName, fontSize, (int)hinting);
            if (_runtimeFonts.TryGetValue(key, out FreeTypeFont existing))
            {
                return existing;
            }

            FontDefinition def = fonts.Find(f => f.fontName == fontName);
            if (def == null || def.fontAsset == null) return null;

            byte[] bytes = LoadFontBytes(def.fontAsset);
            if (bytes == null || bytes.Length == 0) return null;

            var newFont = new FreeTypeFont(bytes, fontSize, (int)hinting);
            _runtimeFonts[key] = newFont;
            return newFont;
        }

        private byte[] LoadFontBytes(UnityEngine.Object asset)
        {
            if (asset is TextAsset textAsset) return textAsset.bytes;

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
                return File.ReadAllBytes(assetPath);
#endif
            return null;
        }
    }
}
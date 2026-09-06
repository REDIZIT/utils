using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace REDIZIT.RUI
{
    public class UIAssetDatabase : IInitializable, IDisposable
    {
        public CanvasService canvasService;

        public readonly Dictionary<string, string> fileSources = new Dictionary<string, string>();
        public readonly Dictionary<string, Node_Root> fileAsts = new Dictionary<string, Node_Root>();

        // Каталог байтов шрифтов: ключ - нормализованное имя (например "segoe-ui")
        public readonly Dictionary<string, byte[]> fontBytes = new Dictionary<string, byte[]>();
        public readonly Dictionary<string, string> fontPaths = new Dictionary<string, string>();

        // Каталог спрайтов (по имени файла)
        public readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public FileSystemWatcher folderWatcher;
        public string rootFolderPath;

        public event Action<string> onFileChanged;

        public bool isPendingReload;
        public float reloadCooldown;
        public string changedFilePath;

        public UIAssetDatabase(CanvasService canvasService)
        {
            this.canvasService = canvasService;
            this.canvasService.assetDatabase = this;
        }

        public void Initialize()
        {
            string uiPath = Application.dataPath;
            rootFolderPath = Path.GetFullPath(uiPath);

            ScanAndRegisterAll();
            SetupFolderWatcher();
        }

        public static string NormalizeKey(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;
            return rawName.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "").Replace(" ", "");
        }

        public void ScanAndRegisterAll()
        {
            if (!Directory.Exists(rootFolderPath)) return;

            canvasService.templates.Clear();
            fileSources.Clear();
            fileAsts.Clear();
            fontBytes.Clear();
            fontPaths.Clear();
            sprites.Clear();

            string[] files = Directory.GetFiles(rootFolderPath, "*.*", SearchOption.AllDirectories);

            List<string> foundUiFiles = new List<string>();
            List<string> foundFonts = new List<string>();
            List<string> foundSprites = new List<string>();

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string ext = Path.GetExtension(path).ToLowerInvariant();
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
                string key = NormalizeKey(fileNameWithoutExt);

                // 1. Разметка UI
                if (ext == ".ui")
                {
                    LoadAndProcessFile(path);
                    foundUiFiles.Add(Path.GetFileName(path));
                }
                // 2. Шрифты (TrueType / OpenType)
                else if (ext == ".ttf" || ext == ".otf")
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        fontBytes[key] = bytes;
                        fontPaths[key] = path;
                        foundFonts.Add($"{fileNameWithoutExt} ({ext})");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UIAssetDatabase] Ошибка чтения шрифта '{path}': {ex.Message}");
                    }
                }
                // 3. Изображения / Спрайты
                else if (ext == ".png" || ext == ".jpg")
                {
#if UNITY_EDITOR
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
                    Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                    if (spr != null)
                    {
                        sprites[key] = spr;
                        foundSprites.Add(fileNameWithoutExt);
                    }
#endif
                }
            }

            // Подробный читаемый отчет в консоль
            string report = $"<color=cyan>[UIAssetDatabase]</color> Сканирование завершено:\n" +
                            $" ├─ UI Файлы ({foundUiFiles.Count}): {string.Join(", ", foundUiFiles)}\n" +
                            $" ├─ Шаблоны ({canvasService.templates.Count}): {string.Join(", ", canvasService.templates.Keys.Select(k => k.Name))}\n" +
                            $" ├─ Шрифты ({fontBytes.Count}): {string.Join(", ", foundFonts)}\n" +
                            $" └─ Спрайты ({sprites.Count}): {string.Join(", ", foundSprites)}";
            Debug.Log(report);
        }

        public byte[] GetFontBytes(string fontName)
        {
            string key = NormalizeKey(fontName);
            if (fontBytes.TryGetValue(key, out byte[] bytes))
                return bytes;

            return null;
        }

        public Sprite GetSprite(string spriteName)
        {
            string key = NormalizeKey(spriteName);
            if (sprites.TryGetValue(key, out Sprite spr))
                return spr;

            return null;
        }

        public void LoadAndProcessFile(string path)
        {
	        string text = null;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    text = reader.ReadToEnd();
                }

                fileSources[path] = text;

                Tokenizer tokenizer = new Tokenizer();
                List<Token> tokens = tokenizer.Tokenize(text);
                Node_Root rootNode = Parser.Parse(tokens);

                fileAsts[path] = rootNode;

                for (int i = 0; i < rootNode.elements.Count; i++)
                {
                    canvasService.CollectTemplates(rootNode.elements[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIAssetDatabase Error] Ошибка при загрузке '{Path.GetFileName(path)}': {ex.Message}\ntext: '{text}'");
            }
        }

        public Node_Root GetAst(string pathOrFileName)
        {
            if (fileAsts.TryGetValue(pathOrFileName, out Node_Root root))
                return root;

            foreach (var kvp in fileAsts)
            {
                if (Path.GetFileName(kvp.Key).Equals(pathOrFileName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return null;
        }

        private void SetupFolderWatcher()
        {
            if (!Directory.Exists(rootFolderPath)) return;

            folderWatcher = new FileSystemWatcher(rootFolderPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            folderWatcher.Changed += OnFileWatcherEvent;
            folderWatcher.Created += OnFileWatcherEvent;
            folderWatcher.Renamed += (s, e) => OnFileWatcherEvent(s, e);

            folderWatcher.EnableRaisingEvents = true;
        }

        private void OnFileWatcherEvent(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (ext == ".ui" || ext == ".ttf" || ext == ".otf")
            {
                changedFilePath = e.FullPath;
                isPendingReload = true;
                reloadCooldown = 0.05f;
            }
        }

        public void Update()
        {
            if (!isPendingReload) return;

            reloadCooldown -= Time.unscaledDeltaTime;
            if (reloadCooldown <= 0f)
            {
                isPendingReload = false;
                if (!string.IsNullOrEmpty(changedFilePath) && File.Exists(changedFilePath))
                {
                    string ext = Path.GetExtension(changedFilePath).ToLowerInvariant();
                    if (ext == ".ui")
                    {
                        LoadAndProcessFile(changedFilePath);
                    }
                    else if (ext == ".ttf" || ext == ".otf")
                    {
                        string key = NormalizeKey(Path.GetFileNameWithoutExtension(changedFilePath));
                        fontBytes[key] = File.ReadAllBytes(changedFilePath);
                        canvasService.ClearSubpixelCache(); // Сбрасываем кэш шрифтов в сервисе
                    }

                    onFileChanged?.Invoke(changedFilePath);
                    Debug.Log($"<color=lime>[UIAssetDatabase HotReload]</color> Обновлен ассет: {Path.GetFileName(changedFilePath)}");
                }
            }
        }

        public void Dispose()
        {
            if (folderWatcher != null)
            {
                folderWatcher.EnableRaisingEvents = false;
                folderWatcher.Dispose();
                folderWatcher = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zenject;

namespace InGame.UI
{
    public class UIAssetDatabase : IInitializable, IDisposable
    {
        public CanvasService canvasService;

        // Кеш загруженного исходного текста файлов по относительному или полному пути
        public readonly Dictionary<string, string> fileSources = new Dictionary<string, string>();
        
        // Кеш спарсенных AST-деревьев файлов
        public readonly Dictionary<string, Node_Root> fileAsts = new Dictionary<string, Node_Root>();

        public FileSystemWatcher folderWatcher;
        public string rootFolderPath;

        // Событие, когда любой UI-файл в проекте изменился (для Hot Reload)
        public event Action<string> onFileChanged;

        public bool isPendingReload;
        public float reloadCooldown;
        public string changedFilePath;

        public UIAssetDatabase(CanvasService canvasService)
        {
            this.canvasService = canvasService;
        }

        public void Initialize()
        {
            string uiPath = Application.dataPath;

            rootFolderPath = Path.GetFullPath(uiPath);

            ScanAndRegisterAll();
            SetupFolderWatcher();
        }

        // Полное сканирование и регистрация всех .ui и .txt файлов в папке
        public void ScanAndRegisterAll()
        {
            if (!Directory.Exists(rootFolderPath)) return;

            canvasService.templates.Clear();
            fileSources.Clear();
            fileAsts.Clear();
            
            string[] files = Directory.GetFiles(rootFolderPath, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                string ext = Path.GetExtension(path).ToLowerInvariant();

                // Фильтруем расширения UI разметки (в будущем сюда добавим .style)
                if (ext == ".ui")
                {
                    LoadAndProcessFile(path);
                }
            }

            // Debug.Log($"<color=cyan>[UIAssetDatabase]</color> Сканирование завершено. Зарегистрировано шаблонов: {canvasService.templates.Count} at '{rootFolderPath}'");
        }

        public void LoadAndProcessFile(string path)
        {
            try
            {
                string text;
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

                // Собираем шаблоны из каждого файла в общий глобальный реестр CanvasService!
                for (int i = 0; i < rootNode.elements.Count; i++)
                {
                    canvasService.CollectTemplates(rootNode.elements[i]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIAssetDatabase Error] Ошибка при загрузке '{Path.GetFileName(path)}': {ex.Message}");
            }
        }

        // Получить AST-корень файла по пути или имени файла
        public Node_Root GetAst(string pathOrFileName)
        {
            // Сначала пробуем прямой путь
            if (fileAsts.TryGetValue(pathOrFileName, out Node_Root root))
                return root;

            // Ищем по имени файла (например "WorkspaceHierarchy.ui")
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
            if (ext == ".ui")
            {
                changedFilePath = e.FullPath;
                isPendingReload = true;
                reloadCooldown = 0.05f;
            }
        }

        // Вызывается в Update холста или тике системы
        public void Update()
        {
            if (!isPendingReload) return;

            reloadCooldown -= Time.unscaledDeltaTime;
            if (reloadCooldown <= 0f)
            {
                isPendingReload = false;
                if (!string.IsNullOrEmpty(changedFilePath) && File.Exists(changedFilePath))
                {
                    LoadAndProcessFile(changedFilePath);
                    onFileChanged?.Invoke(changedFilePath);
                    Debug.Log($"<color=lime>[UIAssetDatabase HotReload]</color> Обновлен файл: {Path.GetFileName(changedFilePath)}");
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
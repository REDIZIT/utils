using System;
using System.IO;
using UnityEngine;

namespace InGame.UI
{
	public class ObservableCanvasAsset : IDisposable
	{
		public string filePath;
		public CanvasElement rootElement;
		public Node_Root ast;

		public event Action onReloaded;

		public FileSystemWatcher watcher;
		public CanvasService service;

		public bool isPendingReload;
		public float reloadCooldown;

		public ObservableCanvasAsset(string fullPath, CanvasService service)
		{
			this.filePath = fullPath;
			this.service = service;

			// Начальная загрузка
			ReloadNow();

			// Запускаем отслеживание файла на диске
			SetupWatcher();
		}

		private void SetupWatcher()
		{
			string directory = Path.GetDirectoryName(filePath);
			string fileName = Path.GetFileName(filePath);

			if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
				return;

			watcher = new FileSystemWatcher(directory, fileName)
			{
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
			};

			watcher.Changed += OnFileChanged;
			watcher.Created += OnFileChanged;
			watcher.Renamed += (s, e) => OnFileChanged(s, e);

			watcher.EnableRaisingEvents = true;
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			// Событие приходит из другого потока: просто взводим флаг
			isPendingReload = true;
			reloadCooldown = 0.05f; // Небольшая задержка от повторных срабатываний редактора
		}

		// Вызывается в главном потоке из Update()
		public void Update()
		{
			if (!isPendingReload) return;

			reloadCooldown -= Time.unscaledDeltaTime;
			if (reloadCooldown <= 0f)
			{
				isPendingReload = false;
				ReloadNow();
			}
		}

		public void ReloadNow()
		{
			if (!File.Exists(filePath))
			{
				Debug.LogError($"[ObservableCanvasAsset] Файл не найден: {filePath}");
				return;
			}

			try
			{
				// Читаем файл с FileShare.ReadWrite, чтобы избежать ошибок блокировки файла редактором кода
				string sourceText;
				using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(fs))
				{
					sourceText = reader.ReadToEnd();
				}

				Tokenizer tokenizer = new Tokenizer();
				var tokens = tokenizer.Tokenize(sourceText);
				Node_Root newAst = Parser.Parse(tokens);

				if (newAst.elements.Count == 0) return;

				ast = newAst;
				Node_Element rootNode = newAst.elements[0];

				// 1. Собираем и валидируем шаблоны
				service.CollectTemplates(rootNode);

				if (rootElement == null)
				{
					rootElement = new CanvasElement();
					rootElement.key = rootNode.key;
				}

				// 2. Реконсилим DOM
				CanvasReconciler.Reconcile(rootElement, rootNode, service.componentTypes, service.container, service);
				CanvasReconciler.PostProcessBindings(rootElement, service);

				onReloaded?.Invoke();
				Debug.Log($"<color=lime>[HotReload]</color> UI файл успешно перезагружен: {Path.GetFileName(filePath)}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[HotReload Error] Ошибка при парсинге/обновлении {Path.GetFileName(filePath)}: {ex.Message}");
			}
		}

		public void Dispose()
		{
			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
				watcher.Dispose();
				watcher = null;
			}
		}
	}
}
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Zenject;
using Debug = UnityEngine.Debug;

namespace REDIZIT.RUI
{
	public class CanvasRenderer : MonoBehaviour
	{
		public static CanvasRenderer Instance { get; set; }

		public Material combinedMaterial;
		public TextAsset rootFile;

		private Material textMaterial;

		public readonly CanvasGenerationContext context = new();
		
		private CanvasElement root;
		private bool isDirty = true;
		private Camera cam;
		
		private float2 lastScreenSize;
		private string rootFilePath;

		[Inject] private CanvasService canvasService;
		[Inject] private Assets assetDatabase;
		[Inject] private CanvasReconciler reconciler;
		[Inject] private CanvasInputManager inputManager;
		[Inject] private ILogger<CanvasRenderer> logger;

		public void OnEnable()
		{
			Instance = this;
		}

		public void Start()
		{
			cam = Camera.main;
			
			if (canvasService == null || rootFile == null || assetDatabase == null) return;

			InitResources();
			LoadRootScreen();

			// Подписываемся на глобальное событие изменения любых UI файлов в проекте!
			assetDatabase.onFileChanged += OnAnyUIFileChanged;
		}

		private void InitResources()
		{
			// Материал для субпиксельного шейдера
			var subpixelShader = Shader.Find("REDIZIT/RUI/SubpixelText");
			Material subpixelMat = subpixelShader != null ? new Material(subpixelShader) : null;

			// Если в UIAssetDatabase нашелся любой шрифт — берем его дефолтным:
			byte[] defaultFont = assetDatabase.fontBytes.Values.FirstOrDefault();

			canvasService.SetDefaultSubpixelResources(defaultFont, subpixelMat);
			canvasService.SetDefaultResources(combinedMaterial);
		}

		private void LoadRootScreen()
		{
#if UNITY_EDITOR
			rootFilePath = Path.GetFullPath(AssetDatabase.GetAssetPath(rootFile));
#endif
			if (string.IsNullOrEmpty(rootFilePath)) return;

			Node_Root ast = assetDatabase.GetAst(rootFilePath);
			if (ast == null || ast.elements.Count == 0) return;

			Node_Element mainNode = ast.elements[0];

			if (root == null)
			{
				root = new() { key = mainNode.key };
				root.onTreeDirty = MarkDirty;
			}

			// 1. Сборка всего дерева узлов и компонентов
			reconciler.Reconcile(root, mainNode);

			// 2. Связывание всех полей по всему дереву (теперь ContextMenusUI гарантированно существует в памяти!)
			reconciler.PostProcessBindings(root);

			// 3. Вызов OnAttached у всех компонентов (WorkspaceHierarchyWindow создаст лоты, и они найдут ContextMenusUI!)
			reconciler.NotifyAttached();
    
			MarkDirty();
		}

		private void OnAnyUIFileChanged(string changedPath)
		{
			// Если изменился главный файл окна или любой файл шаблона — пересобираем DOM!
			LoadRootScreen();
		}

		public void OnDisable()
		{
			if (Instance == this) Instance = null;
			if (assetDatabase != null)
			{
				assetDatabase.onFileChanged -= OnAnyUIFileChanged;
			}
			context.Dispose();
		}

		public void MarkDirty() => isDirty = true;

		public void Update()
		{
			assetDatabase?.Update();

			if (root == null) return;

			// 1. Отслеживаем изменение размера Game View или разрешения экрана:
			float2 currentScreenSize = GetScreenSize();
			if (!math.all(currentScreenSize == lastScreenSize))
			{
				lastScreenSize = currentScreenSize;
				MarkDirty();
			}

			// 2. Update pass
			Stopwatch w = Stopwatch.StartNew();
			Stopwatch w1 = Stopwatch.StartNew();
			if (Application.isPlaying)
			{
				inputManager?.ProcessInput(root);
				root.UpdateTree();
			}
			w1.Stop();

			if (isDirty)
			{
				// 3. Layout pass
				Stopwatch w2 = Stopwatch.StartNew();
				SizeConstraints constraints = new()
				{
					x = AxisConstraints.LessOrEqual(currentScreenSize.x),
					y = AxisConstraints.LessOrEqual(currentScreenSize.y),
				};
				root.Measure(constraints);
				root.Arrange(new(0, currentScreenSize));
				w2.Stop();
	        
				// 4. Render pass
				Stopwatch w3 = Stopwatch.StartNew();
				context.Clear();
				root.RenderTree(context);
				context.FinalizeBatches();
				w3.Stop();
				w.Stop();
	        
				logger.LogDebug($"Canvas built in {w.ElapsedMilliseconds} ms (update: {w1.ElapsedMilliseconds}, layout: {w2.ElapsedMilliseconds}, render: {w3.ElapsedMilliseconds})");
	        
				isDirty = false;
			}
		}
		
		private float2 GetScreenSize()
		{
			// Use camera screen size instead of Screen.width/height
			// due to unstable behaviour while using EditorGUI functions
			return new(cam.pixelWidth, cam.pixelHeight);
		}
		
		/// <summary>
		/// Проверяет, находится ли текущая позиция курсора мыши над элементами UI.
		/// </summary>
		public bool IsPointerOverUI()
		{
		    return IsPointerOverUI(Input.mousePosition);
		}

		/// <summary>
		/// Проверяет, находится ли указанная экранная точка над элементами UI.
		/// </summary>
		public bool IsPointerOverUI(Vector2 screenPos)
		{
		    return Raycast(screenPos) != null;
		}

		/// <summary>
		/// Выполняет Raycast по дереву UI и возвращает самый верхний элемент под курсором (или null).
		/// </summary>
		public CanvasElement Raycast(Vector2 screenPos)
		{
		    if (root == null || !root.isEnabled) return null;
		    return RaycastRecursive(root, screenPos, null);
		}

		private CanvasElement RaycastRecursive(CanvasElement element, Vector2 screenPos, Rect? currentClipRect)
		{
		    if (!element.isEnabled) return null;

		    // 1. Учитываем маску (Mask), если она есть на текущем элементе
		    Mask mask = element.TryGetComponent<Mask>();
		    if (mask != null && mask.enabled)
		    {
		        Rect maskRect = mask.GetWorldClipRect();
		        currentClipRect = currentClipRect.HasValue 
		            ? IntersectRects(currentClipRect.Value, maskRect) 
		            : maskRect;
		    }

		    // Если точка отсечена внешней маской — глубже не идем
		    if (currentClipRect.HasValue)
		    {
		        Rect clip = currentClipRect.Value;
		        if (screenPos.x < clip.xMin || screenPos.x > clip.xMax ||
		            screenPos.y < clip.yMin || screenPos.y > clip.yMax)
		        {
		            return null;
		        }
		    }

		    // 2. Опрашиваем детей с учетом layerOffset
		    var children = element.Children;
		    if (children.Count > 0)
		    {
		        bool hasCustomLayers = false;
		        for (int i = 0; i < children.Count; i++)
		        {
		            if (children.ElementAt(i).layerOffset != 0)
		            {
		                hasCustomLayers = true;
		                break;
		            }
		        }

		        if (!hasCustomLayers)
		        {
		            // Быстрый путь без аллокаций: идем с конца к началу
		            for (int i = children.Count - 1; i >= 0; i--)
		            {
		                CanvasElement child = children.ElementAt(i);
		                CanvasElement hit = RaycastRecursive(child, screenPos, currentClipRect);
		                if (hit != null) return hit;
		            }
		        }
		        else
		        {
		            // Если у элементов есть разные слои: элементы с высоким слоем опрашиваются первыми
		            var sorted = children.OrderByDescending(c => c.layerOffset);
		            foreach (var child in sorted)
		            {
		                CanvasElement hit = RaycastRecursive(child, screenPos, currentClipRect);
		                if (hit != null) return hit;
		            }
		        }
		    }

		    // 3. Проверяем сам элемент: попадает ли точка в его физические границы на экране
		    Rect bounds = element.GetScreenBounds();
		    if (bounds.Contains(screenPos))
		    {
		        // Проверяем, блокирует ли этот элемент луч (есть ли на нем визуал или инпут)
		        if (IsRaycastBlocker(element))
		        {
		            return element;
		        }
		    }

		    return null;
		}

		/// <summary>
		/// Определяет, перехватывает ли узел рейкаст (не пустой ли это технический узел-контейнер).
		/// </summary>
		private bool IsRaycastBlocker(CanvasElement element)
		{
		    var comps = element.Components;
		    for (int i = 0; i < comps.Count; i++)
		    {
		        CanvasComponent comp = comps.ElementAt(i);
		        if (!comp.isEnabled) continue;

		        // Интерактивные элементы (кнопки, скроллы, кастомные кликабельные компоненты)
		        if (comp is Button || comp is ScrollView || 
		            comp is IPointerDownHandler || comp is IPointerScrollHandler)
		        {
		            return true;
		        }

		        // Визуальные элементы (фоновые плашки, иконки, текст)
		        if (comp is Image image)
		        {
		            // Не блокируем, если цвет полностью прозрачный и нет спрайта
		            if (image.color.a > 0.001f || image.sprite != null) return true;
		        }

		        if (comp is Label label && !string.IsNullOrEmpty(label.text))
		        {
		            return true;
		        }
		    }

		    return false;
		}

		private static Rect IntersectRects(Rect a, Rect b)
		{
			return Rect.MinMaxRect(
				math.max(a.xMin, b.xMin),
				math.max(a.yMin, b.yMin),
				math.min(a.xMax, b.xMax),
				math.min(a.yMax, b.yMax)
			);
		}
	}
}
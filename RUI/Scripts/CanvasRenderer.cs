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

			reconciler.Reconcile(root, mainNode);
			reconciler.PostProcessBindings(root);
            
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
	}
}
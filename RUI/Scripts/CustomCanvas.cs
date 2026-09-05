using System.IO;
using TMPro;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InGame.UI
{
    [ExecuteAlways]
    public class CustomCanvas : MonoBehaviour
    {
        public static CustomCanvas Instance { get; set; }

        public Material combinedMaterial;
        public TMP_FontAsset fontAsset;

        [Header("Главный файл экрана")]
        public TextAsset rootFile;

        private Material textMaterial;

        public readonly CanvasGenerationContext context = new CanvasGenerationContext();
        public CanvasElement root;
        public bool isDirty = true;

        [Inject] public CanvasService canvasService;
        [Inject] public UIAssetDatabase assetDatabase;

        private string rootFilePath;

        public void OnEnable()
        {
            Instance = this;
        }

        public void Start()
        {
            if (canvasService == null || rootFile == null || assetDatabase == null) return;

            InitResources();
            LoadRootScreen();

            // Подписываемся на глобальное событие изменения любых UI файлов в проекте!
            assetDatabase.onFileChanged += OnAnyUIFileChanged;
        }

        private void InitResources()
        {
	        // Загружаем байты шрифта (например, Segoe UI или LiberationSans)
	        string fontPath = "C:/Windows/Fonts/segoeui.ttf";
	        if (!File.Exists(fontPath)) fontPath = "C:/Windows/Fonts/arial.ttf";

	        byte[] fontBytes = File.Exists(fontPath) ? File.ReadAllBytes(fontPath) : null;

	        // Создаем базовый двухпроходный субпиксельный материал
	        var subpixelShader = Shader.Find("InGame/UI/SubpixelText");
	        Material subpixelMat = subpixelShader != null ? new Material(subpixelShader) : null;

	        // Регистрируем ресурсы в сервисе:
	        canvasService.SetDefaultSubpixelResources(fontBytes, subpixelMat);
	        canvasService.SetDefaultResources(combinedMaterial, null, fontAsset);
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
                root = new CanvasElement { key = mainNode.key };
                root.onTreeDirty = MarkDirty;
            }

            CanvasReconciler.Reconcile(root, mainNode, canvasService.componentTypes, canvasService.container, canvasService);
            CanvasReconciler.PostProcessBindings(root, canvasService);

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

	        if (Application.isPlaying)
	        {
		        root.UpdateTree();
	        }

	        if (isDirty)
	        {
		        root.SolveLayout();

		        context.Clear();
		        root.RenderTree(context);
		        context.FinalizeBatches();
		        isDirty = false;
	        }
        }
    }
}
using System.IO;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace REDIZIT.RUI
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
        [Inject] public CanvasReconciler reconciler;

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
                root = new CanvasElement { key = mainNode.key };
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

	        if (Application.isPlaying)
	        {
		        root.UpdateTree();
	        }

	        if (isDirty)
	        {
		        var screenConstraints = SizeConstraints.Tight(new(Screen.width, Screen.height));
		        root.SolveLayout(screenConstraints);

		        context.Clear();
		        root.RenderTree(context);
		        context.FinalizeBatches();
		        isDirty = false;
	        }
        }
    }
}
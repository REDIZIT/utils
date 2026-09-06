using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class ContextMenuService
	{
		private readonly CanvasService canvasService;
		private readonly CanvasReconciler reconciler;
		private readonly CanvasInputManager inputManager;

		private CanvasElement rootElement;

		private class ActiveMenu
		{
			public int level;
			public CanvasElement element;
		}

		private readonly List<ActiveMenu> activeMenus = new List<ActiveMenu>();

		public ContextMenuService(CanvasService canvasService, CanvasReconciler reconciler, CanvasInputManager inputManager)
		{
			this.canvasService = canvasService;
			this.reconciler = reconciler;
			this.inputManager = inputManager;

			// Слушаем клики мыши для закрытия меню мимо!
			this.inputManager.onGlobalPointerDown += OnGlobalPointerDown;
		}

		public void Attach(CanvasElement root)
		{
			this.rootElement = root;
		}

		public void Show(Vector2 screenPos, List<ContextMenuItem> items)
		{
			CloseAll();
			OpenSubmenu(screenPos, items, 0, Vector4.zero);
		}

		public void OpenSubmenu(Vector2 requestedPos, List<ContextMenuItem> items, int level, Vector4 parentBounds)
		{
			if (rootElement == null || items == null || items.Count == 0) return;

			// Закрываем меню того же уровня или выше
			CloseSubmenusAbove(level - 1);

			if (!canvasService.module.templates.TryGetValue(typeof(ContextMenuComponent), out var template))
			{
				Debug.LogError("[RUI] Шаблон 'ContextMenu' не найден!");
				return;
			}

			var menuElement = new CanvasElement
			{
				key = $"ContextMenu_Level_{level}",
				parent = rootElement,
				service = canvasService,
				reconciler = reconciler,
				// Каждый уровень меню получает +100 к слою отрисовки. 
				// Этого с запасом хватит, чтобы перекрыть любые внутренние слои Main UI (0-3).
				layerOffset = (level + 1) * 100 
			};

			reconciler.Reconcile(menuElement, template.templateAst);
			reconciler.PostProcessBindings(menuElement);

			var menuComp = menuElement.GetComponent<ContextMenuComponent>();
			menuComp.SetItems(items, this, level);
			
			// 1. Предварительный расчет верстки меню под его реальные размеры
			menuElement.SolveLayout(SizeConstraints.Loose(Screen.width, Screen.height));
			float2 menuSize = menuElement.transform.calculatedSize;

			// 2. Расчет позиции
			float x = requestedPos.x;
			float y = requestedPos.y - menuSize.y; // В Unity (0,0) внизу, поэтому открываем вниз от клика

			// Если меню вылезает за правый край экрана:
			if (x + menuSize.x > Screen.width)
			{
				if (level > 0 && parentBounds.x > 0)
					x = parentBounds.x - menuSize.x; // Подменю открываем слева от родительского пункта
				else
					x = Screen.width - menuSize.x - 4f;
			}
			if (x < 4f) x = 4f;

			// Если меню вылезает за нижний край экрана:
			if (y < 4f)
			{
				if (level > 0 && parentBounds.w > 0)
					y = parentBounds.y; // Подменю выравниваем по нижней кромке родителя
				else
					y = requestedPos.y; // Обычное меню открываем вверх от клика
			}
			if (y + menuSize.y > Screen.height)
			{
				y = Screen.height - menuSize.y - 4f;
			}

			menuElement.transform.localPos = new float2(x, y);

			rootElement.AddChild(menuElement);
			activeMenus.Add(new ActiveMenu { level = level, element = menuElement });

			rootElement.MarkDirty();
		}

		public void CloseSubmenusAbove(int level)
		{
			for (int i = activeMenus.Count - 1; i >= 0; i--)
			{
				if (activeMenus[i].level > level)
				{
					rootElement.RemoveChild(activeMenus[i].element);
					activeMenus.RemoveAt(i);
					rootElement.MarkDirty();
				}
			}
		}

		public void CloseAll()
		{
			if (activeMenus.Count == 0) return;

			for (int i = 0; i < activeMenus.Count; i++)
			{
				rootElement.RemoveChild(activeMenus[i].element);
			}
			activeMenus.Clear();
			rootElement.MarkDirty();
		}

		// ПРОВЕРКА КЛИКА МИМО МЕНЮ (Без ModalBackdrop!)
		private void OnGlobalPointerDown(PointerDownEvent e, List<CanvasElement> hits)
		{
			if (activeMenus.Count == 0) return;

			// Проверяем, попал ли клик внутрь хотя бы одного открытого меню или подменю
			bool hitInsideAnyMenu = false;

			for (int i = 0; i < hits.Count; i++)
			{
				var hit = hits[i];
				for (int m = 0; m < activeMenus.Count; m++)
				{
					if (IsChildOf(hit, activeMenus[m].element))
					{
						hitInsideAnyMenu = true;
						break;
					}
				}
				if (hitInsideAnyMenu) break;
			}

			// Если кликнули мимо — закрываем все меню!
			if (!hitInsideAnyMenu)
			{
				CloseAll();
			}
		}

		private bool IsChildOf(CanvasElement child, CanvasElement potentialParent)
		{
			var p = child;
			while (p != null)
			{
				if (p == potentialParent) return true;
				p = p.parent;
			}
			return false;
		}
	}
}
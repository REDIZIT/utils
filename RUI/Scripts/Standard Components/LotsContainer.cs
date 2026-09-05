using System;
using System.Collections.Generic;
using Zenject;

namespace InGame.UI
{
	public class LotsContainer : CanvasComponent
	{
		[Inject] public CanvasService canvasService;

		public void Refresh<TLot, TModel>(IEnumerable<TModel> models) where TLot : CanvasLot<TModel>
		{
			if (Element == null) return;

			// Достаем сохраненный зарегистрированный шаблон для типа TLot
			if (!canvasService.templates.TryGetValue(typeof(TLot), out CanvasTemplate canvasTemplate))
			{
				throw new InvalidOperationException($"Не найден зарегистрированный template для компонента типа '{typeof(TLot).Name}'!");
			}

			List<TModel> modelList = models != null ? new List<TModel>(models) : new List<TModel>();
			int targetCount = modelList.Count;
			int currentCount = Element.children.Count;

			// 1. Если элементов больше чем нужно — удаляем лишние с конца
			if (currentCount > targetCount)
			{
				int toRemove = currentCount - targetCount;
				Element.children.RemoveRange(targetCount, toRemove);
			}

			// 2. Если элементов не хватает — создаем новые из шаблона AST
			for (int i = currentCount; i < targetCount; i++)
			{
				CanvasElement lotElement = new CanvasElement();
				lotElement.parent = Element;

				// Передаем canvasService 5-м аргументом!
				CanvasReconciler.Reconcile(
					lotElement, 
					canvasTemplate.templateAst, 
					canvasService.componentTypes, 
					canvasService.container,
					canvasService);

				// Гарантированно связываем поля для только что созданной строки
				CanvasReconciler.PostProcessBindings(lotElement, canvasService);

				Element.children.Add(lotElement);
			}

			// 3. Обновляем модели у всех элементов через ICanvasLot.Refresh
			for (int i = 0; i < targetCount; i++)
			{
				CanvasElement childElement = Element.children[i];
				var lot = childElement.GetComponent<TLot>();

				if (lot == null)
				{
					throw new InvalidOperationException($"Дочерний элемент #{i} не содержит компонент '{typeof(TLot).Name}'!");
				}

				lot.Refresh(modelList[i]);
			}

			MarkDirty();
		}
	}
}
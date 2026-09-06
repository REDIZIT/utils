using System;
using System.Collections.Generic;
using Zenject;

namespace REDIZIT.RUI
{
	public class LotsContainer : CanvasComponent
	{
		[Inject] public CanvasService canvasService;
		[Inject] public CanvasReconciler reconciler; // Инжектим экземпляр!

		public void Refresh<TLot, TModel>(IEnumerable<TModel> models) 
			where TLot : CanvasLot<TModel>
		{
			if (Element == null) return;

			if (!canvasService.templates.TryGetValue(typeof(TLot), out CanvasTemplate canvasTemplate))
				return;

			var modelList = models != null ? new List<TModel>(models) : new List<TModel>();
			int targetCount = modelList.Count;

			// Синхронизация количества элементов
			while (Element.children.Count > targetCount)
				Element.children.RemoveAt(Element.children.Count - 1);

			while (Element.children.Count < targetCount)
			{
				CanvasElement lotElement = new CanvasElement { parent = Element };
                
				// Используем экземпляр реконсилера
				reconciler.Reconcile(lotElement, canvasTemplate.templateAst);

				reconciler.PostProcessBindings(lotElement);

				Element.children.Add(lotElement);
			}

			// Обновление данных
			for (int i = 0; i < targetCount; i++)
			{
				var lot = Element.children[i].GetComponent<TLot>();
				if (lot != null) lot.Refresh(modelList[i]);
			}

			MarkDirty();
		}
	}
}

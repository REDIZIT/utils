using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace REDIZIT.RUI
{
    public class LotsContainer : CanvasComponent
    {
        [Inject] public CanvasService canvasService;
        [Inject] public CanvasReconciler reconciler;

        public void Refresh<TLot, TModel>(IEnumerable<TModel> models) where TLot : CanvasLot<TModel>
        {
	        if (Element == null) return;

	        if (!canvasService.module.templates.TryGetValue(typeof(TLot), out CanvasTemplate canvasTemplate))
		        return;

	        IList<TModel> modelList = models as IList<TModel> ?? new List<TModel>(models);
	        int targetCount = modelList.Count;
	        int currentCount = Element.Children.Count;

	        // 1. УДАЛЯЕМ ЛИШНИЕ С КОНЦА (если список уменьшился)
	        if (currentCount > targetCount)
	        {
		        Element.RemoveChildren(targetCount, currentCount - targetCount);
	        }

	        // 2. ДОСОЗДАЕМ ТОЛЬКО НЕДОСТАЮЩИЕ (если список вырос)
	        // Если в прошлый раз уже было создано 600 элементов, этот цикл сделает 0 итераций!
	        for (int i = currentCount; i < targetCount; i++)
	        {
		        CanvasElement lotElement = new CanvasElement { parent = Element, service = canvasService, reconciler = reconciler };
        
		        reconciler.Reconcile(lotElement, canvasTemplate.templateAst);
		        reconciler.PostProcessBindings(lotElement);

		        Element.AddChild(lotElement);
	        }

	        // 3. МГНОВЕННОЕ ОБНОВЛЕНИЕ ДАННЫХ В ПУЛЕ
	        for (int i = 0; i < targetCount; i++)
	        {
		        var lot = Element.Children.ElementAt(i).TryGetComponent<TLot>();
		        lot?.Refresh(modelList[i]);
	        }

	        MarkDirty();
        }
    }
}
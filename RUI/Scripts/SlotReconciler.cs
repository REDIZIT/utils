using System;
using System.Linq;
using UnityEngine;

namespace REDIZIT.RUI
{
	public static class SlotReconciler
	{
		// Переиспользуемый билдер для исключения GC Alloc при каждом тике
		[ThreadStatic]
		private static SlotBuilder cachedBuilder;

		public static void Sync(this CanvasElement container, Action<SlotBuilder> buildAction)
		{
			if (container == null) return;

			// Ищем сервис и реконсилер вверх по иерархии
			var service = container.service;
			var reconciler = container.reconciler;

			if (service == null || reconciler == null)
			{
				var p = container.parent;
				while (p != null)
				{
					service ??= p.service;
					reconciler ??= p.reconciler;
					if (service != null && reconciler != null) break;
					p = p.parent;
				}
			}

			if (service == null || reconciler == null)
			{
				Debug.LogError("[RUI] Ошибка Sync: CanvasService или CanvasReconciler не найдены в иерархии.");
				return;
			}

			cachedBuilder ??= new();
			cachedBuilder.slots.Clear();

			buildAction(cachedBuilder);

			var desired = cachedBuilder.slots;
			var children = container.Children;
			int targetCount = desired.Count;
			bool changed = false;

			for (int i = 0; i < targetCount; i++)
			{
				SlotDescriptor slot = desired[i];
				CanvasElement child = null;

				// 1. Проверяем элемент на текущей позиции
				if (i < children.Count)
				{
					CanvasElement existing = children.ElementAt(i);
					if (string.Equals(existing.key, slot.key, StringComparison.OrdinalIgnoreCase) && existing.GetComponent(slot.lotType) != null)
					{
						child = existing;
					}
				}

				// 2. Если на этой позиции другой элемент, ищем совпадение дальше по списку
				if (child == null)
				{
					int foundIdx = -1;
					for (int j = i + 1; j < children.Count; j++)
					{
						CanvasElement c = children.ElementAt(j);
						if (string.Equals(c.key, slot.key, StringComparison.OrdinalIgnoreCase) && c.GetComponent(slot.lotType) != null)
						{
							foundIdx = j;
							break;
						}
					}

					if (foundIdx != -1)
					{
						child = children.ElementAt(foundIdx);
						container.RemoveChild(foundIdx);
						container.InsertChild(i, child);
						changed = true;
					}
					else
					{
						// 3. Создаем новый элемент из шаблона slot.lotType
						if (!service.module.templates.TryGetValue(slot.lotType, out CanvasTemplate template))
						{
							Debug.LogError($"[RUI] Шаблон для типа '{slot.lotType.Name}' не зарегистрирован!");
							continue;
						}

						child = reconciler.Spawn(template, container, slot.key);

						changed = true;
					}
				}

				// 4. Мгновенно обновляем данные модели
				if (child.GetComponent(slot.lotType) is ICanvasLot lot && slot.model != null)
				{
					lot.RefreshUntyped(slot.model);
				}
			}

			// 5. Удаляем лишние элементы с конца
			if (children.Count > targetCount)
			{
				container.RemoveChildren(targetCount, children.Count - targetCount);
				changed = true;
			}

			if (changed)
			{
				container.MarkDirty();
			}
		}
	}
}
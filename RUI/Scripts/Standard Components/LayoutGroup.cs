using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace InGame.UI
{
	public class LayoutGroup : CanvasComponent
	{
		public LayoutDirection direction = LayoutDirection.Vertical;
		public LayoutAlignment alignment = LayoutAlignment.Begin;
        
		// X = Left, Y = Bottom, Z = Right, W = Top (в экранных пикселях)
		public float4 padding = float4.zero;
		public float spacing = 0f;
		public bool fitContent = true;

		public void Solve()
		{
			if (Element == null) return;

			var children = Element.children;

			// 1. Сначала рекурсивно вызываем Solve у всех вложенных LayoutGroup снизу-вверх (Bottom-Up)
			for (int i = 0; i < children.Count; i++)
			{
				var childLayout = children[i].GetComponent<LayoutGroup>();
				childLayout?.Solve();
			}

			// 2. Собираем активных участников верстки
			var layoutChildren = new List<CanvasElement>();
			for (int i = 0; i < children.Count; i++)
			{
				var child = children[i];
				var le = child.GetComponent<LayoutElement>();
				if (le != null && le.ignoreLayout) continue;

				layoutChildren.Add(child);
			}

			if (layoutChildren.Count == 0)
			{
				if (fitContent)
				{
					if (direction == LayoutDirection.Vertical) Transform.size.y = padding.y + padding.w;
					else Transform.size.x = padding.x + padding.z;
				}
				return;
			}

			// 3. Первый проход: считаем суммарный размер контента по главной оси
			float totalContentLength = 0f;

			for (int i = 0; i < layoutChildren.Count; i++)
			{
				var child = layoutChildren[i];
				var le = child.GetComponent<LayoutElement>();

				if (le != null) totalContentLength += le.spacingBefore;

				totalContentLength += (direction == LayoutDirection.Horizontal)
					? child.transform.size.x
					: child.transform.size.y;

				if (i < layoutChildren.Count - 1)
					totalContentLength += spacing;

				if (le != null) totalContentLength += le.spacingAfter;
			}

			// 4. Если fitContent = true — меняем размер текущего контейнера
			if (fitContent)
			{
				if (direction == LayoutDirection.Horizontal) Transform.size.x = padding.x + totalContentLength + padding.z;
				else Transform.size.y = padding.y + totalContentLength + padding.w;
			}

			// 5. Второй проход: расставляем позиции детей (напоминаем: в нашем Canvas (0,0) — левый нижний угол)
			float availableLength = (direction == LayoutDirection.Horizontal) ? Transform.size.x : Transform.size.y;
			float totalPadding = (direction == LayoutDirection.Horizontal) ? (padding.x + padding.z) : (padding.y + padding.w);

			// Начальная точка в зависимости от выравнивания
			float cursor = 0f;
			if (direction == LayoutDirection.Horizontal)
			{
				if (alignment == LayoutAlignment.Begin) cursor = padding.x;
				else if (alignment == LayoutAlignment.Center) cursor = padding.x + (availableLength - totalPadding - totalContentLength) * 0.5f;
				else cursor = availableLength - padding.z - totalContentLength;
			}
			else // Vertical (идем сверху вниз: от Top к Bottom)
			{
				if (alignment == LayoutAlignment.Begin) cursor = Transform.size.y - padding.w; // верхний край
				else if (alignment == LayoutAlignment.Center) cursor = Transform.size.y - padding.w - (availableLength - totalPadding - totalContentLength) * 0.5f;
				else cursor = padding.y + totalContentLength;
			}
			
			

			// Применяем позиции
			for (int i = 0; i < layoutChildren.Count; i++)
			{
				var child = layoutChildren[i];
				var le = child.GetComponent<LayoutElement>();

				if (direction == LayoutDirection.Horizontal)
				{
					if (le != null) cursor += le.spacingBefore;

					// Выравнивание по поперечной оси (Y) по центру или по низу
					float yPos = padding.y;
					child.transform.localPos = new float2(cursor, yPos);

					cursor += child.transform.size.x + spacing;
					if (le != null) cursor += le.spacingAfter;
				}
				else // Vertical
				{
					if (le != null) cursor -= le.spacingBefore;

					// Двигаемся сверху вниз: от текущего курсора отнимаем высоту элемента
					cursor -= child.transform.size.y;

					float xPos = padding.x;
					child.transform.localPos = new float2(xPos, cursor);

					cursor -= spacing;
					if (le != null) cursor -= le.spacingAfter;
				}
			}
		}
	}
}
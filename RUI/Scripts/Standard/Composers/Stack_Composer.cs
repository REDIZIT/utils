using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public enum StackAxis
	{
		Vertical,
		Horizontal
	}

	public enum CrossFill
	{
		None,   // Дети сохраняют свой собственный DesiredSize по cross-оси
		Child,  // Стек подгоняется под самого крупного ребенка (hug), все дети растягиваются под него
		Parent  // Стек занимает весь доступный размер родителя, все дети растягиваются на всю ширину/высоту
	}

	public class Stack_Composer : CanvasComponent, IMeasurable, IComposer
	{
		public StackAxis axis = StackAxis.Vertical;
		public CrossFill fillCross = CrossFill.None;
		public float spacing = 0f;
		public float4 padding = 0f; // x: left, y: top, z: right, w: bottom
		public bool reverse = false;

		private float cachedMaxChildCross = 0f;

		public DesiredSize Measure(SizeConstraints constraints)
		{
			bool isVertical = axis == StackAxis.Vertical;
			
			Debug.Log("isVertical? " + isVertical);

			float padMain = isVertical ? (padding.y + padding.w) : (padding.x + padding.z);
			float padCross = isVertical ? (padding.x + padding.z) : (padding.y + padding.w);

			// 1. Уменьшаем ограничения родителя на размер padding для контента
			SizeConstraints innerConstraints = constraints;
			innerConstraints.Shrink(padding.x + padding.z, padding.y + padding.w);

			// 2. Формируем ограничения для детей:
			// Main-ось всегда неограничена (Unlimited)
			// Cross-ось транслирует оставшееся место
			SizeConstraints childConstraints = new()
			{
				x = isVertical ? innerConstraints.x : AxisConstraints.Unlimited(),
				y = isVertical ? AxisConstraints.Unlimited() : innerConstraints.y
			};

			float maxChildCross = 0f;
			float totalMain = 0f;
			int visibleCount = 0;

			// 3. Измеряем детей
			foreach (CanvasElement child in Element.Children)
			{
				if (!child.isEnabled) continue;

				DesiredSize childSize = child.Measure(childConstraints);
				float childMain = isVertical ? childSize.y : childSize.x;
				float childCross = isVertical ? childSize.x : childSize.y;

				maxChildCross = math.max(maxChildCross, childCross);
				totalMain += childMain;
				visibleCount++;
			}

			if (visibleCount > 1)
			{
				totalMain += spacing * (visibleCount - 1);
			}

			cachedMaxChildCross = maxChildCross;

			// 4. Определяем желаемый размер контента по Cross-оси в зависимости от режима fillCross
			float desiredCross = maxChildCross;
			if (fillCross == CrossFill.Parent)
			{
				AxisConstraints crossConstraint = isVertical ? innerConstraints.x : innerConstraints.y;
				if (crossConstraint.TryGetMax(out float maxCrossAvailable))
				{
					desiredCross = maxCrossAvailable;
				}
			}

			// 5. Суммируем с padding и зажимаем в исходные ограничения родителя
			float finalDesiredWidth = isVertical ? (desiredCross + padCross) : (totalMain + padMain);
			float finalDesiredHeight = isVertical ? (totalMain + padMain) : (desiredCross + padCross);

			return constraints.Clamp(new DesiredSize(finalDesiredWidth, finalDesiredHeight));
		}

		public void Arrange(ArrangeRect finalRect)
		{
			bool isVertical = axis == StackAxis.Vertical;

			// Правило инверсии: при Horizontal равен reverse, при Vertical = !reverse
			bool shouldReverse = isVertical ? !reverse : reverse;

			float padCrossStart = isVertical ? padding.x : padding.y;
			float padMainStart = isVertical ? padding.y : padding.x;

			// Доступное контенту пространство по поперечной оси
			float contentCrossSize = isVertical
				? math.max(0f, finalRect.size.x - (padding.x + padding.z))
				: math.max(0f, finalRect.size.y - (padding.y + padding.w));

			float currentMain = padMainStart;

			// Итерация без лишних аллокаций памяти (List<T> реализует IList<T>)
			IList<CanvasElement> childrenList = Element.Children as IList<CanvasElement> ?? Element.Children.ToList();
			int count = childrenList.Count;

			for (int i = 0; i < count; i++)
			{
				int index = shouldReverse ? (count - 1 - i) : i;
				CanvasElement child = childrenList[index];

				if (!child.isEnabled) continue;

				float childMain = isVertical ? child.DesiredSize.y : child.DesiredSize.x;
				float childCross;

				switch (fillCross)
				{
					case CrossFill.Parent:
						childCross = contentCrossSize;
						break;
					case CrossFill.Child:
						childCross = cachedMaxChildCross;
						break;
					case CrossFill.None:
					default:
						childCross = isVertical ? child.DesiredSize.x : child.DesiredSize.y;
						break;
				}

				float2 childPos = isVertical
					? new float2(padCrossStart, currentMain)
					: new float2(currentMain, padCrossStart);

				float2 childSize = isVertical
					? new float2(childCross, childMain)
					: new float2(childMain, childCross);

				child.Arrange(new ArrangeRect(childPos, childSize));

				currentMain += childMain + spacing;
			}
		}
	}
}
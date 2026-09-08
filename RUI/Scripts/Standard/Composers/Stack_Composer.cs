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
		None,
		Child,
		Parent
	}

	public enum StackAlign
	{
		Start,  // По умолчанию: сверху для Vertical, слева для Horizontal
		Center, // По центру главной оси
		End     // Снизу для Vertical, справа (snap=End) для Horizontal
	}

	public class Stack_Composer : CanvasComponent, IMeasurable, IComposer
	{
		public StackAxis axis = StackAxis.Vertical;
		public CrossFill fillCross = CrossFill.None;
		public StackAlign alignMain = StackAlign.Start;
		public float spacing = 0f;
		public float4 padding = 0f; // x: left, y: top, z: right, w: bottom
		public bool reverse = false;

		private float cachedMaxChildCross = 0f;

		public DesiredSize Measure(SizeConstraints constraints)
		{
			bool isVertical = axis == StackAxis.Vertical;

			float padMain = isVertical ? (padding.y + padding.w) : (padding.x + padding.z);
			float padCross = isVertical ? (padding.x + padding.z) : (padding.y + padding.w);

			SizeConstraints innerConstraints = constraints;
			innerConstraints.Shrink(padding.x + padding.z, padding.y + padding.w);

			SizeConstraints childConstraints = new()
			{
				x = isVertical ? innerConstraints.x : AxisConstraints.Unlimited(),
				y = isVertical ? AxisConstraints.Unlimited() : innerConstraints.y
			};

			float maxChildCross = 0f;
			float totalMain = 0f;
			int visibleCount = 0;

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

			float desiredCross = maxChildCross;
			if (fillCross == CrossFill.Parent)
			{
				AxisConstraints crossConstraint = isVertical ? innerConstraints.x : innerConstraints.y;
				if (crossConstraint.TryGetMax(out float maxCrossAvailable))
				{
					desiredCross = maxCrossAvailable;
				}
			}

			float finalDesiredWidth = isVertical ? (desiredCross + padCross) : (totalMain + padMain);
			float finalDesiredHeight = isVertical ? (totalMain + padMain) : (desiredCross + padCross);

			return constraints.Clamp(new DesiredSize(finalDesiredWidth, finalDesiredHeight));
		}

		public void Arrange(ArrangeRect finalRect)
		{
			bool isVertical = axis == StackAxis.Vertical;

			IList<CanvasElement> childrenList = Element.Children as IList<CanvasElement> ?? Element.Children.ToList();
			int count = childrenList.Count;

			// 1. Считаем суммарную длину всех видимых детей вдоль главной оси для расчета выравнивания (alignMain)
			float totalChildrenMain = 0f;
			int visibleCount = 0;
			for (int i = 0; i < count; i++)
			{
				CanvasElement child = childrenList[i];
				if (!child.isEnabled) continue;
				float childMain = isVertical ? child.DesiredSize.y : child.DesiredSize.x;
				totalChildrenMain += childMain;
				visibleCount++;
			}
			if (visibleCount > 1) totalChildrenMain += spacing * (visibleCount - 1);

			// 2. Расчет доступного пространства
			float padCrossStart = isVertical ? padding.x : padding.w; // w: отступ снизу для Horizontal
			float contentCrossSize = isVertical
				? math.max(0f, finalRect.size.x - (padding.x + padding.z))
				: math.max(0f, finalRect.size.y - (padding.y + padding.w));

			float availableMain = isVertical
				? math.max(0f, finalRect.size.y - (padding.y + padding.w))
				: math.max(0f, finalRect.size.x - (padding.x + padding.z));

			float freeMainSpace = math.max(0f, availableMain - totalChildrenMain);

			// 3. Начальная позиция курсора с учетом alignMain
			float currentMain;
			if (isVertical)
			{
				// Для вертикали Y=0 внизу, поэтому верх — это (finalRect.size.y - padding.y)
				float topStart = finalRect.size.y - padding.y;

				if (alignMain == StackAlign.Center) topStart -= freeMainSpace * 0.5f;
				else if (alignMain == StackAlign.End) topStart -= freeMainSpace; // Прижать к низу

				currentMain = topStart;
			}
			else
			{
				// Для горизонтали X=0 слева, поэтому левый край — это padding.x
				float leftStart = padding.x;

				if (alignMain == StackAlign.Center) leftStart += freeMainSpace * 0.5f;
				else if (alignMain == StackAlign.End) leftStart += freeMainSpace; // Прижать к правому краю (snap=End)

				currentMain = leftStart;
			}

			// 4. Размещение детей
			for (int i = 0; i < count; i++)
			{
				int index = reverse ? (count - 1 - i) : i;
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

				float2 childPos;
				float2 childSize;

				if (isVertical)
				{
					// Опускаем курсор вниз на высоту текущего элемента
					currentMain -= childMain;

					childPos = new float2(padCrossStart, currentMain);
					childSize = new float2(childCross, childMain);

					// Сдвигаем курсор дальше вниз с учетом spacing
					currentMain -= spacing;
				}
				else
				{
					childPos = new float2(currentMain, padCrossStart);
					childSize = new float2(childMain, childCross);

					// Сдвигаем курсор вправо с учетом spacing
					currentMain += childMain + spacing;
				}

				child.Arrange(new ArrangeRect(childPos, childSize));
			}
		}
	}
}
using Unity.Mathematics;
using UnityEngine;

namespace REDIZIT.RUI
{
	public class SizedBox_Composer : CanvasComponent, IMeasurable, IComposer
	{
		public float2 size;
		
		public DesiredSize Measure(SizeConstraints constraints)
		{
			// 1. SizedBox навязывает свой размер (учитывая ограничения родителя)
			DesiredSize mySize = constraints.Clamp(new(size));
			
			// 2. Создаем строгие ограничения (Tight Constraints) для детей.
			// Ребенок ДОЛЖЕН быть размером с этот SizedBox (или меньше, если вы так решите).
			SizeConstraints childConstraints = new SizeConstraints
			{
				x = AxisConstraints.Equal(mySize.x), // Вам нужно добавить метод Exactly(float v)
				y = AxisConstraints.Equal(mySize.y)
			};

			// 3. Обязательно просим детей измерить себя!
			foreach (CanvasElement child in Element.Children)
			{
				child.Measure(childConstraints);
			}

			// 4. Возвращаем свой размер наверх
			return mySize;
		}
		
		public void Arrange(ArrangeRect finalRect)
		{
			// SizedBox позиционирует детей ровно по своим границам.
			// Локальная позиция детей внутри SizedBox равна (0, 0)
			ArrangeRect childRect = new ArrangeRect(0, finalRect.size);
			
			foreach (CanvasElement child in Element.Children)
			{
				child.Arrange(childRect);
			}
		}
	}
}
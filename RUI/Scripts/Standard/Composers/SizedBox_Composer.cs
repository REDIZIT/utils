using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class SizedBox_Composer : IComposer
	{
		public CanvasElement e;

		public float2 Solve(SizeConstraints c)
		{
			float2 preferred = e.transform.size;
            
			// Если размер элемента не задан в разметке, опрашиваем компоненты (Label/Image)
			float2 contentSize = e.GetPreferredContentSize();
			if (preferred.x <= 0) preferred.x = contentSize.x;
			if (preferred.y <= 0) preferred.y = contentSize.y;

			// Ограничения для детей:
			// По ширине дети ограничены шириной этого SizedBox!
			float2 childMax = new float2(
				preferred.x > 0 ? preferred.x : c.max.x,
				preferred.y > 0 ? preferred.y : c.max.y
			);

			// Если на элементе висит ScrollView, он разблокирует бесконечность по оси Y
			if (e.GetComponent<ScrollView>() != null)
			{
				childMax.y = float.PositiveInfinity;
			}

			float2 childrenMax = float2.zero;
			for (int i = 0; i < e.children.Count; i++)
			{
				float2 childSize = e.children[i].SolveLayout(SizeConstraints.Loose(childMax));
				childrenMax = math.max(childrenMax, e.children[i].transform.localPos + childSize);
			}

			if (preferred.x <= 0) preferred.x = childrenMax.x;
			if (preferred.y <= 0) preferred.y = childrenMax.y;

			float2 finalSize = c.Constrain(preferred);
			e.transform.size = finalSize;
			return finalSize;
		}
	}
}
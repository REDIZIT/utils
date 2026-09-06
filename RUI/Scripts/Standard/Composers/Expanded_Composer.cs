using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Expanded_Composer : IComposer
	{
		public CanvasElement e;
		public float flex = 1f;

		public float2 Solve(SizeConstraints c)
		{
			// Решаем вложенных детей в пределах выделенного места
			for (int i = 0; i < e.children.Count; i++)
			{
				e.children[i].SolveLayout(SizeConstraints.Loose(c.max));
			}

			// Expanded всегда занимает ровно то пространство, которое ему выделил родитель
			float2 finalSize = c.max;
			if (float.IsInfinity(finalSize.x)) finalSize.x = e.transform.size.x;
			if (float.IsInfinity(finalSize.y)) finalSize.y = e.transform.size.y;

			e.transform.size = finalSize;
			return finalSize;
		}
	}
}
using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Fill_Composer : IComposer
	{
		public CanvasElement e;

		public float2 Solve(SizeConstraints c)
		{
			// ВАЖНО: Даем детям Loose ограничения, чтобы они НЕ растягивались принудительно,
			// если они сами этого не просят.
			SizeConstraints childConstraints = SizeConstraints.Loose(c.max);

			for (int i = 0; i < e.children.Count; i++)
			{
				e.children[i].SolveLayout(childConstraints);
			}

			// Сами занимаем максимум
			float2 finalSize = c.max;
			if (float.IsInfinity(finalSize.x)) finalSize.x = e.transform.size.x;
			if (float.IsInfinity(finalSize.y)) finalSize.y = e.transform.size.y;

			e.transform.size = finalSize;
			return finalSize;
		}
	}
}

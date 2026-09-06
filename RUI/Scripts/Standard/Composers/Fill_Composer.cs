using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Fill_Composer : IComposer
	{
		public CanvasElement e;

		public float2 Solve(SizeConstraints c)
		{
			CanvasTransform t = e.transform;

			for (int i = 0; i < e.children.Count; i++)
			{
				if (!e.children[i].isEnabled) continue;
				e.children[i].SolveLayout(SizeConstraints.Loose(c.maxX, c.maxY));
			}

			float2 finalSize = 0;
			finalSize.x = c.maxX ?? (t.width ?? 0f);
			finalSize.y = c.maxY ?? (t.height ?? 0f);

			t.calculatedSize = finalSize;
			return t.calculatedSize;
		}
	}
}
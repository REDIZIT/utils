using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Expanded_Composer : IComposer
	{
		public CanvasElement e;
		public float flex = 1f;

		public float2 Solve(SizeConstraints c)
		{
			CanvasTransform t = e.transform;

			for (int i = 0; i < e.children.Count; i++)
			{
				if (!e.children[i].isEnabled) continue;
				e.children[i].SolveLayout(SizeConstraints.Loose(c.maxX, c.maxY));
			}

			float2 contentSize = e.GetPreferredContentSize();

			float2 preferred = float2.zero;
			preferred.x = c.maxX ?? (t.width ?? contentSize.x);
			preferred.y = c.maxY ?? (t.height ?? contentSize.y);

			float2 finalSize = c.Constrain(preferred);
			t.calculatedSize = finalSize;
			return t.calculatedSize;
		}
	}
}
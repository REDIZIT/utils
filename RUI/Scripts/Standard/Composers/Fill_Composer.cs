using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Fill_Composer : IComposer
	{
		public CanvasElement e;

		public float2 Solve(SizeConstraints c)
		{
			CanvasTransform t = e.transform;
			
			foreach (CanvasElement child in e.Children)
			{
				if (!child.isEnabled) continue;
				child.SolveLayout(SizeConstraints.Loose(c.maxX, c.maxY));
			}

			float2 finalSize = 0;
			finalSize.x = c.maxX ?? (t.width ?? 0f);
			finalSize.y = c.maxY ?? (t.height ?? 0f);

			t.calculatedSize = finalSize;
			return t.calculatedSize;
		}
	}
}
using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class SizedBox_Composer : IComposer
	{
		public CanvasElement e;

		public float2 Solve(SizeConstraints c)
		{
			float2 preferred = e.transform.size;
            
			// Если размер задан как auto (0), он будет высчитан по детям
			float2 childrenMax = 0;
			for (int i = 0; i < e.children.Count; i++)
			{
				float2 childSize = e.children[i].SolveLayout(SizeConstraints.Loose(new float2(float.PositiveInfinity)));
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
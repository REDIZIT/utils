using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public class Fill_Composer : IComposer
	{
		public CanvasElement e;
	
		public PreferredSize Measure(SizeConstraints c)
		{
			foreach (var child in e.Children)
			{
				if (child.isEnabled) child.Measure(c);
			}

			return new(c.maxX ?? 0, c.maxY ?? 0);
		}

		public void Arrange(float2 size)
		{
			foreach (var child in e.Children)
			{
				child.Arrange(new(16, size - 16 * 2));
			}
		}
	}
}
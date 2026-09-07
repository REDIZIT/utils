using Unity.Mathematics;

namespace REDIZIT.RUI
{
	// public class Absolute_Composer : IComposer
	// {
	// 	public CanvasElement e;
	//
	// 	public float2 pos = 64;
	// 	public float2 size = 100;
	//
	// 	public PreferredSize Measure(SizeConstraints c)
	// 	{
	// 		foreach (var child in e.Children)
	// 		{
	// 			if (child.isEnabled) child.Measure(c);
	// 		}
	//
	// 		return new(c.maxX ?? 0, c.maxY ?? 0);
	// 	}
	//
	// 	public void Arrange(float2 size)
	// 	{
	// 		foreach (var child in e.Children)
	// 		{
	// 			child.Arrange(new(pos, this.size));
	// 		}
	// 	}
	// }
}
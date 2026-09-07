using Unity.Mathematics;

namespace REDIZIT.RUI
{
	// public class SizedBox_Composer : IComposer
	// {
	// 	public CanvasElement e;
	//
	// 	public float2 Solve(SizeConstraints c)
	// 	{
	// 		CanvasTransform t = e.transform;
	//
	// 		// Ограничения для детей:
	// 		float? childMaxX = t.width ?? c.maxX;
	// 		float? childMaxY = t.height ?? c.maxY;
	//
	// 		// ScrollView снимает ограничение по Y (делает его null / auto)
	// 		if (e.GetComponent<ScrollView>() != null)
	// 		{
	// 			childMaxY = null;
	// 		}
	//
	// 		SizeConstraints childConstraints = SizeConstraints.Loose(childMaxX, childMaxY);
 //            
	// 		float2 childrenMax = float2.zero;
	// 		foreach (CanvasElement child in e.Children)
	// 		{
	// 			if (!child.isEnabled) continue;
	// 			float2 childSize = child.SolveLayout(childConstraints);
	// 			childrenMax = math.max(childrenMax, child.transform.localPos + childSize);
	// 		}
	//
	// 		float2 contentSize = e.GetPreferredContentSize();
	// 		float2 autoSize = math.max(childrenMax, contentSize);
	//
	// 		// Если размер задан (not null, даже 0) — используем его! Если null — берем autoSize:
	// 		float2 preferred = float2.zero;
	// 		preferred.x = t.width ?? autoSize.x;
	// 		preferred.y = t.height ?? autoSize.y;
	//
	// 		t.calculatedSize = c.Clamp(preferred);
	// 		return t.calculatedSize;
	// 	}
	// }
}
using Unity.Mathematics;

namespace REDIZIT.RUI
{
	// public class Fill_LayoutSolver : CanvasComponent, ILayoutSolver
	// {
	// 	public float4 padding;
	// 	
	// 	public void Solve(SizeConstraints constraints)
	// 	{
	// 		Transform.pos = new(padding.x, padding.y);
	//
	// 		float2 containerSize = 0;
	//
	// 		if (Element.parent != null)
	// 		{
	// 			containerSize = Element.parent.transform.size;
	// 		}
	// 		
	// 		if (constraints.maxX.HasValue) containerSize.x = constraints.maxX.Value;
	// 		if (constraints.maxY.HasValue) containerSize.y = constraints.maxY.Value;
	// 		
	// 		Transform.size = containerSize - new float2(padding.x + padding.z, padding.y + padding.w);
	// 		
	// 		Element.SolveChildren(new(Transform.size));
	// 	}
	// }
}
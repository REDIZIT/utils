namespace REDIZIT.RUI
{
	public class Fill_LayoutSolver : CanvasComponent, ILayoutSolver
	{
		public void Solve(SizeConstraints constraints)
		{
			foreach (CanvasElement child in Element.Children)
			{
				child.Solve(constraints);
			}
			
			Apply(Transform, constraints);
		}

		public static void Apply(CanvasTransform transform, SizeConstraints constraints)
		{
			transform.pos = 0;
			transform.size = new(constraints.maxX ?? 0, constraints.maxY ?? 0);
		}
	}
}
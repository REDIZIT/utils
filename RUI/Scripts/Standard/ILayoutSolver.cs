namespace REDIZIT.RUI
{
	public interface ILayoutSolver
	{
		ResolvedTransform Solve(SolveContext ctx);
	}
}
using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public interface IComposer
	{
		float2 Solve(SizeConstraints c);
	}
}

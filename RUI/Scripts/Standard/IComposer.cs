using Unity.Mathematics;

namespace InGame.UI
{
	public interface IComposer
	{
		float2 Solve(SizeConstraints c);
	}
}
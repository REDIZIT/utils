using Unity.Mathematics;

namespace REDIZIT.RUI
{
	public interface IComposer
	{
		PreferredSize Measure(SizeConstraints c);
		void Arrange(float2 size);
	}
}
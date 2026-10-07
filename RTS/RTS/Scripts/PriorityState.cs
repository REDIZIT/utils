using System;

namespace RTS
{
	public struct PriorityState
	{
		public int priority;
		public DateTime? lastTouchedUTC;

		public void Touch()
		{
			lastTouchedUTC = DateTime.UtcNow;
		}
	}
}
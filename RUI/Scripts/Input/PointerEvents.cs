using UnityEngine;

namespace REDIZIT.RUI
{
	public class PointerEvent
	{
		public Vector2 position;
		public int pointerId;

		public PointerEvent(Vector2 position, int pointerId = 0)
		{
			this.position = position;
			this.pointerId = pointerId;
		}
	}

	public class PointerDownEvent : PointerEvent
	{
		public int button;
		public PointerDownEvent(Vector2 position, int button = 0, int pointerId = 0) : base(position, pointerId)
		{
			this.button = button;
		}
	}

	public class PointerMoveEvent : PointerEvent
	{
		public Vector2 delta;
		public PointerMoveEvent(Vector2 position, Vector2 delta, int pointerId = 0) : base(position, pointerId)
		{
			this.delta = delta;
		}
	}

	public class PointerUpEvent : PointerEvent
	{
		public int button;
		public PointerUpEvent(Vector2 position, int button = 0, int pointerId = 0) : base(position, pointerId)
		{
			this.button = button;
		}
	}

	public class PointerScrollEvent : PointerEvent
	{
		public Vector2 scrollDelta;
		public PointerScrollEvent(Vector2 position, Vector2 scrollDelta, int pointerId = 0) : base(position, pointerId)
		{
			this.scrollDelta = scrollDelta;
		}
	}
}
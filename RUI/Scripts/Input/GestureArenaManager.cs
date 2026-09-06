using System.Collections.Generic;

namespace REDIZIT.RUI
{
	public class GestureArenaManager
	{
		private readonly Dictionary<int, GestureArena> arenas = new Dictionary<int, GestureArena>();

		public GestureArena OpenArena(int pointerId)
		{
			if (arenas.TryGetValue(pointerId, out var existing))
			{
				existing.Sweep();
			}

			var arena = new GestureArena();
			arenas[pointerId] = arena;
			return arena;
		}

		public GestureArena GetArena(int pointerId)
		{
			arenas.TryGetValue(pointerId, out var arena);
			return arena;
		}

		public void CloseArena(int pointerId)
		{
			if (arenas.TryGetValue(pointerId, out var arena))
			{
				arena.Close();
			}
		}

		public void Sweep(int pointerId)
		{
			if (arenas.TryGetValue(pointerId, out var arena))
			{
				arena.Sweep();
				arenas.Remove(pointerId);
			}
		}

		public void CancelAll()
		{
			foreach (var arena in arenas.Values)
			{
				arena.Sweep();
			}
			arenas.Clear();
		}
	}
}
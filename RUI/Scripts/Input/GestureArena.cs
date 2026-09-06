using System.Collections.Generic;

namespace REDIZIT.RUI
{
	public class GestureArena
	{
		public readonly List<IGestureArenaMember> members = new List<IGestureArenaMember>();
		public bool isOpen = true;
		public bool isResolved = false;
		public IGestureArenaMember winner = null;

		public void Add(IGestureArenaMember member)
		{
			if (!isOpen || isResolved) return;
			members.Add(member);
		}

		public void Close()
		{
			isOpen = false;
		}

		public void Resolve(IGestureArenaMember member, GestureDisposition disposition)
		{
			if (isResolved) return;

			if (disposition == GestureDisposition.Accepted)
			{
				DeclareWinner(member);
			}
			else if (disposition == GestureDisposition.Rejected)
			{
				members.Remove(member);
				member.RejectGesture();

				// Если остался ровно один участник, он автоматически побеждает
				if (!isOpen && members.Count == 1)
				{
					DeclareWinner(members[0]);
				}
			}
		}

		public void Sweep()
		{
			if (isResolved) return;

			// Если палец отпущен, а явного победителя нет, побеждает первый зарегистрированный
			// (самый глубокий дочерний элемент в дереве, например стрелка, а не тело строки!)
			if (members.Count > 0)
			{
				DeclareWinner(members[0]);
			}
		}

		private void DeclareWinner(IGestureArenaMember target)
		{
			if (isResolved) return;
			isResolved = true;
			winner = target;

			target.AcceptGesture();

			// Всем проигравшим участникам шлем сигнал отмены
			for (int i = 0; i < members.Count; i++)
			{
				if (members[i] != target)
				{
					members[i].RejectGesture();
				}
			}
			members.Clear();
		}
	}
}
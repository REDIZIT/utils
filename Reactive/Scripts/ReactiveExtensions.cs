using System;
using UnityEngine;

public static class ReactiveExtensions
{
	public static T AddTo<T>(this T disposable, MonoBehaviour behaviour) where T : IDisposable
	{
		if (behaviour == null) return disposable;

		if (!behaviour.TryGetComponent<DisposableTracker>(out var tracker))
		{
			tracker = behaviour.gameObject.AddComponent<DisposableTracker>();
		}

		tracker.Add(disposable);
		return disposable;
	}

	public static void Restart(this SubsContainer container, MonoBehaviour behaviour)
	{
		container.Clear();
		container.AddTo(behaviour);
	}
	
	public static SubsContainer Subscribe(this SubsContainer container, Action drawLogic)
	{
		container.Add(new AutoSub(drawLogic));
		return container;
	}
}
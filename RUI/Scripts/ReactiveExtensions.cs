using System;
using REDIZIT.RUI;

public static class ReactiveExtensions
{
	public static void Restart(this SubsContainer container, CanvasComponent comp)
	{
		Restart(container, comp.Element);
	}
	
	public static void Restart(this SubsContainer container, CanvasElement e)
	{
		container.Clear();
		container.AddTo(e);
	}
	
	public static T AddTo<T>(this T disposable, CanvasElement e) where T : IDisposable
	{
		if (e == null) return disposable;

		if (!e.TryGetComponent(out DisposableTracker_CanvasComponent tracker))
		{
			tracker = e.AddComponent<DisposableTracker_CanvasComponent>();
		}

		tracker.Add(disposable);
		return disposable;
	}
}
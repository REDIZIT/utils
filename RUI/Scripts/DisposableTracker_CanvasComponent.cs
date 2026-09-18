using System;
using System.Collections.Generic;
using REDIZIT.RUI;

public class DisposableTracker_CanvasComponent : CanvasComponent
{
	private readonly List<IDisposable> disposables = new();

	public void Add(IDisposable disposable)
	{
		disposables.Add(disposable);
	}

	public override void OnDetached()
	{
		base.OnDetached();
		
		foreach (IDisposable t in disposables) t?.Dispose();
		disposables.Clear();
	}
}
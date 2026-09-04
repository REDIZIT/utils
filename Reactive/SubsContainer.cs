using System;
using System.Collections.Generic;

public class SubsContainer : IDisposable
{
	private readonly List<IDisposable> disposables = new();

	public void Add(IDisposable disposable)
	{
		if (disposable != null)
		{
			disposables.Add(disposable);
		}
	}
	
	public void Clear()
	{
		for (int i = 0; i < disposables.Count; i++)
		{
			disposables[i]?.Dispose();
		}
		disposables.Clear();
	}

	public void Dispose()
	{
		Clear();
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class DisposableTracker : MonoBehaviour
{
	private readonly List<IDisposable> disposables = new();

	public void Add(IDisposable disposable)
	{
		disposables.Add(disposable);
	}

	private void OnDestroy()
	{
		for (int i = 0; i < disposables.Count; i++)
		{
			disposables[i]?.Dispose();
		}
		disposables.Clear();
	}
}
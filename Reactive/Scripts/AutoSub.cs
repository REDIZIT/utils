using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoSub : IDisposable
{
	private readonly Action executeFunc;
	private readonly HashSet<IReactive> dependencies = new();
	private readonly Action onDependencyChanged;
	private bool isDisposed;

	public AutoSub(Action executeFunc)
	{
		this.executeFunc = executeFunc;
		this.onDependencyChanged = ScheduleRun; // Планируем, а не вызываем сразу
		Run(); // Первый запуск выполняем синхронно для первичной отрисовки и сбора зависимостей
	}

	private void ScheduleRun()
	{
		if (isDisposed) return;
		ReactiveTracker.Schedule(this);
	}

	internal void Run()
	{
		if (isDisposed) return;

		// 1. Отписываемся от старых зависимостей
		foreach (var dep in dependencies) dep.Unsubscribe(onDependencyChanged);
		dependencies.Clear();

		// 2. Включаем "Шпиона"
		var previousTracker = ReactiveTracker.CurrentDependencies;
		ReactiveTracker.CurrentDependencies = dependencies;

		try
		{
			// 3. Выполняем код UI
			executeFunc();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
		finally
		{
			// 4. Выключаем "Шпиона"
			ReactiveTracker.CurrentDependencies = previousTracker;
		}

		// 5. Подписываемся на новые зависимости
		foreach (var dep in dependencies) dep.Subscribe(onDependencyChanged);
	}

	public void Dispose()
	{
		if (isDisposed) return;
		isDisposed = true;

		ReactiveTracker.Unschedule(this);
		foreach (var dep in dependencies) dep.Unsubscribe(onDependencyChanged);
		dependencies.Clear();
	}
}
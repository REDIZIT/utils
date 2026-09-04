using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoSub : IDisposable
{
	private readonly Action executeFunc;
	private readonly HashSet<IReactive> dependencies = new();
	private readonly Action onDependencyChanged;

	public AutoSub(Action executeFunc)
	{
		this.executeFunc = executeFunc;
		this.onDependencyChanged = Run; // Кешируем делегат для подписок
		Run(); // Запускаем первый раз
	}

	private void Run()
	{
		// 1. Отписываемся от старых зависимостей
		foreach (var dep in dependencies) dep.Unsubscribe(onDependencyChanged);
		dependencies.Clear();

		// 2. Включаем "Шпиона"
		var previousTracker = ReactiveTracker.CurrentDependencies;
		ReactiveTracker.CurrentDependencies = dependencies;

		try
		{
			// 3. Выполняем код UI. 
			// Все вызовы .Value внутри запишутся в коллекцию dependencies!
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
		foreach (var dep in dependencies) dep.Unsubscribe(onDependencyChanged);
		dependencies.Clear();
	}
}
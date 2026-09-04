using System.Collections.Generic;

public static class ReactiveTracker
{
	// Хранит список зависимостей для ТЕКУЩЕГО выполняемого блока кода
	public static HashSet<IReactive> CurrentDependencies;

	public static List<IReactive> changedReactives = new();

	// Вызывается изнутри свойств при их чтении (get)
	public static void ReportRead(IReactive reactive)
	{
		CurrentDependencies?.Add(reactive);
	}

	public static void OnChanged(IReactive reactive)
	{
		changedReactives.Add(reactive);
	}
}
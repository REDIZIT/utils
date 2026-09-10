using System.Collections.Generic;

public static class ReactiveTracker
{
	public static HashSet<IReactive> CurrentDependencies;

	// Очередь реакций, ожидающих выполнения
	private static readonly HashSet<AutoSub> pendingAutoSubs = new();
	private static readonly List<AutoSub> executionBuffer = new();

	public static void ReportRead(IReactive reactive)
	{
		CurrentDependencies?.Add(reactive);
	}

	public static void Schedule(AutoSub sub)
	{
		if (sub == null) return;
		pendingAutoSubs.Add(sub); // HashSet гарантирует, что один AutoSub не добавится дважды за кадр
		ReactiveDispatcher.EnsureInitialized();
	}

	public static void Unschedule(AutoSub sub)
	{
		pendingAutoSubs.Remove(sub);
	}

	public static void Flush()
	{
		if (pendingAutoSubs.Count == 0) return;

		// Копируем в буфер, чтобы избежать ошибок модификации коллекции, 
		// если внутри Run() будут запланированы новые зависимости
		executionBuffer.AddRange(pendingAutoSubs);
		pendingAutoSubs.Clear();

		for (int i = 0; i < executionBuffer.Count; i++)
		{
			executionBuffer[i].Run();
		}

		executionBuffer.Clear();
	}
}
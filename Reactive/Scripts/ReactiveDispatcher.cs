using UnityEngine;

public class ReactiveDispatcher : MonoBehaviour
{
	private static ReactiveDispatcher instance;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		EnsureInitialized();
	}

	public static void EnsureInitialized()
	{
		if (instance == null)
		{
			var go = new GameObject("[ReactiveDispatcher]")
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			instance = go.AddComponent<ReactiveDispatcher>();
			DontDestroyOnLoad(go);
		}
	}

	// Срабатывает каждый кадр и выстреливает накопленными реакциями
	private void Update()
	{
		ReactiveTracker.Flush();
	}
}
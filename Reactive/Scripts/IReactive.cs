using System;
using System.Collections.Generic;

public interface IReactive
{
	ReactiveScope Scope { get; set; }
	object RawValue { get; set; }
	void Subscribe(Action callback);
	void Unsubscribe(Action callback);
}

public interface IReactiveCollection<T> : IEnumerable<T>
{
	Action<T> onAdded { get; set; }
	Action<T> onRemoved { get; set; }
}
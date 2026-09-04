using System;

public interface IReactive
{
	object RawValue { get; set; }
	void Subscribe(Action callback);
	void Unsubscribe(Action callback);
}
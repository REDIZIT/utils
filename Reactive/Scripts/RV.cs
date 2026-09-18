using System;
using System.Collections.Generic;

public class RV<T> : IReactive
{
	public T Value
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return value;
		}
		set
		{
			bool isDifferent = !EqualityComparer<T>.Default.Equals(this.value, value);
			this.value = value;
			onSetValue?.Invoke();
			if (isDifferent) OnChanged();
		}
	}

	public bool HasValue
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return value != null;
		}
	}
	
	public object RawValue
	{
		get => value;
		set => this.value = value == null ? default : (T)value;
	}

	public Action onSetValue;
	public Action onChanged;

	public bool isReporter = true;
	private T value;

	public RV()
	{
	}

	public RV(T defaultValue)
	{
		value = defaultValue;
		this.isReporter = isReporter;
	}

	public void SetValueWithoutNotify(T value)
	{
		this.value = value;
	}

	private void OnChanged()
	{
		if (isReporter) ReactiveTracker.OnChanged(this);
		onChanged?.Invoke();
	}

	public override string ToString()
	{
		if (Value == null) return null;
		else return Value.ToString();
	}

	public void Subscribe(Action callback) => onChanged += callback;
	public void Unsubscribe(Action callback) => onChanged -= callback;

	public static implicit operator T(RV<T> rv) => rv.Value;
}
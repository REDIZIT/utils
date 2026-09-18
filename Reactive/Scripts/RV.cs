using System;
using System.Collections.Generic;

public class RV<T> : IReactive
{
	private ReactiveScope scope;
    
	public ReactiveScope Scope
	{
		get => scope;
		set
		{
			scope = value;
			// Если скоуп проставился, когда внутри уже лежит значение (например, ScanSession)
			if (scope != null && this.value != null)
			{
				ScopeBinder.Bind(this.value, scope);
			}
		}
	}
	
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
			
			// Если этот RV сохраняемый, новое значение АВТОМАТИЧЕСКИ получает Scope
			if (scope != null && value != null)
			{
				ScopeBinder.Bind(value, scope);
			}
			
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

	private T value;

	public RV()
	{
	}

	public RV(T defaultValue)
	{
		value = defaultValue;
	}

	public void SetValueWithoutNotify(T value)
	{
		this.value = value;
	}

	private void OnChanged()
	{
		scope?.MarkDirty(); 
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
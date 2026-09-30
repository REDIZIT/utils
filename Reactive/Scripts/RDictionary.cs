using System;
using System.Collections;
using System.Collections.Generic;

public class RDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReactive, IReactiveDictionary<TKey, TValue>
{
	private ReactiveScope scope;

	public ReactiveScope Scope
	{
		get => scope;
		set
		{
			scope = value;
			if (scope != null)
			{
				foreach (var kvp in dict)
				{
					if (kvp.Key != null) ScopeBinder.Bind(kvp.Key, scope);
					if (kvp.Value != null) ScopeBinder.Bind(kvp.Value, scope);
				}
			}
		}
	}

	public Action<TKey, TValue> onAdded { get; set; }
	public Action<TKey, TValue> onRemoved { get; set; }

	private Action onChanged;
	private Dictionary<TKey, TValue> dict = new();

	public int Count
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return dict.Count;
		}
	}

	public bool IsReadOnly => false;

	public object RawValue
	{
		get => dict;
		set => dict = value == null ? new() : (Dictionary<TKey, TValue>)value;
	}

	public ICollection<TKey> Keys
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return dict.Keys;
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return dict.Values;
		}
	}

	public TValue this[TKey key]
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return dict[key];
		}
		set
		{
			if (dict.TryGetValue(key, out TValue oldValue))
			{
				if (EqualityComparer<TValue>.Default.Equals(oldValue, value))
					return;

				// Уведомляем о замене старого значения
				onRemoved?.Invoke(key, oldValue);

				dict[key] = value;
				if (scope != null && value != null) ScopeBinder.Bind(value, scope);

				OnChanged();
				onAdded?.Invoke(key, value);
			}
			else
			{
				Add(key, value);
			}
		}
	}

	public RDictionary()
	{
	}

	public RDictionary(Dictionary<TKey, TValue> defaultValue)
	{
		dict = defaultValue ?? new();
	}

	public RDictionary(int capacity)
	{
		dict = new(capacity);
	}

	public RDictionary(IEqualityComparer<TKey> comparer)
	{
		dict = new(comparer);
	}

	public void Add(TKey key, TValue value)
	{
		if (scope != null)
		{
			if (key != null) ScopeBinder.Bind(key, scope);
			if (value != null) ScopeBinder.Bind(value, scope);
		}

		AddWithoutNotify(key, value);
		OnChanged();
		onAdded?.Invoke(key, value);
	}

	public void Add(IEnumerable<KeyValuePair<TKey, TValue>> elements)
	{
		foreach (var kvp in elements)
		{
			Add(kvp.Key, kvp.Value);
		}
	}

	public bool Remove(TKey key)
	{
		if (dict.TryGetValue(key, out TValue value))
		{
			dict.Remove(key);
			OnChanged();
			onRemoved?.Invoke(key, value);
			return true;
		}

		return false;
	}

	public void Clear()
	{
		foreach (var kvp in dict)
		{
			onRemoved?.Invoke(kvp.Key, kvp.Value);
		}
		dict.Clear();
		OnChanged();
	}

	public bool ContainsKey(TKey key)
	{
		ReactiveTracker.ReportRead(this);
		return dict.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		ReactiveTracker.ReportRead(this);
		return dict.TryGetValue(key, out value);
	}

	public TValue TryGetValue(TKey key)
	{
		return dict.GetValueOrDefault(key);
	}

	public void AddWithoutNotify(TKey key, TValue value)
	{
		dict.Add(key, value);
	}

	public bool RemoveWithoutNotify(TKey key)
	{
		return dict.Remove(key);
	}

	public void SetIfNotNull(IDictionary<TKey, TValue> elements)
	{
		if (elements == null) return;
		Set(elements);
	}

	public void Set(IDictionary<TKey, TValue> elements)
	{
		foreach (var kvp in dict) onRemoved?.Invoke(kvp.Key, kvp.Value);
		dict.Clear();

		if (elements != null)
		{
			foreach (var kvp in elements)
			{
				dict.Add(kvp.Key, kvp.Value);
				if (scope != null)
				{
					if (kvp.Key != null) ScopeBinder.Bind(kvp.Key, scope);
					if (kvp.Value != null) ScopeBinder.Bind(kvp.Value, scope);
				}
			}
		}

		OnChanged();
	}

	private void OnChanged()
	{
		ReactiveTracker.OnChanged(this);
		scope?.MarkDirty();
		onChanged?.Invoke();
	}

	#region ICollection<KeyValuePair<TKey, TValue>> Implementation

	public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

	public bool Contains(KeyValuePair<TKey, TValue> item)
	{
		ReactiveTracker.ReportRead(this);
		return ((ICollection<KeyValuePair<TKey, TValue>>)dict).Contains(item);
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
	}

	public bool Remove(KeyValuePair<TKey, TValue> item)
	{
		if (Contains(item))
		{
			return Remove(item.Key);
		}
		return false;
	}

	#endregion

	#region IEnumerable Implementation

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		ReactiveTracker.ReportRead(this);
		return dict.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	public void Subscribe(Action callback) => onChanged += callback;
	public void Unsubscribe(Action callback) => onChanged -= callback;
}
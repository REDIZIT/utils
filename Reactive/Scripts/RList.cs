using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RList<T> : ICollection<T>, IReactive, IReactiveCollection<T>
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
				for (int i = 0; i < ls.Count; i++)
				{
					if (ls[i] != null) ScopeBinder.Bind(ls[i], scope);
				}
			}
		}
	}
	
	public Action<T> onAdded { get; set; }
	public Action<T> onRemoved { get; set; }
	
	private Action onChanged;
	private List<T> ls = new();

	public int Count
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return ls.Count;
		}
	}

	public bool IsReadOnly { get; }

	public object RawValue
	{
		get => ls;
		set => ls = value == null ? default : (List<T>)value;
	}
	
	public T this[int index]
	{
		get
		{
			ReactiveTracker.ReportRead(this);
			return ls[index];
		}
		set
		{
			ls[index] = value;
			OnChanged();
		}
	}

	public RList()
	{
	}

	public RList(List<T> defaultValue)
	{
		ls = defaultValue;
	}
	
	public RList(int capacity)
	{
		ls = new(capacity);
	}
	
	public void Add(T element)
	{
		// Каскадируем скоуп на новый элемент при добавлении в список
		if (scope != null && element != null)
		{
			ScopeBinder.Bind(element, scope);
		}
		
		AddWithoutNotify(element);
		OnChanged();
		onAdded?.Invoke(element);
	}

	public void Add(IEnumerable<T> elements)
	{
		foreach (T element in elements)
		{
			Add(element);
		}
	}
	
	public bool Remove(T item)
	{
		bool anyRemoved = RemoveWithoutNotify(item);
		if (anyRemoved)
		{
			OnChanged();
			onRemoved?.Invoke(item);
		}
		return anyRemoved;
	}

	public void Clear()
	{
		foreach (T e in ls) onRemoved?.Invoke(e);
		ls.Clear();
		OnChanged();
	}

	public bool Contains(T item)
	{
		ReactiveTracker.ReportRead(this);
		return ls.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		ls.CopyTo(array, arrayIndex);
	}

	public void AddWithoutNotify(T element)
	{
		ls.Add(element);
	}
	
	public bool RemoveWithoutNotify(T element)
	{
		return ls.Remove(element);
	}

	public void SetIfNotNull(IEnumerable<T> elements)
	{
		if (elements == null) return;
		Set(elements);
	}
	
	public void Set(IEnumerable<T> elements)
	{
		foreach (T e in ls) onRemoved?.Invoke(e);
		ls.Clear();
		
		if (elements != null)
		{
			ls.AddRange(elements);
			if (scope != null)
			{
				foreach (var e in elements)
				{
					if (e != null) ScopeBinder.Bind(e, scope);
				}
			}
		}
		OnChanged();
	}

	public void RemoveAll(Predicate<T> predicate)
	{
		foreach (T e in ls)
		{
			if (predicate(e)) onRemoved?.Invoke(e);
		}
		ls.RemoveAll(predicate);
		OnChanged();
	}
	
	private void OnChanged()
	{
		ReactiveTracker.OnChanged(this);
		scope?.MarkDirty();
		onChanged?.Invoke();
	}
	
	public IEnumerator<T> GetEnumerator()
	{
		ReactiveTracker.ReportRead(this);
		return ls.GetEnumerator();
	}
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void Subscribe(Action callback) => onChanged += callback;
	public void Unsubscribe(Action callback) => onChanged -= callback;
}
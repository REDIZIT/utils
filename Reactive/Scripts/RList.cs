using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RList<T> : ICollection<T>, IReactive
{
	public Action<T> onAdded, onRemoved;
	
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

	public RList()
	{
	}

	public RList(List<T> defaultValue)
	{
		ls = defaultValue;
	}
	
	public void Add(T element)
	{
		AddWithoutNotify(element);
		OnChanged();
		onAdded?.Invoke(element);
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
		
		ls.AddRange(elements);
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
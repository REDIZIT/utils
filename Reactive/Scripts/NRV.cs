using System;

public class NRV<T> : RV<T?> where T : struct
{
	public NRV() : base() { }
	public NRV(T? defaultValue) : base(defaultValue) { }

	public T OrDefault => Value.GetValueOrDefault();
	public T NonNullValue => Value ?? throw new InvalidOperationException($"Value in {GetType().Name} is null!");

	public static implicit operator T(NRV<T> rv) => rv.NonNullValue;
	public static implicit operator T?(NRV<T> rv) => rv != null ? rv.Value : null;
}
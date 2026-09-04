using System;
using System.Collections.Generic;
using System.Linq;

public static class CSharpExtensions
{
	public static readonly char[] hexChars =
	{
		'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
		'a', 'b', 'c', 'd', 'e', 'f'
	};
	
	public static string ToSepString<T>(this IEnumerable<T> ls, string sepStr = ", ") => string.Join(sepStr, ls.Select(e => e.ToString()));
	public static string ToSepString<T>(this IEnumerable<T> ls, Func<T, string> selector, string sepStr = ", ") => string.Join(sepStr, ls.Select(selector));
	public static string ToSepString(this IEnumerable<int> ls, string sepStr = ", ") => string.Join(sepStr, ls);
	public static string ToSepString(this IEnumerable<float> ls, string sepStr = ", ") => string.Join(sepStr, ls);
	public static string ToSepString(this IEnumerable<string> ls, string sepStr = ", ") => string.Join(sepStr, ls);
	
	public static bool IsInside<T>(this IEnumerable<T> ls, int index)
	{
		return index >= 0 && index < ls.Count();
	}
}
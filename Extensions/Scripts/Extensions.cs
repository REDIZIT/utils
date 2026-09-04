using System;
using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
	public static string ToSepString<T>(this IEnumerable<T> ls, string sepStr = ", ") => string.Join(sepStr, ls.Select(e => e.ToString()));
	public static string ToSepString<T>(this IEnumerable<T> ls, Func<T, string> selector, string sepStr = ", ") => string.Join(sepStr, ls.Select(selector));
	public static string ToSepString(this IEnumerable<int> ls, string sepStr = ", ") => string.Join(sepStr, ls);
	public static string ToSepString(this IEnumerable<float> ls, string sepStr = ", ") => string.Join(sepStr, ls);
	public static string ToSepString(this IEnumerable<string> ls, string sepStr = ", ") => string.Join(sepStr, ls);
}
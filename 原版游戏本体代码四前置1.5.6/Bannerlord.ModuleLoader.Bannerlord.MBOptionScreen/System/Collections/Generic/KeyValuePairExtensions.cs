using System.ComponentModel;

namespace System.Collections.Generic;

internal static class KeyValuePairExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> source, out TKey key, out TValue value)
	{
		key = source.Key;
		value = source.Value;
	}
}

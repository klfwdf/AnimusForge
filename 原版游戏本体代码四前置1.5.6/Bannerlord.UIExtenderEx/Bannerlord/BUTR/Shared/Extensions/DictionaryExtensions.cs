using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Bannerlord.BUTR.Shared.Extensions;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal static class DictionaryExtensions
{
	public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> tuple, out TKey key, out TValue value)
	{
		key = tuple.Key;
		value = tuple.Value;
	}

	public static void AddRangeCautiously<TKey, TValue>(this Dictionary<TKey, TValue> destinationDict, Dictionary<TKey, TValue> otherDict, bool overrideDuplicates = false)
	{
		if (destinationDict == null)
		{
			destinationDict = new Dictionary<TKey, TValue>();
		}
		if (otherDict == null)
		{
			otherDict = new Dictionary<TKey, TValue>();
		}
		foreach (KeyValuePair<TKey, TValue> item in otherDict)
		{
			if (!destinationDict.ContainsKey(item.Key))
			{
				destinationDict.Add(item.Key, item.Value);
			}
			else if (overrideDuplicates)
			{
				destinationDict[item.Key] = item.Value;
			}
		}
	}

	public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> destinationDict, Dictionary<TKey, TValue> otherDict)
	{
		foreach (KeyValuePair<TKey, TValue> item in otherDict)
		{
			destinationDict.Add(item.Key, item.Value);
		}
	}
}

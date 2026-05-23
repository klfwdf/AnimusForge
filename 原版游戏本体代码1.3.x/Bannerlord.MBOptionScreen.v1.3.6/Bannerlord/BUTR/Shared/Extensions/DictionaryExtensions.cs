using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Extensions
{
	// Token: 0x02000053 RID: 83
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class DictionaryExtensions
	{
		// Token: 0x060002D1 RID: 721 RVA: 0x0000A1D1 File Offset: 0x000083D1
		public static void Deconstruct<[Nullable(2)] TKey, [Nullable(2)] TValue>([Nullable(new byte[] { 0, 1, 1 })] this KeyValuePair<TKey, TValue> tuple, out TKey key, out TValue value)
		{
			key = tuple.Key;
			value = tuple.Value;
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x0000A1F0 File Offset: 0x000083F0
		public static void AddRangeCautiously<[Nullable(2)] TKey, [Nullable(2)] TValue>(this Dictionary<TKey, TValue> destinationDict, Dictionary<TKey, TValue> otherDict, bool overrideDuplicates = false)
		{
			if (destinationDict == null)
			{
				destinationDict = new Dictionary<TKey, TValue>();
			}
			if (otherDict == null)
			{
				otherDict = new Dictionary<TKey, TValue>();
			}
			foreach (KeyValuePair<TKey, TValue> entry in otherDict)
			{
				if (!destinationDict.ContainsKey(entry.Key))
				{
					destinationDict.Add(entry.Key, entry.Value);
				}
				else if (overrideDuplicates)
				{
					destinationDict[entry.Key] = entry.Value;
				}
			}
		}

		// Token: 0x060002D3 RID: 723 RVA: 0x0000A288 File Offset: 0x00008488
		public static void AddRange<[Nullable(2)] TKey, [Nullable(2)] TValue>(this Dictionary<TKey, TValue> destinationDict, Dictionary<TKey, TValue> otherDict)
		{
			foreach (KeyValuePair<TKey, TValue> entry in otherDict)
			{
				destinationDict.Add(entry.Key, entry.Value);
			}
		}
	}
}

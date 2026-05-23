using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bannerlord.BUTR.Shared.Extensions
{
	// Token: 0x02000011 RID: 17
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class DictionaryExtensions
	{
		// Token: 0x060000C8 RID: 200 RVA: 0x00004E7E File Offset: 0x0000307E
		public static void Deconstruct<[Nullable(2)] TKey, [Nullable(2)] TValue>([Nullable(new byte[] { 0, 1, 1 })] this KeyValuePair<TKey, TValue> tuple, out TKey key, out TValue value)
		{
			key = tuple.Key;
			value = tuple.Value;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00004E9C File Offset: 0x0000309C
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
				bool flag = !destinationDict.ContainsKey(entry.Key);
				if (flag)
				{
					destinationDict.Add(entry.Key, entry.Value);
				}
				else if (overrideDuplicates)
				{
					destinationDict[entry.Key] = entry.Value;
				}
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00004F48 File Offset: 0x00003148
		public static void AddRange<[Nullable(2)] TKey, [Nullable(2)] TValue>(this Dictionary<TKey, TValue> destinationDict, Dictionary<TKey, TValue> otherDict)
		{
			foreach (KeyValuePair<TKey, TValue> entry in otherDict)
			{
				destinationDict.Add(entry.Key, entry.Value);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace HarmonyLib
{
	/// <summary>General extensions for collections</summary>
	// Token: 0x020001C3 RID: 451
	public static class CollectionExtensions
	{
		/// <summary>A simple way to execute code for every element in a collection</summary>
		/// <typeparam name="T">The inner type of the collection</typeparam>
		/// <param name="sequence">The collection</param>
		/// <param name="action">The action to execute</param>
		// Token: 0x060007DC RID: 2012 RVA: 0x00019D24 File Offset: 0x00017F24
		public static void Do<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			if (sequence == null)
			{
				return;
			}
			foreach (T obj in sequence)
			{
				action(obj);
			}
		}

		/// <summary>A simple way to execute code for elements in a collection matching a condition</summary>
		/// <typeparam name="T">The inner type of the collection</typeparam>
		/// <param name="sequence">The collection</param>
		/// <param name="condition">The predicate</param>
		/// <param name="action">The action to execute</param>
		// Token: 0x060007DD RID: 2013 RVA: 0x00019D52 File Offset: 0x00017F52
		public static void DoIf<T>(this IEnumerable<T> sequence, Func<T, bool> condition, Action<T> action)
		{
			sequence.Where(condition).Do(action);
		}

		/// <summary>A helper to add an item to a collection</summary>
		/// <typeparam name="T">The inner type of the collection</typeparam>
		/// <param name="sequence">The collection</param>
		/// <param name="item">The item to add</param>
		/// <returns>The collection containing the item</returns>
		// Token: 0x060007DE RID: 2014 RVA: 0x00019D61 File Offset: 0x00017F61
		public static IEnumerable<T> AddItem<T>(this IEnumerable<T> sequence, T item)
		{
			return (sequence ?? Array.Empty<T>()).Concat(new T[] { item });
		}

		/// <summary>A helper to add an item to an array</summary>
		/// <typeparam name="T">The inner type of the collection</typeparam>
		/// <param name="sequence">The array</param>
		/// <param name="item">The item to add</param>
		/// <returns>The array containing the item</returns>
		// Token: 0x060007DF RID: 2015 RVA: 0x00019D80 File Offset: 0x00017F80
		public static T[] AddToArray<T>(this T[] sequence, T item)
		{
			return sequence.AddItem(item).ToArray<T>();
		}

		/// <summary>A helper to add items to an array</summary>
		/// <typeparam name="T">The inner type of the collection</typeparam>
		/// <param name="sequence">The array</param>
		/// <param name="items">The items to add</param>
		/// <returns>The array containing the items</returns>
		// Token: 0x060007E0 RID: 2016 RVA: 0x00019D90 File Offset: 0x00017F90
		public static T[] AddRangeToArray<T>(this T[] sequence, T[] items)
		{
			List<T> list = new List<T>();
			list.AddRange(sequence ?? Enumerable.Empty<T>());
			list.AddRange(items);
			return list.ToArray();
		}

		// Token: 0x060007E1 RID: 2017 RVA: 0x00019DC0 File Offset: 0x00017FC0
		internal static Dictionary<K, V> Merge<K, V>(this IEnumerable<KeyValuePair<K, V>> firstDict, params IEnumerable<KeyValuePair<K, V>>[] otherDicts)
		{
			Dictionary<K, V> dict = new Dictionary<K, V>();
			foreach (KeyValuePair<K, V> pair in firstDict)
			{
				dict[pair.Key] = pair.Value;
			}
			foreach (IEnumerable<KeyValuePair<K, V>> otherDict in otherDicts)
			{
				foreach (KeyValuePair<K, V> pair2 in otherDict)
				{
					dict[pair2.Key] = pair2.Value;
				}
			}
			return dict;
		}

		// Token: 0x060007E2 RID: 2018 RVA: 0x00019E84 File Offset: 0x00018084
		internal static Dictionary<K, V> TransformKeys<K, V>(this Dictionary<K, V> origDict, Func<K, K> transform)
		{
			Dictionary<K, V> dict = new Dictionary<K, V>();
			foreach (KeyValuePair<K, V> pair in origDict)
			{
				dict.Add(transform(pair.Key), pair.Value);
			}
			return dict;
		}
	}
}

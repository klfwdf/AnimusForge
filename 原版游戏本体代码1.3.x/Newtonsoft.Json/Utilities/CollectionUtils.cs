using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000044 RID: 68
	[NullableContext(1)]
	[Nullable(0)]
	internal static class CollectionUtils
	{
		/// <summary>
		/// Determines whether the collection is <c>null</c> or empty.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <returns>
		/// 	<c>true</c> if the collection is <c>null</c> or empty; otherwise, <c>false</c>.
		/// </returns>
		// Token: 0x0600043F RID: 1087 RVA: 0x00010AEC File Offset: 0x0000ECEC
		public static bool IsNullOrEmpty<[Nullable(2)] T>(ICollection<T> collection)
		{
			return collection == null || collection.Count == 0;
		}

		/// <summary>
		/// Adds the elements of the specified collection to the specified generic <see cref="T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <param name="initial">The list to add to.</param>
		/// <param name="collection">The collection of elements to add.</param>
		// Token: 0x06000440 RID: 1088 RVA: 0x00010AFC File Offset: 0x0000ECFC
		public static void AddRange<[Nullable(2)] T>(this IList<T> initial, IEnumerable<T> collection)
		{
			if (initial == null)
			{
				throw new ArgumentNullException("initial");
			}
			if (collection == null)
			{
				return;
			}
			foreach (T item in collection)
			{
				initial.Add(item);
			}
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x00010B58 File Offset: 0x0000ED58
		public static bool IsDictionaryType(Type type)
		{
			ValidationUtils.ArgumentNotNull(type, "type");
			return typeof(IDictionary).IsAssignableFrom(type) || ReflectionUtils.ImplementsGenericDefinition(type, typeof(IDictionary<, >)) || ReflectionUtils.ImplementsGenericDefinition(type, typeof(IReadOnlyDictionary<, >));
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x00010BB0 File Offset: 0x0000EDB0
		[return: Nullable(2)]
		public static ConstructorInfo ResolveEnumerableCollectionConstructor(Type collectionType, Type collectionItemType)
		{
			Type constructorArgumentType = typeof(IList<>).MakeGenericType(new Type[] { collectionItemType });
			return CollectionUtils.ResolveEnumerableCollectionConstructor(collectionType, collectionItemType, constructorArgumentType);
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x00010BE0 File Offset: 0x0000EDE0
		[return: Nullable(2)]
		public static ConstructorInfo ResolveEnumerableCollectionConstructor(Type collectionType, Type collectionItemType, Type constructorArgumentType)
		{
			Type left = typeof(IEnumerable<>).MakeGenericType(new Type[] { collectionItemType });
			ConstructorInfo constructorInfo = null;
			foreach (ConstructorInfo constructorInfo2 in collectionType.GetConstructors(BindingFlags.Instance | BindingFlags.Public))
			{
				IList<ParameterInfo> parameters = constructorInfo2.GetParameters();
				if (parameters.Count == 1)
				{
					Type parameterType = parameters[0].ParameterType;
					if (left == parameterType)
					{
						constructorInfo = constructorInfo2;
						break;
					}
					if (constructorInfo == null && parameterType.IsAssignableFrom(constructorArgumentType))
					{
						constructorInfo = constructorInfo2;
					}
				}
			}
			return constructorInfo;
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x00010C6D File Offset: 0x0000EE6D
		public static bool AddDistinct<[Nullable(2)] T>(this IList<T> list, T value)
		{
			return list.AddDistinct(value, EqualityComparer<T>.Default);
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x00010C7B File Offset: 0x0000EE7B
		public static bool AddDistinct<[Nullable(2)] T>(this IList<T> list, T value, IEqualityComparer<T> comparer)
		{
			if (list.ContainsValue(value, comparer))
			{
				return false;
			}
			list.Add(value);
			return true;
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x00010C94 File Offset: 0x0000EE94
		public static bool ContainsValue<[Nullable(2)] TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
		{
			if (comparer == null)
			{
				comparer = EqualityComparer<TSource>.Default;
			}
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			foreach (TSource x in source)
			{
				if (comparer.Equals(x, value))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x00010D00 File Offset: 0x0000EF00
		public static bool AddRangeDistinct<[Nullable(2)] T>(this IList<T> list, IEnumerable<T> values, IEqualityComparer<T> comparer)
		{
			bool result = true;
			foreach (T value in values)
			{
				if (!list.AddDistinct(value, comparer))
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x00010D50 File Offset: 0x0000EF50
		public static int IndexOf<[Nullable(2)] T>(this IEnumerable<T> collection, Func<T, bool> predicate)
		{
			int num = 0;
			foreach (T arg in collection)
			{
				if (predicate(arg))
				{
					return num;
				}
				num++;
			}
			return -1;
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x00010DA8 File Offset: 0x0000EFA8
		public static bool Contains<[Nullable(2)] T>(this List<T> list, T value, IEqualityComparer comparer)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (comparer.Equals(value, list[i]))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x00010DE4 File Offset: 0x0000EFE4
		public static int IndexOfReference<[Nullable(2)] T>(this List<T> list, T item)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (item == list[i])
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x00010E1C File Offset: 0x0000F01C
		public static void FastReverse<[Nullable(2)] T>(this List<T> list)
		{
			int i = 0;
			int num = list.Count - 1;
			while (i < num)
			{
				T value = list[i];
				list[i] = list[num];
				list[num] = value;
				i++;
				num--;
			}
		}

		// Token: 0x0600044C RID: 1100 RVA: 0x00010E60 File Offset: 0x0000F060
		private static IList<int> GetDimensions(IList values, int dimensionsCount)
		{
			IList<int> list = new List<int>();
			IList list2 = values;
			for (;;)
			{
				list.Add(list2.Count);
				if (list.Count == dimensionsCount || list2.Count == 0)
				{
					break;
				}
				IList list3 = list2[0] as IList;
				if (list3 == null)
				{
					break;
				}
				list2 = list3;
			}
			return list;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x00010EA8 File Offset: 0x0000F0A8
		private static void CopyFromJaggedToMultidimensionalArray(IList values, Array multidimensionalArray, int[] indices)
		{
			int num = indices.Length;
			if (num == multidimensionalArray.Rank)
			{
				multidimensionalArray.SetValue(CollectionUtils.JaggedArrayGetValue(values, indices), indices);
				return;
			}
			int length = multidimensionalArray.GetLength(num);
			if (((IList)CollectionUtils.JaggedArrayGetValue(values, indices)).Count != length)
			{
				throw new Exception("Cannot deserialize non-cubical array as multidimensional array.");
			}
			int[] array = new int[num + 1];
			for (int i = 0; i < num; i++)
			{
				array[i] = indices[i];
			}
			for (int j = 0; j < multidimensionalArray.GetLength(num); j++)
			{
				array[num] = j;
				CollectionUtils.CopyFromJaggedToMultidimensionalArray(values, multidimensionalArray, array);
			}
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x00010F38 File Offset: 0x0000F138
		private static object JaggedArrayGetValue(IList values, int[] indices)
		{
			IList list = values;
			for (int i = 0; i < indices.Length; i++)
			{
				int index = indices[i];
				if (i == indices.Length - 1)
				{
					return list[index];
				}
				list = (IList)list[index];
			}
			return list;
		}

		// Token: 0x0600044F RID: 1103 RVA: 0x00010F78 File Offset: 0x0000F178
		public static Array ToMultidimensionalArray(IList values, Type type, int rank)
		{
			IList<int> dimensions = CollectionUtils.GetDimensions(values, rank);
			while (dimensions.Count < rank)
			{
				dimensions.Add(0);
			}
			Array array = Array.CreateInstance(type, dimensions.ToArray<int>());
			CollectionUtils.CopyFromJaggedToMultidimensionalArray(values, array, CollectionUtils.ArrayEmpty<int>());
			return array;
		}

		// Token: 0x06000450 RID: 1104 RVA: 0x00010FB9 File Offset: 0x0000F1B9
		public static T[] ArrayEmpty<[Nullable(2)] T>()
		{
			return CollectionUtils.EmptyArrayContainer<T>.Empty;
		}

		// Token: 0x02000167 RID: 359
		[NullableContext(0)]
		private static class EmptyArrayContainer<[Nullable(2)] T>
		{
			// Token: 0x04000698 RID: 1688
			[Nullable(1)]
			public static readonly T[] Empty = new T[0];
		}
	}
}

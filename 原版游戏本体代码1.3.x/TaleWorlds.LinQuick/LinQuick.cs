using System;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.LinQuick
{
	// Token: 0x02000002 RID: 2
	public static class LinQuick
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		public static bool AllQ<T>(this T[] source, Func<T, bool> predicate)
		{
			int num = source.Length;
			for (int i = 0; i < num; i++)
			{
				if (!predicate(source[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002078 File Offset: 0x00000278
		public static bool AllQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (!predicate(source[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020AC File Offset: 0x000002AC
		public static bool AllQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			List<T> list = source as List<T>;
			if (list != null)
			{
				return list.AllQ(predicate);
			}
			T[] array = source as T[];
			if (array != null)
			{
				return array.AllQ(predicate);
			}
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (!predicate(source[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002104 File Offset: 0x00000304
		public static bool AllQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.AllQ(predicate);
			}
			foreach (T arg in source)
			{
				if (!predicate(arg))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002168 File Offset: 0x00000368
		public static bool AnyQ<T>(this T[] source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002177 File Offset: 0x00000377
		public static bool AnyQ<T>(this List<T> source)
		{
			return source.Count > 0;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002182 File Offset: 0x00000382
		public static bool AnyQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002191 File Offset: 0x00000391
		public static bool AnyQ<T>(this IReadOnlyList<T> source)
		{
			return source.Count > 0;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000219C File Offset: 0x0000039C
		public static bool AnyQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000021AC File Offset: 0x000003AC
		public static bool AnyQ<T>(this IEnumerable<T> source)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.AnyQ<T>();
			}
			return source.GetEnumerator().MoveNext();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000021D8 File Offset: 0x000003D8
		public static bool AnyQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.AnyQ(predicate);
			}
			foreach (T arg in source)
			{
				if (predicate(arg))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000223C File Offset: 0x0000043C
		public static float AverageQ(this float[] source)
		{
			if (source.Length == 0)
			{
				throw Error.NoElements();
			}
			float num = 0f;
			int num2 = source.Length;
			for (int i = 0; i < num2; i++)
			{
				num += source[i];
			}
			return num / (float)source.Length;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002278 File Offset: 0x00000478
		public static float AverageQ(this IEnumerable<float> source)
		{
			float num = 0f;
			int num2 = 0;
			foreach (float num3 in source)
			{
				num += num3;
				num2++;
			}
			if (num2 == 0)
			{
				throw Error.NoElements();
			}
			return num / (float)num2;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000022D8 File Offset: 0x000004D8
		public static float AverageQ<T>(this T[] source, Func<T, float> selector)
		{
			if (source.Length == 0)
			{
				throw Error.NoElements();
			}
			float num = 0f;
			int num2 = source.Length;
			for (int i = 0; i < num2; i++)
			{
				num += selector(source[i]);
			}
			return num / (float)source.Length;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000231C File Offset: 0x0000051C
		public static float AverageQ<T>(this List<T> source, Func<T, float> selector)
		{
			int count = source.Count;
			if (count == 0)
			{
				throw Error.NoElements();
			}
			float num = 0f;
			for (int i = 0; i < count; i++)
			{
				num += selector(source[i]);
			}
			return num / (float)count;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002360 File Offset: 0x00000560
		public static float AverageQ<T>(this IReadOnlyList<T> source, Func<T, float> selector)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.AverageQ(selector);
			}
			T[] source3;
			if ((source3 = source as T[]) != null)
			{
				return source3.AverageQ(selector);
			}
			int count = source.Count;
			if (count == 0)
			{
				throw Error.NoElements();
			}
			float num = 0f;
			for (int i = 0; i < count; i++)
			{
				num += selector(source[i]);
			}
			return num / (float)count;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000023CC File Offset: 0x000005CC
		public static float AverageQ<T>(this IEnumerable<T> source, Func<T, float> selector)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.AverageQ(selector);
			}
			float num = 0f;
			int num2 = 0;
			foreach (T arg in source)
			{
				float num3 = selector(arg);
				num += num3;
				num2++;
			}
			if (num2 == 0)
			{
				throw Error.NoElements();
			}
			return num / (float)num2;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002448 File Offset: 0x00000648
		public static bool ContainsQ<T>(this T[] source, T value)
		{
			return source.FindIndexQ(value) != -1;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002457 File Offset: 0x00000657
		public static bool ContainsQ<T>(this List<T> source, T value)
		{
			return source.FindIndexQ(value) != -1;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002466 File Offset: 0x00000666
		public static bool ContainsQ<T>(this IReadOnlyList<T> source, T value)
		{
			return source.FindIndexQ(value) != -1;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002475 File Offset: 0x00000675
		public static bool ContainsQ<T>(this IEnumerable<T> source, T value)
		{
			return source.FindIndexQ(value) != -1;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002484 File Offset: 0x00000684
		public static bool ContainsQ<T>(this Queue<T> source, T value)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int count = source.Count;
			bool result = false;
			for (int i = 0; i < count; i++)
			{
				T t = source.Dequeue();
				if (@default.Equals(t, value))
				{
					result = true;
				}
				source.Enqueue(t);
			}
			return result;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000024CA File Offset: 0x000006CA
		public static bool ContainsQ<T>(this T[] source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000024D9 File Offset: 0x000006D9
		public static bool ContainsQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000024E8 File Offset: 0x000006E8
		public static bool ContainsQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000024F7 File Offset: 0x000006F7
		public static bool ContainsQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			return source.FindIndexQ(predicate) != -1;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002508 File Offset: 0x00000708
		public static bool ContainsQ<T>(this Queue<T> source, Func<T, bool> predicate)
		{
			int count = source.Count;
			bool result = false;
			for (int i = 0; i < count; i++)
			{
				T t = source.Dequeue();
				if (predicate(t))
				{
					result = true;
				}
				source.Enqueue(t);
			}
			return result;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002544 File Offset: 0x00000744
		public static int CountQ<T>(this T[] source, T value)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int num = 0;
			int num2 = source.Length;
			for (int i = 0; i < num2; i++)
			{
				T x = source[i];
				if (@default.Equals(x, value))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002584 File Offset: 0x00000784
		public static int CountQ<T>(this List<T> source, T value)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int num = 0;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (@default.Equals(source[i], value))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000025C4 File Offset: 0x000007C4
		public static int CountQ<T>(this IReadOnlyList<T> source, T value)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.CountQ(value);
			}
			T[] array = source as T[];
			if (array != null)
			{
				return array.CountQ(value);
			}
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int num = 0;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (@default.Equals(source[i], value))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000262C File Offset: 0x0000082C
		public static int CountQ<T>(this T[] source, Func<T, bool> predicate)
		{
			int num = 0;
			int num2 = source.Length;
			for (int i = 0; i < num2; i++)
			{
				if (predicate(source[i]))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002660 File Offset: 0x00000860
		public static int CountQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			int num = 0;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (predicate(source[i]))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002698 File Offset: 0x00000898
		public static int CountQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.CountQ(predicate);
			}
			int num = 0;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (predicate(source[i]))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000026E0 File Offset: 0x000008E0
		public static int CountQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.CountQ(predicate);
			}
			int num = 0;
			foreach (T arg in source)
			{
				if (predicate(arg))
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002744 File Offset: 0x00000944
		public static int CountQ<T>(this IEnumerable<T> source)
		{
			IReadOnlyList<T> readOnlyList;
			if ((readOnlyList = source as IReadOnlyList<T>) != null)
			{
				return readOnlyList.Count;
			}
			int num = 0;
			using (IEnumerator<T> enumerator = source.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					num++;
				}
			}
			return num;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002798 File Offset: 0x00000998
		public static int FindIndexQ<T>(this T[] source, T value)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int result = -1;
			int num = source.Length;
			for (int i = 0; i < num; i++)
			{
				if (@default.Equals(source[i], value))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000027D4 File Offset: 0x000009D4
		public static int FindIndexQ<T>(this List<T> source, T value)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int result = -1;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (@default.Equals(source[i], value))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002814 File Offset: 0x00000A14
		public static int FindIndexQ<T>(this IReadOnlyList<T> source, T value)
		{
			List<T> list = source as List<T>;
			if (list != null)
			{
				return list.FindIndexQ(value);
			}
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int result = -1;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (@default.Equals(source[i], value))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x0000286C File Offset: 0x00000A6C
		public static int FindIndexQ<T>(this IEnumerable<T> source, T value)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.FindIndexQ(value);
			}
			if (value != null && value is IComparable)
			{
				return source.FindIndexComparableQ(value);
			}
			return source.FindIndexNonComparableQ(value);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000028B0 File Offset: 0x00000AB0
		public static int FindIndexQ<T>(this T[] source, Func<T, bool> predicate)
		{
			int result = -1;
			int num = source.Length;
			for (int i = 0; i < num; i++)
			{
				if (predicate(source[i]))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000028E4 File Offset: 0x00000AE4
		public static int FindIndexQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			int result = -1;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (predicate(source[i]))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x0000291C File Offset: 0x00000B1C
		public static int FindIndexQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			List<T> list = source as List<T>;
			if (list != null)
			{
				return list.FindIndexQ(predicate);
			}
			T[] array = source as T[];
			if (array != null)
			{
				return array.FindIndexQ(predicate);
			}
			int result = -1;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (predicate(source[i]))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x0000297C File Offset: 0x00000B7C
		public static int FindIndexQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.FindIndexQ(predicate);
			}
			using (IEnumerator<T> enumerator = source.GetEnumerator())
			{
				int num = 0;
				while (enumerator.MoveNext())
				{
					T arg = enumerator.Current;
					if (predicate(arg))
					{
						return num;
					}
					num++;
				}
			}
			return -1;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x000029E8 File Offset: 0x00000BE8
		private static int FindIndexComparableQ<T>(this IEnumerable<T> source, T value)
		{
			Comparer<T> @default = Comparer<T>.Default;
			using (IEnumerator<T> enumerator = source.GetEnumerator())
			{
				int num = 0;
				while (enumerator.MoveNext())
				{
					T x = enumerator.Current;
					if (@default.Compare(x, value) == 0)
					{
						return num;
					}
					num++;
				}
			}
			return -1;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002A48 File Offset: 0x00000C48
		private static int FindIndexNonComparableQ<T>(this IEnumerable<T> source, T value)
		{
			using (IEnumerator<T> enumerator = source.GetEnumerator())
			{
				int num = 0;
				while (enumerator.MoveNext())
				{
					T t = enumerator.Current;
					if (t.Equals(value))
					{
						return num;
					}
					num++;
				}
			}
			return -1;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002AAC File Offset: 0x00000CAC
		public static T FirstOrDefaultQ<T>(this T[] source, Func<T, bool> predicate)
		{
			int num = source.FindIndexQ(predicate);
			if (num == -1)
			{
				return default(T);
			}
			return source[num];
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002AD8 File Offset: 0x00000CD8
		public static T FirstOrDefaultQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			int num = source.FindIndexQ(predicate);
			if (num == -1)
			{
				return default(T);
			}
			return source[num];
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002B04 File Offset: 0x00000D04
		public static T FirstOrDefaultQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			int num = source.FindIndexQ(predicate);
			if (num == -1)
			{
				return default(T);
			}
			return source[num];
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002B30 File Offset: 0x00000D30
		public static T FirstOrDefaultQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.FirstOrDefaultQ(predicate);
			}
			foreach (T t in source)
			{
				if (predicate(t))
				{
					return t;
				}
			}
			return default(T);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002B9C File Offset: 0x00000D9C
		public static int MaxQ(this int[] source)
		{
			int num = source[0];
			for (int i = 0; i < source.Length; i++)
			{
				if (source[i] > num)
				{
					num = source[i];
				}
			}
			return num;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002BC8 File Offset: 0x00000DC8
		public static int MaxQ(this List<int> source)
		{
			int num = source[0];
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (source[i] > num)
				{
					num = source[i];
				}
			}
			return num;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002C04 File Offset: 0x00000E04
		public static T MaxQ<T>(this T[] source) where T : IComparable<T>
		{
			if (source.Length == 0)
			{
				throw Error.NoElements();
			}
			T t = source[0];
			for (int i = 0; i < source.Length; i++)
			{
				if (source[i].CompareTo(t) > 0)
				{
					t = source[i];
				}
			}
			return t;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00002C54 File Offset: 0x00000E54
		public static T MaxQ<T>(this List<T> source) where T : IComparable<T>
		{
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			T t = source[0];
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				T t2 = source[i];
				if (t2.CompareTo(t) > 0)
				{
					t = source[i];
				}
			}
			return t;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002CAC File Offset: 0x00000EAC
		public static int MaxQ(this IReadOnlyList<int> source)
		{
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			int num = source[0];
			List<int> list = source as List<int>;
			if (list != null)
			{
				return list.MaxQ();
			}
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				if (source[i] > num)
				{
					num = source[i];
				}
			}
			return num;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002D08 File Offset: 0x00000F08
		public static T MaxQ<T>(this IReadOnlyList<T> source) where T : IComparable<T>
		{
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			List<T> list = source as List<T>;
			if (list != null)
			{
				return list.MaxQ<T>();
			}
			T[] array = source as T[];
			if (array != null)
			{
				return array.MaxQ<T>();
			}
			T t = source[0];
			for (int i = 0; i < source.Count; i++)
			{
				T t2 = source[i];
				if (t2.CompareTo(t) > 0)
				{
					t = source[i];
				}
			}
			return t;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002D84 File Offset: 0x00000F84
		public static float MaxQ<T>(this T[] source, Func<T, float> selector)
		{
			if (source.Length == 0)
			{
				throw Error.NoElements();
			}
			float num = selector(source[0]);
			for (int i = 0; i < source.Length; i++)
			{
				float num2 = selector(source[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002DCC File Offset: 0x00000FCC
		public static int MaxQ<T>(this T[] source, Func<T, int> selector)
		{
			if (source.Length == 0)
			{
				throw Error.NoElements();
			}
			int num = selector(source[0]);
			for (int i = 0; i < source.Length; i++)
			{
				int num2 = selector(source[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002E14 File Offset: 0x00001014
		public static float MaxQ<T>(this List<T> source, Func<T, float> selector)
		{
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			float num = selector(source[0]);
			for (int i = 0; i < source.Count; i++)
			{
				float num2 = selector(source[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002E64 File Offset: 0x00001064
		public static int MaxQ<T>(this List<T> source, Func<T, int> selector)
		{
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			int num = selector(source[0]);
			for (int i = 0; i < source.Count; i++)
			{
				int num2 = selector(source[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00002EB4 File Offset: 0x000010B4
		public static float MaxQ<T>(this IReadOnlyList<T> source, Func<T, float> selector)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.MaxQ(selector);
			}
			T[] source3;
			if ((source3 = source as T[]) != null)
			{
				return source3.MaxQ(selector);
			}
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			float num = selector(source[0]);
			for (int i = 0; i < source.Count; i++)
			{
				float num2 = selector(source[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00002F2C File Offset: 0x0000112C
		public static int MaxQ<T>(this IReadOnlyList<T> source, Func<T, int> selector)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.MaxQ(selector);
			}
			T[] source3;
			if ((source3 = source as T[]) != null)
			{
				return source3.MaxQ(selector);
			}
			if (source.Count == 0)
			{
				throw Error.NoElements();
			}
			int num = selector(source[0]);
			for (int i = 0; i < source.Count; i++)
			{
				int num2 = selector(source[i]);
				if (num2 > num)
				{
					num = num2;
				}
			}
			return num;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00002FA4 File Offset: 0x000011A4
		public static float MaxQ<T>(this IEnumerable<T> source, Func<T, float> selector)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.MaxQ(selector);
			}
			float num = 0f;
			bool flag = false;
			foreach (T arg in source)
			{
				float num2 = selector(arg);
				if (!flag)
				{
					num = num2;
					flag = true;
				}
				else if (num2 > num)
				{
					num = num2;
				}
			}
			if (!flag)
			{
				Error.NoElements();
			}
			return num;
		}

		// Token: 0x0600003F RID: 63 RVA: 0x00003024 File Offset: 0x00001224
		public static int MaxQ<T>(this IEnumerable<T> source, Func<T, int> selector)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.MaxQ(selector);
			}
			int num = 0;
			bool flag = false;
			foreach (T arg in source)
			{
				int num2 = selector(arg);
				if (!flag)
				{
					num = num2;
					flag = true;
				}
				else if (num2 > num)
				{
					num = num2;
				}
			}
			if (!flag)
			{
				Error.NoElements();
			}
			return num;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000030A0 File Offset: 0x000012A0
		public static ValueTuple<T, T, T> MaxElements3<T>(this IEnumerable<T> collection, Func<T, float> func)
		{
			float num = float.MinValue;
			float num2 = float.MinValue;
			float num3 = float.MinValue;
			T t = default(T);
			T t2 = default(T);
			T item = default(T);
			foreach (T t3 in collection)
			{
				float num4 = func(t3);
				if (num4 > num3)
				{
					if (num4 > num2)
					{
						num3 = num2;
						item = t2;
						if (num4 > num)
						{
							num2 = num;
							t2 = t;
							num = num4;
							t = t3;
						}
						else
						{
							num2 = num4;
							t2 = t3;
						}
					}
					else
					{
						num3 = num4;
						item = t3;
					}
				}
			}
			return new ValueTuple<T, T, T>(t, t2, item);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003158 File Offset: 0x00001358
		public static IOrderedEnumerable<T> OrderByQ<T, S>(this IEnumerable<T> source, Func<T, S> selector)
		{
			return source.OrderBy(selector);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003164 File Offset: 0x00001364
		public static T[] OrderByQ<T, TKey>(this T[] source, Func<T, TKey> selector)
		{
			Comparer<TKey> @default = Comparer<TKey>.Default;
			TKey[] array = new TKey[source.Length];
			for (int i = 0; i < source.Length; i++)
			{
				array[i] = selector(source[i]);
			}
			T[] array2 = new T[source.Length];
			for (int j = 0; j < source.Length; j++)
			{
				array2[j] = source[j];
			}
			Array.Sort<TKey, T>(array, array2, @default);
			return array2;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000031D8 File Offset: 0x000013D8
		public static T[] OrderByQ<T, TKey>(this List<T> source, Func<T, TKey> selector)
		{
			Comparer<TKey> @default = Comparer<TKey>.Default;
			int count = source.Count;
			TKey[] array = new TKey[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = selector(source[i]);
			}
			T[] array2 = new T[count];
			for (int j = 0; j < count; j++)
			{
				array2[j] = source[j];
			}
			Array.Sort<TKey, T>(array, array2, @default);
			return array2;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003250 File Offset: 0x00001450
		public static T[] OrderByQ<T, TKey>(this IReadOnlyList<T> source, Func<T, TKey> selector)
		{
			Comparer<TKey> @default = Comparer<TKey>.Default;
			int count = source.Count;
			TKey[] array = new TKey[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = selector(source[i]);
			}
			T[] array2 = new T[count];
			for (int j = 0; j < count; j++)
			{
				array2[j] = source[j];
			}
			Array.Sort<TKey, T>(array, array2, @default);
			return array2;
		}

		// Token: 0x06000045 RID: 69 RVA: 0x000032C7 File Offset: 0x000014C7
		public static IEnumerable<R> SelectQ<T, R>(this T[] source, Func<T, R> selector)
		{
			int len = source.Length;
			int num;
			for (int i = 0; i < len; i = num)
			{
				R r = selector(source[i]);
				yield return r;
				num = i + 1;
			}
			yield break;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x000032DE File Offset: 0x000014DE
		public static IEnumerable<R> SelectQ<T, R>(this List<T> source, Func<T, R> selector)
		{
			int len = source.Count;
			int num;
			for (int i = 0; i < len; i = num)
			{
				R r = selector(source[i]);
				yield return r;
				num = i + 1;
			}
			yield break;
		}

		// Token: 0x06000047 RID: 71 RVA: 0x000032F5 File Offset: 0x000014F5
		public static IEnumerable<R> SelectQ<T, R>(this IReadOnlyList<T> source, Func<T, R> selector)
		{
			int len = source.Count;
			int num;
			for (int i = 0; i < len; i = num)
			{
				R r = selector(source[i]);
				yield return r;
				num = i + 1;
			}
			yield break;
		}

		// Token: 0x06000048 RID: 72 RVA: 0x0000330C File Offset: 0x0000150C
		public static IEnumerable<R> SelectQ<T, R>(this IEnumerable<T> source, Func<T, R> selector)
		{
			foreach (T arg in source)
			{
				R r = selector(arg);
				yield return r;
			}
			IEnumerator<T> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x00003324 File Offset: 0x00001524
		public static int SumQ<T>(this T[] source, Func<T, int> func)
		{
			int num = 0;
			int num2 = source.Length;
			for (int i = 0; i < num2; i++)
			{
				num += func(source[i]);
			}
			return num;
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003354 File Offset: 0x00001554
		public static float SumQ<T>(this T[] source, Func<T, float> func)
		{
			float num = 0f;
			int num2 = source.Length;
			for (int i = 0; i < num2; i++)
			{
				num += func(source[i]);
			}
			return num;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003388 File Offset: 0x00001588
		public static int SumQ<T>(this List<T> source, Func<T, int> func)
		{
			int num = 0;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				num += func(source[i]);
			}
			return num;
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000033BC File Offset: 0x000015BC
		public static float SumQ<T>(this List<T> source, Func<T, float> func)
		{
			float num = 0f;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				num += func(source[i]);
			}
			return num;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000033F4 File Offset: 0x000015F4
		public static int SumQ<T>(this IReadOnlyList<T> source, Func<T, int> func)
		{
			List<T> list = source as List<T>;
			if (list != null)
			{
				return list.SumQ(func);
			}
			int num = 0;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				num += func(source[i]);
			}
			return num;
		}

		// Token: 0x0600004E RID: 78 RVA: 0x0000343C File Offset: 0x0000163C
		public static float SumQ<T>(this IReadOnlyList<T> source, Func<T, float> func)
		{
			List<T> list = source as List<T>;
			if (list != null)
			{
				return list.SumQ(func);
			}
			float num = 0f;
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				num += func(source[i]);
			}
			return num;
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003488 File Offset: 0x00001688
		public static float SumQ<T>(this IEnumerable<T> source, Func<T, float> func)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.SumQ(func);
			}
			float num = 0f;
			foreach (T arg in source)
			{
				float num2 = func(arg);
				num += num2;
			}
			return num;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000034EC File Offset: 0x000016EC
		public static int SumQ<T>(this IEnumerable<T> source, Func<T, int> func)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.SumQ(func);
			}
			int num = 0;
			foreach (T arg in source)
			{
				int num2 = func(arg);
				num += num2;
			}
			return num;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x0000354C File Offset: 0x0000174C
		public static T[] ToArrayQ<T>(this T[] source)
		{
			int num = source.Length;
			T[] array = new T[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = source[i];
			}
			return array;
		}

		// Token: 0x06000052 RID: 82 RVA: 0x0000357F File Offset: 0x0000177F
		public static T[] ToArrayQ<T>(this List<T> source)
		{
			return source.ToArray();
		}

		// Token: 0x06000053 RID: 83 RVA: 0x00003588 File Offset: 0x00001788
		public static T[] ToArrayQ<T>(this IReadOnlyList<T> source)
		{
			List<T> list;
			if ((list = source as List<T>) != null)
			{
				return list.ToArray();
			}
			T[] source2;
			if ((source2 = source as T[]) != null)
			{
				return source2.ToArrayQ<T>();
			}
			int count = source.Count;
			T[] array = new T[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = source[i];
			}
			return array;
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000035E8 File Offset: 0x000017E8
		public static T[] ToArrayQ<T>(this IEnumerable<T> source)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.ToArrayQ<T>();
			}
			List<T> list = new List<T>();
			foreach (T item in source)
			{
				list.Add(item);
			}
			return list.ToArray();
		}

		// Token: 0x06000055 RID: 85 RVA: 0x00003650 File Offset: 0x00001850
		public static List<T> ToListQ<T>(this T[] source)
		{
			List<T> list = new List<T>(source.Length);
			list.AddRange(source);
			return list;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00003661 File Offset: 0x00001861
		public static List<T> ToListQ<T>(this List<T> source)
		{
			List<T> list = new List<T>(source.Count);
			list.AddRange(source);
			return list;
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00003678 File Offset: 0x00001878
		public static List<T> ToListQ<T>(this IReadOnlyList<T> source)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.ToListQ<T>();
			}
			T[] source3;
			if ((source3 = source as T[]) != null)
			{
				return source3.ToListQ<T>();
			}
			List<T> list = new List<T>(source.Count);
			list.AddRange(source);
			return list;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x000036BC File Offset: 0x000018BC
		public static List<T> ToListQ<T>(this IEnumerable<T> source)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.ToListQ<T>();
			}
			List<T> list = new List<T>();
			list.AddRange(source);
			return list;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x000036E6 File Offset: 0x000018E6
		public static IEnumerable<T> WhereQ<T>(this T[] source, Func<T, bool> predicate)
		{
			int length = source.Length;
			int num;
			for (int i = 0; i < length; i = num)
			{
				T t = source[i];
				if (predicate(t))
				{
					yield return t;
				}
				num = i + 1;
			}
			yield break;
		}

		// Token: 0x0600005A RID: 90 RVA: 0x000036FD File Offset: 0x000018FD
		public static IEnumerable<T> WhereQ<T>(this List<T> source, Func<T, bool> predicate)
		{
			int length = source.Count;
			int num;
			for (int i = 0; i < length; i = num)
			{
				T t = source[i];
				if (predicate(t))
				{
					yield return t;
				}
				num = i + 1;
			}
			yield break;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003714 File Offset: 0x00001914
		public static IEnumerable<T> WhereQ<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			List<T> source2;
			if ((source2 = source as List<T>) != null)
			{
				return source2.WhereQ(predicate);
			}
			return LinQuick.WhereQImp<T>(source, predicate);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x0000373A File Offset: 0x0000193A
		private static IEnumerable<T> WhereQImp<T>(IReadOnlyList<T> source, Func<T, bool> predicate)
		{
			int length = source.Count;
			int num;
			for (int i = 0; i < length; i = num)
			{
				T t = source[i];
				if (predicate(t))
				{
					yield return t;
				}
				num = i + 1;
			}
			yield break;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003754 File Offset: 0x00001954
		public static IEnumerable<T> WhereQ<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		{
			IReadOnlyList<T> source2;
			if ((source2 = source as IReadOnlyList<T>) != null)
			{
				return source2.WhereQ(predicate);
			}
			return LinQuick.WhereQImp<T>(source, predicate);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x0000377A File Offset: 0x0000197A
		private static IEnumerable<T> WhereQImp<T>(IEnumerable<T> source, Func<T, bool> predicate)
		{
			foreach (T t in source)
			{
				if (predicate(t))
				{
					yield return t;
				}
			}
			IEnumerator<T> enumerator = null;
			yield break;
			yield break;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.Library
{
	// Token: 0x0200002D RID: 45
	public static class Extensions
	{
		// Token: 0x06000169 RID: 361 RVA: 0x00005E68 File Offset: 0x00004068
		public static List<Type> GetTypesSafe(this Assembly assembly, Func<Type, bool> func = null)
		{
			List<Type> list = new List<Type>();
			Type[] array;
			try
			{
				array = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				array = ex.Types;
				foreach (Exception ex2 in ex.LoaderExceptions)
				{
				}
			}
			catch (Exception)
			{
				array = Array.Empty<Type>();
			}
			try
			{
				foreach (Type type in array)
				{
					if (type != null && (func == null || func(type)))
					{
						list.Add(type);
					}
				}
			}
			catch (Exception)
			{
			}
			return list;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00005F14 File Offset: 0x00004114
		public static Assembly[] GetReferencingAssembliesSafe(this Assembly baseAssembly, Func<Assembly, bool> func = null)
		{
			Assembly[] assemblies;
			try
			{
				assemblies = AppDomain.CurrentDomain.GetAssemblies();
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetReferencingAssembliesSafe", 72);
				return Array.Empty<Assembly>();
			}
			List<Assembly> list = new List<Assembly>();
			foreach (Assembly assembly in assemblies)
			{
				foreach (AssemblyName assemblyName in assembly.GetReferencedAssemblies())
				{
					try
					{
						if (assemblyName.ToString() == baseAssembly.GetName().ToString() && (func == null || func(assembly)))
						{
							list.Add(assembly);
							break;
						}
					}
					catch
					{
						Debug.FailedAssert(string.Format("Error while resolving references of assembly: {0}", assembly), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetReferencingAssembliesSafe", 93);
					}
				}
			}
			return list.ToArray();
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00006010 File Offset: 0x00004210
		public static object[] GetCustomAttributesSafe(this Type type, Type attributeType, bool inherit)
		{
			try
			{
				return type.GetCustomAttributes(attributeType, inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(string.Concat(new string[] { "Failed to get custom attributes (", attributeType.Name, ") for type: ", type.Name, ". Exception: ", ex.Message }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 113);
			}
			return Array.Empty<object>();
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00006094 File Offset: 0x00004294
		public static object[] GetCustomAttributesSafe(this Type type, bool inherit)
		{
			try
			{
				return type.GetCustomAttributes(inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for type: " + type.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 128);
			}
			return Array.Empty<object>();
		}

		// Token: 0x0600016D RID: 365 RVA: 0x000060F4 File Offset: 0x000042F4
		public static IEnumerable<Attribute> GetCustomAttributesSafe(this Type type, Type attributeType)
		{
			try
			{
				return type.GetCustomAttributes(attributeType);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(string.Concat(new string[] { "Failed to get custom attributes (", attributeType.Name, ") for type: ", type.Name, ". Exception: ", ex.Message }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 143);
			}
			return new List<Attribute>();
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00006178 File Offset: 0x00004378
		public static object[] GetCustomAttributesSafe(this PropertyInfo property, Type attributeType, bool inherit)
		{
			try
			{
				return property.GetCustomAttributes(attributeType, inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(string.Concat(new string[] { "Failed to get custom attributes (", attributeType.Name, ") for property: ", property.Name, ". Exception: ", ex.Message }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 158);
			}
			return Array.Empty<object>();
		}

		// Token: 0x0600016F RID: 367 RVA: 0x000061FC File Offset: 0x000043FC
		public static object[] GetCustomAttributesSafe(this PropertyInfo property, bool inherit)
		{
			try
			{
				return property.GetCustomAttributes(inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for property: " + property.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 173);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000625C File Offset: 0x0000445C
		public static IEnumerable<Attribute> GetCustomAttributesSafe(this PropertyInfo property, Type attributeType)
		{
			try
			{
				return property.GetCustomAttributes(attributeType);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for property: " + property.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 188);
			}
			return new List<Attribute>();
		}

		// Token: 0x06000171 RID: 369 RVA: 0x000062BC File Offset: 0x000044BC
		public static object[] GetCustomAttributesSafe(this FieldInfo field, Type attributeType, bool inherit)
		{
			try
			{
				return field.GetCustomAttributes(attributeType, false);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(string.Concat(new string[] { "Failed to get custom attributes (", attributeType.Name, ") for field: ", field.Name, ". Exception: ", ex.Message }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 203);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00006340 File Offset: 0x00004540
		public static object[] GetCustomAttributesSafe(this FieldInfo field, bool inherit)
		{
			try
			{
				return field.GetCustomAttributes(inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for field: " + field.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 218);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000173 RID: 371 RVA: 0x000063A0 File Offset: 0x000045A0
		public static IEnumerable<Attribute> GetCustomAttributesSafe(this FieldInfo field, Type attributeType)
		{
			try
			{
				return field.GetCustomAttributes(attributeType);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for field: " + field.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 233);
			}
			return new List<Attribute>();
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00006400 File Offset: 0x00004600
		public static object[] GetCustomAttributesSafe(this MethodInfo method, Type attributeType, bool inherit)
		{
			try
			{
				return method.GetCustomAttributes(attributeType, false);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(string.Concat(new string[] { "Failed to get custom attributes (", attributeType.Name, ") for method: ", method.Name, ". Exception: ", ex.Message }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 248);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00006484 File Offset: 0x00004684
		public static object[] GetCustomAttributesSafe(this MethodInfo method, bool inherit)
		{
			try
			{
				return method.GetCustomAttributes(inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for method: " + method.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 263);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000176 RID: 374 RVA: 0x000064E4 File Offset: 0x000046E4
		public static IEnumerable<Attribute> GetCustomAttributesSafe(this MethodInfo method, Type attributeType)
		{
			try
			{
				return method.GetCustomAttributes(attributeType);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for method: " + method.Name + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 278);
			}
			return new List<Attribute>();
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00006544 File Offset: 0x00004744
		public static object[] GetCustomAttributesSafe(this Assembly assembly, Type attributeType, bool inherit)
		{
			try
			{
				return assembly.GetCustomAttributes(attributeType, false);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert(string.Concat(new string[] { "Failed to get custom attributes (", attributeType.Name, ") for assembly: ", assembly.FullName, ". Exception: ", ex.Message }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 293);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000178 RID: 376 RVA: 0x000065C8 File Offset: 0x000047C8
		public static object[] GetCustomAttributesSafe(this Assembly assembly, bool inherit)
		{
			try
			{
				return assembly.GetCustomAttributes(inherit);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for assembly: " + assembly.FullName + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 308);
			}
			return Array.Empty<object>();
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00006628 File Offset: 0x00004828
		public static IEnumerable<Attribute> GetCustomAttributesSafe(this Assembly assembly, Type attributeType)
		{
			try
			{
				return assembly.GetCustomAttributes(attributeType);
			}
			catch (Exception ex)
			{
				Debug.FailedAssert("Failed to get custom attributes for assembly: " + assembly.FullName + ". Exception: " + ex.Message, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\Extensions.cs", "GetCustomAttributesSafe", 323);
			}
			return new List<Attribute>();
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00006688 File Offset: 0x00004888
		public static MBList<T> ToMBList<T>(this T[] source)
		{
			MBList<T> mblist = new MBList<T>(source.Length);
			mblist.AddRange(source);
			return mblist;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00006699 File Offset: 0x00004899
		public static MBList<T> ToMBList<T>(this List<T> source)
		{
			MBList<T> mblist = new MBList<T>(source.Count);
			mblist.AddRange(source);
			return mblist;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x000066B0 File Offset: 0x000048B0
		public static MBList<T> ToMBList<T>(this IEnumerable<T> source)
		{
			T[] source2;
			if ((source2 = source as T[]) != null)
			{
				return source2.ToMBList<T>();
			}
			List<T> source3;
			if ((source3 = source as List<T>) != null)
			{
				return source3.ToMBList<T>();
			}
			MBList<T> mblist = new MBList<T>();
			mblist.AddRange(source);
			return mblist;
		}

		// Token: 0x0600017D RID: 381 RVA: 0x000066EC File Offset: 0x000048EC
		public static void AppendList<T>(this List<T> list1, List<T> list2)
		{
			if (list1.Count + list2.Count > list1.Capacity)
			{
				list1.Capacity = list1.Count + list2.Count;
			}
			for (int i = 0; i < list2.Count; i++)
			{
				list1.Add(list2[i]);
			}
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000673F File Offset: 0x0000493F
		public static MBReadOnlyDictionary<TKey, TValue> GetReadOnlyDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
		{
			return new MBReadOnlyDictionary<TKey, TValue>(dictionary);
		}

		// Token: 0x0600017F RID: 383 RVA: 0x00006747 File Offset: 0x00004947
		public static bool HasAnyFlag<T>(this T p1, T p2) where T : struct
		{
			return EnumHelper<T>.HasAnyFlag(p1, p2);
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00006755 File Offset: 0x00004955
		public static bool HasAllFlags<T>(this T p1, T p2) where T : struct
		{
			return EnumHelper<T>.HasAllFlags(p1, p2);
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00006763 File Offset: 0x00004963
		public static int GetDeterministicHashCode(this string text)
		{
			return Common.GetDJB2(text);
		}

		// Token: 0x06000182 RID: 386 RVA: 0x0000676C File Offset: 0x0000496C
		public static int IndexOfMin<TSource>(this IReadOnlyList<TSource> self, Func<TSource, int> func)
		{
			int num = int.MaxValue;
			int result = -1;
			for (int i = 0; i < self.Count; i++)
			{
				int num2 = func(self[i]);
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x06000183 RID: 387 RVA: 0x000067AC File Offset: 0x000049AC
		public static int IndexOfMin<TSource>(this MBReadOnlyList<TSource> self, Func<TSource, int> func)
		{
			int num = int.MaxValue;
			int result = -1;
			for (int i = 0; i < self.Count; i++)
			{
				int num2 = func(self[i]);
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x06000184 RID: 388 RVA: 0x000067EC File Offset: 0x000049EC
		public static int IndexOfMax<TSource>(this IReadOnlyList<TSource> self, Func<TSource, int> func)
		{
			int num = int.MinValue;
			int result = -1;
			for (int i = 0; i < self.Count; i++)
			{
				int num2 = func(self[i]);
				if (num2 > num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x06000185 RID: 389 RVA: 0x0000682C File Offset: 0x00004A2C
		public static int IndexOfMax<TSource>(this MBReadOnlyList<TSource> self, Func<TSource, int> func)
		{
			int num = int.MinValue;
			int result = -1;
			for (int i = 0; i < self.Count; i++)
			{
				int num2 = func(self[i]);
				if (num2 > num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		// Token: 0x06000186 RID: 390 RVA: 0x0000686C File Offset: 0x00004A6C
		public static int IndexOf<TValue>(this TValue[] source, TValue item)
		{
			for (int i = 0; i < source.Length; i++)
			{
				if (source[i].Equals(item))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x000068A8 File Offset: 0x00004AA8
		public static int FindIndex<TValue>(this IReadOnlyList<TValue> source, Func<TValue, bool> predicate)
		{
			for (int i = 0; i < source.Count; i++)
			{
				if (predicate(source[i]))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000188 RID: 392 RVA: 0x000068D8 File Offset: 0x00004AD8
		public static int FindIndex<TValue>(this MBReadOnlyList<TValue> source, Func<TValue, bool> predicate)
		{
			for (int i = 0; i < source.Count; i++)
			{
				if (predicate(source[i]))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00006908 File Offset: 0x00004B08
		public static int FindLastIndex<TValue>(this IReadOnlyList<TValue> source, Func<TValue, bool> predicate)
		{
			for (int i = source.Count - 1; i >= 0; i--)
			{
				if (predicate(source[i]))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000693C File Offset: 0x00004B3C
		public static int FindLastIndex<TValue>(this MBReadOnlyList<TValue> source, Func<TValue, bool> predicate)
		{
			for (int i = source.Count - 1; i >= 0; i--)
			{
				if (predicate(source[i]))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00006970 File Offset: 0x00004B70
		public static void Randomize<T>(this IList<T> array)
		{
			Random random = new Random();
			int i = array.Count;
			while (i > 1)
			{
				i--;
				int index = random.Next(0, i + 1);
				T value = array[index];
				array[index] = array[i];
				array[i] = value;
			}
		}
	}
}

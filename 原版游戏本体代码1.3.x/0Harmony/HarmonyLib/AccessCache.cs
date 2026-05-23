using System;
using System.Collections.Generic;
using System.Reflection;

namespace HarmonyLib
{
	// Token: 0x0200000C RID: 12
	internal class AccessCache
	{
		// Token: 0x06000029 RID: 41 RVA: 0x00002810 File Offset: 0x00000A10
		private static T Get<T>(Dictionary<Type, Dictionary<string, T>> dict, Type type, string name, Func<T> fetcher)
		{
			T result;
			lock (dict)
			{
				Dictionary<string, T> valuesByName;
				if (!dict.TryGetValue(type, out valuesByName))
				{
					valuesByName = new Dictionary<string, T>();
					dict[type] = valuesByName;
				}
				T value;
				if (!valuesByName.TryGetValue(name, out value))
				{
					value = fetcher();
					valuesByName[name] = value;
				}
				result = value;
			}
			return result;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002880 File Offset: 0x00000A80
		private static T Get<T>(Dictionary<Type, Dictionary<string, Dictionary<int, T>>> dict, Type type, string name, Type[] arguments, Func<T> fetcher)
		{
			T result;
			lock (dict)
			{
				Dictionary<string, Dictionary<int, T>> valuesByName;
				if (!dict.TryGetValue(type, out valuesByName))
				{
					valuesByName = new Dictionary<string, Dictionary<int, T>>();
					dict[type] = valuesByName;
				}
				Dictionary<int, T> valuesByArgument;
				if (!valuesByName.TryGetValue(name, out valuesByArgument))
				{
					valuesByArgument = new Dictionary<int, T>();
					valuesByName[name] = valuesByArgument;
				}
				int argumentsHash = AccessTools.CombinedHashCode(arguments);
				T value;
				if (!valuesByArgument.TryGetValue(argumentsHash, out value))
				{
					value = fetcher();
					valuesByArgument[argumentsHash] = value;
				}
				result = value;
			}
			return result;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002914 File Offset: 0x00000B14
		internal FieldInfo GetFieldInfo(Type type, string name, AccessCache.MemberType memberType = AccessCache.MemberType.Any, bool declaredOnly = false)
		{
			FieldInfo value = AccessCache.Get<FieldInfo>(this.declaredFields, type, name, () => type.GetField(name, AccessCache.declaredOnlyBindingFlags[memberType]));
			if (value == null && !declaredOnly)
			{
				Func<Type, FieldInfo> <>9__2;
				value = AccessCache.Get<FieldInfo>(this.inheritedFields, type, name, delegate()
				{
					Type type2 = type;
					Func<Type, FieldInfo> func;
					if ((func = <>9__2) == null)
					{
						func = (<>9__2 = (Type t) => t.GetField(name, AccessTools.all));
					}
					return AccessTools.FindIncludingBaseTypes<FieldInfo>(type2, func);
				});
			}
			return value;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000298C File Offset: 0x00000B8C
		internal PropertyInfo GetPropertyInfo(Type type, string name, AccessCache.MemberType memberType = AccessCache.MemberType.Any, bool declaredOnly = false)
		{
			PropertyInfo value = AccessCache.Get<PropertyInfo>(this.declaredProperties, type, name, () => type.GetProperty(name, AccessCache.declaredOnlyBindingFlags[memberType]));
			if (value == null && !declaredOnly)
			{
				Func<Type, PropertyInfo> <>9__2;
				value = AccessCache.Get<PropertyInfo>(this.inheritedProperties, type, name, delegate()
				{
					Type type2 = type;
					Func<Type, PropertyInfo> func;
					if ((func = <>9__2) == null)
					{
						func = (<>9__2 = (Type t) => t.GetProperty(name, AccessTools.all));
					}
					return AccessTools.FindIncludingBaseTypes<PropertyInfo>(type2, func);
				});
			}
			return value;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002A04 File Offset: 0x00000C04
		internal MethodBase GetMethodInfo(Type type, string name, Type[] arguments, AccessCache.MemberType memberType = AccessCache.MemberType.Any, bool declaredOnly = false)
		{
			MethodBase value = AccessCache.Get<MethodBase>(this.declaredMethods, type, name, arguments, () => type.GetMethod(name, AccessCache.declaredOnlyBindingFlags[memberType], null, arguments, null));
			if (value == null && !declaredOnly)
			{
				value = AccessCache.Get<MethodBase>(this.inheritedMethods, type, name, arguments, () => AccessTools.Method(type, name, arguments, null));
			}
			return value;
		}

		// Token: 0x04000004 RID: 4
		private const BindingFlags BasicFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;

		// Token: 0x04000005 RID: 5
		private static readonly Dictionary<AccessCache.MemberType, BindingFlags> declaredOnlyBindingFlags = new Dictionary<AccessCache.MemberType, BindingFlags>
		{
			{
				AccessCache.MemberType.Any,
				BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty
			},
			{
				AccessCache.MemberType.Instance,
				BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty
			},
			{
				AccessCache.MemberType.Static,
				BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty
			}
		};

		// Token: 0x04000006 RID: 6
		private readonly Dictionary<Type, Dictionary<string, FieldInfo>> declaredFields = new Dictionary<Type, Dictionary<string, FieldInfo>>();

		// Token: 0x04000007 RID: 7
		private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> declaredProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

		// Token: 0x04000008 RID: 8
		private readonly Dictionary<Type, Dictionary<string, Dictionary<int, MethodBase>>> declaredMethods = new Dictionary<Type, Dictionary<string, Dictionary<int, MethodBase>>>();

		// Token: 0x04000009 RID: 9
		private readonly Dictionary<Type, Dictionary<string, FieldInfo>> inheritedFields = new Dictionary<Type, Dictionary<string, FieldInfo>>();

		// Token: 0x0400000A RID: 10
		private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> inheritedProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

		// Token: 0x0400000B RID: 11
		private readonly Dictionary<Type, Dictionary<string, Dictionary<int, MethodBase>>> inheritedMethods = new Dictionary<Type, Dictionary<string, Dictionary<int, MethodBase>>>();

		// Token: 0x0200000D RID: 13
		internal enum MemberType
		{
			// Token: 0x0400000D RID: 13
			Any,
			// Token: 0x0400000E RID: 14
			Static,
			// Token: 0x0400000F RID: 15
			Instance
		}
	}
}

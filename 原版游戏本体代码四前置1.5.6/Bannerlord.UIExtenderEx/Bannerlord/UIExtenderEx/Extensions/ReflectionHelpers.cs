using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

namespace Bannerlord.UIExtenderEx.Extensions;

internal static class ReflectionHelpers
{
	private delegate void SetMemberValue<in T>(object instance, T? value);

	private delegate T? GetMemberValue<out T>(object instance);

	private static readonly ConcurrentDictionary<Type, Dictionary<string, MemberInfo>> _fieldPropertyCache = new ConcurrentDictionary<Type, Dictionary<string, MemberInfo>>();

	private static readonly ConcurrentDictionary<MemberInfo, Delegate?> _getDelegateCache = new ConcurrentDictionary<MemberInfo, Delegate>();

	private static readonly ConcurrentDictionary<MemberInfo, Delegate?> _setDelegateCache = new ConcurrentDictionary<MemberInfo, Delegate>();

	public static T? PrivateValue<T>(this object? o, string fieldPropertyName)
	{
		if (o == null)
		{
			return default(T);
		}
		Dictionary<string, MemberInfo> orAdd = _fieldPropertyCache.GetOrAdd(o.GetType(), (Type x) => x.GetProperties().OfType<MemberInfo>().Concat(x.GetFields())
			.ToDictionary((MemberInfo memberInfo) => memberInfo.Name, (MemberInfo result) => result));
		if (!orAdd.TryGetValue(fieldPropertyName, out var value))
		{
			return default(T);
		}
		Delegate orAdd2 = _getDelegateCache.GetOrAdd(value, delegate(MemberInfo x)
		{
			if (x is FieldInfo fieldInfo)
			{
				return AccessTools2.FieldRefAccess<object, T>(fieldInfo.Name);
			}
			return (x is PropertyInfo propertyInfo) ? AccessTools2.GetPropertyGetterDelegate<GetMemberValue<T>>(propertyInfo) : null;
		});
		if (!(orAdd2 is GetMemberValue<T> getMemberValue))
		{
			if (orAdd2 is AccessTools.FieldRef<object, T> fieldRef)
			{
				return fieldRef(o);
			}
			return default(T);
		}
		return getMemberValue(o);
	}

	public static void PrivateValueSet<T>(this object? o, string fieldPropertyName, T? value)
	{
		if (o == null)
		{
			return;
		}
		Dictionary<string, MemberInfo> orAdd = _fieldPropertyCache.GetOrAdd(o.GetType(), (Type x) => x.GetProperties().OfType<MemberInfo>().Concat(x.GetFields())
			.ToDictionary((MemberInfo memberInfo) => memberInfo.Name, (MemberInfo result) => result));
		if (!orAdd.TryGetValue(fieldPropertyName, out var value2))
		{
			return;
		}
		Delegate orAdd2 = _setDelegateCache.GetOrAdd(value2, delegate(MemberInfo x)
		{
			if (x is FieldInfo fieldInfo)
			{
				return AccessTools2.FieldRefAccess<object, T>(fieldInfo.Name);
			}
			return (x is PropertyInfo propertyInfo) ? AccessTools2.GetPropertySetterDelegate<SetMemberValue<T>>(propertyInfo) : null;
		});
		if (!(orAdd2 is SetMemberValue<T> setMemberValue))
		{
			if (orAdd2 is AccessTools.FieldRef<object, T> fieldRef)
			{
				fieldRef(o) = value;
			}
		}
		else
		{
			setMemberValue(o, value);
		}
	}
}

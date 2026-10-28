using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace HarmonyLib.BUTR.Extensions;

[ExcludeFromCodeCoverage]
internal readonly struct AccessCacheHandle
{
	internal enum MemberType
	{
		Any,
		Static,
		Instance
	}

	private delegate object AccessCacheCtorDelegate();

	private delegate FieldInfo GetFieldInfoDelegate(object instance, Type type, string name, MemberType memberType = MemberType.Any, bool declaredOnly = false);

	private delegate PropertyInfo GetPropertyInfoDelegate(object instance, Type type, string name, MemberType memberType = MemberType.Any, bool declaredOnly = false);

	private delegate MethodBase GetMethodInfoDelegate(object instance, Type type, string name, Type[] arguments, MemberType memberType = MemberType.Any, bool declaredOnly = false);

	private static readonly Type Blank;

	private static readonly AccessCacheCtorDelegate? AccessCacheCtorMethod;

	private static readonly GetFieldInfoDelegate? GetFieldInfoMethod;

	private static readonly GetPropertyInfoDelegate? GetPropertyInfoMethod;

	private static readonly GetMethodInfoDelegate? GetMethodInfoMethod;

	private readonly object _accessCache;

	static AccessCacheHandle()
	{
		Blank = typeof(Harmony);
		AccessCacheCtorMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDeclaredConstructorDelegate<AccessCacheCtorDelegate>("HarmonyLib.AccessCache");
		GetFieldInfoMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegateObjectInstance<GetFieldInfoDelegate>("HarmonyLib.AccessCache:GetFieldInfo");
		GetPropertyInfoMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegateObjectInstance<GetPropertyInfoDelegate>("HarmonyLib.AccessCache:GetPropertyInfo");
		GetMethodInfoMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegateObjectInstance<GetMethodInfoDelegate>("HarmonyLib.AccessCache:GetMethodInfo");
	}

	public static HarmonyLib.BUTR.Extensions.AccessCacheHandle? Create()
	{
		object obj = AccessCacheCtorMethod?.Invoke();
		return (obj != null) ? new HarmonyLib.BUTR.Extensions.AccessCacheHandle?(new HarmonyLib.BUTR.Extensions.AccessCacheHandle(obj)) : ((HarmonyLib.BUTR.Extensions.AccessCacheHandle?)null);
	}

	private AccessCacheHandle(object accessCache)
	{
		_accessCache = accessCache;
	}

	public FieldInfo? GetFieldInfo(Type type, string name, MemberType memberType = MemberType.Any, bool declaredOnly = false)
	{
		return GetFieldInfoMethod?.Invoke(_accessCache, type, name, memberType, declaredOnly);
	}

	public PropertyInfo? GetPropertyInfo(Type type, string name, MemberType memberType = MemberType.Any, bool declaredOnly = false)
	{
		return GetPropertyInfoMethod?.Invoke(_accessCache, type, name, memberType, declaredOnly);
	}

	public MethodBase? GetMethodInfo(Type type, string name, Type[] arguments, MemberType memberType = MemberType.Any, bool declaredOnly = false)
	{
		return GetMethodInfoMethod?.Invoke(_accessCache, type, name, arguments, memberType, declaredOnly);
	}
}

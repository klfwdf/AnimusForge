using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bannerlord.BUTR.Shared.Helpers;
using HarmonyLib.BUTR.Extensions;

namespace Bannerlord.ButterLib.DynamicAPI;

public static class DynamicAPIProvider
{
	private record TypeWithAttribute(Type Type, CustomAttributeData CustomAttributeData);

	private record MethodInfoWithAttribute(MethodInfo MethodInfo, CustomAttributeData CustomAttributeData);

	private delegate object DynamicAPIObjectActivator();

	private static readonly HashSet<string> PossibleClassNames = new HashSet<string> { "Bannerlord.ButterLib.DynamicAPI.DynamicAPIClassAttribute", "Bannerlord.DynamicAPI.DynamicAPIClassAttribute" };

	private static readonly HashSet<string> PossibleMethodNames = new HashSet<string> { "Bannerlord.ButterLib.DynamicAPI.DynamicAPIMethodAttribute", "Bannerlord.DynamicAPI.DynamicAPIMethodAttribute" };

	private static readonly ConcurrentDictionary<string, DynamicAPIObjectActivator?> CachedActivators = new ConcurrentDictionary<string, DynamicAPIObjectActivator>();

	internal static Dictionary<string, Type>? APIClasses;

	internal static Dictionary<Type, Dictionary<string, MethodInfo>>? APIClassMethods;

	private static TypeWithAttribute? GetDynamicAPIClass(Type? type)
	{
		if ((object)type == null)
		{
			return null;
		}
		if (type.IsAbstract)
		{
			return null;
		}
		CustomAttributeData customAttributeData = type.CustomAttributes.FirstOrDefault(Predicate);
		if (customAttributeData == null)
		{
			return null;
		}
		return new TypeWithAttribute(type, customAttributeData);
		static bool Predicate(CustomAttributeData x)
		{
			if (PossibleClassNames.Contains(x.AttributeType.FullName) && x.ConstructorArguments.Count == 1)
			{
				return x.ConstructorArguments[0].ArgumentType == typeof(string);
			}
			return false;
		}
	}

	private static MethodInfoWithAttribute? GetDynamicAPIMethod(MethodInfo? methodInfo)
	{
		if ((object)methodInfo == null)
		{
			return null;
		}
		CustomAttributeData customAttributeData = methodInfo.CustomAttributes.FirstOrDefault(Predicate);
		if (customAttributeData == null)
		{
			return null;
		}
		return new MethodInfoWithAttribute(methodInfo, customAttributeData);
		static bool Predicate(CustomAttributeData x)
		{
			if (PossibleMethodNames.Contains(x.AttributeType.FullName) && x.ConstructorArguments.Count == 1)
			{
				return x.ConstructorArguments[0].ArgumentType == typeof(string);
			}
			return false;
		}
	}

	private static string DynamicAPIClassAttributeName(TypeWithAttribute typeWithAttribute)
	{
		return (string)typeWithAttribute.CustomAttributeData.ConstructorArguments[0].Value;
	}

	private static string DynamicAPIMethodAttributeName(MethodInfoWithAttribute methodInfoWithAttribute)
	{
		string text = (string)methodInfoWithAttribute.CustomAttributeData.ConstructorArguments[0].Value;
		return (methodInfoWithAttribute.MethodInfo.IsStatic ? "0static" : "0instance") + "_" + text;
	}

	private static IEnumerable<Assembly> GetAssembliesToScan()
	{
		List<ModuleInfoExtendedHelper> loadedModules = (from x in ModuleInfoHelper.GetLoadedModules()
			where (object)x != null
			select x).ToList();
		foreach (Assembly assembly in from x in HarmonyLib.BUTR.Extensions.AccessTools2.AllAssemblies()
			where !x.IsDynamic && !string.IsNullOrEmpty(x.Location)
			select x)
		{
			if (loadedModules.Any((ModuleInfoExtendedHelper loadedModule) => ModuleInfoHelper.IsModuleAssembly(loadedModule, assembly)))
			{
				yield return assembly;
			}
		}
	}

	internal static void Initialize()
	{
		APIClasses = GetAssembliesToScan().SelectMany(delegate(Assembly x)
		{
			try
			{
				return x.GetTypes().Select(GetDynamicAPIClass);
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Select(GetDynamicAPIClass);
			}
			catch (Exception)
			{
				return Array.Empty<TypeWithAttribute>();
			}
		}).OfType<TypeWithAttribute>().ToDictionary(DynamicAPIClassAttributeName, (TypeWithAttribute x) => x.Type);
		APIClassMethods = APIClasses.Values.ToDictionary((Type x) => x, (Type x) => x.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Select(GetDynamicAPIMethod).OfType<MethodInfoWithAttribute>()
			.ToDictionary(DynamicAPIMethodAttributeName, (MethodInfoWithAttribute y) => y.MethodInfo));
	}

	public static DynamicAPIInstance? RequestAPIClass(string? @class)
	{
		if (APIClasses == null)
		{
			return null;
		}
		if (@class == null)
		{
			return null;
		}
		DynamicAPIObjectActivator orAdd = CachedActivators.GetOrAdd(@class, delegate(string key)
		{
			if (!APIClasses.TryGetValue(key, out Type value))
			{
				return (DynamicAPIObjectActivator?)null;
			}
			ConstructorInfo constructor = value.GetConstructor(Type.EmptyTypes);
			return ((object)constructor == null) ? null : ((DynamicAPIObjectActivator)Expression.Lambda(typeof(DynamicAPIObjectActivator), Expression.New(constructor)).Compile());
		});
		if (orAdd == null)
		{
			return null;
		}
		return new DynamicAPIInstance(orAdd());
	}

	public static TDelegate? RequestAPIMethod<TDelegate>(string? @class, string? method) where TDelegate : Delegate
	{
		if (APIClasses == null || APIClassMethods == null)
		{
			return null;
		}
		if (@class == null || method == null)
		{
			return null;
		}
		if (!APIClasses.TryGetValue(@class, out Type value))
		{
			return null;
		}
		if (!APIClassMethods.TryGetValue(value, out Dictionary<string, MethodInfo> value2))
		{
			return null;
		}
		if (!value2.TryGetValue("0static_" + method, out var value3))
		{
			return null;
		}
		return HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<TDelegate>(value3);
	}
}

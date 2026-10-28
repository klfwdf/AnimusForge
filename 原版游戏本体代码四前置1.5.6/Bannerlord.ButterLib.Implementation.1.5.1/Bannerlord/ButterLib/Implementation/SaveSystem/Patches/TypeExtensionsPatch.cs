using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaleWorlds.SaveSystem;

namespace Bannerlord.ButterLib.Implementation.SaveSystem.Patches;

internal sealed class TypeExtensionsPatch
{
	private static ILogger _log = null;

	private static readonly Type? TargetType = AccessTools2.TypeByName("TaleWorlds.SaveSystem.TypeExtensions");

	private static readonly Type[] TargetMethodParams = new Type[2]
	{
		typeof(Type),
		typeof(ContainerType).MakeByRefType()
	};

	private static readonly MethodInfo? TargetMethod = AccessTools2.Method(TargetType, "IsContainer", TargetMethodParams);

	private static readonly MethodInfo? PatchMethod = SymbolExtensions2.GetMethodInfo((Type x, ContainerType y, bool z) => IsContainerPrefix(x, out y, ref z));

	internal static bool Enable(Harmony harmony)
	{
		_log = (ButterLibSubModule.Instance?.GetServiceProvider() ?? ButterLibSubModule.Instance?.GetTempServiceProvider())?.GetService<ILogger<TypeExtensionsPatch>>() ?? NullLogger<TypeExtensionsPatch>.Instance;
		if (NotNull(TargetType, "TargetType") & NotNull(TargetMethod, "TargetMethod") & NotNull(PatchMethod, "PatchMethod"))
		{
			return (object)harmony.Patch(TargetMethod, new HarmonyMethod(PatchMethod)) != null;
		}
		return false;
	}

	internal static bool Disable(Harmony harmony)
	{
		if (NotNull(TargetType, "TargetType") & NotNull(TargetMethod, "TargetMethod") & NotNull(PatchMethod, "PatchMethod"))
		{
			harmony.Unpatch(TargetMethod, PatchMethod);
		}
		return true;
	}

	private static bool IsContainerPrefix(Type type, out ContainerType containerType, ref bool __result)
	{
		containerType = ContainerType.None;
		if ((object)type != null && type.IsGenericType && !type.IsGenericTypeDefinition)
		{
			type = type.GetGenericTypeDefinition();
		}
		if (type.IsArray)
		{
			containerType = ContainerType.Array;
		}
		else if (typeof(IDictionary).IsAssignableFrom(type))
		{
			containerType = ContainerType.Dictionary;
		}
		else if (typeof(IList).IsAssignableFrom(type))
		{
			containerType = ContainerType.List;
		}
		else if (type == typeof(Queue<>) || type == typeof(Queue))
		{
			containerType = ContainerType.Queue;
		}
		__result = containerType != ContainerType.None;
		return false;
	}

	private static bool NotNull<T>(T obj, string name) where T : class?
	{
		if (obj == null)
		{
			_log.LogError("{Name} is null!", name);
			return false;
		}
		return true;
	}
}

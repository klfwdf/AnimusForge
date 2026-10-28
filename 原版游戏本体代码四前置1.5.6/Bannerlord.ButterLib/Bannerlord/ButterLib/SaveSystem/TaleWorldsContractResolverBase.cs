using System;
using System.Collections.Generic;
using HarmonyLib.BUTR.Extensions;
using Newtonsoft.Json.Serialization;

namespace Bannerlord.ButterLib.SaveSystem;

public class TaleWorldsContractResolverBase : DefaultContractResolver
{
	protected delegate bool IsContainerDelegate(Type type);

	protected static readonly IsContainerDelegate? _isContainerDelegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<IsContainerDelegate>("TaleWorlds.SaveSystem.TypeExtensions:IsContainer", new Type[1] { typeof(Type) });

	protected static bool IsContainerFallback(Type type)
	{
		if ((object)type != null && type.IsGenericType && !type.IsGenericTypeDefinition)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			if (genericTypeDefinition == typeof(Dictionary<, >))
			{
				return true;
			}
			if (genericTypeDefinition == typeof(List<>))
			{
				return true;
			}
			if (genericTypeDefinition == typeof(Queue<>))
			{
				return true;
			}
		}
		else if (type.IsArray)
		{
			return true;
		}
		return false;
	}
}

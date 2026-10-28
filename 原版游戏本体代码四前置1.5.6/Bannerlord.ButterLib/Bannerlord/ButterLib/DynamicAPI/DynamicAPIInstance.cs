using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib.BUTR.Extensions;

namespace Bannerlord.ButterLib.DynamicAPI;

public class DynamicAPIInstance
{
	private readonly object _instance;

	internal DynamicAPIInstance(object instance)
	{
		_instance = instance ?? throw new ArgumentNullException("instance");
	}

	public TDelegate? RequestMethod<TDelegate>(string? method) where TDelegate : Delegate
	{
		if (DynamicAPIProvider.APIClassMethods == null)
		{
			return null;
		}
		if (method == null)
		{
			return null;
		}
		if (!DynamicAPIProvider.APIClassMethods.TryGetValue(_instance.GetType(), out Dictionary<string, MethodInfo> value))
		{
			return null;
		}
		if (!value.TryGetValue("0instance_" + method, out var value2))
		{
			return null;
		}
		return HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<TDelegate>(_instance, value2);
	}
}

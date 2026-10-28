using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib;

internal static class PatchArgumentExtensions
{
	private static IEnumerable<HarmonyArgument> AllHarmonyArguments(object[] attributes)
	{
		return attributes.Select((object attr) => (attr.GetType().Name != "HarmonyArgument") ? null : AccessTools.MakeDeepCopy<HarmonyArgument>(attr)).OfType<HarmonyArgument>();
	}

	internal static HarmonyArgument GetArgumentAttribute(this ParameterInfo parameter)
	{
		try
		{
			object[] customAttributes = parameter.GetCustomAttributes(inherit: true);
			return AllHarmonyArguments(customAttributes).FirstOrDefault();
		}
		catch (NotSupportedException)
		{
			return null;
		}
	}

	internal static IEnumerable<HarmonyArgument> GetArgumentAttributes(this MethodInfo method)
	{
		try
		{
			object[] customAttributes = method.GetCustomAttributes(inherit: true);
			return AllHarmonyArguments(customAttributes);
		}
		catch (NotSupportedException)
		{
			return Array.Empty<HarmonyArgument>();
		}
	}

	internal static IEnumerable<HarmonyArgument> GetArgumentAttributes(this Type type)
	{
		try
		{
			object[] customAttributes = type.GetCustomAttributes(inherit: true);
			return AllHarmonyArguments(customAttributes);
		}
		catch (NotSupportedException)
		{
			return Array.Empty<HarmonyArgument>();
		}
	}

	internal static string GetRealName(this IEnumerable<HarmonyArgument> attributes, string name, string[] originalParameterNames)
	{
		HarmonyArgument harmonyArgument = attributes.FirstOrDefault((HarmonyArgument p) => p.OriginalName == name);
		if (harmonyArgument == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(harmonyArgument.NewName))
		{
			return harmonyArgument.NewName;
		}
		if (originalParameterNames != null && harmonyArgument.Index >= 0 && harmonyArgument.Index < originalParameterNames.Length)
		{
			return originalParameterNames[harmonyArgument.Index];
		}
		return null;
	}

	private static string GetRealParameterName(this MethodInfo method, string[] originalParameterNames, string name)
	{
		if ((object)method == null || method is DynamicMethod)
		{
			return name;
		}
		string realName = method.GetArgumentAttributes().GetRealName(name, originalParameterNames);
		if (realName != null)
		{
			return realName;
		}
		Type declaringType = method.DeclaringType;
		if ((object)declaringType != null)
		{
			realName = declaringType.GetArgumentAttributes().GetRealName(name, originalParameterNames);
			if (realName != null)
			{
				return realName;
			}
		}
		return name;
	}

	private static string GetRealParameterName(this ParameterInfo parameter, string[] originalParameterNames)
	{
		HarmonyArgument argumentAttribute = parameter.GetArgumentAttribute();
		if (argumentAttribute == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(argumentAttribute.OriginalName))
		{
			return argumentAttribute.OriginalName;
		}
		if (argumentAttribute.Index >= 0 && argumentAttribute.Index < originalParameterNames.Length)
		{
			return originalParameterNames[argumentAttribute.Index];
		}
		return null;
	}

	internal static int GetArgumentIndex(this MethodInfo patch, string[] originalParameterNames, ParameterInfo patchParam)
	{
		if (patch is DynamicMethod)
		{
			return Array.IndexOf(originalParameterNames, patchParam.Name);
		}
		string realParameterName = patchParam.GetRealParameterName(originalParameterNames);
		if (realParameterName != null)
		{
			return Array.IndexOf(originalParameterNames, realParameterName);
		}
		realParameterName = patch.GetRealParameterName(originalParameterNames, patchParam.Name);
		if (realParameterName != null)
		{
			return Array.IndexOf(originalParameterNames, realParameterName);
		}
		return -1;
	}
}

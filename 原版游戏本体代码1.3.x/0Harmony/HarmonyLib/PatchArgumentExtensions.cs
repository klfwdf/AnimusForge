using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HarmonyLib
{
	// Token: 0x02000047 RID: 71
	internal static class PatchArgumentExtensions
	{
		// Token: 0x06000177 RID: 375 RVA: 0x0000BA39 File Offset: 0x00009C39
		private static IEnumerable<HarmonyArgument> AllHarmonyArguments(object[] attributes)
		{
			return attributes.Select(delegate(object attr)
			{
				if (attr.GetType().Name != "HarmonyArgument")
				{
					return null;
				}
				return AccessTools.MakeDeepCopy<HarmonyArgument>(attr);
			}).OfType<HarmonyArgument>();
		}

		// Token: 0x06000178 RID: 376 RVA: 0x0000BA68 File Offset: 0x00009C68
		internal static HarmonyArgument GetArgumentAttribute(this ParameterInfo parameter)
		{
			HarmonyArgument result;
			try
			{
				object[] attributes = parameter.GetCustomAttributes(true);
				result = PatchArgumentExtensions.AllHarmonyArguments(attributes).FirstOrDefault<HarmonyArgument>();
			}
			catch (NotSupportedException)
			{
				result = null;
			}
			return result;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000BAA4 File Offset: 0x00009CA4
		internal static IEnumerable<HarmonyArgument> GetArgumentAttributes(this MethodInfo method)
		{
			IEnumerable<HarmonyArgument> result;
			try
			{
				object[] attributes = method.GetCustomAttributes(true);
				result = PatchArgumentExtensions.AllHarmonyArguments(attributes);
			}
			catch (NotSupportedException)
			{
				result = Array.Empty<HarmonyArgument>();
			}
			return result;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000BADC File Offset: 0x00009CDC
		internal static IEnumerable<HarmonyArgument> GetArgumentAttributes(this Type type)
		{
			IEnumerable<HarmonyArgument> result;
			try
			{
				object[] attributes = type.GetCustomAttributes(true);
				result = PatchArgumentExtensions.AllHarmonyArguments(attributes);
			}
			catch (NotSupportedException)
			{
				result = Array.Empty<HarmonyArgument>();
			}
			return result;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000BB14 File Offset: 0x00009D14
		internal static string GetRealName(this IEnumerable<HarmonyArgument> attributes, string name, string[] originalParameterNames)
		{
			HarmonyArgument attribute = attributes.FirstOrDefault((HarmonyArgument p) => p.OriginalName == name);
			if (attribute == null)
			{
				return null;
			}
			if (!string.IsNullOrEmpty(attribute.NewName))
			{
				return attribute.NewName;
			}
			if (originalParameterNames != null && attribute.Index >= 0 && attribute.Index < originalParameterNames.Length)
			{
				return originalParameterNames[attribute.Index];
			}
			return null;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000BB7C File Offset: 0x00009D7C
		private static string GetRealParameterName(this MethodInfo method, string[] originalParameterNames, string name)
		{
			if (method == null || method is DynamicMethod)
			{
				return name;
			}
			string argumentName = method.GetArgumentAttributes().GetRealName(name, originalParameterNames);
			if (argumentName != null)
			{
				return argumentName;
			}
			Type type = method.DeclaringType;
			if (type != null)
			{
				argumentName = type.GetArgumentAttributes().GetRealName(name, originalParameterNames);
				if (argumentName != null)
				{
					return argumentName;
				}
			}
			return name;
		}

		// Token: 0x0600017D RID: 381 RVA: 0x0000BBC8 File Offset: 0x00009DC8
		private static string GetRealParameterName(this ParameterInfo parameter, string[] originalParameterNames)
		{
			HarmonyArgument attribute = parameter.GetArgumentAttribute();
			if (attribute == null)
			{
				return null;
			}
			if (!string.IsNullOrEmpty(attribute.OriginalName))
			{
				return attribute.OriginalName;
			}
			if (attribute.Index >= 0 && attribute.Index < originalParameterNames.Length)
			{
				return originalParameterNames[attribute.Index];
			}
			return null;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000BC14 File Offset: 0x00009E14
		internal static int GetArgumentIndex(this MethodInfo patch, string[] originalParameterNames, ParameterInfo patchParam)
		{
			if (patch is DynamicMethod)
			{
				return Array.IndexOf<string>(originalParameterNames, patchParam.Name);
			}
			string originalName = patchParam.GetRealParameterName(originalParameterNames);
			if (originalName != null)
			{
				return Array.IndexOf<string>(originalParameterNames, originalName);
			}
			originalName = patch.GetRealParameterName(originalParameterNames, patchParam.Name);
			if (originalName != null)
			{
				return Array.IndexOf<string>(originalParameterNames, originalName);
			}
			return -1;
		}
	}
}

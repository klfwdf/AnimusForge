using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HarmonyLib;

internal class AttributePatch
{
	private static readonly HarmonyPatchType[] allPatchTypes = new HarmonyPatchType[7]
	{
		HarmonyPatchType.Prefix,
		HarmonyPatchType.Postfix,
		HarmonyPatchType.Transpiler,
		HarmonyPatchType.Finalizer,
		HarmonyPatchType.ReversePatch,
		HarmonyPatchType.InnerPrefix,
		HarmonyPatchType.InnerPostfix
	};

	internal HarmonyMethod info;

	internal HarmonyPatchType? type;

	internal static AttributePatch Create(MethodInfo patch)
	{
		if ((object)patch == null)
		{
			throw new NullReferenceException("Patch method cannot be null");
		}
		object[] customAttributes = patch.GetCustomAttributes(inherit: true);
		string name = patch.Name;
		HarmonyPatchType? patchType = GetPatchType(name, customAttributes);
		if (!patchType.HasValue)
		{
			return null;
		}
		if (patchType != HarmonyPatchType.ReversePatch && !patch.IsStatic)
		{
			throw new ArgumentException("Patch method " + patch.FullDescription() + " must be static");
		}
		List<HarmonyMethod> attributes = customAttributes.Where((object attr) => attr.GetType().BaseType.FullName == PatchTools.harmonyAttributeFullName).Select(delegate(object attr)
		{
			FieldInfo fieldInfo = AccessTools.Field(attr.GetType(), "info");
			return fieldInfo.GetValue(attr);
		}).Select(AccessTools.MakeDeepCopy<HarmonyMethod>)
			.ToList();
		HarmonyMethod harmonyMethod = HarmonyMethod.Merge(attributes);
		harmonyMethod.method = patch;
		return new AttributePatch
		{
			info = harmonyMethod,
			type = patchType
		};
	}

	private static HarmonyPatchType? GetPatchType(string methodName, object[] allAttributes)
	{
		HashSet<string> hashSet = new HashSet<string>(from attr in allAttributes
			select attr.GetType().FullName into name
			where name.StartsWith("Harmony")
			select name);
		HarmonyPatchType? result = null;
		HarmonyPatchType[] array = allPatchTypes;
		for (int num = 0; num < array.Length; num++)
		{
			HarmonyPatchType value = array[num];
			string text = value.ToString();
			if (text == methodName || hashSet.Contains("HarmonyLib.Harmony" + text))
			{
				result = value;
				break;
			}
		}
		return result;
	}
}

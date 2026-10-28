using System;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
public class HarmonyReversePatch : HarmonyAttribute
{
	public HarmonyReversePatch(HarmonyReversePatchType type = HarmonyReversePatchType.Original)
	{
		info.reversePatchType = type;
	}
}

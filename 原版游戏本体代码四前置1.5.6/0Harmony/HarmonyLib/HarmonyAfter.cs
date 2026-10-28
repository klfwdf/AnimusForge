using System;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class HarmonyAfter : HarmonyAttribute
{
	public HarmonyAfter(params string[] after)
	{
		info.after = after;
	}
}

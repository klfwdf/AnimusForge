using System;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class HarmonyPriority : HarmonyAttribute
{
	public HarmonyPriority(int priority)
	{
		info.priority = priority;
	}
}

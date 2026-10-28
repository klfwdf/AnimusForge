using System;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class HarmonyDebug : HarmonyAttribute
{
	public HarmonyDebug()
	{
		info.debug = true;
	}
}

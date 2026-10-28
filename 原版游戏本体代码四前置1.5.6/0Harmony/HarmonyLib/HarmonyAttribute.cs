using System;

namespace HarmonyLib;

public class HarmonyAttribute : Attribute
{
	public HarmonyMethod info = new HarmonyMethod();
}

using System;

namespace HarmonyLib;

[AttributeUsage(AttributeTargets.Method)]
public class HarmonyCleanup : Attribute
{
}

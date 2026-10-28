#define TRACE
using System;
using System.Diagnostics;
using System.Reflection;

namespace HarmonyLib.BUTR.Extensions;

internal static class HarmonyExtensions
{
	public static bool TryPatch(this Harmony harmony, MethodBase? original, MethodInfo? prefix = null, MethodInfo? postfix = null, MethodInfo? transpiler = null, MethodInfo? finalizer = null)
	{
		if ((object)original == null || ((object)prefix == null && (object)postfix == null && (object)transpiler == null && (object)finalizer == null))
		{
			Trace.TraceError("HarmonyExtensions.TryPatch: 'original' or all methods are null");
			return false;
		}
		HarmonyMethod prefix2 = (((object)prefix == null) ? null : new HarmonyMethod(prefix));
		HarmonyMethod postfix2 = (((object)postfix == null) ? null : new HarmonyMethod(postfix));
		HarmonyMethod transpiler2 = (((object)transpiler == null) ? null : new HarmonyMethod(transpiler));
		HarmonyMethod finalizer2 = (((object)finalizer == null) ? null : new HarmonyMethod(finalizer));
		try
		{
			harmony.Patch(original, prefix2, postfix2, transpiler2, finalizer2);
		}
		catch (Exception value)
		{
			Trace.TraceError($"HarmonyExtensions.TryPatch: Exception occurred: {value}, original '{original}'");
			return false;
		}
		return true;
	}

	public static ReversePatcher? TryCreateReversePatcher(this Harmony harmony, MethodBase? original, MethodInfo? standin)
	{
		if ((object)original == null || (object)standin == null)
		{
			Trace.TraceError("HarmonyExtensions.TryCreateReversePatcher: 'original' or 'standin' is null");
			return null;
		}
		try
		{
			return harmony.CreateReversePatcher(original, new HarmonyMethod(standin));
		}
		catch (Exception value)
		{
			Trace.TraceError($"HarmonyExtensions.TryCreateReversePatcher: Exception occurred: {value}, original '{original}'");
			return null;
		}
	}

	public static bool TryCreateReversePatcher(this Harmony harmony, MethodBase? original, MethodInfo? standin, out ReversePatcher? result)
	{
		if ((object)original == null || (object)standin == null)
		{
			Trace.TraceError("HarmonyExtensions.TryCreateReversePatcher: 'original' or 'standin' is null");
			result = null;
			return false;
		}
		try
		{
			result = harmony.CreateReversePatcher(original, new HarmonyMethod(standin));
			return true;
		}
		catch (Exception value)
		{
			Trace.TraceError($"HarmonyExtensions.TryCreateReversePatcher: Exception occurred: {value}, original '{original}'");
			result = null;
			return false;
		}
	}
}

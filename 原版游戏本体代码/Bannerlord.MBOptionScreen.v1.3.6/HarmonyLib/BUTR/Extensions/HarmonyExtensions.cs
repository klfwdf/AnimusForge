using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000069 RID: 105
	[NullableContext(2)]
	[Nullable(0)]
	internal static class HarmonyExtensions
	{
		// Token: 0x06000432 RID: 1074 RVA: 0x000102D8 File Offset: 0x0000E4D8
		public static bool TryPatch([Nullable(1)] this Harmony harmony, MethodBase original, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null, MethodInfo finalizer = null)
		{
			if (original == null || (prefix == null && postfix == null && transpiler == null && finalizer == null))
			{
				Trace.TraceError("HarmonyExtensions.TryPatch: 'original' or all methods are null");
				return false;
			}
			HarmonyMethod prefixMethod = ((prefix == null) ? null : new HarmonyMethod(prefix));
			HarmonyMethod postfixMethod = ((postfix == null) ? null : new HarmonyMethod(postfix));
			HarmonyMethod transpilerMethod = ((transpiler == null) ? null : new HarmonyMethod(transpiler));
			HarmonyMethod finalizerMethod = ((finalizer == null) ? null : new HarmonyMethod(finalizer));
			try
			{
				harmony.Patch(original, prefixMethod, postfixMethod, transpilerMethod, finalizerMethod);
			}
			catch (Exception e)
			{
				Trace.TraceError(string.Format("HarmonyExtensions.TryPatch: Exception occurred: {0}, original '{1}'", e, original));
				return false;
			}
			return true;
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x00010378 File Offset: 0x0000E578
		public static ReversePatcher TryCreateReversePatcher([Nullable(1)] this Harmony harmony, MethodBase original, MethodInfo standin)
		{
			if (original == null || standin == null)
			{
				Trace.TraceError("HarmonyExtensions.TryCreateReversePatcher: 'original' or 'standin' is null");
				return null;
			}
			ReversePatcher result;
			try
			{
				result = harmony.CreateReversePatcher(original, new HarmonyMethod(standin));
			}
			catch (Exception e)
			{
				Trace.TraceError(string.Format("HarmonyExtensions.TryCreateReversePatcher: Exception occurred: {0}, original '{1}'", e, original));
				result = null;
			}
			return result;
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x000103D0 File Offset: 0x0000E5D0
		public static bool TryCreateReversePatcher([Nullable(1)] this Harmony harmony, MethodBase original, MethodInfo standin, out ReversePatcher result)
		{
			if (original == null || standin == null)
			{
				Trace.TraceError("HarmonyExtensions.TryCreateReversePatcher: 'original' or 'standin' is null");
				result = null;
				return false;
			}
			bool result2;
			try
			{
				result = harmony.CreateReversePatcher(original, new HarmonyMethod(standin));
				result2 = true;
			}
			catch (Exception e)
			{
				Trace.TraceError(string.Format("HarmonyExtensions.TryCreateReversePatcher: Exception occurred: {0}, original '{1}'", e, original));
				result = null;
				result2 = false;
			}
			return result2;
		}
	}
}

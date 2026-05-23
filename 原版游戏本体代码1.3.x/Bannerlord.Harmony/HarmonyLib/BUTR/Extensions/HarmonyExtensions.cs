using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x0200003F RID: 63
	[NullableContext(2)]
	[Nullable(0)]
	internal static class HarmonyExtensions
	{
		// Token: 0x060003A0 RID: 928 RVA: 0x0000E5E0 File Offset: 0x0000C7E0
		public static bool TryPatch([Nullable(1)] this Harmony harmony, MethodBase original, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null, MethodInfo finalizer = null)
		{
			bool flag = original == null || (prefix == null && postfix == null && transpiler == null && finalizer == null);
			bool result;
			if (flag)
			{
				Trace.TraceError("HarmonyExtensions.TryPatch: 'original' or all methods are null");
				result = false;
			}
			else
			{
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
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 2);
					defaultInterpolatedStringHandler.AppendLiteral("HarmonyExtensions.TryPatch: Exception occurred: ");
					defaultInterpolatedStringHandler.AppendFormatted<Exception>(e);
					defaultInterpolatedStringHandler.AppendLiteral(", original '");
					defaultInterpolatedStringHandler.AppendFormatted<MethodBase>(original);
					defaultInterpolatedStringHandler.AppendLiteral("'");
					Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
				result = true;
			}
			return result;
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x0000E6D8 File Offset: 0x0000C8D8
		public static ReversePatcher TryCreateReversePatcher([Nullable(1)] this Harmony harmony, MethodBase original, MethodInfo standin)
		{
			bool flag = original == null || standin == null;
			ReversePatcher result;
			if (flag)
			{
				Trace.TraceError("HarmonyExtensions.TryCreateReversePatcher: 'original' or 'standin' is null");
				result = null;
			}
			else
			{
				try
				{
					result = harmony.CreateReversePatcher(original, new HarmonyMethod(standin));
				}
				catch (Exception e)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(76, 2);
					defaultInterpolatedStringHandler.AppendLiteral("HarmonyExtensions.TryCreateReversePatcher: Exception occurred: ");
					defaultInterpolatedStringHandler.AppendFormatted<Exception>(e);
					defaultInterpolatedStringHandler.AppendLiteral(", original '");
					defaultInterpolatedStringHandler.AppendFormatted<MethodBase>(original);
					defaultInterpolatedStringHandler.AppendLiteral("'");
					Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					result = null;
				}
			}
			return result;
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x0000E780 File Offset: 0x0000C980
		public static bool TryCreateReversePatcher([Nullable(1)] this Harmony harmony, MethodBase original, MethodInfo standin, out ReversePatcher result)
		{
			bool flag = original == null || standin == null;
			bool result2;
			if (flag)
			{
				Trace.TraceError("HarmonyExtensions.TryCreateReversePatcher: 'original' or 'standin' is null");
				result = null;
				result2 = false;
			}
			else
			{
				try
				{
					result = harmony.CreateReversePatcher(original, new HarmonyMethod(standin));
					result2 = true;
				}
				catch (Exception e)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(76, 2);
					defaultInterpolatedStringHandler.AppendLiteral("HarmonyExtensions.TryCreateReversePatcher: Exception occurred: ");
					defaultInterpolatedStringHandler.AppendFormatted<Exception>(e);
					defaultInterpolatedStringHandler.AppendLiteral(", original '");
					defaultInterpolatedStringHandler.AppendFormatted<MethodBase>(original);
					defaultInterpolatedStringHandler.AppendLiteral("'");
					Trace.TraceError(defaultInterpolatedStringHandler.ToStringAndClear());
					result = null;
					result2 = false;
				}
			}
			return result2;
		}
	}
}

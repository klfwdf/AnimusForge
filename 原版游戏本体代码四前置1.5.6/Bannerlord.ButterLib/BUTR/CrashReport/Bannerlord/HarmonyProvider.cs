#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BUTR.CrashReport.Interfaces;
using BUTR.CrashReport.Models;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

namespace BUTR.CrashReport.Bannerlord;

public class HarmonyProvider : IHarmonyProvider
{
	private static class MonoModUtils
	{
		private delegate object GetCurrentRuntimeDelegate();

		private delegate MethodBase GetIdentifiableOldDelegate(object instance, MethodBase method);

		private delegate IntPtr GetNativeStartDelegate(object instance, MethodBase method);

		private delegate object GetCurrentPlatformTripleDelegate();

		private delegate MethodBase GetIdentifiableDelegate(object instance, MethodBase method);

		private delegate IntPtr GetNativeMethodBodyDelegate(object instance, MethodBase method);

		private static readonly GetCurrentRuntimeDelegate? CurrentRuntimeMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertyGetterDelegate<GetCurrentRuntimeDelegate>("MonoMod.RuntimeDetour.DetourHelper:Runtime", logErrorInTrace: false);

		private static readonly GetIdentifiableOldDelegate? GetIdentifiableOldMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetIdentifiableOldDelegate>("MonoMod.RuntimeDetour.IDetourRuntimePlatform:GetIdentifiable", null, null, logErrorInTrace: false);

		private static readonly GetNativeStartDelegate? GetNativeStartMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetNativeStartDelegate>("MonoMod.RuntimeDetour.IDetourRuntimePlatform:GetNativeStart", null, null, logErrorInTrace: false);

		private static readonly GetCurrentPlatformTripleDelegate? CurrentPlatformTripleMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertyGetterDelegate<GetCurrentPlatformTripleDelegate>("MonoMod.Core.Platforms.PlatformTriple:Current", logErrorInTrace: false);

		private static readonly GetIdentifiableDelegate? GetIdentifiableMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetIdentifiableDelegate>("MonoMod.Core.Platforms.PlatformTriple:GetIdentifiable", null, null, logErrorInTrace: false);

		private static readonly GetNativeMethodBodyDelegate? GetNativeMethodBodyMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<GetNativeMethodBodyDelegate>("MonoMod.Core.Platforms.PlatformTriple:GetNativeMethodBody", null, null, logErrorInTrace: false);

		public static MethodBase? GetIdentifiable(MethodBase method)
		{
			try
			{
				object obj = CurrentRuntimeMethod?.Invoke();
				if (obj != null)
				{
					return GetIdentifiableOldMethod?.Invoke(obj, method);
				}
				object obj2 = CurrentPlatformTripleMethod?.Invoke();
				if (obj2 != null)
				{
					return GetIdentifiableMethod?.Invoke(obj2, method);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
			return null;
		}

		public static IntPtr GetNativeMethodBody(MethodBase method)
		{
			try
			{
				object obj = CurrentRuntimeMethod?.Invoke();
				if (obj != null)
				{
					return GetNativeStartMethod?.Invoke(obj, method) ?? IntPtr.Zero;
				}
				object obj2 = CurrentPlatformTripleMethod?.Invoke();
				if (obj2 != null)
				{
					return GetNativeMethodBodyMethod?.Invoke(obj2, method) ?? IntPtr.Zero;
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
			return IntPtr.Zero;
		}
	}

	public virtual IEnumerable<MethodBase> GetAllPatchedMethods()
	{
		return Harmony.GetAllPatchedMethods();
	}

	public virtual HarmonyPatches? GetPatchInfo(MethodBase originalMethod)
	{
		Patches patchInfo = Harmony.GetPatchInfo(originalMethod);
		if (patchInfo == null)
		{
			return null;
		}
		return new HarmonyPatches
		{
			Prefixes = patchInfo.Prefixes.Select((Patch x) => Convert(x, BUTR.CrashReport.Models.HarmonyPatchType.Prefix)).ToArray(),
			Postfixes = patchInfo.Postfixes.Select((Patch x) => Convert(x, BUTR.CrashReport.Models.HarmonyPatchType.Postfix)).ToArray(),
			Finalizers = patchInfo.Finalizers.Select((Patch x) => Convert(x, BUTR.CrashReport.Models.HarmonyPatchType.Finalizer)).ToArray(),
			Transpilers = patchInfo.Transpilers.Select((Patch x) => Convert(x, BUTR.CrashReport.Models.HarmonyPatchType.Transpiler)).ToArray()
		};
		static BUTR.CrashReport.Models.HarmonyPatch Convert(Patch patch, BUTR.CrashReport.Models.HarmonyPatchType type)
		{
			return new BUTR.CrashReport.Models.HarmonyPatch
			{
				Owner = patch.owner,
				Index = patch.index,
				Priority = patch.priority,
				Before = patch.before,
				After = patch.after,
				PatchMethod = patch.PatchMethod,
				Type = type
			};
		}
	}

	public virtual MethodBase? GetOriginalMethod(MethodInfo replacement)
	{
		try
		{
			return Harmony.GetOriginalMethod(replacement);
		}
		catch (Exception ex)
		{
			Trace.TraceError(ex.ToString());
			return null;
		}
	}

	public virtual MethodBase? GetMethodFromStackframe(StackFrame frame)
	{
		try
		{
			return Harmony.GetMethodFromStackframe(frame);
		}
		catch (Exception ex)
		{
			Trace.TraceError(ex.ToString());
			return null;
		}
	}

	public MethodBase? GetIdentifiable(MethodBase method)
	{
		return MonoModUtils.GetIdentifiable(method);
	}

	public IntPtr GetNativeMethodBody(MethodBase method)
	{
		return MonoModUtils.GetNativeMethodBody(method);
	}
}

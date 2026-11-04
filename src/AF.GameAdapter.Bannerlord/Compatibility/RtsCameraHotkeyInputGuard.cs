using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace AnimusForge;

// RTS Camera / Command System read their configurable hotkeys (L menu, F10 camera, ...) through
// MissionSharedLibrary.Config.HotKey.GameKeySequenceAlternative. With a null IInputContext those
// helpers poll the global Input, bypassing the InputRestrictions of AF text popups, so typing "l"
// in the scene shout box opened the RTS menu. Each RTSCamera DLL embeds its own copy of the type.
// Only the three per-key primitives are patched; every public/InOrder entry point funnels into them.
// Cost per RTS key poll: one static popup-state read. Installed once at startup; absent RTS = no-op.
internal static class RtsCameraHotkeyInputGuard
{
	private const string KeySequenceTypeName = "MissionSharedLibrary.Config.HotKey.GameKeySequenceAlternative";

	private static readonly string[] PrimitiveNames = { "IsKeyDown", "IsKeyPressed", "IsKeyReleased" };

	private static readonly HashSet<MethodBase> Patched = new HashSet<MethodBase>();

	private static bool _subscribed;

	internal static void EnsurePatched(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			PatchAssembly(harmony, assembly);
		}
		if (!_subscribed)
		{
			// RTS may load after AF in some module orders; patch its assembly when it arrives.
			_subscribed = true;
			AppDomain.CurrentDomain.AssemblyLoad += (_, args) => PatchAssembly(harmony, args.LoadedAssembly);
		}
	}

	private static void PatchAssembly(Harmony harmony, Assembly assembly)
	{
		try
		{
			string name = assembly?.GetName()?.Name ?? "";
			if (name.IndexOf("RTSCamera", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return;
			}
			Type type = assembly.GetType(KeySequenceTypeName, false);
			if (type == null)
			{
				return;
			}
			MethodInfo prefix = AccessTools.Method(typeof(RtsCameraHotkeyInputGuard), nameof(BlockWhileAfTextInputFocusedPrefix));
			int count = 0;
			foreach (string primitive in PrimitiveNames)
			{
				MethodInfo target = AccessTools.Method(type, primitive, new[] { AccessTools.TypeByName("TaleWorlds.InputSystem.IInputContext"), typeof(int) });
				if (target == null || target.ReturnType != typeof(bool))
				{
					Logger.Log("RTSCameraCompat", "Hotkey guard skipped; signature changed: " + name + " " + primitive);
					continue;
				}
				lock (Patched)
				{
					if (!Patched.Add(target))
					{
						continue;
					}
				}
				harmony.Patch(target, prefix: new HarmonyMethod(prefix));
				count++;
			}
			if (count > 0)
			{
				Logger.Log("RTSCameraCompat", "Hotkey guard installed for AF text input. assembly=" + name + ", primitives=" + count);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("RTSCameraCompat", "Hotkey guard install failed: " + ex.Message);
		}
	}

	private static bool BlockWhileAfTextInputFocusedPrefix(ref bool __result)
	{
		if (!IsAfTextInputOpen())
		{
			return true;
		}
		__result = false;
		return false;
	}

	// Plain static flags only: this runs for every RTS key poll, so no layer/screen scans here.
	private static bool IsAfTextInputOpen()
	{
		return ShoutTextInputPopup.IsOpen
			|| CourierLetterInputPopup.IsOpen
			|| DevHistoryEditPopup.IsOpen
			|| PlayerRpForgePopup.IsOpen
			|| CustomPolicyComposePopup.IsOpen
			|| LocalPolicyComposePopup.IsOpen
			|| AnimusForgeApiOnboardingPopup.IsOpen;
	}
}

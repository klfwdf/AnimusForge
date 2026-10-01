using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// The castle aftermath reuses AF's inspection battle host for deployment and
/// command UI, but it is not a battle. The native battle music view expects a
/// complete battle spawn logic and throws every frame once deployment ends.
/// </summary>
[HarmonyPatch]
internal static class CastleAftermathBattleMusicUpdatePatch
{
	private const string BattleMusicViewTypeName = "TaleWorlds.MountAndBlade.View.MissionViews.Sound.MusicBattleMissionView";

	private static int _lastLoggedMissionId;

	private static MethodBase TargetMethod()
	{
		Type viewType = AccessTools.TypeByName(BattleMusicViewTypeName);
		if (viewType == null)
		{
			return null;
		}

		foreach (MethodInfo method in viewType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			ParameterInfo[] parameters = method.GetParameters();
			if (method.Name.EndsWith(".OnUpdated", StringComparison.Ordinal)
				&& parameters.Length == 1
				&& parameters[0].ParameterType == typeof(float))
			{
				return method;
			}
		}

		return AccessTools.Method(viewType, "OnUpdated", new[] { typeof(float) });
	}

	[HarmonyPrefix]
	private static bool Prefix()
	{
		try
		{
			Mission mission = Mission.Current;
			if (!CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
			{
				return true;
			}

			int missionId = RuntimeHelpers.GetHashCode(mission);
			if (_lastLoggedMissionId != missionId)
			{
				_lastLoggedMissionId = missionId;
				Logger.Log("CastleAftermath", "Skipped native battle music updates for non-battle castle aftermath mission.");
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}

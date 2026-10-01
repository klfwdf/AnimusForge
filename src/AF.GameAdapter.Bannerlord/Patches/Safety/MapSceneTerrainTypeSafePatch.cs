using System;
using HarmonyLib;
using SandBox;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class MapSceneTerrainTypeSafePatch
{
	private static bool _patched;
	private static int _skippedInvalidFaces;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		try
		{
			var target = AccessTools.Method(typeof(MapScene), nameof(MapScene.GetFaceTerrainType), new[] { typeof(PathFaceRecord) });
			if (target == null)
			{
				Logger.Log("MapSceneSafety", "MapScene.GetFaceTerrainType not found; invalid face guard skipped.");
				return;
			}
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(MapSceneTerrainTypeSafePatch), nameof(GetFaceTerrainTypePrefix)));
			Logger.Log("MapSceneSafety", "MapScene.GetFaceTerrainType invalid face guard applied.");
		}
		catch (Exception ex)
		{
			Logger.Log("MapSceneSafety", "Failed to apply terrain type guard: " + ex.Message);
		}
	}

	public static bool GetFaceTerrainTypePrefix(PathFaceRecord navMeshFace, ref TerrainType __result)
	{
		if (navMeshFace.IsValid())
		{
			return true;
		}
		__result = TerrainType.Plain;
		LogSkippedInvalidFace();
		return false;
	}

	private static void LogSkippedInvalidFace()
	{
		_skippedInvalidFaces++;
		if (_skippedInvalidFaces > 3)
		{
			return;
		}
		Logger.Log("MapSceneSafety", "Skipped invalid nav mesh face terrain lookup; using Plain. count=" + _skippedInvalidFaces);
	}
}

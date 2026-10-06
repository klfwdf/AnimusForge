using System;
using System.Collections.Generic;
using NavalDLC.Missions.NavalPhysics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

namespace NavalDLC.View.MissionViews;

public class NavalMissionDeploymentBoundaryMarker : MissionDeploymentBoundaryMarker
{
	private readonly string _largePrefabName;

	private GameEntity _cachedLargeEntity;

	public NavalMissionDeploymentBoundaryMarker(string smallPrefabName, string largePrefabName, float markerInterval = 20f)
		: base(smallPrefabName, markerInterval)
	{
		_largePrefabName = largePrefabName;
	}

	protected override void MarkLine(Vec3 startPoint, Vec3 endPoint, List<GameEntity> boundary, Banner banner = null)
	{
		Vec3 vec = endPoint - startPoint;
		float length = vec.Length;
		Vec3 vec2 = vec;
		vec2.Normalize();
		vec2 *= MarkerInterval;
		for (float num = 0f; num < length; num += MarkerInterval)
		{
			GameEntity gameEntity = CreateBoundaryEntity((int)(num / MarkerInterval) % 4 == 0);
			NavalPhysics firstScriptOfType = gameEntity.GetFirstScriptOfType<NavalPhysics>();
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation.RotateAboutUp(vec.RotationZ + System.MathF.PI);
			frame.origin = startPoint;
			frame.origin.z = gameEntity.GetWaterLevelAtPosition(frame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) - (firstScriptOfType?.StabilitySubmergedHeightOfShip ?? 0f);
			gameEntity.SetFrame(ref frame);
			firstScriptOfType?.SetAnchor(isAnchored: true, anchorInPlace: true);
			boundary.Add(gameEntity);
			startPoint += vec2;
		}
	}

	private GameEntity CreateBoundaryEntity(bool isLarge)
	{
		Scene scene = Mission.Current.Scene;
		if (isLarge && _cachedLargeEntity == null)
		{
			_cachedLargeEntity = GameEntity.Instantiate(null, _largePrefabName, callScriptCallbacks: false);
		}
		else if (!isLarge && _cachedEntity == null)
		{
			_cachedEntity = GameEntity.Instantiate(null, _prefabName, callScriptCallbacks: false);
		}
		GameEntity gameEntity = GameEntity.CopyFrom(scene, isLarge ? _cachedLargeEntity : _cachedEntity);
		gameEntity.SetMobility(GameEntity.Mobility.Dynamic);
		return gameEntity;
	}
}

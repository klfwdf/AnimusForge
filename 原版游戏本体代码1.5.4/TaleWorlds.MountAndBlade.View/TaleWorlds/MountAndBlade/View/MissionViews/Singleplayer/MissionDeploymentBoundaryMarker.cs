using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class MissionDeploymentBoundaryMarker : MissionView
{
	public const string AttackerStaticDeploymentBoundaryName = "walk_area";

	public const string DefenderStaticDeploymentBoundaryName = "deployment_castle_boundary";

	public readonly float MarkerInterval;

	protected readonly Dictionary<string, List<GameEntity>>[] _boundaryMarkersPerSide = new Dictionary<string, List<GameEntity>>[2];

	protected readonly string _prefabName;

	protected GameEntity _cachedEntity;

	protected bool _boundaryMarkersRemoved = true;

	public MissionDeploymentBoundaryMarker(string prefabName, float markerInterval = 2f)
	{
		_prefabName = prefabName;
		MarkerInterval = Math.Max(markerInterval, 0.0001f);
	}

	public override void AfterStart()
	{
		base.AfterStart();
		for (int i = 0; i < 2; i++)
		{
			_boundaryMarkersPerSide[i] = new Dictionary<string, List<GameEntity>>();
		}
		_boundaryMarkersRemoved = false;
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		TryRemoveBoundaryMarkers();
	}

	public override void OnDeploymentPlanMade(Team team, bool isFirstPlan)
	{
		if (!team.IsPlayerTeam && team != base.Mission.PlayerEnemyTeam)
		{
			return;
		}
		bool flag = base.Mission.DeploymentPlan.HasDeploymentBoundaries(team);
		if (!(isFirstPlan && flag))
		{
			return;
		}
		foreach (var deploymentBoundary in base.Mission.DeploymentPlan.GetDeploymentBoundaries(team))
		{
			AddBoundaryMarkerForSide(team.Side, new KeyValuePair<string, ICollection<Vec2>>(deploymentBoundary.id, deploymentBoundary.points));
		}
	}

	public override void OnRemoveBehavior()
	{
		TryRemoveBoundaryMarkers();
		base.OnRemoveBehavior();
	}

	private void AddBoundaryMarkerForSide(BattleSideEnum side, KeyValuePair<string, ICollection<Vec2>> boundary)
	{
		string key = boundary.Key;
		if (!_boundaryMarkersPerSide[(int)side].ContainsKey(key))
		{
			Banner banner = side switch
			{
				BattleSideEnum.Defender => base.Mission.DefenderTeam.Banner, 
				BattleSideEnum.Attacker => base.Mission.AttackerTeam.Banner, 
				_ => null, 
			};
			List<GameEntity> list = new List<GameEntity>();
			List<Vec2> list2 = boundary.Value.ToList();
			for (int i = 0; i < list2.Count; i++)
			{
				MarkLine(new Vec3(list2[i]), new Vec3(list2[(i + 1) % list2.Count]), list, banner);
			}
			_boundaryMarkersPerSide[(int)side][key] = list;
		}
	}

	private void TryRemoveBoundaryMarkers()
	{
		if (_boundaryMarkersRemoved)
		{
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			foreach (string item in _boundaryMarkersPerSide[i].Keys.ToList())
			{
				RemoveBoundaryMarker(item, (BattleSideEnum)i);
			}
		}
		_boundaryMarkersRemoved = true;
	}

	private void RemoveBoundaryMarker(string boundaryName, BattleSideEnum side)
	{
		if (!_boundaryMarkersPerSide[(int)side].TryGetValue(boundaryName, out var value))
		{
			return;
		}
		foreach (GameEntity item in value)
		{
			item.Remove(103);
		}
		_boundaryMarkersPerSide[(int)side].Remove(boundaryName);
	}

	protected virtual void MarkLine(Vec3 startPoint, Vec3 endPoint, List<GameEntity> boundary, Banner banner = null)
	{
		Scene scene = base.Mission.Scene;
		Vec3 vec = endPoint - startPoint;
		float length = vec.Length;
		Vec3 vec2 = vec;
		vec2.Normalize();
		vec2 *= MarkerInterval;
		for (float num = 0f; num < length; num += MarkerInterval)
		{
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation.RotateAboutUp(vec.RotationZ + System.MathF.PI / 2f);
			frame.origin = startPoint;
			if (!scene.GetHeightAtPoint(frame.origin.AsVec2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref frame.origin.z))
			{
				frame.origin.z = 0f;
			}
			frame.origin.z -= 0.5f;
			frame.Scale(Vec3.One * 0.4f);
			GameEntity gameEntity = MakeEntity(banner);
			gameEntity.SetFrame(ref frame);
			boundary.Add(gameEntity);
			startPoint += vec2;
		}
	}

	private GameEntity MakeEntity(Banner banner = null)
	{
		Scene scene = base.Mission.Scene;
		if (_cachedEntity == null)
		{
			_cachedEntity = GameEntity.Instantiate(null, _prefabName, callScriptCallbacks: false);
		}
		GameEntity gameEntity = GameEntity.CopyFrom(scene, _cachedEntity);
		gameEntity.SetMobility(GameEntity.Mobility.Dynamic);
		if (banner != null)
		{
			Mesh firstMesh = gameEntity.GetFirstMesh();
			Material material = firstMesh.GetMaterial();
			Material tableauMaterial = material.CreateCopy();
			banner.GetTableauTextureSmall(BannerDebugInfo.CreateManual(GetType().Name), delegate(Texture tex)
			{
				tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, tex);
			});
			firstMesh.SetMaterial(tableauMaterial);
		}
		return gameEntity;
	}
}

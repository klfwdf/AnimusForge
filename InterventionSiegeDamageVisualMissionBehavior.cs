using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class InterventionSiegeDamageVisualMissionBehavior : MissionLogic
{
	private readonly float[] _wallHitPointPercentages;
	private readonly string _settlementId;
	private bool _applied;

	public InterventionSiegeDamageVisualMissionBehavior(float[] wallHitPointPercentages, string settlementId)
	{
		_wallHitPointPercentages = CloneWallHitPointPercentages(wallHitPointPercentages);
		_settlementId = string.IsNullOrWhiteSpace(settlementId) ? "N/A" : settlementId;
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public override void AfterStart()
	{
		base.AfterStart();
		if (_applied)
		{
			return;
		}
		_applied = TryApplyInterventionSiegeDamageVisuals();
	}

	internal static float[] CloneWallHitPointPercentages(float[] wallHitPointPercentages)
	{
		if (wallHitPointPercentages == null || wallHitPointPercentages.Length == 0)
		{
			return new float[0];
		}
		float[] clone = new float[wallHitPointPercentages.Length];
		for (int i = 0; i < wallHitPointPercentages.Length; i++)
		{
			clone[i] = MBMath.ClampFloat(wallHitPointPercentages[i], 0f, 1f);
		}
		return clone;
	}

	internal static string BuildWallHitPointSummary(float[] wallHitPointPercentages)
	{
		if (wallHitPointPercentages == null || wallHitPointPercentages.Length == 0)
		{
			return "";
		}
		string[] parts = new string[wallHitPointPercentages.Length];
		for (int i = 0; i < wallHitPointPercentages.Length; i++)
		{
			parts[i] = MBMath.ClampFloat(wallHitPointPercentages[i], 0f, 1f).ToString("0.00");
		}
		return string.Join(",", parts);
	}

	private bool TryApplyInterventionSiegeDamageVisuals()
	{
		if (base.Mission == null || _wallHitPointPercentages.Length == 0)
		{
			return true;
		}
		try
		{
			int damageDecalsShown = ApplyDamageDecals();
			int wallSegmentsTotal;
			int brokenWalls = ApplyBrokenWallSegments(out wallSegmentsTotal);
			Logger.Log("SiegeAiIntervention", "Applied visual-only siege damage state. Settlement=" + _settlementId + ", WallRatios=[" + BuildWallHitPointSummary(_wallHitPointPercentages) + "], BrokenWalls=" + brokenWalls + ", WallSegments=" + wallSegmentsTotal + ", DamageDecalsShown=" + damageDecalsShown + ", TownCenterMission=true, SiegeDeployment=false, PreDestroy=false");
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Apply visual-only siege damage state failed. Settlement=" + _settlementId + ", Error=" + ex.Message);
			return false;
		}
	}

	private int ApplyDamageDecals()
	{
		try
		{
			if (base.Mission?.Scene == null)
			{
				return 0;
			}
			float averageWallRatio = GetAverageWallHitPointRatio();
			float damageFraction = MBMath.Lerp(0f, 0.7f, 1f - averageWallRatio);
			if (damageFraction <= 0f)
			{
				return 0;
			}
			List<WeakGameEntity> decals = base.Mission.Scene.FindWeakEntitiesWithTag("damage_decal").ToList();
			if (decals.Count == 0)
			{
				return 0;
			}
			foreach (WeakGameEntity decal in decals)
			{
				SynchedMissionObject syncObject = decal.GetFirstScriptOfType<SynchedMissionObject>();
				if (syncObject != null)
				{
					syncObject.SetVisibleSynched(value: false);
				}
			}
			int targetCount = Math.Min(decals.Count, MathF.Floor((float)decals.Count * damageFraction));
			int shown = 0;
			for (int i = 0; i < targetCount; i++)
			{
				SynchedMissionObject syncObject = decals[i].GetFirstScriptOfType<SynchedMissionObject>();
				if (syncObject == null)
				{
					continue;
				}
				syncObject.SetVisibleSynched(value: true);
				shown++;
			}
			return shown;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Apply visual-only damage decals skipped. Settlement=" + _settlementId + ", Error=" + ex.Message);
			return 0;
		}
	}

	private int ApplyBrokenWallSegments(out int wallSegmentsTotal)
	{
		wallSegmentsTotal = 0;
		try
		{
			if (base.Mission?.ActiveMissionObjects == null)
			{
				return 0;
			}
			List<WallSegment> wallSegments = base.Mission.ActiveMissionObjects.FindAllWithType<WallSegment>()
				.Where(HasBrokenWallChild)
				.ToList();
			wallSegmentsTotal = wallSegments.Count;
			if (wallSegments.Count == 0)
			{
				return 0;
			}
			int brokenWalls = 0;
			foreach (float wallHitPointPercentage in _wallHitPointPercentages)
			{
				if (wallSegments.Count == 0)
				{
					break;
				}
				WallSegment wallSegment = ChooseWallSegment(wallSegments);
				if (wallSegment == null)
				{
					break;
				}
				bool isBroken = MathF.Abs(wallHitPointPercentage) < 1E-05f;
				if (TrySetWallSegmentBroken(wallSegment, isBroken) && isBroken)
				{
					brokenWalls++;
				}
				wallSegments.Remove(wallSegment);
			}
			foreach (WallSegment wallSegment in wallSegments)
			{
				TrySetWallSegmentBroken(wallSegment, false);
			}
			return brokenWalls;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Apply visual-only broken wall segments skipped. Settlement=" + _settlementId + ", Error=" + ex.Message);
			return 0;
		}
	}

	private static bool HasBrokenWallChild(WallSegment wallSegment)
	{
		try
		{
			return wallSegment != null
				&& wallSegment.DefenseSide != FormationAI.BehaviorSide.BehaviorSideNotSet
				&& wallSegment.GameEntity.GetChildren().Any((WeakGameEntity child) => child.HasTag("broken_child"));
		}
		catch
		{
			return false;
		}
	}

	private WallSegment ChooseWallSegment(List<WallSegment> wallSegments)
	{
		if (wallSegments == null || wallSegments.Count == 0)
		{
			return null;
		}
		if (wallSegments.Count == 1)
		{
			return wallSegments[0];
		}
		try
		{
			BatteringRam batteringRam = base.Mission?.ActiveMissionObjects?.FindAllWithType<BatteringRam>().FirstOrDefault();
			if (batteringRam != null && wallSegments.Count >= 2)
			{
				Vec3 leftVector = wallSegments[0].GameEntity.GlobalPosition - batteringRam.GameEntity.GlobalPosition;
				Vec3 rightVector = wallSegments[1].GameEntity.GlobalPosition - batteringRam.GameEntity.GlobalPosition;
				return Vec3.CrossProduct(leftVector, rightVector).z < 0f ? wallSegments[1] : wallSegments[0];
			}
		}
		catch
		{
		}
		return wallSegments.OrderBy((WallSegment wallSegment) => wallSegment.GameEntity.GlobalPosition.x).FirstOrDefault();
	}

	private bool TrySetWallSegmentBroken(WallSegment wallSegment, bool isBroken)
	{
		try
		{
			wallSegment.OnChooseUsedWallSegment(isBroken);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("SiegeAiIntervention", "Set visual-only wall segment state skipped. Settlement=" + _settlementId + ", Broken=" + isBroken + ", Error=" + ex.Message);
			return false;
		}
	}

	private float GetAverageWallHitPointRatio()
	{
		if (_wallHitPointPercentages.Length == 0)
		{
			return 1f;
		}
		float total = 0f;
		for (int i = 0; i < _wallHitPointPercentages.Length; i++)
		{
			total += MBMath.ClampFloat(_wallHitPointPercentages[i], 0f, 1f);
		}
		return MBMath.ClampFloat(total / _wallHitPointPercentages.Length, 0f, 1f);
	}
}

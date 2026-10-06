using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TeamAINavalRaidDefenderComponent : TeamAIComponent
{
	private bool _hasLandingStarted;

	private bool _hasAttackersBreachedDesignatedPoint;

	private MBList<VolumeBox> _volumeBoxes;

	public bool LandingCompleted { get; private set; }

	public TeamAINavalRaidDefenderComponent(Mission currentMission, Team currentTeam, float thinkTimerTime = 10f, float applyTimerTime = 1f)
		: base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
	{
		_volumeBoxes = new MBList<VolumeBox>();
		List<GameEntity> entities = new List<GameEntity>();
		currentMission.Scene.GetAllEntitiesWithScriptComponent<VolumeBox>(ref entities);
		foreach (GameEntity item in entities)
		{
			_volumeBoxes.Add(item.GetFirstScriptOfType<VolumeBox>());
		}
	}

	public override void TickOccasionally()
	{
		if (!_hasAttackersBreachedDesignatedPoint)
		{
			foreach (VolumeBox volumeBox in _volumeBoxes)
			{
				if (!volumeBox.HasAgentsInAttackerSide())
				{
					continue;
				}
				_hasAttackersBreachedDesignatedPoint = true;
				MBList<StrategicArea> mBList = new MBList<StrategicArea>();
				foreach (StrategicArea strategicArea in base.StrategicAreas)
				{
					if (strategicArea.GameEntity.HasTag("volume_box_archer_point"))
					{
						mBList.Add(strategicArea);
					}
				}
				foreach (StrategicArea item in mBList)
				{
					item.IsActive = false;
				}
				break;
			}
		}
		base.TickOccasionally();
	}

	public void OnLandingCompleted()
	{
		LandingCompleted = true;
	}

	public void OnShipLanded()
	{
		if (_hasLandingStarted)
		{
			return;
		}
		_hasLandingStarted = true;
		MBList<StrategicArea> mBList = new MBList<StrategicArea>();
		foreach (StrategicArea strategicArea in base.StrategicAreas)
		{
			if (strategicArea.GameEntity.HasTag("unsafe_archer_point"))
			{
				mBList.Add(strategicArea);
			}
		}
		foreach (StrategicArea item in mBList)
		{
			item.IsActive = false;
		}
		MBReadOnlyList<Agent> activeAgents = Mission.Current.DefenderTeam.ActiveAgents;
		if (activeAgents.Count > 0)
		{
			Agent agent = activeAgents[MBRandom.RandomInt(activeAgents.Count)];
			SoundManager.StartOneShotEvent("event:/alerts/nods/stop", agent.Position);
		}
	}

	public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
	{
		if (GameNetwork.IsServer)
		{
			formation.ForceCalculateCaches();
			if (formation.AI.GetBehavior<BehaviorCharge>() == null)
			{
				if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
				{
					formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
				}
				else if (formation.FormationIndex == FormationClass.Bodyguard)
				{
					formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
				}
				formation.AI.AddAiBehavior(new BehaviorCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
				formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
				formation.AI.AddAiBehavior(new BehaviorReserve(formation));
				formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
				formation.AI.AddAiBehavior(new BehaviorStop(formation));
				formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPInfantry(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPLastFlagLastStand(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPMounted(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPMountedRanged(formation));
				formation.AI.AddAiBehavior(new BehaviorSergeantMPRanged(formation));
			}
		}
		else
		{
			if (GameNetwork.IsClientOrReplay)
			{
				return;
			}
			formation.ForceCalculateCaches();
			if (formation.AI.GetBehavior<BehaviorCharge>() == null)
			{
				if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
				{
					formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
				}
				else if (formation.FormationIndex == FormationClass.Bodyguard)
				{
					formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
				}
				formation.AI.AddAiBehavior(new BehaviorCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
				formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
				formation.AI.AddAiBehavior(new BehaviorReserve(formation));
				formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
				formation.AI.AddAiBehavior(new BehaviorStop(formation));
				formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
				formation.AI.AddAiBehavior(new BehaviorAdvance(formation));
				formation.AI.AddAiBehavior(new BehaviorCautiousAdvance(formation));
				formation.AI.AddAiBehavior(new BehaviorCavalryScreen(formation));
				formation.AI.AddAiBehavior(new BehaviorDefensiveRing(formation));
				formation.AI.AddAiBehavior(new BehaviorFireFromInfantryCover(formation));
				formation.AI.AddAiBehavior(new BehaviorFlank(formation));
				formation.AI.AddAiBehavior(new BehaviorHoldHighGround(formation));
				formation.AI.AddAiBehavior(new BehaviorHorseArcherSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorMountedSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorProtectFlank(formation));
				formation.AI.AddAiBehavior(new BehaviorScreenedSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmishBehindFormation(formation));
				formation.AI.AddAiBehavior(new BehaviorSkirmishLine(formation));
				formation.AI.AddAiBehavior(new BehaviorVanguard(formation));
				formation.AI.AddAiBehavior(new BehaviorShootFromCliff(formation));
				formation.AI.AddAiBehavior(new BehaviorNavalRaidCliffShooting(formation));
				formation.AI.AddAiBehavior(new BehaviorNavalRaidHoldChokePoint(formation));
			}
		}
	}
}

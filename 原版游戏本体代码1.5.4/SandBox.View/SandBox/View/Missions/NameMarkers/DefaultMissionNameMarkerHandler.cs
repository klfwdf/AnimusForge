using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects;
using SandBox.Objects.AreaMarkers;
using SandBox.ViewModelCollection.Missions.NameMarker;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.View.Missions.NameMarkers;

public class DefaultMissionNameMarkerHandler : MissionNameMarkerProvider
{
	private MissionMode _lastMissionMode;

	private DisguiseMissionLogic _disguiseMissionLogic;

	protected override void OnInitialize(Mission mission)
	{
		base.OnInitialize(mission);
		_disguiseMissionLogic = mission.GetMissionBehavior<DisguiseMissionLogic>();
		_lastMissionMode = mission.Mode;
	}

	protected override void OnDestroy(Mission mission)
	{
		base.OnDestroy(mission);
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_lastMissionMode != Mission.Current?.Mode)
		{
			SetMarkersDirty();
			_lastMissionMode = Mission.Current.Mode;
		}
	}

	public override void CreateMarkers(List<MissionNameMarkerTargetBaseVM> markers)
	{
		Mission current = Mission.Current;
		if (current.MainAgent == null || current.Mode == MissionMode.Battle || current.Mode == MissionMode.Deployment)
		{
			return;
		}
		List<MissionAgentMarkerTargetVM> list = new List<MissionAgentMarkerTargetVM>();
		foreach (Agent agent in current.Agents)
		{
			AddAgentTarget(agent, list);
		}
		for (int i = 0; i < list.Count; i++)
		{
			markers.Add(list[i]);
		}
		if (Hero.MainHero.CurrentSettlement == null)
		{
			return;
		}
		List<CommonAreaMarker> list2 = (from x in current.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>()
			where x.GameEntity.HasTag("alley_marker")
			select x).ToList();
		if (Hero.MainHero.CurrentSettlement.Alleys.Count > 0)
		{
			foreach (CommonAreaMarker item in list2)
			{
				Alley alley = item.GetAlley();
				if (alley != null && alley.Owner != null)
				{
					markers.Add(new MissionCommonAreaMarkerTargetVM(item));
				}
			}
		}
		List<PassageUsePoint> source = current.ActiveMissionObjects.FindAllWithType<PassageUsePoint>().ToList();
		List<string> passagePointFilter = new List<string> { "Empty Shop" };
		foreach (PassageUsePoint item2 in source.Where((PassageUsePoint passage) => passage.ToLocation != null && !passagePointFilter.Exists((string s) => passage.ToLocation.Name.Contains(s))))
		{
			if (!item2.ToLocation.CanBeReserved || item2.ToLocation.IsReserved)
			{
				markers.Add(new MissionPassageUsePointNameMarkerTargetVM(item2));
			}
		}
		foreach (BasicAreaIndicator item3 in from b in current.ActiveMissionObjects.FindAllWithType<BasicAreaIndicator>().ToList()
			where b.IsActive
			select b)
		{
			markers.Add(new MissionBasicAreaIndicatorMarkerTargetVM(item3, item3.GetPosition()));
		}
		if (!current.HasMissionBehavior<WorkshopMissionHandler>())
		{
			return;
		}
		foreach (Tuple<Workshop, GameEntity> item4 in from s in current.GetMissionBehavior<WorkshopMissionHandler>().WorkshopSignEntities.ToList()
			where s.Item1.WorkshopType != null
			select s)
		{
			markers.Add(new MissionWorkshopNameMarkerTargetVM(item4.Item1, item4.Item2.GlobalPosition - MissionNameMarkerHelper.DefaultHeightOffset));
		}
	}

	private void AddAgentTarget(Agent agent, List<MissionAgentMarkerTargetVM> markers, bool isAdditional = false)
	{
		if (agent?.Character == null || agent == Agent.Main || !agent.IsActive() || markers.Any((MissionAgentMarkerTargetVM t) => t.Target == agent))
		{
			return;
		}
		if (!isAdditional && !agent.Character.IsHero)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if ((currentSettlement == null || currentSettlement.LocationComplex?.FindCharacter(agent)?.IsVisualTracked != true) && (!(agent.Character is CharacterObject characterObject) || (characterObject.Occupation != Occupation.RansomBroker && characterObject.Occupation != Occupation.Tavernkeeper)) && agent.Character != Settlement.CurrentSettlement?.Culture?.Blacksmith && agent.Character != Settlement.CurrentSettlement?.Culture?.Barber && agent.Character != Settlement.CurrentSettlement?.Culture?.TavernGamehost && agent.Character != Settlement.CurrentSettlement?.Culture?.Merchant && !(agent.Character.StringId == "sp_hermit") && agent.Character != Settlement.CurrentSettlement?.Culture?.Shipwright)
			{
				DisguiseMissionLogic disguiseMissionLogic = _disguiseMissionLogic;
				if (disguiseMissionLogic == null || !disguiseMissionLogic.IsContactAgentTracked(agent))
				{
					return;
				}
			}
		}
		MissionAgentMarkerTargetVM item = new MissionAgentMarkerTargetVM(agent);
		markers.Add(item);
	}
}

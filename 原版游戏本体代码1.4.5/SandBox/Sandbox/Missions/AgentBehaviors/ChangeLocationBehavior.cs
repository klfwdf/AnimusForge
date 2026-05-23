using System.Collections.Generic;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class ChangeLocationBehavior : AgentBehavior
{
	private readonly MissionAgentHandler _missionAgentHandler;

	private readonly float _initializeTime;

	private Passage _selectedDoor;

	public ChangeLocationBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		_initializeTime = base.Mission.CurrentTime;
	}

	public override void Tick(float dt, bool isSimulation)
	{
		if (_selectedDoor == null)
		{
			Passage passage = SelectADoor();
			if (passage != null)
			{
				_selectedDoor = passage;
				base.Navigator.SetTarget(_selectedDoor);
			}
		}
		else if (_selectedDoor.ToLocation.CharacterCount >= _selectedDoor.ToLocation.ProsperityMax)
		{
			base.Navigator.SetTarget(null);
			base.Navigator.ForceThink(0f);
			_selectedDoor = null;
		}
	}

	private Passage SelectADoor()
	{
		Passage result = null;
		List<Passage> list = new List<Passage>();
		foreach (Passage townPassageProp in _missionAgentHandler.TownPassageProps)
		{
			if (townPassageProp.GetVacantStandingPointForAI(base.OwnerAgent) != null && townPassageProp.ToLocation.CharacterCount < townPassageProp.ToLocation.ProsperityMax)
			{
				list.Add(townPassageProp);
			}
		}
		if (list.Count > 0)
		{
			result = list[MBRandom.RandomInt(list.Count)];
		}
		return result;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_selectedDoor = null;
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_selectedDoor = null;
	}

	public override string GetDebugInfo()
	{
		if (_selectedDoor != null)
		{
			return "Go to " + _selectedDoor.ToLocation.StringId;
		}
		return "Change Location no target";
	}

	public override float GetAvailability(bool isSimulation)
	{
		float result = 0f;
		bool flag = false;
		bool flag2 = false;
		LocationCharacter locationCharacter = CampaignMission.Current.Location.GetLocationCharacter(base.OwnerAgent.Origin);
		if (base.Mission.CurrentTime < 5f || locationCharacter.FixedLocation || !_missionAgentHandler.HasPassages())
		{
			return 0f;
		}
		foreach (UsableMachine townPassageProp in _missionAgentHandler.TownPassageProps)
		{
			Passage passage = townPassageProp as Passage;
			if (passage.ToLocation.CanAIEnter(locationCharacter) && passage.ToLocation.CharacterCount < passage.ToLocation.ProsperityMax)
			{
				flag = true;
				if (passage.PilotStandingPoint.GameEntity.GetGlobalFrame().origin.Distance(base.OwnerAgent.Position) < 1f)
				{
					flag2 = true;
					break;
				}
			}
		}
		if (flag)
		{
			if (!flag2)
			{
				result = (CampaignMission.Current.Location.IsIndoor ? 0.1f : 0.05f);
			}
			else if (base.Mission.CurrentTime - _initializeTime > 10f)
			{
				result = 0.01f;
			}
		}
		return result;
	}
}

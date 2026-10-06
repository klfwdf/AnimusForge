using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class BattleSurgeonLogic : MissionLogic
{
	private Dictionary<string, Agent> _surgeonAgents = new Dictionary<string, Agent>();

	protected override void OnGetAgentState(Agent agent, bool usedSurgery)
	{
		if (usedSurgery)
		{
			PartyBase ownerParty = agent.GetComponent<CampaignAgentComponent>().OwnerParty;
			if (ownerParty != null && _surgeonAgents.TryGetValue(ownerParty.Id, out var value) && value.State == AgentState.Active)
			{
				SkillLevelingManager.OnSurgeryApplied(ownerParty.MobileParty, surgerySuccess: true, ((CharacterObject)agent.Character).Tier);
			}
		}
	}

	public override void OnAgentCreated(Agent agent)
	{
		base.OnAgentCreated(agent);
		CharacterObject characterObject = (CharacterObject)agent.Character;
		if (characterObject?.HeroObject?.PartyBelongedTo != null && characterObject.HeroObject == characterObject.HeroObject.PartyBelongedTo.EffectiveSurgeon)
		{
			string id = characterObject.HeroObject.PartyBelongedTo.Party.Id;
			if (_surgeonAgents.ContainsKey(id))
			{
				_surgeonAgents.Remove(id);
			}
			_surgeonAgents.Add(id, agent);
		}
	}
}

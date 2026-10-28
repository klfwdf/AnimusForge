using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class CampaignAgentComponent : AgentComponent
{
	public AgentNavigator AgentNavigator { get; private set; }

	public PartyBase OwnerParty => (PartyBase)(Agent.Origin?.BattleCombatant);

	public CampaignAgentComponent(Agent agent)
		: base(agent)
	{
	}

	public AgentNavigator CreateAgentNavigator(LocationCharacter locationCharacter)
	{
		AgentNavigator = new AgentNavigator(Agent, locationCharacter);
		return AgentNavigator;
	}

	public AgentNavigator CreateAgentNavigator()
	{
		AgentNavigator = new AgentNavigator(Agent);
		return AgentNavigator;
	}

	public void OnAgentRemoved(Agent agent)
	{
		AgentNavigator?.OnAgentRemoved(agent);
	}

	public override void OnTick(float dt)
	{
		if (Agent.Mission.AllowAiTicking && Agent.IsAIControlled)
		{
			AgentNavigator?.Tick(dt);
		}
	}

	public override float GetMoraleDecreaseConstant()
	{
		if (OwnerParty?.MapEvent == null || !OwnerParty.MapEvent.IsSiegeAssault)
		{
			return 1f;
		}
		if (OwnerParty.MapEvent.AttackerSide.Parties.FindIndexQ((MapEventParty p) => p.Party == OwnerParty) < 0)
		{
			return 0.5f;
		}
		return 0.33f;
	}

	public override float GetMoraleAddition()
	{
		float num = 0f;
		if (OwnerParty?.MapEvent != null)
		{
			OwnerParty.MapEvent.GetStrengthsRelativeToParty(OwnerParty.Side, out var partySideStrength, out var opposingSideStrength);
			if (OwnerParty.IsMobile)
			{
				float num2 = (OwnerParty.MobileParty.Morale - 50f) / 2f;
				num += num2;
			}
			float num3 = partySideStrength / (partySideStrength + opposingSideStrength) * 10f - 5f;
			num += num3;
		}
		return num;
	}

	public override void OnStopUsingGameObject()
	{
		if (Agent.IsAIControlled)
		{
			AgentNavigator?.OnStopUsingGameObject();
		}
	}
}

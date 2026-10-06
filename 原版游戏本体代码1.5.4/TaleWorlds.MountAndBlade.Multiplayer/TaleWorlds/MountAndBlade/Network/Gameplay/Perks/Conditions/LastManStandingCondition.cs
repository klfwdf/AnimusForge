using System.Xml;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class LastManStandingCondition : MPPerkCondition
{
	protected static string StringType = "LastManStanding";

	public override PerkEventFlags EventFlags => PerkEventFlags.AliveBotCountChange | PerkEventFlags.SpawnEnd;

	protected LastManStandingCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		MissionPeer missionPeer = agent?.MissionPeer ?? agent?.OwningAgentMissionPeer;
		if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0 && missionPeer?.ControlledFormation != null && agent.IsActive())
		{
			if (!agent.IsPlayerControlled)
			{
				return missionPeer.BotsUnderControlAlive == 1;
			}
			return missionPeer.BotsUnderControlAlive == 0;
		}
		return false;
	}
}

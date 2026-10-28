using System.Xml;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class ControllerCondition : MPPerkCondition
{
	protected static string StringType = "Controller";

	private bool _isPlayerControlled;

	public override PerkEventFlags EventFlags => PerkEventFlags.PeerControlledAgentChange;

	protected ControllerCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		_isPlayerControlled = (node?.Attributes?["is_player_controlled"]?.Value)?.ToLower() == "true";
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		if (agent != null)
		{
			return agent.IsPlayerControlled == _isPlayerControlled;
		}
		return false;
	}
}

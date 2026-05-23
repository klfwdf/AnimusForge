using System;
using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class AgentStatusCondition : MPPerkCondition
{
	private enum AgentStatus
	{
		OnFoot,
		OnMount
	}

	protected static string StringType = "AgentStatus";

	private AgentStatus _status;

	public override PerkEventFlags EventFlags => PerkEventFlags.MountChange;

	protected AgentStatusCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		if (!Enum.TryParse<AgentStatus>(node?.Attributes?["agent_status"]?.Value, ignoreCase: true, out _status))
		{
			Debug.FailedAssert("provided 'agent_status' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\AgentStatusCondition.cs", "Deserialize", 31);
		}
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		if (agent != null)
		{
			if (agent.MountAgent == null)
			{
				return _status == AgentStatus.OnFoot;
			}
			return _status == AgentStatus.OnMount;
		}
		return false;
	}
}

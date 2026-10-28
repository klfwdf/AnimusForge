using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class MoraleCondition : MPPerkCondition<MissionMultiplayerFlagDomination>
{
	protected static string StringType = "FlagDominationMorale";

	private float _min;

	private float _max;

	public override PerkEventFlags EventFlags => PerkEventFlags.MoraleChange;

	public override bool IsPeerCondition => true;

	protected MoraleCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		string text = node?.Attributes?["min"]?.Value;
		if (text == null)
		{
			_min = -1f;
		}
		else if (!float.TryParse(text, out _min))
		{
			Debug.FailedAssert("provided 'min' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\MoraleCondition.cs", "Deserialize", 35);
		}
		string text2 = node?.Attributes?["max"]?.Value;
		if (text2 == null)
		{
			_max = 1f;
		}
		else if (!float.TryParse(text2, out _max))
		{
			Debug.FailedAssert("provided 'max' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\MoraleCondition.cs", "Deserialize", 45);
		}
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		Team team = agent?.Team;
		if (team != null)
		{
			MissionMultiplayerFlagDomination gameModeInstance = base.GameModeInstance;
			float num = ((team.Side == BattleSideEnum.Attacker) ? gameModeInstance.MoraleRounded : (0f - gameModeInstance.MoraleRounded));
			if (num >= _min)
			{
				return num <= _max;
			}
			return false;
		}
		return false;
	}
}

using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class TroopCountCondition : MPPerkCondition
{
	protected static string StringType = "TroopCount";

	private bool _isRatio;

	private float _min;

	private float _max;

	public override PerkEventFlags EventFlags => PerkEventFlags.AliveBotCountChange | PerkEventFlags.SpawnEnd;

	public override bool IsPeerCondition => true;

	protected TroopCountCondition()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		_isRatio = (node?.Attributes?["is_ratio"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["min"]?.Value;
		if (text == null)
		{
			_min = 0f;
		}
		else if (!float.TryParse(text, out _min))
		{
			Debug.FailedAssert("provided 'min' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\TroopCountCondition.cs", "Deserialize", 39);
		}
		string text2 = node?.Attributes?["max"]?.Value;
		if (text2 == null)
		{
			_max = (_isRatio ? 1f : float.MaxValue);
		}
		else if (!float.TryParse(text2, out _max))
		{
			Debug.FailedAssert("provided 'max' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\TroopCountCondition.cs", "Deserialize", 49);
		}
	}

	public override bool Check(MissionPeer peer)
	{
		if (peer != null && MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0 && peer.ControlledFormation != null)
		{
			int num = (peer.IsControlledAgentActive ? (peer.BotsUnderControlAlive + 1) : peer.BotsUnderControlAlive);
			if (_isRatio)
			{
				float num2 = (float)num / (float)(peer.BotsUnderControlTotal + 1);
				if (num2 >= _min)
				{
					return num2 <= _max;
				}
				return false;
			}
			if ((float)num >= _min)
			{
				return (float)num <= _max;
			}
			return false;
		}
		return false;
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		return Check(agent?.MissionPeer ?? agent?.OwningAgentMissionPeer);
	}
}

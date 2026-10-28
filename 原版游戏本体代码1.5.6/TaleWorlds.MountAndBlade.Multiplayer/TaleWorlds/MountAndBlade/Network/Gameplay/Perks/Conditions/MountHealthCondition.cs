using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

public class MountHealthCondition : MPPerkCondition
{
	protected static string StringType = "MountHealth";

	private bool _isRatio;

	private float _min;

	private float _max;

	public override PerkEventFlags EventFlags => PerkEventFlags.MountHealthChange | PerkEventFlags.MountChange;

	protected MountHealthCondition()
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
			Debug.FailedAssert("provided 'min' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\MountHealthCondition.cs", "Deserialize", 34);
		}
		string text2 = node?.Attributes?["max"]?.Value;
		if (text2 == null)
		{
			_max = (_isRatio ? 1f : float.MaxValue);
		}
		else if (!float.TryParse(text2, out _max))
		{
			Debug.FailedAssert("provided 'max' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Conditions\\MountHealthCondition.cs", "Deserialize", 44);
		}
	}

	public override bool Check(MissionPeer peer)
	{
		return Check(peer?.ControlledAgent);
	}

	public override bool Check(Agent agent)
	{
		agent = ((agent != null && !agent.IsMount) ? agent.MountAgent : agent);
		if (agent != null)
		{
			float num = (_isRatio ? (agent.Health / agent.HealthLimit) : agent.Health);
			if (num >= _min)
			{
				return num <= _max;
			}
			return false;
		}
		return false;
	}
}

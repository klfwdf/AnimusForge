using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class MountSpeedEffect : MPPerkEffect
{
	protected static string StringType = "MountSpeed";

	private float _value;

	protected MountSpeedEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\MountSpeedEffect.cs", "Deserialize", 23);
		}
	}

	public override void OnUpdate(Agent agent, bool newState)
	{
		agent = ((agent != null && !agent.IsMount) ? agent.MountAgent : agent);
		agent?.UpdateAgentProperties();
	}

	public override float GetMountSpeed()
	{
		return _value;
	}
}

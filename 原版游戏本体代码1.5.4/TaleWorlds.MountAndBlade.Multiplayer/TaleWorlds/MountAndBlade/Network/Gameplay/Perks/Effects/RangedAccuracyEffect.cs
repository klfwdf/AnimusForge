using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class RangedAccuracyEffect : MPPerkEffect
{
	protected static string StringType = "RangedAccuracy";

	private float _value;

	protected RangedAccuracyEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\RangedAccuracyEffect.cs", "Deserialize", 23);
		}
	}

	public override void OnUpdate(Agent agent, bool newState)
	{
		agent = ((agent != null && agent.IsMount) ? agent.RiderAgent : agent);
		agent?.UpdateAgentProperties();
	}

	public override float GetRangedAccuracy()
	{
		return _value;
	}
}

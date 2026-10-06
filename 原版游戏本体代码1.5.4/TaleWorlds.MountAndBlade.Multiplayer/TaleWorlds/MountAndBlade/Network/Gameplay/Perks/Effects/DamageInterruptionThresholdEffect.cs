using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class DamageInterruptionThresholdEffect : MPPerkEffect
{
	protected static string StringType = "DamageInterruptionThreshold";

	private float _value;

	protected DamageInterruptionThresholdEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\DamageInterruptionThresholdEffect.cs", "Deserialize", 23);
		}
	}

	public override float GetDamageInterruptionThreshold()
	{
		return _value;
	}
}

using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class RangedHeadShotDamageEffect : MPPerkEffect
{
	protected static string StringType = "RangedHeadShotDamage";

	private float _value;

	protected RangedHeadShotDamageEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\RangedHeadShotDamageEffect.cs", "Deserialize", 22);
		}
	}

	public override float GetRangedHeadShotDamage()
	{
		return _value;
	}
}

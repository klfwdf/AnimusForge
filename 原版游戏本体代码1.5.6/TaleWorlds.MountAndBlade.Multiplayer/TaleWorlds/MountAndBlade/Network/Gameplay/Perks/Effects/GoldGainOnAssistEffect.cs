using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class GoldGainOnAssistEffect : MPPerkEffect
{
	protected static string StringType = "GoldGainOnAssist";

	private int _value;

	protected GoldGainOnAssistEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !int.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\GoldGainOnAssistEffect.cs", "Deserialize", 22);
		}
	}

	public override int GetGoldOnAssist()
	{
		return _value;
	}
}

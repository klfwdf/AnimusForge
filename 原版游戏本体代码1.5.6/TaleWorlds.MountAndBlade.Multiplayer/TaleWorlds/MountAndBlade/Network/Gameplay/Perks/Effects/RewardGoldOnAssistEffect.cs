using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class RewardGoldOnAssistEffect : MPPerkEffect
{
	protected static string StringType = "RewardGoldOnAssist";

	private int _value;

	protected RewardGoldOnAssistEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !int.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\RewardGoldOnAssistEffect.cs", "Deserialize", 22);
		}
	}

	public override int GetRewardedGoldOnAssist()
	{
		return _value;
	}
}

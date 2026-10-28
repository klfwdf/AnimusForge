using System.Xml;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class TroopCountEffect : MPOnSpawnPerkEffect
{
	protected static string StringType = "TroopCountOnSpawn";

	private int _value;

	protected TroopCountEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !int.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\TroopCountEffect.cs", "Deserialize", 20);
		}
	}

	public override int GetExtraTroopCount()
	{
		return _value;
	}
}

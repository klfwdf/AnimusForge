using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class ArmorEffect : MPOnSpawnPerkEffect
{
	protected static string StringType = "ArmorOnSpawn";

	private float _value;

	protected ArmorEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\ArmorEffect.cs", "Deserialize", 21);
		}
	}

	public override float GetDrivenPropertyBonusOnSpawn(bool isPlayer, DrivenProperty drivenProperty, float baseValue)
	{
		if ((drivenProperty == DrivenProperty.ArmorHead || drivenProperty == DrivenProperty.ArmorTorso || drivenProperty == DrivenProperty.ArmorLegs || drivenProperty == DrivenProperty.ArmorArms) && (EffectTarget == Target.Any || (isPlayer ? (EffectTarget == Target.Player) : (EffectTarget == Target.Troops))))
		{
			return _value;
		}
		return 0f;
	}
}

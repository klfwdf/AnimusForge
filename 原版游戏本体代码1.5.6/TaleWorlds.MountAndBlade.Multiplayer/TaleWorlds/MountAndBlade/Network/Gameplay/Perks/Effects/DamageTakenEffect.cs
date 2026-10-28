using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class DamageTakenEffect : MPCombatPerkEffect
{
	protected static string StringType = "DamageTaken";

	private float _value;

	protected DamageTakenEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.Deserialize(node);
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\DamageTakenEffect.cs", "Deserialize", 22);
		}
	}

	public override float GetDamageTaken(WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		if (!IsSatisfied(attackerWeapon, damageType))
		{
			return 0f;
		}
		return _value;
	}
}

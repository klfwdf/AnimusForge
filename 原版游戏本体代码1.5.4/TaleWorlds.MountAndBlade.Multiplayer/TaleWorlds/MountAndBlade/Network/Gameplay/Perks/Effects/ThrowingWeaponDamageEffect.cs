using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class ThrowingWeaponDamageEffect : MPPerkEffect
{
	protected static string StringType = "ThrowingWeaponDamage";

	private float _value;

	protected ThrowingWeaponDamageEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\ThrowingWeaponDamageEffect.cs", "Deserialize", 24);
		}
	}

	public override float GetDamage(WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
	{
		if (isAlternativeAttack || attackerWeapon == null || WeaponComponentData.GetItemTypeFromWeaponClass(attackerWeapon.WeaponClass) != ItemObject.ItemTypeEnum.Thrown)
		{
			return 0f;
		}
		return _value;
	}
}

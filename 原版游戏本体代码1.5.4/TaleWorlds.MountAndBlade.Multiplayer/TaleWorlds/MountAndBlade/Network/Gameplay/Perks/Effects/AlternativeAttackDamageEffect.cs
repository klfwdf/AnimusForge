using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class AlternativeAttackDamageEffect : MPPerkEffect
{
	private enum AttackType
	{
		Any,
		Kick,
		Bash
	}

	protected static string StringType = "AlternativeAttackDamage";

	private AttackType _attackType;

	private float _value;

	protected AlternativeAttackDamageEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["attack_type"]?.Value;
		_attackType = AttackType.Any;
		if (text != null && !Enum.TryParse<AttackType>(text, ignoreCase: true, out _attackType))
		{
			_attackType = AttackType.Any;
			Debug.FailedAssert("provided 'attack_type' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\AlternativeAttackDamageEffect.cs", "Deserialize", 34);
		}
		string text2 = node?.Attributes?["value"]?.Value;
		if (text2 == null || !float.TryParse(text2, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\AlternativeAttackDamageEffect.cs", "Deserialize", 40);
		}
	}

	public override float GetDamage(WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
	{
		if (isAlternativeAttack)
		{
			switch (_attackType)
			{
			case AttackType.Any:
				return _value;
			case AttackType.Kick:
				if (attackerWeapon != null)
				{
					return 0f;
				}
				return _value;
			case AttackType.Bash:
				if (attackerWeapon == null)
				{
					return 0f;
				}
				return _value;
			}
		}
		return 0f;
	}
}

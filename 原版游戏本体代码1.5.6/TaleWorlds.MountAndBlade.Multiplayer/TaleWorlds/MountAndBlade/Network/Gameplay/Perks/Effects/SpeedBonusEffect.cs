using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class SpeedBonusEffect : MPCombatPerkEffect
{
	protected static string StringType = "SpeedBonus";

	private float _value;

	protected SpeedBonusEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\SpeedBonusEffect.cs", "Deserialize", 24);
		}
	}

	public override float GetSpeedBonusEffectiveness(Agent attacker, WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		attacker = ((attacker != null && attacker.IsMount) ? attacker.RiderAgent : attacker);
		if (attacker != null)
		{
			if (!IsSatisfied(attackerWeapon, damageType))
			{
				return 0f;
			}
			return _value;
		}
		return 0f;
	}
}

using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;

public class ThrowingWeaponSpeedEffect : MPPerkEffect
{
	protected static string StringType = "ThrowingWeaponSpeed";

	private float _value;

	protected ThrowingWeaponSpeedEffect()
	{
	}

	protected override void Deserialize(XmlNode node)
	{
		base.IsDisabledInWarmup = (node?.Attributes?["is_disabled_in_warmup"]?.Value)?.ToLower() == "true";
		string text = node?.Attributes?["value"]?.Value;
		if (text == null || !float.TryParse(text, out _value))
		{
			Debug.FailedAssert("provided 'value' is invalid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\Perks\\Effects\\ThrowingWeaponSpeedEffect.cs", "Deserialize", 24);
		}
	}

	public override float GetThrowingWeaponSpeed(WeaponComponentData attackerWeapon)
	{
		if (attackerWeapon == null || WeaponComponentData.GetItemTypeFromWeaponClass(attackerWeapon.WeaponClass) != ItemObject.ItemTypeEnum.Thrown)
		{
			return 0f;
		}
		return _value;
	}
}

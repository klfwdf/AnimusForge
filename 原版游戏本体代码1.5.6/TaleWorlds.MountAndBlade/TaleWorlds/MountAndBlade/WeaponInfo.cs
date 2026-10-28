namespace TaleWorlds.MountAndBlade;

public struct WeaponInfo
{
	public bool IsValid { get; private set; }

	public bool IsMeleeWeapon { get; private set; }

	public bool IsRangedWeapon { get; private set; }

	public WeaponInfo(bool isValid, bool isMeleeWeapon, bool isRangedWeapon)
	{
		IsValid = isValid;
		IsMeleeWeapon = isMeleeWeapon;
		IsRangedWeapon = isRangedWeapon;
	}
}

using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class JavelinBarrel : AmmoBarrelBase
{
	private readonly string _pickupSoundEventString = "event:/mission/combat/pickup_arrows";

	protected override int GetSoundEvent()
	{
		return SoundEvent.GetEventIdFromString(_pickupSoundEventString);
	}

	protected override WeaponClass[] GetRequiredWeaponClasses()
	{
		return new WeaponClass[7]
		{
			WeaponClass.Arrow,
			WeaponClass.Bolt,
			WeaponClass.SlingStone,
			WeaponClass.Cartridge,
			WeaponClass.ThrowingAxe,
			WeaponClass.ThrowingKnife,
			WeaponClass.Javelin
		};
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=ybGIoUvT}Ammunition Barrels");
	}
}

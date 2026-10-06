using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public class ArrowBarrel : AmmoBarrelBase
{
	private readonly string _pickupSoundEventString = "event:/mission/combat/pickup_arrows";

	protected override int GetSoundEvent()
	{
		return SoundEvent.GetEventIdFromString(_pickupSoundEventString);
	}

	protected override WeaponClass[] GetRequiredWeaponClasses()
	{
		return new WeaponClass[2]
		{
			WeaponClass.Arrow,
			WeaponClass.Bolt
		};
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=bWi4aMO9}Arrow Barrel");
	}
}

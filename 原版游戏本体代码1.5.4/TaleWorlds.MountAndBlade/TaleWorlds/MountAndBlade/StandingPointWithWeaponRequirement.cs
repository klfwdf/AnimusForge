using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class StandingPointWithWeaponRequirement : StandingPoint
{
	private ItemObject _requiredWeapon;

	private ItemObject _givenWeapon;

	private WeaponClass[] _requiredWeaponClasses;

	private bool _hasAlternative;

	public StandingPointWithWeaponRequirement()
	{
		AutoSheathWeapons = false;
		_requiredWeaponClasses = new WeaponClass[0];
		_hasAlternative = base.HasAlternative();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
	}

	public void InitRequiredWeaponClasses(WeaponClass[] requiredWeaponClasses)
	{
		_requiredWeaponClasses = requiredWeaponClasses;
	}

	public void InitRequiredWeapon(ItemObject weapon)
	{
		_requiredWeapon = weapon;
	}

	public void InitGivenWeapon(ItemObject weapon)
	{
		_givenWeapon = weapon;
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		if (_requiredWeapon != null)
		{
			if (primaryWieldedItemIndex != EquipmentIndex.None && agent.Equipment[primaryWieldedItemIndex].Item == _requiredWeapon)
			{
				return base.IsDisabledForAgent(agent);
			}
		}
		else if (_givenWeapon != null)
		{
			if (primaryWieldedItemIndex == EquipmentIndex.None || agent.Equipment[primaryWieldedItemIndex].Item != _givenWeapon)
			{
				return base.IsDisabledForAgent(agent);
			}
		}
		else if (!_requiredWeaponClasses.IsEmpty())
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!agent.Equipment[equipmentIndex].IsEmpty && _requiredWeaponClasses.Contains(agent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass) && (!agent.Equipment[equipmentIndex].CurrentUsageItem.IsConsumable || agent.Equipment[equipmentIndex].Amount < agent.Equipment[equipmentIndex].ModifiedMaxAmount || equipmentIndex == EquipmentIndex.ExtraWeaponSlot))
				{
					return base.IsDisabledForAgent(agent);
				}
			}
		}
		return true;
	}

	public void SetHasAlternative(bool hasAlternative)
	{
		_hasAlternative = hasAlternative;
	}

	public override bool HasAlternative()
	{
		return _hasAlternative;
	}

	public void SetUsingBattleSide(BattleSideEnum side)
	{
		StandingPointSide = side;
	}
}

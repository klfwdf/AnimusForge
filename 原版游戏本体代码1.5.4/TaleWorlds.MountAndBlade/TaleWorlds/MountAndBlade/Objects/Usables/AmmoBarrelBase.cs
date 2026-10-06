using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Objects.Usables;

public abstract class AmmoBarrelBase : UsableMachine
{
	private readonly WeaponClass[] _requiredWeaponClasses;

	private int _pickupSoundFromBarrel = -1;

	private bool _isVisible = true;

	private bool _needsSingleThreadTickOnce;

	private int PickupSoundFromBarrelCache
	{
		get
		{
			if (_pickupSoundFromBarrel == -1)
			{
				_pickupSoundFromBarrel = GetSoundEvent();
			}
			return _pickupSoundFromBarrel;
		}
	}

	public AmmoBarrelBase()
	{
		_requiredWeaponClasses = GetRequiredWeaponClasses();
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			(standingPoint as StandingPointWithWeaponRequirement).InitRequiredWeaponClasses(_requiredWeaponClasses);
		}
		SetScriptComponentToTick(GetTickRequirement());
		MakeVisibilityCheck = false;
		_isVisible = base.GameEntity.IsVisibleIncludeParents();
	}

	protected abstract WeaponClass[] GetRequiredWeaponClasses();

	public override void OnDeploymentFinished()
	{
		if (base.StandingPoints == null)
		{
			return;
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.LockUserFrames = false;
		}
	}

	protected abstract int GetSoundEvent();

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public abstract override TextObject GetDescriptionText(WeakGameEntity gameEntity);

	public override TickRequirement GetTickRequirement()
	{
		if (GameNetwork.IsClientOrReplay)
		{
			return base.GetTickRequirement();
		}
		return TickRequirement.Tick | TickRequirement.TickParallel | base.GetTickRequirement();
	}

	protected internal override void OnTickParallel(float dt)
	{
		TickAux(isParallel: true);
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (_needsSingleThreadTickOnce)
		{
			_needsSingleThreadTickOnce = false;
			TickAux(isParallel: false);
		}
	}

	private void TickAux(bool isParallel)
	{
		if (!_isVisible || GameNetwork.IsClientOrReplay)
		{
			return;
		}
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			if (!standingPoint.HasUser)
			{
				continue;
			}
			Agent userAgent = standingPoint.UserAgent;
			ActionIndexCache currentAction = userAgent.GetCurrentAction(0);
			ActionIndexCache currentAction2 = userAgent.GetCurrentAction(1);
			if (currentAction2 == ActionIndexCache.act_none && (currentAction == ActionIndexCache.act_pickup_down_begin || currentAction == ActionIndexCache.act_pickup_down_begin_left_stance))
			{
				continue;
			}
			if (currentAction2 == ActionIndexCache.act_none && (currentAction == ActionIndexCache.act_pickup_down_end || currentAction == ActionIndexCache.act_pickup_down_end_left_stance))
			{
				if (isParallel)
				{
					_needsSingleThreadTickOnce = true;
					continue;
				}
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
				{
					if (!userAgent.Equipment[equipmentIndex].IsEmpty && _requiredWeaponClasses.Contains(userAgent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass) && userAgent.Equipment[equipmentIndex].Amount < userAgent.Equipment[equipmentIndex].ModifiedMaxAmount)
					{
						userAgent.SetWeaponAmountInSlot(equipmentIndex, userAgent.Equipment[equipmentIndex].ModifiedMaxAmount, enforcePrimaryItem: true);
						Mission.Current.MakeSoundOnlyOnRelatedPeer(PickupSoundFromBarrelCache, userAgent.Position, userAgent.Index);
					}
				}
				userAgent.StopUsingGameObject();
			}
			else if (currentAction2 != ActionIndexCache.act_none || !userAgent.SetActionChannel(0, userAgent.GetIsLeftStance() ? ActionIndexCache.act_pickup_down_begin_left_stance : ActionIndexCache.act_pickup_down_begin, ignorePriority: false, (AnimFlags)0uL))
			{
				if (isParallel)
				{
					_needsSingleThreadTickOnce = true;
				}
				else
				{
					userAgent.StopUsingGameObject();
				}
			}
		}
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		return OrderType.None;
	}
}

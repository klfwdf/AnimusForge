using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SiegeWeaponController
{
	private readonly Mission _mission;

	private readonly Team _team;

	private List<SiegeWeapon> _availableWeapons;

	private MBList<SiegeWeapon> _selectedWeapons;

	public MBReadOnlyList<SiegeWeapon> SelectedWeapons => _selectedWeapons;

	public event Action<SiegeWeaponOrderType, IEnumerable<SiegeWeapon>> OnOrderIssued;

	public event Action OnSelectedSiegeWeaponsChanged;

	public SiegeWeaponController(Mission mission, Team team)
	{
		_mission = mission;
		_team = team;
		_selectedWeapons = new MBList<SiegeWeapon>();
		InitializeWeaponsForDeployment();
	}

	private void InitializeWeaponsForDeployment()
	{
		IEnumerable<SiegeWeapon> source = from w in (from dp in _mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
				where dp.Side == _team.Side
				select dp).SelectMany((DeploymentPoint dp) => dp.DeployableWeapons)
			select w as SiegeWeapon;
		_availableWeapons = source.ToList();
	}

	private void InitializeWeapons()
	{
		_availableWeapons = new List<SiegeWeapon>();
		_availableWeapons.AddRange(from w in _mission.ActiveMissionObjects.FindAllWithType<RangedSiegeWeapon>()
			where w.Side == _team.Side
			select w);
		if (_team.Side == BattleSideEnum.Attacker)
		{
			_availableWeapons.AddRange(from w in _mission.ActiveMissionObjects.FindAllWithType<SiegeWeapon>()
				where w is IPrimarySiegeWeapon && !(w is RangedSiegeWeapon)
				select w);
		}
		_availableWeapons.Sort((SiegeWeapon w1, SiegeWeapon w2) => GetShortcutIndexOf(w1).CompareTo(GetShortcutIndexOf(w2)));
	}

	public void Select(SiegeWeapon weapon)
	{
		if (!SelectedWeapons.Contains(weapon) && IsWeaponSelectable(weapon))
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new SelectSiegeWeapon(weapon.Id));
				GameNetwork.EndModuleEventAsClient();
			}
			_selectedWeapons.Add(weapon);
			this.OnSelectedSiegeWeaponsChanged?.Invoke();
		}
		else
		{
			Debug.FailedAssert("Weapon already selected or is not selectable", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponController.cs", "Select", 82);
		}
	}

	public void ClearSelectedWeapons()
	{
		_ = GameNetwork.IsClient;
		_selectedWeapons.Clear();
		this.OnSelectedSiegeWeaponsChanged?.Invoke();
	}

	public void Deselect(SiegeWeapon weapon)
	{
		if (SelectedWeapons.Contains(weapon))
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new UnselectSiegeWeapon(weapon.Id));
				GameNetwork.EndModuleEventAsClient();
			}
			_selectedWeapons.Remove(weapon);
			this.OnSelectedSiegeWeaponsChanged?.Invoke();
		}
		else
		{
			Debug.FailedAssert("Trying to deselect an unselected weapon", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponController.cs", "Deselect", 113);
		}
	}

	public void SelectAll()
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SelectAllSiegeWeapons());
			GameNetwork.EndModuleEventAsClient();
		}
		_selectedWeapons.Clear();
		foreach (SiegeWeapon availableWeapon in _availableWeapons)
		{
			if (IsWeaponSelectable(availableWeapon))
			{
				_selectedWeapons.Add(availableWeapon);
			}
		}
		this.OnSelectedSiegeWeaponsChanged?.Invoke();
	}

	public static bool IsWeaponSelectable(SiegeWeapon weapon)
	{
		if (!weapon.IsDestroyed)
		{
			return !weapon.IsDeactivated;
		}
		return false;
	}

	public static SiegeWeaponOrderType GetActiveOrderOf(SiegeWeapon weapon)
	{
		if (!weapon.ForcedUse)
		{
			return SiegeWeaponOrderType.Stop;
		}
		if (weapon is RangedSiegeWeapon)
		{
			switch (((RangedSiegeWeapon)weapon).Focus)
			{
			case RangedSiegeWeapon.FiringFocus.Walls:
				return SiegeWeaponOrderType.FireAtWalls;
			case RangedSiegeWeapon.FiringFocus.Troops:
				return SiegeWeaponOrderType.FireAtTroops;
			case RangedSiegeWeapon.FiringFocus.RangedSiegeWeapons:
				return SiegeWeaponOrderType.FireAtRangedSiegeWeapons;
			case RangedSiegeWeapon.FiringFocus.PrimarySiegeWeapons:
				return SiegeWeaponOrderType.FireAtPrimarySiegeWeapons;
			default:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponController.cs", "GetActiveOrderOf", 169);
				return SiegeWeaponOrderType.FireAtTroops;
			}
		}
		return SiegeWeaponOrderType.Attack;
	}

	public static SiegeWeaponOrderType GetActiveMovementOrderOf(SiegeWeapon weapon)
	{
		if (!weapon.ForcedUse)
		{
			return SiegeWeaponOrderType.Stop;
		}
		return SiegeWeaponOrderType.Attack;
	}

	public static SiegeWeaponOrderType GetActiveFacingOrderOf(SiegeWeapon weapon)
	{
		if (weapon is RangedSiegeWeapon)
		{
			switch (((RangedSiegeWeapon)weapon).Focus)
			{
			case RangedSiegeWeapon.FiringFocus.Walls:
				return SiegeWeaponOrderType.FireAtWalls;
			case RangedSiegeWeapon.FiringFocus.Troops:
				return SiegeWeaponOrderType.FireAtTroops;
			case RangedSiegeWeapon.FiringFocus.RangedSiegeWeapons:
				return SiegeWeaponOrderType.FireAtRangedSiegeWeapons;
			case RangedSiegeWeapon.FiringFocus.PrimarySiegeWeapons:
				return SiegeWeaponOrderType.FireAtPrimarySiegeWeapons;
			default:
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponController.cs", "GetActiveFacingOrderOf", 207);
				return SiegeWeaponOrderType.FireAtTroops;
			}
		}
		return SiegeWeaponOrderType.FireAtWalls;
	}

	public static SiegeWeaponOrderType GetActiveFiringOrderOf(SiegeWeapon weapon)
	{
		if (!weapon.ForcedUse)
		{
			return SiegeWeaponOrderType.Stop;
		}
		return SiegeWeaponOrderType.Attack;
	}

	public static SiegeWeaponOrderType GetActiveAIControlOrderOf(SiegeWeapon weapon)
	{
		if (weapon.ForcedUse)
		{
			return SiegeWeaponOrderType.AIControlOn;
		}
		return SiegeWeaponOrderType.AIControlOff;
	}

	private void SetOrderAux(SiegeWeaponOrderType order, SiegeWeapon weapon)
	{
		switch (order)
		{
		case SiegeWeaponOrderType.Stop:
		case SiegeWeaponOrderType.AIControlOff:
			weapon.SetForcedUse(value: false);
			break;
		case SiegeWeaponOrderType.Attack:
		case SiegeWeaponOrderType.AIControlOn:
			weapon.SetForcedUse(value: true);
			break;
		case SiegeWeaponOrderType.FireAtTroops:
			weapon.SetForcedUse(value: true);
			if (weapon is RangedSiegeWeapon rangedSiegeWeapon2)
			{
				rangedSiegeWeapon2.Focus = RangedSiegeWeapon.FiringFocus.Troops;
			}
			break;
		case SiegeWeaponOrderType.FireAtWalls:
			weapon.SetForcedUse(value: true);
			if (weapon is RangedSiegeWeapon rangedSiegeWeapon4)
			{
				rangedSiegeWeapon4.Focus = RangedSiegeWeapon.FiringFocus.Walls;
			}
			break;
		case SiegeWeaponOrderType.FireAtRangedSiegeWeapons:
			weapon.SetForcedUse(value: true);
			if (weapon is RangedSiegeWeapon rangedSiegeWeapon3)
			{
				rangedSiegeWeapon3.Focus = RangedSiegeWeapon.FiringFocus.RangedSiegeWeapons;
			}
			break;
		case SiegeWeaponOrderType.FireAtPrimarySiegeWeapons:
			weapon.SetForcedUse(value: true);
			if (weapon is RangedSiegeWeapon rangedSiegeWeapon)
			{
				rangedSiegeWeapon.Focus = RangedSiegeWeapon.FiringFocus.PrimarySiegeWeapons;
			}
			break;
		default:
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponController.cs", "SetOrderAux", 297);
			break;
		}
	}

	public void SetOrder(SiegeWeaponOrderType order)
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new ApplySiegeWeaponOrder(order));
			GameNetwork.EndModuleEventAsClient();
		}
		foreach (SiegeWeapon selectedWeapon in SelectedWeapons)
		{
			SetOrderAux(order, selectedWeapon);
		}
		this.OnOrderIssued?.Invoke(order, SelectedWeapons);
	}

	public int GetShortcutIndexOf(SiegeWeapon weapon)
	{
		int num = GetSideOf(weapon) switch
		{
			FormationAI.BehaviorSide.Right => 2, 
			FormationAI.BehaviorSide.Left => 1, 
			_ => 0, 
		};
		if (!(weapon is IPrimarySiegeWeapon))
		{
			num += 3;
		}
		return num;
	}

	private static FormationAI.BehaviorSide GetSideOf(SiegeWeapon weapon)
	{
		if (weapon is IPrimarySiegeWeapon primarySiegeWeapon)
		{
			return primarySiegeWeapon.WeaponSide;
		}
		if (weapon is RangedSiegeWeapon)
		{
			return FormationAI.BehaviorSide.Middle;
		}
		Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponController.cs", "GetSideOf", 349);
		return FormationAI.BehaviorSide.Middle;
	}
}

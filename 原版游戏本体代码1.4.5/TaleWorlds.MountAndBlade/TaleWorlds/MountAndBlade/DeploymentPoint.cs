using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions;
using TaleWorlds.MountAndBlade.Objects.Siege;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace TaleWorlds.MountAndBlade;

public class DeploymentPoint : SynchedMissionObject
{
	public enum DeploymentPointType
	{
		BatteringRam,
		TowerLadder,
		Breach,
		Ranged
	}

	public enum DeploymentPointState
	{
		NotDeployed,
		BatteringRam,
		SiegeLadder,
		SiegeTower,
		Breach,
		Ranged
	}

	public BattleSideEnum Side = BattleSideEnum.Attacker;

	public float Radius = 3f;

	public string SiegeWeaponTag = "dpWeapon";

	private readonly List<GameEntity> _highlightedEntites = new List<GameEntity>();

	private DeploymentPointType _deploymentPointType;

	private List<SiegeLadder> _associatedSiegeLadders;

	private bool _isBreachSideDeploymentPoint;

	private MBList<SynchedMissionObject> _weapons;

	public Vec3 DeploymentTargetPosition { get; private set; }

	public WallSegment AssociatedWallSegment { get; private set; }

	public IEnumerable<SynchedMissionObject> DeployableWeapons => _weapons.Where((SynchedMissionObject w) => !w.IsDisabled);

	public bool IsDeployed => DeployedWeapon != null;

	public SynchedMissionObject DeployedWeapon { get; private set; }

	public SynchedMissionObject DisbandedWeapon { get; private set; }

	public IEnumerable<Type> DeployableWeaponTypes => DeployableWeapons.Select(MissionSiegeWeaponsController.GetWeaponType);

	public event Action<DeploymentPoint, SynchedMissionObject> OnDeploymentStateChanged;

	public event Action<DeploymentPoint> OnDeploymentPointTypeDetermined;

	public event Action<DeploymentPoint> OnDeployOrDisband;

	protected internal override void OnInit()
	{
		_weapons = new MBList<SynchedMissionObject>();
	}

	public override void AfterMissionStart()
	{
		base.OnInit();
		if (!GameNetwork.IsClientOrReplay)
		{
			_weapons = GetWeaponsUnder();
			_associatedSiegeLadders = new List<SiegeLadder>();
			if (DeployableWeapons.IsEmpty())
			{
				SetVisibleSynched(value: false);
				SetBreachSideDeploymentPoint();
			}
			base.AfterMissionStart();
			if (!GameNetwork.IsClientOrReplay)
			{
				DetermineDeploymentPointType();
			}
			HideAllWeapons();
		}
	}

	private void SetBreachSideDeploymentPoint()
	{
		Debug.Print("Deployment point " + (base.GameEntity.IsValid ? ("upgrade level mask " + base.GameEntity.GetUpgradeLevelMask()) : "no game entity.") + "\n");
		_isBreachSideDeploymentPoint = true;
		_deploymentPointType = DeploymentPointType.Breach;
		FormationAI.BehaviorSide deploymentPointSide = (_weapons.FirstOrDefault((SynchedMissionObject w) => w is SiegeTower) as IPrimarySiegeWeapon).WeaponSide;
		AssociatedWallSegment = Mission.Current.ActiveMissionObjects.FindAllWithType<WallSegment>().FirstOrDefault((WallSegment ws) => ws.DefenseSide == deploymentPointSide);
		DeploymentTargetPosition = AssociatedWallSegment.GameEntity.GlobalPosition;
	}

	public Vec3 GetDeploymentOrigin()
	{
		return base.GameEntity.GlobalPosition;
	}

	public DeploymentPointState GetDeploymentPointState()
	{
		switch (_deploymentPointType)
		{
		case DeploymentPointType.BatteringRam:
			if (!IsDeployed)
			{
				return DeploymentPointState.NotDeployed;
			}
			return DeploymentPointState.BatteringRam;
		case DeploymentPointType.Breach:
			return DeploymentPointState.Breach;
		case DeploymentPointType.Ranged:
			if (!IsDeployed)
			{
				return DeploymentPointState.NotDeployed;
			}
			return DeploymentPointState.Ranged;
		case DeploymentPointType.TowerLadder:
			if (!IsDeployed)
			{
				return DeploymentPointState.SiegeLadder;
			}
			return DeploymentPointState.SiegeTower;
		default:
			MBDebug.ShowWarning("Undefined deployment point type fetched.");
			return DeploymentPointState.NotDeployed;
		}
	}

	public DeploymentPointType GetDeploymentPointType()
	{
		return _deploymentPointType;
	}

	public List<SiegeLadder> GetAssociatedSiegeLadders()
	{
		return _associatedSiegeLadders;
	}

	private void DetermineDeploymentPointType()
	{
		if (_isBreachSideDeploymentPoint)
		{
			_deploymentPointType = DeploymentPointType.Breach;
		}
		else if (_weapons.Any((SynchedMissionObject w) => w is BatteringRam))
		{
			_deploymentPointType = DeploymentPointType.BatteringRam;
			DeploymentTargetPosition = (_weapons.First((SynchedMissionObject w) => w is BatteringRam) as IPrimarySiegeWeapon).TargetCastlePosition.GameEntity.GlobalPosition;
		}
		else if (_weapons.Any((SynchedMissionObject w) => w is SiegeTower))
		{
			SiegeTower tower = _weapons.FirstOrDefault((SynchedMissionObject w) => w is SiegeTower) as SiegeTower;
			_deploymentPointType = DeploymentPointType.TowerLadder;
			DeploymentTargetPosition = tower.TargetCastlePosition.GameEntity.GlobalPosition;
			_associatedSiegeLadders = (from sl in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeLadder>()
				where sl.WeaponSide == tower.WeaponSide
				select sl).ToList();
		}
		else
		{
			_deploymentPointType = DeploymentPointType.Ranged;
			DeploymentTargetPosition = Vec3.Invalid;
		}
		this.OnDeploymentPointTypeDetermined?.Invoke(this);
	}

	public MBList<SynchedMissionObject> GetWeaponsUnder()
	{
		List<SiegeWeapon> list;
		if (Mission.Current.Teams[0].TeamAI is TeamAISiegeComponent teamAISiegeComponent)
		{
			list = teamAISiegeComponent.SceneSiegeWeapons;
		}
		else
		{
			List<GameEntity> entities = new List<GameEntity>();
			base.GameEntity.Scene.GetEntities(ref entities);
			list = (from se in entities
				where se.HasScriptOfType<SiegeWeapon>()
				select se.GetScriptComponents<SiegeWeapon>().FirstOrDefault()).ToList();
		}
		MBList<SynchedMissionObject> mBList = new MBList<SynchedMissionObject>();
		float num = Radius * Radius;
		foreach (SiegeWeapon item in list)
		{
			if (item.GameEntity.HasTag(SiegeWeaponTag) || (item.GameEntity.Parent.IsValid && item.GameEntity.Parent.HasTag(SiegeWeaponTag)) || (item.GameEntity != base.GameEntity && item.GameEntity.GlobalPosition.DistanceSquared(base.GameEntity.GlobalPosition) < num))
			{
				mBList.Add(item);
			}
		}
		return mBList;
	}

	public IEnumerable<SpawnerBase> GetSpawnersForEditor()
	{
		List<GameEntity> entities = new List<GameEntity>();
		base.GameEntity.Scene.GetEntities(ref entities);
		IEnumerable<SpawnerBase?> source = from se in entities
			where se.HasScriptOfType<SpawnerBase>()
			select se.GetScriptComponents<SpawnerBase>().FirstOrDefault();
		IEnumerable<SpawnerBase> first = from ssw in source
			where ssw.GameEntity.HasTag(SiegeWeaponTag)
			select (ssw);
		Vec3 globalPosition = base.GameEntity.GlobalPosition;
		float radiusSquared = Radius * Radius;
		IEnumerable<SpawnerBase> second = from ssw in source
			where ssw.GameEntity != base.GameEntity && ssw.GameEntity.GlobalPosition.DistanceSquared(globalPosition) < radiusSquared
			select (ssw);
		return first.Concat(second).Distinct();
	}

	protected internal override void OnEditorInit()
	{
		base.OnEditorInit();
		_weapons = null;
	}

	protected internal override void OnEditorTick(float dt)
	{
		base.OnEditorTick(dt);
		foreach (GameEntity highlightedEntite in _highlightedEntites)
		{
			highlightedEntite.SetContourColor(null);
		}
		_highlightedEntites.Clear();
		if (!MBEditor.IsEntitySelected(base.GameEntity))
		{
			return;
		}
		uint num = 4294901760u;
		if (Radius > 0f)
		{
			DebugExtensions.RenderDebugCircleOnTerrain(base.Scene, base.GameEntity.GetGlobalFrame(), Radius, num);
		}
		foreach (SpawnerBase item in GetSpawnersForEditor())
		{
			item.GameEntity.SetContourColor(num);
			_highlightedEntites.Add(TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item.GameEntity));
		}
	}

	private void OnDeploymentStateChangedAux(SynchedMissionObject targetObject)
	{
		if (IsDeployed)
		{
			targetObject.SetVisibleSynched(value: true);
			targetObject.SetPhysicsStateSynched(value: true);
		}
		else
		{
			targetObject.SetVisibleSynched(value: false);
			targetObject.SetPhysicsStateSynched(value: false);
		}
		this.OnDeploymentStateChanged?.Invoke(this, targetObject);
		if (targetObject is SiegeWeapon siegeWeapon)
		{
			siegeWeapon.OnDeploymentStateChanged(IsDeployed);
		}
	}

	public void Deploy(Type t)
	{
		DeployedWeapon = _weapons.First((SynchedMissionObject w) => MissionSiegeWeaponsController.GetWeaponType(w) == t);
		OnDeploymentStateChangedAux(DeployedWeapon);
		ToggleDeploymentPointVisibility(visible: false);
		ToggleDeployedWeaponVisibility(visible: true);
		this.OnDeployOrDisband?.Invoke(this);
	}

	public void Deploy(SiegeWeapon s)
	{
		DeployedWeapon = s;
		DisbandedWeapon = null;
		OnDeploymentStateChangedAux(s);
		ToggleDeploymentPointVisibility(visible: false);
		ToggleDeployedWeaponVisibility(visible: true);
		this.OnDeployOrDisband?.Invoke(this);
	}

	public ScriptComponentBehavior Disband()
	{
		ToggleDeploymentPointVisibility(visible: true);
		ToggleDeployedWeaponVisibility(visible: false);
		DisbandedWeapon = DeployedWeapon;
		DeployedWeapon = null;
		OnDeploymentStateChangedAux(DisbandedWeapon);
		this.OnDeployOrDisband?.Invoke(this);
		return DisbandedWeapon;
	}

	public void Hide()
	{
		ToggleDeploymentPointVisibility(visible: false);
		foreach (SynchedMissionObject item in GetWeaponsUnder())
		{
			if (item != null)
			{
				item.SetVisibleSynched(value: false);
				item.SetPhysicsStateSynched(value: false);
			}
		}
	}

	public void Show()
	{
		ToggleDeploymentPointVisibility(!IsDeployed);
		if (IsDeployed)
		{
			ToggleDeployedWeaponVisibility(visible: true);
		}
	}

	private void ToggleDeploymentPointVisibility(bool visible)
	{
		SetVisibleSynched(visible);
		SetPhysicsStateSynched(visible);
	}

	private void ToggleDeployedWeaponVisibility(bool visible)
	{
		ToggleWeaponVisibility(visible, DeployedWeapon);
	}

	public void ToggleWeaponVisibility(bool visible, SynchedMissionObject weapon)
	{
		WeakGameEntity weakGameEntity = weapon?.GameEntity.Parent ?? WeakGameEntity.Invalid;
		SynchedMissionObject synchedMissionObject = (weakGameEntity.IsValid ? weakGameEntity.GetFirstScriptOfType<SynchedMissionObject>() : null);
		if (synchedMissionObject != null)
		{
			synchedMissionObject.SetVisibleSynched(visible);
			synchedMissionObject.SetPhysicsStateSynched(visible);
		}
		else
		{
			weapon?.SetVisibleSynched(visible);
			weapon?.SetPhysicsStateSynched(visible);
		}
		if (!(weapon is SiegeWeapon) || !weapon.GameEntity.Parent.IsValid)
		{
			return;
		}
		foreach (WeakGameEntity child in weapon.GameEntity.Parent.GetChildren())
		{
			SiegeMachineStonePile firstScriptOfType = child.GetFirstScriptOfType<SiegeMachineStonePile>();
			if (firstScriptOfType != null)
			{
				firstScriptOfType.SetPhysicsStateSynched(visible);
				break;
			}
		}
	}

	public void HideAllWeapons()
	{
		foreach (SynchedMissionObject deployableWeapon in DeployableWeapons)
		{
			ToggleWeaponVisibility(visible: false, deployableWeapon);
		}
	}
}

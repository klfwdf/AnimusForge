using System;
using System.Collections.Generic;
using System.Linq;
using NavalDLC.GameComponents;
using NavalDLC.Missions.AI.Behaviors;
using NavalDLC.Missions.AI.TeamAI;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Missions.ShipActuators;
using NavalDLC.Missions.ShipControl;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

public class NavalShipsLogic : MissionLogic, IVehicleHandler, IMissionBehavior
{
	public enum NavalBoundaryCheckType
	{
		HardBoundary,
		DeploymentBoundary
	}

	public delegate bool ShipFilter(ShipAssignment assignment);

	public const int MaxTeamShipDeploymentLimit = 8;

	private readonly Dictionary<int, Mission.Missile> _shipSiegeEngineMissileDictionary;

	private readonly Dictionary<int, float> _missileHittingSailDictionary;

	private readonly ShipAssignment[,] _shipAssignments;

	private readonly MBList<MissionShip> _allShips;

	private readonly NavalShipDeploymentLimit[] _teamDeploymentLimits;

	private readonly MBList<MissionShip> _removedShipsPool;

	private int _shipIndexGenerator;

	private readonly PriorityQueue<int, (int distance, Vec2i index)> _tmpCollisionFreeFrameSearchQueue;

	public MissionShip PlayerControlledShip { get; private set; }

	public bool SeaPathfindingEnabled { get; private set; }

	public bool IsTeleportingShips { get; private set; }

	public bool IsMissionEnding { get; private set; }

	public bool IsDeploymentMode { get; private set; }

	public MBReadOnlyList<MissionShip> AllShips => _allShips;

	public bool CanHaveConnectionCooldown { get; private set; } = true;

	public event Action<MissionShip> ShipSpawnedEvent;

	public event Action<MissionShip, Formation> BeforeShipTransferredToFormationEvent;

	public event Action<MissionShip, Formation> ShipTransferredToFormationEvent;

	public event Action<MissionShip, Team, Formation> BeforeShipTransferredToTeamEvent;

	public event Action<MissionShip, Team, Formation> ShipTransferredToTeamEvent;

	public event Action<MissionShip, MissionShip, Formation, Formation> ShipCapturedEvent;

	public event Action<MissionShip> ShipSunkEvent;

	public event Action<ShipAttachmentMachine, ShipAttachmentPointMachine> ShipAttachmentBrokenEvent;

	public event Action<MissionShip, MatrixFrame, MatrixFrame> ShipTeleportedEvent;

	public event Action<MissionShip> BeforeShipRemovedEvent;

	public event Action<MissionShip> ShipRemovedEvent;

	public event Action<MissionShip> ShipControllerChanged;

	public event Action<MissionShip, MissionShip, float, bool, CapsuleData, int> ShipRammingEvent;

	public event Action<MissionShip, MissionShip> ShipsConnectedEvent;

	public event Action<MissionShip, WeakGameEntity, BodyFlags, Vec3, Vec3, bool> ShipCollisionEvent;

	public event Action<MissionShip, MissionShip> ShipHookThrowEvent;

	public event Action MissionEndEvent;

	public event Action<MissionShip, Agent, int, Vec3, Vec3, MissionWeapon, int> ShipHitEvent;

	public event Action<MissionShip> ShipBurnedEvent;

	public event Action<MissionShip> ShipPreparedForAbandonmentEvent;

	public event Action<MissionShip> SailsDeadEvent;

	public event Action<MissionShip> ShipLowHealthEvent;

	public event Action<MissionShip, MissionShip, float, float> ShipAboutToBeRammedEvent;

	public event Action<MissionShip, MissionShip> ShipAttachmentLostEvent;

	public event Action<MissionShip, MissionShip> BridgeConnectedEvent;

	public event Action<MissionShip> CutLooseOrderEvent;

	public event Action<MissionShip, MissionShip> BoardingOrderEvent;

	public event Action<Mission.Missile> AddShipSiegeEngineMissileEvent;

	public NavalShipsLogic()
	{
		_shipIndexGenerator = 0;
		_allShips = new MBList<MissionShip>();
		_tmpCollisionFreeFrameSearchQueue = new PriorityQueue<int, (int, Vec2i)>();
		_removedShipsPool = new MBList<MissionShip>();
		_shipAssignments = new ShipAssignment[3, 11];
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 11; j++)
			{
				_shipAssignments[i, j] = ShipAssignment.Create((TeamSideEnum)i, (FormationClass)j);
			}
		}
		int num = 3;
		_teamDeploymentLimits = new NavalShipDeploymentLimit[num];
		for (int k = 0; k < num; k++)
		{
			_teamDeploymentLimits[k] = NavalShipDeploymentLimit.Invalid();
		}
		_shipSiegeEngineMissileDictionary = new Dictionary<int, Mission.Missile>();
		_missileHittingSailDictionary = new Dictionary<int, float>();
	}

	public bool IsMissileFromShipSiegeEngine(int missileIndex)
	{
		return _shipSiegeEngineMissileDictionary.ContainsKey(missileIndex);
	}

	public void AddShipSiegeEngineMissile(Mission.Missile missile)
	{
		if (!_shipSiegeEngineMissileDictionary.ContainsKey(missile.Index))
		{
			_shipSiegeEngineMissileDictionary.Add(missile.Index, missile);
			this.AddShipSiegeEngineMissileEvent?.Invoke(missile);
		}
	}

	private void AddMissileHittingSail(Mission.Missile missile)
	{
		if (!_missileHittingSailDictionary.ContainsKey(missile.Index))
		{
			_missileHittingSailDictionary.Add(missile.Index, missile.GetVelocity().Length);
		}
	}

	public float GetMissileVelocityLengthOnFirstSailHit(int missileIndex)
	{
		if (_missileHittingSailDictionary.TryGetValue(missileIndex, out var value))
		{
			return value;
		}
		return -1f;
	}

	public override void OnMissileRemoved(int MissileIndex)
	{
		base.OnMissileRemoved(MissileIndex);
		if (_shipSiegeEngineMissileDictionary.ContainsKey(MissileIndex))
		{
			_shipSiegeEngineMissileDictionary.Remove(MissileIndex);
		}
		else if (_missileHittingSailDictionary.ContainsKey(MissileIndex))
		{
			_missileHittingSailDictionary.Remove(MissileIndex);
		}
	}

	public override void OnDeploymentFinished()
	{
	}

	public override void OnBehaviorInitialize()
	{
		Mission.Current.OnMissileRemovedEvent += OnMissileRemoved;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		MissionShipFactory.ResetShipUniqueBitwiseIDNext();
		SeaPathfindingEnabled = (base.Mission.IsNavalBattle || base.Mission.IsNavalRaidBattle) && base.Mission.Scene.SetAbilityOfFacesWithId(1, isEnabled: false) > 0;
	}

	public override void OnMissionTick(float dt)
	{
		foreach (Mission.Missile missiles in base.Mission.MissilesList)
		{
			WeaponComponentData currentUsageItem = missiles.Weapon.CurrentUsageItem;
			if (currentUsageItem == null || !currentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.Burning))
			{
				continue;
			}
			Vec3 missileOldPosition = missiles.GetOldPosition();
			Vec3 missilePosition = missiles.GetPosition();
			MissionSail missionSail = null;
			foreach (MissionShip allShip in AllShips)
			{
				missionSail = allShip.CheckHitSails(missiles.ShooterAgent, missiles, in missileOldPosition, in missilePosition, missiles.Weapon);
				if (missionSail != null)
				{
					HandleSailsHit(allShip, missionSail, missiles.ShooterAgent, missiles, missiles.Weapon);
					missiles.PassThroughEntity(missionSail.SailEntity);
				}
			}
		}
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		IsMissionEnding = true;
		this.MissionEndEvent?.Invoke();
		foreach (MissionShip item in _allShips.ToList())
		{
			RemoveShip(item);
		}
		SetDeploymentMode(value: false);
		Mission.Current.OnMissileRemovedEvent -= OnMissileRemoved;
		if (MissionGameModels.Current?.AgentStatCalculateModel is NavalAgentStatCalculateModel navalAgentStatCalculateModel)
		{
			navalAgentStatCalculateModel.ClearMissionState();
		}
	}

	public void OnShipControllerChanged(MissionShip ship)
	{
		if (ship.IsPlayerControlled && PlayerControlledShip != ship)
		{
			PlayerControlledShip = ship;
		}
		else if (!ship.IsPlayerControlled && PlayerControlledShip == ship)
		{
			PlayerControlledShip = null;
		}
		this.ShipControllerChanged?.Invoke(ship);
	}

	public void OnShipSunk(MissionShip ship)
	{
		this.ShipSunkEvent?.Invoke(ship);
	}

	public void OnAttachmentBroken(ShipAttachmentMachine attachmentMachine, ShipAttachmentPointMachine attachmentPointMachine)
	{
		this.ShipAttachmentBrokenEvent?.Invoke(attachmentMachine, attachmentPointMachine);
	}

	public void OnShipCollision(MissionShip ship, WeakGameEntity targetEntity, BodyFlags bodyFlags, Vec3 averageContactPoint, Vec3 totalImpulseOnShip, bool isFirstImpact)
	{
		this.ShipCollisionEvent?.Invoke(ship, targetEntity, bodyFlags, averageContactPoint, totalImpulseOnShip, isFirstImpact);
	}

	public void OnShipRamming(MissionShip rammingShip, MissionShip rammedShip, float damagePercent, bool isFirstImpact, CapsuleData capsuleData, int ramQuality)
	{
		this.ShipRammingEvent?.Invoke(rammingShip, rammedShip, damagePercent, isFirstImpact, capsuleData, ramQuality);
	}

	public void OnShipsConnected(MissionShip ownerShip, MissionShip targetShip)
	{
		this.ShipsConnectedEvent?.Invoke(ownerShip, targetShip);
	}

	public void OnSuccessfulHookThrow(MissionShip hookingShip, MissionShip hookedShip)
	{
		this.ShipHookThrowEvent?.Invoke(hookingShip, hookedShip);
	}

	public void OnShipHit(MissionShip ship, Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex)
	{
		this.ShipHitEvent?.Invoke(ship, attackerAgent, damage, impactPosition, impactDirection, weapon, affectorWeaponSlotOrMissileIndex);
	}

	public void OnShipPreparedForAbandonment(MissionShip ship)
	{
		this.ShipPreparedForAbandonmentEvent?.Invoke(ship);
	}

	public void OnShipBurned(MissionShip ship)
	{
		this.ShipBurnedEvent?.Invoke(ship);
	}

	public void OnShipAttachmentLost(MissionShip hookingShip, MissionShip hookedShip)
	{
		this.ShipAttachmentLostEvent?.Invoke(hookingShip, hookedShip);
	}

	public void OnSailsDead(MissionShip ship)
	{
		this.SailsDeadEvent?.Invoke(ship);
	}

	public void OnShipLowHealth(MissionShip ship)
	{
		this.ShipLowHealthEvent?.Invoke(ship);
	}

	public void OnShipAboutToBeRammed(MissionShip rammingShip, MissionShip rammedShip, float distance, float speedInRamDirection)
	{
		this.ShipAboutToBeRammedEvent?.Invoke(rammingShip, rammedShip, distance, speedInRamDirection);
	}

	public void OnCutLooseOrder(MissionShip ship)
	{
		this.CutLooseOrderEvent?.Invoke(ship);
	}

	public void OnBoardingOrder(MissionShip boardingShip, MissionShip boardedShip)
	{
		this.BoardingOrderEvent?.Invoke(boardingShip, boardedShip);
	}

	public void OnBridgeConnected(MissionShip sourceShip, MissionShip targetShip)
	{
		this.BridgeConnectedEvent?.Invoke(sourceShip, targetShip);
	}

	public void SetDeploymentMode(bool value)
	{
		if (value == IsDeploymentMode)
		{
			return;
		}
		IsDeploymentMode = value;
		if (value)
		{
			return;
		}
		foreach (MissionShip item in _removedShipsPool)
		{
			base.Mission.Scene.RemoveEntity(item.GameEntity, 121);
		}
		_removedShipsPool.Clear();
	}

	public int GetNumTeamShips(TeamSideEnum teamSide)
	{
		int num = 0;
		for (int i = 0; i < 11; i++)
		{
			if (_shipAssignments[(int)teamSide, i].HasMissionShip)
			{
				num++;
			}
		}
		return num;
	}

	public bool GetShip(Formation formation, out MissionShip ship)
	{
		return GetShip(formation.Team.TeamSide, formation.FormationIndex, out ship);
	}

	public bool GetShip(TeamSideEnum teamSide, FormationClass formationIndex, out MissionShip ship)
	{
		ShipAssignment shipAssignment = GetShipAssignment(teamSide, formationIndex);
		if (shipAssignment.HasMissionShip)
		{
			ship = shipAssignment.MissionShip;
			return true;
		}
		ship = null;
		return false;
	}

	public void FillTeamShips(TeamSideEnum teamSide, MBList<MissionShip> teamShips)
	{
		for (int i = 0; i < 11; i++)
		{
			ShipAssignment shipAssignment = _shipAssignments[(int)teamSide, i];
			if (shipAssignment.HasMissionShip)
			{
				teamShips.Add(shipAssignment.MissionShip);
			}
		}
	}

	public ShipAssignment GetShipAssignment(TeamSideEnum teamSide, FormationClass formationIndex)
	{
		return _shipAssignments[(int)teamSide, (int)formationIndex];
	}

	public bool GetShipAssignmentWithShipIndex(int shipIndex, out ShipAssignment shipAssignment)
	{
		ShipAssignment[,] shipAssignments = _shipAssignments;
		foreach (ShipAssignment shipAssignment2 in shipAssignments)
		{
			if (shipAssignment2.HasMissionShip && shipAssignment2.MissionShip.Index == shipIndex)
			{
				shipAssignment = shipAssignment2;
				return true;
			}
		}
		shipAssignment = null;
		return false;
	}

	public int GetCountOfSetShipAssignments(TeamSideEnum teamSide)
	{
		int num = 0;
		ShipAssignment[,] shipAssignments = _shipAssignments;
		foreach (ShipAssignment shipAssignment in shipAssignments)
		{
			if (shipAssignment.TeamSide == teamSide && shipAssignment.IsSet)
			{
				num++;
			}
		}
		return num;
	}

	public bool GetShipWithShipIndex(int shipIndex, out MissionShip missionShip)
	{
		if (GetShipAssignmentWithShipIndex(shipIndex, out var shipAssignment))
		{
			missionShip = shipAssignment.MissionShip;
			return true;
		}
		missionShip = null;
		return false;
	}

	internal MissionShip GetConnectedTeamShip(TeamSideEnum teamSide, ulong shipUniqueBitwiseID)
	{
		MissionShip result = null;
		for (int i = 0; i < 11; i++)
		{
			ShipAssignment shipAssignment = _shipAssignments[(int)teamSide, i];
			if (!shipAssignment.HasMissionShip)
			{
				continue;
			}
			MissionShip missionShip = shipAssignment.MissionShip;
			if ((missionShip.ShipIslandCombinedID & shipUniqueBitwiseID) != 0L)
			{
				if (missionShip.ShipUniqueBitwiseID == shipUniqueBitwiseID)
				{
					return missionShip;
				}
				result = missionShip;
			}
		}
		return result;
	}

	internal MissionShip GetNearestTeamShip(TeamSideEnum teamSide, in Vec3 position, float maxDistance = float.MaxValue, Func<MissionShip, bool> shipFilter = null)
	{
		MissionShip result = null;
		float num = maxDistance;
		for (int i = 0; i < 11; i++)
		{
			ShipAssignment shipAssignment = _shipAssignments[(int)teamSide, i];
			if (!shipAssignment.HasMissionShip)
			{
				continue;
			}
			MissionShip missionShip = shipAssignment.MissionShip;
			if (shipFilter == null || shipFilter(missionShip))
			{
				float num2 = missionShip.GlobalFrame.origin.DistanceSquared(position);
				if (num2 <= num)
				{
					num = num2;
					result = missionShip;
				}
			}
		}
		return result;
	}

	public void FillClosestShips(in MatrixFrame shipEntityGlobalFrame, float distanceLimit, MBList<(MissionShip ship, OarSidePhaseController.OarSide shipSide)> closestShips, MissionShip ignoreShip = null)
	{
		foreach (MissionShip allShip in _allShips)
		{
			if (allShip != null && allShip != ignoreShip)
			{
				float num = distanceLimit + Vec2.Max(Vec2.Abs(allShip.Physics.PhysicsBoundingBoxWithChildren.max.AsVec2), Vec2.Abs(allShip.Physics.PhysicsBoundingBoxWithChildren.min.AsVec2)).Length;
				Vec3 origin = allShip.GameEntity.GetBodyWorldTransform().origin;
				if (shipEntityGlobalFrame.origin.DistanceSquared(origin) <= num * num)
				{
					Vec3 vb = origin - shipEntityGlobalFrame.origin;
					OarSidePhaseController.OarSide item = ((!(Vec3.CrossProduct(shipEntityGlobalFrame.rotation.f, vb).z >= 0f)) ? OarSidePhaseController.OarSide.Right : OarSidePhaseController.OarSide.Left);
					closestShips.Add((allShip, item));
				}
			}
		}
	}

	public MatrixFrame GetMeanFrameOfTeamShips(TeamSideEnum teamSide)
	{
		Vec3 o = Vec3.Zero;
		Vec3 vec = Vec3.Forward;
		int num = 0;
		ShipAssignment[,] shipAssignments = _shipAssignments;
		foreach (ShipAssignment shipAssignment in shipAssignments)
		{
			if (shipAssignment.HasMissionShip && shipAssignment.TeamSide == teamSide)
			{
				MatrixFrame globalFrame = shipAssignment.MissionShip.GlobalFrame;
				o += globalFrame.origin;
				float percent = 1f / ((float)num + 1f);
				vec = Vec3.Slerp(vec, globalFrame.rotation.f, percent);
				num++;
			}
		}
		Mat3 rot = Mat3.Identity;
		rot.f = vec;
		rot.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		return new MatrixFrame(in rot, in o);
	}

	public bool GetCollisionFreeShipFrame(in MatrixFrame shipFrame, in Vec2 shipDimensions, out MatrixFrame collisionFreeFrame, bool checkBoundaries = true, NavalBoundaryCheckType boundaryToCheck = NavalBoundaryCheckType.HardBoundary, Team team = null, float searchCellWidth = 10f, int searchCellDistance = 10, float clearanceMargin = 1f, int ignoreShipIndex = -1)
	{
		Vec2 globalForward = shipFrame.rotation.f.AsVec2.Normalized();
		Vec2 asVec = shipFrame.origin.AsVec2;
		searchCellWidth = TaleWorlds.Library.MathF.Max(1f, searchCellWidth);
		searchCellDistance = TaleWorlds.Library.MathF.Max(0, searchCellDistance);
		clearanceMargin = TaleWorlds.Library.MathF.Max(0f, clearanceMargin);
		_tmpCollisionFreeFrameSearchQueue.Clear();
		_tmpCollisionFreeFrameSearchQueue.Enqueue(0, (0, Vec2i.Zero));
		bool flag = false;
		Oriented2DArea area = new Oriented2DArea(in Vec2.Zero, in globalForward, in shipDimensions);
		do
		{
			(int, Vec2i) value = _tmpCollisionFreeFrameSearchQueue.Dequeue().Value;
			Vec2i item = value.Item2;
			Vec2 globalCenter = asVec + area.GlobalForward.RightVec() * searchCellWidth * item.X + area.GlobalForward * searchCellWidth * item.Y;
			area.SetGlobalCenter(in globalCenter);
			bool flag2 = !checkBoundaries || IsPositionInsideBoundaries(in globalCenter, boundaryToCheck, team);
			flag = flag2 && IsAreaFreeOfShipCollision(in area, clearanceMargin, ignoreShipIndex);
			if (flag || value.Item1 >= searchCellDistance)
			{
				continue;
			}
			int num = value.Item1 + 1;
			int priority = num + ((!flag2) ? searchCellDistance : 0);
			if (item.X <= 0)
			{
				_tmpCollisionFreeFrameSearchQueue.Enqueue(priority, (num, new Vec2i(item.X - 1, item.Y)));
			}
			if (item.X >= 0)
			{
				_tmpCollisionFreeFrameSearchQueue.Enqueue(priority, (num, new Vec2i(item.X + 1, item.Y)));
			}
			if (item.X == 0)
			{
				if (item.Y >= 0)
				{
					_tmpCollisionFreeFrameSearchQueue.Enqueue(priority, (num, new Vec2i(item.X, item.Y + 1)));
				}
				if (item.Y <= 0)
				{
					_tmpCollisionFreeFrameSearchQueue.Enqueue(priority, (num, new Vec2i(item.X, item.Y - 1)));
				}
			}
		}
		while (!_tmpCollisionFreeFrameSearchQueue.IsEmpty() && !flag);
		collisionFreeFrame = MatrixFrame.Identity;
		if (flag)
		{
			Vec3 origin = area.GlobalCenter.ToVec3();
			origin.z = base.Mission.Scene.GetWaterLevelAtPosition(area.GlobalCenter, useWaterRenderer: true, checkWaterBodyEntities: false);
			collisionFreeFrame.origin = origin;
			collisionFreeFrame.rotation.f = globalForward.ToVec3();
			collisionFreeFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
		}
		return flag;
	}

	public bool IsShipFrameCollisionFree(in MatrixFrame frame, in Vec2 localDimensions, bool checkBoundaries = true, NavalBoundaryCheckType boundaryToCheck = NavalBoundaryCheckType.HardBoundary, Team team = null, float clearanceMargin = 1f, int ignoreShipIndex = -1)
	{
		Vec2 globalCenter = frame.origin.AsVec2;
		Oriented2DArea area = new Oriented2DArea(in globalCenter, frame.rotation.f.AsVec2.Normalized(), in localDimensions);
		if (!checkBoundaries || IsPositionInsideBoundaries(in globalCenter, boundaryToCheck, team))
		{
			return IsAreaFreeOfShipCollision(in area, clearanceMargin, ignoreShipIndex);
		}
		return false;
	}

	public float ComputeSpawnPathDeploymentOffset(Path path)
	{
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 11; j++)
			{
				if (_shipAssignments[i, j].IsSet)
				{
					num++;
				}
			}
		}
		float num2 = 400f;
		if (num > 2)
		{
			int num3 = 14;
			float x = (float)Math.Min(num - 2, num3) / (float)num3;
			num2 += 460f * TaleWorlds.Library.MathF.Pow(x, 0.6f);
		}
		num2 = TaleWorlds.Library.MathF.Min(path.GetTotalLength(), num2);
		return (0f - num2) / 2f + 1f;
	}

	public bool FindAssignmentOfShipOrigin(IShipOrigin shipOrigin, out ShipAssignment shipAssignment)
	{
		shipAssignment = null;
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 11; j++)
			{
				ShipAssignment shipAssignment2 = _shipAssignments[i, j];
				if (shipAssignment2.HasMissionShip && shipAssignment2.MissionShip.ShipOrigin == shipOrigin)
				{
					shipAssignment = shipAssignment2;
					return true;
				}
			}
		}
		return false;
	}

	public void SetTeleportShips(bool value)
	{
		IsTeleportingShips = value;
	}

	public void TeleportShip(MissionShip ship, MatrixFrame targetFrame, bool checkFreeArea, bool anchorShip = false, bool snapToWater = true)
	{
		MatrixFrame globalFrame = ship.GameEntity.GetGlobalFrame();
		TeleportShipAux(ship, ref targetFrame, checkFreeArea, anchorShip, snapToWater);
		this.ShipTeleportedEvent?.Invoke(ship, globalFrame, targetFrame);
	}

	public bool IsAreaFreeOfShipCollision(in Oriented2DArea area, float clearanceMargin = 1f, int ignoreShipIndex = -1)
	{
		foreach (MissionShip allShip in _allShips)
		{
			if (allShip.Index != ignoreShipIndex && allShip.Physics.NavalSinkingState != NavalDLC.Missions.NavalPhysics.NavalPhysics.SinkingState.Sunk)
			{
				Oriented2DArea otherArea = allShip.Physics.GetGlobalMaximal2DArea();
				if (area.Overlaps(in otherArea, clearanceMargin))
				{
					return false;
				}
			}
		}
		return true;
	}

	public bool IsAShipAssignedToFormation(Formation formation)
	{
		return GetShipAssignment(formation.Team.TeamSide, formation.FormationIndex).HasMissionShip;
	}

	public bool IsShipAssignedToFormation(MissionShip ship, Formation formation)
	{
		ShipAssignment shipAssignment = GetShipAssignment(formation.Team.TeamSide, formation.FormationIndex);
		if (shipAssignment.HasMissionShip)
		{
			return shipAssignment.MissionShip == ship;
		}
		return false;
	}

	public void ClearShipAssignments()
	{
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 11; j++)
			{
				TeamSideEnum teamSide = (TeamSideEnum)i;
				ClearShipAssignment(teamSide, j);
			}
		}
	}

	public ShipAssignment SetShipAssignment(TeamSideEnum teamSide, FormationClass formationIndex, IShipOrigin shipOrigin)
	{
		ShipAssignment shipAssignment = _shipAssignments[(int)teamSide, (int)formationIndex];
		shipAssignment.Set(shipOrigin);
		return shipAssignment;
	}

	public MissionShip SpawnShip(IShipOrigin shipOrigin, in MatrixFrame shipFrame, Team team, Formation formation = null, bool spawnAnchored = false, FormationClass formationSearchRange = FormationClass.NumberOfRegularFormations, bool checkForFreeArea = true)
	{
		if (formation == null)
		{
			formation = FindFirstFormationWithoutShip(team, formationSearchRange);
		}
		TeamSideEnum teamSide = team.TeamSide;
		FormationClass formationIndex = formation.FormationIndex;
		GetShipAssignment(teamSide, formationIndex).Set(shipOrigin);
		return SpawnShip(formation, in shipFrame, spawnAnchored, checkForFreeArea);
	}

	public MissionShip SpawnShip(Formation formation, in MatrixFrame spawnFrame, bool spawnAnchored = true, bool checkForFreeArea = true)
	{
		TeamSideEnum teamSide = formation.Team.TeamSide;
		FormationClass formationIndex = formation.FormationIndex;
		int num = (int)teamSide;
		ShipAssignment shipAssignment = _shipAssignments[num, (int)formationIndex];
		MatrixFrame shipFrame = spawnFrame;
		IMissionDeploymentPlan deploymentPlan = base.Mission.DeploymentPlan;
		bool flag = deploymentPlan.IsPlanMade(formation.Team);
		if (shipFrame.IsZero && flag)
		{
			shipFrame = deploymentPlan.GetFormationsCenterFrameAndExtents(formation.Team, out var _);
		}
		if (shipFrame.IsZero)
		{
			shipFrame = GetMeanFrameOfTeamShips(teamSide);
		}
		if (checkForFreeArea && GetCollisionFreeShipFrame(in shipFrame, shipAssignment.MissionShipObject.DeploymentArea, out var collisionFreeFrame, checkBoundaries: true, flag ? NavalBoundaryCheckType.DeploymentBoundary : NavalBoundaryCheckType.HardBoundary, formation.Team, 1f, 400))
		{
			shipFrame = collisionFreeFrame;
		}
		float waterLevelAtPosition = base.Mission.Scene.GetWaterLevelAtPosition(shipFrame.origin.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		shipFrame.origin.z = waterLevelAtPosition;
		MissionShip missionShip = null;
		MissionShip missionShip2 = _removedShipsPool.FirstOrDefaultQ((MissionShip ship) => ship.ShipOrigin == shipAssignment.ShipOrigin);
		if (missionShip2 != null)
		{
			_removedShipsPool.Remove(missionShip2);
			missionShip2.SetRemoved(value: false);
			missionShip2.SetEnabledAndMakeVisible(isParentObject: true, enableFaces: true);
			missionShip2.SetFormation(formation);
			shipAssignment.SetMissionShip(missionShip2);
			missionShip = missionShip2;
			missionShip.ResetFormationPositioning();
		}
		else
		{
			(uint, uint)? defaultSailColors = null;
			if (formation.Team != null)
			{
				defaultSailColors = (formation.Team.Color, formation.Team.Color2);
			}
			MissionShipFactory.CreateMissionShip(_shipIndexGenerator, shipAssignment, this, in shipFrame, defaultSailColors);
			_shipIndexGenerator++;
			missionShip = shipAssignment.MissionShip;
		}
		missionShip.SetOriginalTeamSide(formation.Team.TeamSide);
		_allShips.Add(missionShip);
		if (missionShip2 != null)
		{
			TeleportShipAux(missionShip, ref shipFrame, checkFreeArea: false);
		}
		if (spawnAnchored)
		{
			missionShip.SetAnchor(isAnchored: true, anchorInPlace: true);
		}
		this.ShipSpawnedEvent?.Invoke(missionShip);
		return missionShip;
	}

	public void RemoveShip(MissionShip ship)
	{
		FindAssignmentOfShipOrigin(ship.ShipOrigin, out var shipAssignment);
		if (shipAssignment != null)
		{
			RemoveShipAux(shipAssignment);
		}
	}

	public void RemoveShip(Formation formation)
	{
		TeamSideEnum teamSide = formation.Team.TeamSide;
		ShipAssignment shipAssignment = GetShipAssignment(teamSide, formation.FormationIndex);
		RemoveShipAux(shipAssignment);
	}

	public void TransferShipToFormation(MissionShip ship, Formation toFormation)
	{
		FindAssignmentOfShipOrigin(ship.ShipOrigin, out var shipAssignment);
		TransferShipToFormation(ship.ShipOrigin, shipAssignment.Formation, toFormation);
	}

	public void TransferShipToTeam(MissionShip ship, Team targetTeam, Formation targetFormation = null, FormationClass searchRange = FormationClass.NumberOfRegularFormations)
	{
		ShipAssignment shipAssignment;
		bool flag = FindAssignmentOfShipOrigin(ship.ShipOrigin, out shipAssignment);
		if (targetFormation == null)
		{
			targetFormation = FindFirstFormationWithoutShip(targetTeam, searchRange);
		}
		this.BeforeShipTransferredToTeamEvent?.Invoke(ship, targetTeam, targetFormation);
		Team team = ship.Team;
		Formation formation = ship.Formation;
		ShipAssignment shipAssignment2 = GetShipAssignment(targetFormation.Team.TeamSide, targetFormation.FormationIndex);
		if (flag)
		{
			shipAssignment.RemoveShip();
			shipAssignment.Clear();
		}
		shipAssignment2.Set(ship.ShipOrigin);
		shipAssignment2.SetMissionShip(ship);
		ship.SetFormation(targetFormation);
		this.ShipTransferredToTeamEvent?.Invoke(ship, team, formation);
	}

	private void RefreshTeamAIBehaviorShipReferences(Team team)
	{
		foreach (Formation item in team.FormationsIncludingSpecialAndEmpty)
		{
			if (item.AI == null)
			{
				continue;
			}
			for (int i = 0; i < item.AI.BehaviorCount; i++)
			{
				if (item.AI.GetBehaviorAtIndex(i) is NavalBehaviorComponent navalBehaviorComponent)
				{
					navalBehaviorComponent.RefreshShipReferences();
				}
			}
		}
	}

	public void OnShipCaptured(MissionShip ship, Formation targetFormation)
	{
		Formation formation = ship.Formation;
		ShipAssignment shipAssignment;
		bool num = FindAssignmentOfShipOrigin(ship.ShipOrigin, out shipAssignment);
		ShipAssignment shipAssignment2 = GetShipAssignment(targetFormation.Team.TeamSide, targetFormation.FormationIndex);
		MissionShip missionShip = shipAssignment2.MissionShip;
		shipAssignment2.SetMissionShip(ship);
		ship.SetFormation(null);
		missionShip.SetFormation(null);
		if (num)
		{
			shipAssignment.RemoveShip();
			shipAssignment.Clear();
		}
		ship.SetFormation(targetFormation);
		ship.SetController(missionShip.Controller?.ControllerType ?? ShipControllerType.None);
		missionShip.SetController(ShipControllerType.None);
		foreach (MissionShip allShip in AllShips)
		{
			allShip.ShipOrder.OnShipCaptured(ship, missionShip);
		}
		((TeamAINavalComponent)(formation?.Team.TeamAI))?.TeamNavalQuerySystem.ForceExpireAll();
		((TeamAINavalComponent)targetFormation.Team.TeamAI).TeamNavalQuerySystem.ForceExpireAll();
		this.ShipCapturedEvent?.Invoke(ship, missionShip, formation, targetFormation);
		if (formation != null)
		{
			RefreshTeamAIBehaviorShipReferences(formation.Team);
		}
		RefreshTeamAIBehaviorShipReferences(targetFormation.Team);
	}

	public Formation FindFirstFormationWithoutShip(Team team, FormationClass searchRange = FormationClass.NumberOfRegularFormations)
	{
		int teamSide = (int)team.TeamSide;
		FormationClass formationClass = FormationClass.NumberOfAllFormationsWithUnset;
		for (int i = 0; i < (int)searchRange; i++)
		{
			ShipAssignment shipAssignment = _shipAssignments[teamSide, i];
			if (!shipAssignment.IsSet && !shipAssignment.HasMissionShip)
			{
				formationClass = shipAssignment.FormationIndex;
				break;
			}
		}
		if (formationClass != FormationClass.NumberOfAllFormationsWithUnset)
		{
			return team.GetFormation(formationClass);
		}
		return null;
	}

	public void SetTeamShipDeploymentLimit(TeamSideEnum teamSide, NavalShipDeploymentLimit deploymentLimit)
	{
		_teamDeploymentLimits[(int)teamSide] = deploymentLimit;
	}

	public void TransferShipToFormation(IShipOrigin shipOrigin, Formation fromFormation, Formation toFormation)
	{
		int teamSide = (int)fromFormation.Team.TeamSide;
		int formationIndex = (int)fromFormation.FormationIndex;
		int formationIndex2 = (int)toFormation.FormationIndex;
		ShipAssignment shipAssignment = _shipAssignments[teamSide, formationIndex];
		ShipAssignment shipAssignment2 = _shipAssignments[teamSide, formationIndex2];
		MissionShip missionShip = shipAssignment.MissionShip;
		this.BeforeShipTransferredToFormationEvent?.Invoke(missionShip, toFormation);
		if (!shipAssignment2.IsSet)
		{
			shipAssignment2.Set(missionShip.ShipOrigin);
		}
		shipAssignment2.SetMissionShip(missionShip);
		shipAssignment.RemoveShip();
		shipAssignment.Clear();
		missionShip.SetFormation(toFormation);
		missionShip.ResetFormationPositioning();
		this.ShipTransferredToFormationEvent?.Invoke(missionShip, fromFormation);
	}

	public int GetShipDeploymentLimit(TeamSideEnum teamSide, out NavalShipDeploymentLimit deploymentLimit)
	{
		deploymentLimit = _teamDeploymentLimits[(int)teamSide];
		return deploymentLimit.NetDeploymentLimit;
	}

	public void SetCanHaveConnectionCooldown(bool value)
	{
		CanHaveConnectionCooldown = value;
	}

	private void ClearShipAssignment(TeamSideEnum teamSide, int formationIndex)
	{
		_ = _shipAssignments[(int)teamSide, formationIndex];
		_shipAssignments[(int)teamSide, formationIndex].Clear();
	}

	bool IVehicleHandler.IsAgentInVehicle(Agent agent, out WeakGameEntity vehicleEntity)
	{
		foreach (MissionShip allShip in AllShips)
		{
			if (allShip.GetIsAgentOnShip(agent))
			{
				vehicleEntity = allShip.GameEntity;
				return true;
			}
		}
		vehicleEntity = WeakGameEntity.Invalid;
		return false;
	}

	private void RemoveShipAux(ShipAssignment shipAssignment)
	{
		MissionShip shipToRemove = shipAssignment.MissionShip;
		this.BeforeShipRemovedEvent?.Invoke(shipToRemove);
		shipAssignment.RemoveShip();
		shipAssignment.Clear();
		_allShips.RemoveAll((MissionShip s) => s == shipToRemove);
		this.ShipRemovedEvent?.Invoke(shipToRemove);
		shipToRemove.SetFormation(null);
		shipToRemove.SetRemoved(value: true);
		WeakGameEntity gameEntity = shipToRemove.GameEntity;
		if (IsDeploymentMode && !IsMissionEnding)
		{
			MatrixFrame frame = shipToRemove.GlobalFrame;
			frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			gameEntity.SetGlobalFrame(in frame);
			gameEntity.SetAngularVelocity(Vec3.Zero);
			gameEntity.SetLinearVelocity(Vec3.Zero);
			shipToRemove.SetDisabledAndMakeInvisible(isParentObject: true, disableFaces: true);
			_removedShipsPool.Add(shipToRemove);
		}
		else
		{
			base.Mission.Scene.RemoveEntity(gameEntity, 121);
		}
		foreach (MissionShip allShip in AllShips)
		{
			if (allShip != shipToRemove)
			{
				if (allShip.HasController && allShip.Controller.IsAIControlled && allShip.AIController.TargetShip == shipToRemove)
				{
					allShip.AIController.ClearTarget();
				}
				if (allShip.ShipSiegeWeapon != null && allShip.ShipSiegeWeapon.Ai != null)
				{
					(allShip.ShipSiegeWeapon.Ai as RangedSiegeWeaponAi).ResetThreatSeeker();
				}
			}
		}
	}

	private void TeleportShipAux(MissionShip ship, ref MatrixFrame targetFrame, bool checkFreeArea, bool anchorShip = false, bool snapToWater = true)
	{
		if (checkFreeArea && GetCollisionFreeShipFrame(in targetFrame, ship.MissionShipObject.DeploymentArea, out var collisionFreeFrame, checkBoundaries: true, (base.Mission.Mode == MissionMode.Deployment) ? NavalBoundaryCheckType.DeploymentBoundary : NavalBoundaryCheckType.HardBoundary, ship.Team, 1f, 400, 1f, ship.Index))
		{
			targetFrame = collisionFreeFrame;
		}
		if (snapToWater)
		{
			targetFrame.origin.z = base.Mission.Scene.GetWaterLevelAtPosition(new Vec2(targetFrame.origin.x, targetFrame.origin.y), useWaterRenderer: true, checkWaterBodyEntities: false);
			targetFrame.origin.z -= ship.Physics.StabilitySubmergedHeightOfShip;
		}
		ship.GameEntity.SetGlobalFrame(in targetFrame);
		ship.GameEntity.UpdateAttachedNavigationMeshFaces();
		ship.ResetFormationPositioning();
		if (anchorShip)
		{
			ship.SetAnchor(isAnchored: true, anchorInPlace: true);
		}
		if (ship.ShipOrder != null)
		{
			ship.ShipOrder.RefreshOrders();
		}
	}

	internal void HandleSailsHit(MissionShip ship, MissionSail sail, Agent attackerAgent, Mission.Missile missile, MissionWeapon missileWeapon)
	{
		float num = missileWeapon.CurrentUsageItem.FireDamage;
		float missileVelocityLengthOnFirstSailHit = GetMissileVelocityLengthOnFirstSailHit(missile.Index);
		if (missileVelocityLengthOnFirstSailHit < 0f)
		{
			AddMissileHittingSail(missile);
		}
		else
		{
			float length = missile.GetVelocity().Length;
			num *= length / missileVelocityLengthOnFirstSailHit;
		}
		float inflictedDamage = MissionGameModels.Current.AgentApplyDamageModel.CalculateSailFireDamage(attackerAgent, ship.ShipOrigin, num, IsMissileFromShipSiegeEngine(missile.Index));
		ship.DealDamageToSails(attackerAgent, num, inflictedDamage, sail);
	}

	public static bool IsPositionInsideBoundaries(in Vec2 position, NavalBoundaryCheckType boundaryType = NavalBoundaryCheckType.HardBoundary, Team team = null)
	{
		return boundaryType switch
		{
			NavalBoundaryCheckType.HardBoundary => Mission.Current.IsPositionInsideHardBoundaries(position), 
			NavalBoundaryCheckType.DeploymentBoundary => Mission.Current.DeploymentPlan.IsPositionInsideDeploymentBoundaries(team, in position), 
			_ => false, 
		};
	}

	private static bool FindAndRemoveClosestDeckFrameToPosition(MBList<MatrixFrame> deckFrames, in Vec3 position, out MatrixFrame foundFrame)
	{
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < deckFrames.Count; i++)
		{
			float lengthSquared = (deckFrames[i].origin - position).LengthSquared;
			if (lengthSquared <= num2)
			{
				num = i;
				num2 = lengthSquared;
			}
		}
		if (num >= 0)
		{
			foundFrame = deckFrames[num];
			Vec3 vec = 1f * Vec3.Up;
			foundFrame.origin += vec;
			deckFrames.RemoveAt(num);
			return true;
		}
		foundFrame = MatrixFrame.Identity;
		return false;
	}
}

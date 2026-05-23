using System;
using System.Collections.Generic;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace TaleWorlds.MountAndBlade;

public abstract class RangedSiegeWeapon : SiegeWeapon
{
	[DefineSynchedMissionObjectType(typeof(RangedSiegeWeapon))]
	public struct RangedSiegeWeaponRecord : ISynchedMissionObjectReadableRecord
	{
		public int State { get; private set; }

		public float TargetDirection { get; private set; }

		public float TargetReleaseAngle { get; private set; }

		public int AmmoCount { get; private set; }

		public int ProjectileIndex { get; private set; }

		public bool ReadFromNetwork(ref bool bufferReadValid)
		{
			State = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponStateCompressionInfo, ref bufferReadValid);
			TargetDirection = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.RadianCompressionInfo, ref bufferReadValid);
			TargetReleaseAngle = GameNetworkMessage.ReadFloatFromPacket(CompressionBasic.RadianCompressionInfo, ref bufferReadValid);
			AmmoCount = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponAmmoCompressionInfo, ref bufferReadValid);
			ProjectileIndex = GameNetworkMessage.ReadIntFromPacket(CompressionMission.RangedSiegeWeaponAmmoIndexCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}
	}

	public enum WeaponState
	{
		Invalid = -1,
		Idle,
		WaitingBeforeProjectileLeaving,
		Shooting,
		WaitingAfterShooting,
		WaitingBeforeReloading,
		LoadingAmmo,
		WaitingBeforeIdle,
		Reloading,
		ReloadingPaused,
		NumberOfStates
	}

	public enum FiringFocus
	{
		Troops,
		Walls,
		RangedSiegeWeapons,
		PrimarySiegeWeapons
	}

	public enum CameraState
	{
		StickToWeapon,
		DoNotMove,
		MoveDownToReload,
		RememberLastShotDirection,
		FreeMove,
		ApproachToCamera
	}

	public enum ForceUseState
	{
		NotForced,
		ForcefullyUsed
	}

	public delegate void OnSiegeWeaponReloadDone();

	private const float DefaultMissileRadius = 0.01f;

	public const float DefaultDirectionRestriction = System.MathF.PI * 2f / 3f;

	public const string CanGoAmmoPickupTag = "can_pick_up_ammo";

	public const string DontApplySidePenaltyTag = "no_ammo_pick_up_penalty";

	public const string ReloadTag = "reload";

	public const string AmmoLoadTag = "ammoload";

	public const string CameraHolderTag = "cameraHolder";

	public const string ProjectileTag = "projectile";

	public string MissileItemID;

	protected bool UsesMouseForAiming;

	[EditableScriptComponentVariable(true, "")]
	protected int MultipleProjectileCount = 5;

	private WeaponState _state;

	public FiringFocus Focus;

	private int _projectileIndex;

	protected GameEntity MissileStartingPositionEntityForSimulation;

	protected Skeleton[] Skeletons;

	protected SynchedMissionObject[] SkeletonOwnerObjects;

	protected string[] SkeletonNames;

	protected string[] FireAnimations;

	protected string[] SetUpAnimations;

	protected int[] FireAnimationIndices;

	protected int[] SetUpAnimationIndices;

	protected SynchedMissionObject RotationObject;

	private MatrixFrame _rotationObjectInitialFrame;

	protected SoundEvent MoveSound;

	protected SoundEvent ReloadSound;

	protected int MoveSoundIndex = -1;

	protected int ReloadSoundIndex = -1;

	protected int FireSoundIndex = -1;

	protected ItemObject OriginalMissileItem;

	protected WeaponStatsData OriginalMissileWeaponStatsDataForTargeting;

	private ItemObject _loadedMissileItem;

	protected List<StandingPoint> CanPickUpAmmoStandingPoints;

	protected List<StandingPoint> ReloadStandingPoints;

	protected StandingPointWithWeaponRequirement LoadAmmoStandingPoint;

	protected Dictionary<StandingPoint, float> PilotReservePriorityValues = new Dictionary<StandingPoint, float>();

	protected Agent ReloaderAgent;

	protected StandingPoint ReloaderAgentOriginalPoint;

	protected bool AttackClickWillReload;

	protected bool WeaponNeedsClickToReload;

	protected float FinalReloadSpeed = 1f;

	protected float BaseReloadSpeed = 1f;

	public int StartingAmmoCount;

	protected int CurrentAmmo = 1;

	protected float TargetDirection;

	protected float TargetReleaseAngle;

	protected float CameraDirection;

	protected float CameraReleaseAngle;

	protected float ReloadTargetReleaseAngle;

	private MatrixFrame _cameraHolderInitialFrame;

	protected float MaxRotateSpeed;

	private CameraState _cameraState;

	private bool _inputGiven;

	protected float DontMoveTimer;

	private float _inputX;

	private float _inputY;

	private bool _exactInputGiven;

	private float _inputTargetX;

	private float _inputTargetY;

	private Vec3 _ammoPickupCenter;

	private float _lastSyncedDirection;

	private float _lastSyncedReleaseAngle;

	private float _syncTimer;

	private float _cameraMoveBackFactor;

	public float TopReleaseAngleRestriction = System.MathF.PI / 2f;

	public float BottomReleaseAngleRestriction = -System.MathF.PI / 2f;

	protected float CurrentDirection;

	protected float CurrentReleaseAngle;

	protected float ReleaseAngleRestrictionCenter;

	protected float ReleaseAngleRestrictionAngle;

	private float _animationTimeElapsed;

	protected float TimeGapBetweenShootingEndAndReloadingStart = 0.6f;

	protected float TimeGapBetweenShootActionAndProjectileLeaving;

	private int _currentReloaderCount;

	protected Agent LastShooterAgent;

	private float _lastCanPickUpAmmoStandingPointsSortedAngle = -System.MathF.PI;

	protected BattleSideEnum DefaultSide;

	private bool _aiRequestsShoot;

	private bool _aiRequestsManualReload;

	private bool _hasFrameChangedInPreviousFrame;

	private string _lastLoadedMissileItemId;

	private float _projectileRadiusCached;

	public virtual string MultipleFireProjectileId => "grapeshot_fire_stack";

	public virtual string MultipleFireProjectileFlyingId => "grapeshot_fire_projectile";

	public virtual string MultipleProjectileId => "grapeshot_stack";

	public virtual string MultipleProjectileFlyingId => "grapeshot_projectile";

	public virtual string SingleFireProjectileId => "pot";

	public virtual string SingleFireProjectileFlyingId => "pot_projectile";

	public virtual string SingleProjectileId => "boulder";

	public virtual string SingleProjectileFlyingId => "boulder_projectile";

	public WeaponState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state != value)
			{
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SetRangedSiegeWeaponState(base.Id, value));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
				}
				_state = value;
				OnRangedSiegeWeaponStateChange();
			}
		}
	}

	protected virtual float MaximumBallisticError => 1f;

	protected abstract float ShootingSpeed { get; }

	public virtual Vec3 CanShootAtPointCheckingOffset => Vec3.Zero;

	public GameEntity CameraHolder { get; private set; }

	protected SynchedMissionObject Projectile { get; private set; }

	protected Vec3 MissileStartingGlobalPositionForSimulation
	{
		get
		{
			if (MissileStartingPositionEntityForSimulation != null)
			{
				return MissileStartingPositionEntityForSimulation.GlobalPosition;
			}
			return Projectile?.GameEntity.GlobalPosition ?? Vec3.Zero;
		}
	}

	protected string SkeletonName
	{
		set
		{
			SkeletonNames = new string[1] { value };
		}
	}

	protected string FireAnimation
	{
		set
		{
			FireAnimations = new string[1] { value };
		}
	}

	protected string SetUpAnimation
	{
		set
		{
			SetUpAnimations = new string[1] { value };
		}
	}

	protected int FireAnimationIndex
	{
		set
		{
			FireAnimationIndices = new int[1] { value };
		}
	}

	protected int SetUpAnimationIndex
	{
		set
		{
			SetUpAnimationIndices = new int[1] { value };
		}
	}

	protected ItemObject LoadedMissileItem
	{
		get
		{
			return _loadedMissileItem;
		}
		set
		{
			_loadedMissileItem = value;
			OnLoadedMissileItemChanged();
		}
	}

	protected virtual bool WeaponMovesDownToReload => false;

	public int AmmoCount
	{
		get
		{
			return CurrentAmmo;
		}
		protected set
		{
			CurrentAmmo = value;
		}
	}

	protected virtual bool HasAmmo { get; set; } = true;

	public virtual float DirectionRestriction => System.MathF.PI * 2f / 3f;

	protected virtual float HorizontalAimSensitivity => 0.2f;

	protected virtual float VerticalAimSensitivity => 0.2f;

	protected virtual float ReloadSpeedMultiplier => 1f;

	public bool PlayerForceUse { get; private set; }

	protected virtual Vec3 ShootingDirection => Projectile.GameEntity.GetGlobalFrame().rotation.u.NormalizedCopy();

	public virtual Vec3 ProjectileEntityCurrentGlobalPosition => Projectile.GameEntity.GetGlobalFrame().origin;

	public override BattleSideEnum Side
	{
		get
		{
			if (base.PilotAgent != null)
			{
				return base.PilotAgent.Team.Side;
			}
			return DefaultSide;
		}
	}

	public event Action<RangedSiegeWeapon, Agent> OnAgentLoadsMachine;

	public event OnSiegeWeaponReloadDone OnReloadDone;

	protected abstract void RegisterAnimationParameters();

	protected abstract void GetSoundEventIndices();

	protected virtual void ConsumeAmmo()
	{
		AmmoCount--;
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetRangedSiegeWeaponAmmo(base.Id, AmmoCount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		UpdateAmmoMesh();
		CheckAmmo();
	}

	public virtual void SetAmmo(int ammoLeft)
	{
		if (AmmoCount != ammoLeft)
		{
			AmmoCount = ammoLeft;
			UpdateAmmoMesh();
			CheckAmmo();
		}
	}

	public virtual void SetStartAmmo(int ammoLeft)
	{
		if (AmmoCount != ammoLeft)
		{
			AmmoCount = ammoLeft;
			UpdateAmmoMesh();
			CheckAmmo();
		}
	}

	protected virtual void CheckAmmo()
	{
		if (AmmoCount > 0 || StartingAmmoCount <= 0)
		{
			return;
		}
		HasAmmo = false;
		SetForcedUse(value: false);
		foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
		{
			ammoPickUpPoint.IsDeactivated = true;
		}
	}

	protected void ChangeProjectileEntityServer(Agent loadingAgent, string missileItemID)
	{
		List<SynchedMissionObject> list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("projectile");
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].GameEntity.HasTag(missileItemID))
			{
				Projectile = list[i];
				_projectileIndex = i;
				break;
			}
		}
		LoadedMissileItem = Game.Current.ObjectManager.GetObject<ItemObject>(missileItemID);
		if (GameNetwork.IsServerOrRecorder)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new RangedSiegeWeaponChangeProjectile(base.Id, _projectileIndex));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		this.OnAgentLoadsMachine?.Invoke(this, loadingAgent);
	}

	public void ChangeProjectileEntityClient(int index)
	{
		List<SynchedMissionObject> list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("projectile");
		Projectile = list[index];
		_projectileIndex = index;
	}

	protected internal override void OnInit()
	{
		base.OnInit();
		DetermineDefaultBattleSide();
		ReleaseAngleRestrictionCenter = (TopReleaseAngleRestriction + BottomReleaseAngleRestriction) * 0.5f;
		ReleaseAngleRestrictionAngle = TopReleaseAngleRestriction - BottomReleaseAngleRestriction;
		CurrentReleaseAngle = (_lastSyncedReleaseAngle = ReleaseAngleRestrictionCenter);
		OriginalMissileItem = Game.Current.ObjectManager.GetObject<ItemObject>(MissileItemID);
		_projectileRadiusCached = -1f;
		LoadedMissileItem = OriginalMissileItem;
		OriginalMissileWeaponStatsDataForTargeting = new MissionWeapon(OriginalMissileItem, null, null).GetWeaponStatsDataForUsage(0);
		if (RotationObject == null)
		{
			RotationObject = this;
		}
		_rotationObjectInitialFrame = RotationObject.GameEntity.GetFrame();
		CurrentDirection = (_lastSyncedDirection = 0f);
		_syncTimer = 0f;
		List<WeakGameEntity> list = base.GameEntity.CollectChildrenEntitiesWithTag("cameraHolder");
		if (list.Count > 0)
		{
			CameraHolder = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(list[0]);
			_cameraHolderInitialFrame = CameraHolder.GetFrame();
			if (GameNetwork.IsClientOrReplay)
			{
				MakeVisibilityCheck = false;
			}
		}
		List<SynchedMissionObject> list2 = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("projectile");
		foreach (SynchedMissionObject item in list2)
		{
			item.GameEntity.SetVisibilityExcludeParents(visible: false);
		}
		Projectile = list2.FirstOrDefault((SynchedMissionObject x) => x.GameEntity.HasTag(MissileItemID));
		_projectileIndex = list2.IndexOf(Projectile);
		Projectile.GameEntity.SetVisibilityExcludeParents(visible: true);
		WeakGameEntity weakEntity = base.GameEntity.GetChildren().FirstOrDefault((WeakGameEntity x) => x.Name == "clean");
		if (weakEntity.IsValid)
		{
			weakEntity = weakEntity.GetChildren().FirstOrDefault((WeakGameEntity x) => x.Name == "projectile_leaving_position");
		}
		MissileStartingPositionEntityForSimulation = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity);
		TargetDirection = CurrentDirection;
		TargetReleaseAngle = CurrentReleaseAngle;
		CanPickUpAmmoStandingPoints = new List<StandingPoint>();
		ReloadStandingPoints = new List<StandingPoint>();
		if (base.StandingPoints != null)
		{
			foreach (StandingPoint standingPoint in base.StandingPoints)
			{
				standingPoint.AddComponent(new ResetAnimationOnStopUsageComponent(ActionIndexCache.act_none, alwaysResetWithAction: false));
				if (standingPoint.GameEntity.HasTag("reload"))
				{
					ReloadStandingPoints.Add(standingPoint);
				}
				if (standingPoint.GameEntity.HasTag("can_pick_up_ammo"))
				{
					CanPickUpAmmoStandingPoints.Add(standingPoint);
				}
			}
		}
		List<StandingPointWithWeaponRequirement> list3 = base.StandingPoints.OfType<StandingPointWithWeaponRequirement>().ToList();
		List<StandingPointWithWeaponRequirement> list4 = new List<StandingPointWithWeaponRequirement>();
		foreach (StandingPointWithWeaponRequirement item2 in list3)
		{
			if (item2.GameEntity.HasTag(AmmoPickUpTag))
			{
				item2.InitGivenWeapon(OriginalMissileItem);
				item2.SetupOnUsingStoppedBehavior(autoAttach: false, OnAmmoPickupUsingCancelled);
				continue;
			}
			list4.Add(item2);
			item2.SetupOnUsingStoppedBehavior(autoAttach: false, OnLoadingAmmoPointUsingCancelled);
			item2.InitRequiredWeaponClasses(new WeaponClass[1] { OriginalMissileItem.PrimaryWeapon.WeaponClass });
		}
		if (base.AmmoPickUpPoints.Count > 1)
		{
			_ammoPickupCenter = default(Vec3);
			foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
			{
				((StandingPointWithWeaponRequirement)ammoPickUpPoint).SetHasAlternative(hasAlternative: true);
				_ammoPickupCenter += ammoPickUpPoint.GameEntity.GlobalPosition;
			}
			_ammoPickupCenter /= (float)base.AmmoPickUpPoints.Count;
		}
		else
		{
			_ammoPickupCenter = base.GameEntity.GlobalPosition;
		}
		list4.Sort(delegate(StandingPointWithWeaponRequirement element1, StandingPointWithWeaponRequirement element2)
		{
			if (element1.GameEntity.GlobalPosition.DistanceSquared(_ammoPickupCenter) > element2.GameEntity.GlobalPosition.DistanceSquared(_ammoPickupCenter))
			{
				return 1;
			}
			return (element1.GameEntity.GlobalPosition.DistanceSquared(_ammoPickupCenter) < element2.GameEntity.GlobalPosition.DistanceSquared(_ammoPickupCenter)) ? (-1) : 0;
		});
		LoadAmmoStandingPoint = list4.FirstOrDefault();
		SortCanPickUpAmmoStandingPoints();
		Vec3 vec = base.PilotStandingPoint.GameEntity.GlobalPosition - base.GameEntity.GlobalPosition;
		foreach (StandingPoint canPickUpAmmoStandingPoint in CanPickUpAmmoStandingPoints)
		{
			if (canPickUpAmmoStandingPoint != base.PilotStandingPoint)
			{
				float length = (canPickUpAmmoStandingPoint.GameEntity.GlobalPosition - base.GameEntity.GlobalPosition + vec).Length;
				PilotReservePriorityValues.Add(canPickUpAmmoStandingPoint, length);
			}
		}
		AmmoCount = TaleWorlds.Library.MathF.Max(0, StartingAmmoCount - 1);
		UpdateAmmoMesh();
		RegisterAnimationParameters();
		GetSoundEventIndices();
		InitAnimations();
		SetScriptComponentToTick(GetTickRequirement());
	}

	protected virtual void DetermineDefaultBattleSide()
	{
		DestructableComponent firstScriptOfType = base.GameEntity.GetFirstScriptOfType<DestructableComponent>();
		DefaultSide = firstScriptOfType.BattleSide;
	}

	private void SortCanPickUpAmmoStandingPoints()
	{
		if (!(MBMath.GetSmallestDifferenceBetweenTwoAngles(_lastCanPickUpAmmoStandingPointsSortedAngle, CurrentDirection) > System.MathF.PI * 3f / 50f))
		{
			return;
		}
		_lastCanPickUpAmmoStandingPointsSortedAngle = CurrentDirection;
		int signOfAmmoPile = Math.Sign(Vec3.DotProduct(base.GameEntity.GetGlobalFrame().rotation.s, _ammoPickupCenter - base.GameEntity.GlobalPosition));
		CanPickUpAmmoStandingPoints.Sort(delegate(StandingPoint element1, StandingPoint element2)
		{
			Vec3 vec = _ammoPickupCenter - element1.GameEntity.GlobalPosition;
			Vec3 vec2 = _ammoPickupCenter - element2.GameEntity.GlobalPosition;
			float num = vec.LengthSquared;
			float num2 = vec2.LengthSquared;
			float num3 = Vec3.DotProduct(base.GameEntity.GetGlobalFrame().rotation.s, element1.GameEntity.GlobalPosition - base.GameEntity.GlobalPosition);
			float num4 = Vec3.DotProduct(base.GameEntity.GetGlobalFrame().rotation.s, element2.GameEntity.GlobalPosition - base.GameEntity.GlobalPosition);
			if (!element1.GameEntity.HasTag("no_ammo_pick_up_penalty") && signOfAmmoPile != Math.Sign(num3))
			{
				num += num3 * num3 * 64f;
			}
			if (!element2.GameEntity.HasTag("no_ammo_pick_up_penalty") && signOfAmmoPile != Math.Sign(num4))
			{
				num2 += num4 * num4 * 64f;
			}
			if (element1.GameEntity.HasTag(PilotStandingPointTag))
			{
				num += 25f;
			}
			else if (element2.GameEntity.HasTag(PilotStandingPointTag))
			{
				num2 += 25f;
			}
			if (num > num2)
			{
				return 1;
			}
			return (num < num2) ? (-1) : 0;
		});
	}

	protected internal override void OnEditorInit()
	{
		List<SynchedMissionObject> list = base.GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>("projectile");
		if (list.Count > 0)
		{
			Projectile = list[0];
		}
	}

	private void InitAnimations()
	{
		for (int i = 0; i < Skeletons.Length; i++)
		{
			Skeletons[i].SetAnimationAtChannel(SetUpAnimations[i], 0, 1f, 0f);
			Skeletons[i].SetAnimationParameterAtChannel(0, 1f);
			Skeletons[i].TickAnimations(0.0001f, MatrixFrame.Identity, tickAnimsForChildren: true);
		}
	}

	protected internal override void OnMissionReset()
	{
		base.OnMissionReset();
		Projectile.GameEntity.SetVisibilityExcludeParents(visible: true);
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.UserAgent?.StopUsingGameObject();
			standingPoint.IsDeactivated = false;
		}
		_state = WeaponState.Idle;
		CurrentDirection = (_lastSyncedDirection = 0f);
		_syncTimer = 0f;
		CurrentReleaseAngle = (_lastSyncedReleaseAngle = ReleaseAngleRestrictionCenter);
		TargetDirection = CurrentDirection;
		TargetReleaseAngle = CurrentReleaseAngle;
		ApplyCurrentDirectionToEntity();
		AmmoCount = TaleWorlds.Library.MathF.Max(0, StartingAmmoCount - 1);
		UpdateAmmoMesh();
		if (MoveSound != null)
		{
			MoveSound.Stop();
			MoveSound = null;
		}
		_hasFrameChangedInPreviousFrame = false;
		Skeleton[] skeletons = Skeletons;
		for (int i = 0; i < skeletons.Length; i++)
		{
			skeletons[i].Freeze(p: false);
		}
		foreach (StandingPoint ammoPickUpPoint in base.AmmoPickUpPoints)
		{
			ammoPickUpPoint.IsDeactivated = false;
		}
		InitAnimations();
		UpdateProjectilePosition();
		if (!GameNetwork.IsClientOrReplay)
		{
			SetActivationLoadAmmoPoint(activate: false);
		}
	}

	public override void WriteToNetwork()
	{
		base.WriteToNetwork();
		GameNetworkMessage.WriteIntToPacket((int)State, CompressionMission.RangedSiegeWeaponStateCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(TargetDirection, CompressionBasic.RadianCompressionInfo);
		GameNetworkMessage.WriteFloatToPacket(TargetReleaseAngle, CompressionBasic.RadianCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(AmmoCount, CompressionMission.RangedSiegeWeaponAmmoCompressionInfo);
		GameNetworkMessage.WriteIntToPacket(_projectileIndex, CompressionMission.RangedSiegeWeaponAmmoIndexCompressionInfo);
	}

	protected virtual void UpdateProjectilePosition()
	{
	}

	public override bool IsInRangeToCheckAlternativePoints(Agent agent)
	{
		float num = ((base.AmmoPickUpPoints.Count > 0) ? (agent.GetInteractionDistanceToUsable(base.AmmoPickUpPoints[0]) + 2f) : 2f);
		return _ammoPickupCenter.DistanceSquared(agent.Position) < num * num;
	}

	public override StandingPoint GetBestPointAlternativeTo(StandingPoint standingPoint, Agent agent)
	{
		if (base.AmmoPickUpPoints.Contains(standingPoint))
		{
			IEnumerable<StandingPoint> enumerable = base.AmmoPickUpPoints.Where((StandingPoint sp) => !sp.IsDeactivated && (sp.IsInstantUse || (!sp.HasUser && !sp.HasAIMovingTo)) && !sp.IsDisabledForAgent(agent));
			float num = standingPoint.GameEntity.GlobalPosition.DistanceSquared(agent.Position);
			StandingPoint result = standingPoint;
			{
				foreach (StandingPoint item in enumerable)
				{
					float num2 = item.GameEntity.GlobalPosition.DistanceSquared(agent.Position);
					if (num2 < num)
					{
						num = num2;
						result = item;
					}
				}
				return result;
			}
		}
		return standingPoint;
	}

	protected virtual void OnRangedSiegeWeaponStateChange()
	{
		switch (State)
		{
		case WeaponState.Reloading:
			if (ReloadSound != null && ReloadSound.IsValid)
			{
				if (ReloadSound.IsPaused())
				{
					ReloadSound.Resume();
				}
				else
				{
					ReloadSound.PlayInPosition(base.GameEntity.GetGlobalFrame().origin);
				}
			}
			else
			{
				ReloadSound = SoundEvent.CreateEvent(ReloadSoundIndex, base.Scene);
				ReloadSound.PlayInPosition(base.GameEntity.GetGlobalFrame().origin);
			}
			break;
		case WeaponState.ReloadingPaused:
			if (ReloadSound != null && ReloadSound.IsValid)
			{
				ReloadSound.Pause();
			}
			break;
		case WeaponState.WaitingBeforeProjectileLeaving:
			AttackClickWillReload = WeaponNeedsClickToReload;
			if (!GameNetwork.IsDedicatedServer)
			{
				int fireSoundIndex = FireSoundIndex;
				MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
				SoundManager.StartOneShotEventWithIndex(fireSoundIndex, in globalFrame.origin);
			}
			break;
		case WeaponState.Shooting:
			if (CameraHolder != null)
			{
				_cameraState = CameraState.DoNotMove;
				DontMoveTimer = 0.35f;
			}
			break;
		case WeaponState.LoadingAmmo:
			if (ReloadSound != null && ReloadSound.IsValid)
			{
				ReloadSound.Stop();
			}
			ReloadSound = null;
			break;
		case WeaponState.WaitingAfterShooting:
			AttackClickWillReload = WeaponNeedsClickToReload;
			CheckAmmo();
			break;
		case WeaponState.WaitingBeforeReloading:
			AttackClickWillReload = false;
			if (CameraHolder != null && WeaponMovesDownToReload)
			{
				_cameraState = CameraState.MoveDownToReload;
			}
			CheckAmmo();
			break;
		case WeaponState.Idle:
		case WeaponState.WaitingBeforeIdle:
			_cameraState = ((_cameraState == CameraState.FreeMove) ? CameraState.ApproachToCamera : CameraState.StickToWeapon);
			break;
		default:
			Debug.FailedAssert("Invalid WeaponState.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\RangedSiegeWeapon.cs", "OnRangedSiegeWeaponStateChange", 894);
			break;
		}
		if (GameNetwork.IsClientOrReplay)
		{
			return;
		}
		switch (State)
		{
		case WeaponState.Reloading:
		{
			for (int j = 0; j < SkeletonOwnerObjects.Length; j++)
			{
				if (SkeletonOwnerObjects[j].GameEntity.IsSkeletonAnimationPaused())
				{
					SkeletonOwnerObjects[j].ResumeSkeletonAnimationSynched();
				}
				else
				{
					SkeletonOwnerObjects[j].SetAnimationAtChannelSynched(SetUpAnimations[j], 0);
				}
			}
			_currentReloaderCount = 1;
			break;
		}
		case WeaponState.ReloadingPaused:
		{
			SynchedMissionObject[] skeletonOwnerObjects = SkeletonOwnerObjects;
			for (int k = 0; k < skeletonOwnerObjects.Length; k++)
			{
				skeletonOwnerObjects[k].PauseSkeletonAnimationSynched();
			}
			break;
		}
		case WeaponState.WaitingBeforeProjectileLeaving:
		{
			for (int i = 0; i < SkeletonOwnerObjects.Length; i++)
			{
				SkeletonOwnerObjects[i].SetAnimationAtChannelSynched(FireAnimations[i], 0);
			}
			break;
		}
		case WeaponState.Shooting:
			ShootProjectile();
			break;
		case WeaponState.LoadingAmmo:
			SetActivationLoadAmmoPoint(activate: true);
			ReloaderAgent = null;
			break;
		case WeaponState.WaitingBeforeIdle:
			SendReloaderAgentToOriginalPoint();
			SetActivationLoadAmmoPoint(activate: false);
			break;
		default:
			Debug.FailedAssert("Invalid WeaponState.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\RangedSiegeWeapon.cs", "OnRangedSiegeWeaponStateChange", 970);
			break;
		case WeaponState.Idle:
		case WeaponState.WaitingAfterShooting:
		case WeaponState.WaitingBeforeReloading:
			break;
		}
	}

	protected virtual void SetActivationLoadAmmoPoint(bool activate)
	{
	}

	protected override float GetDetachmentWeightAux(BattleSideEnum side)
	{
		if (HasAmmo)
		{
			return base.GetDetachmentWeightAux(side);
		}
		return float.MinValue;
	}

	protected float GetDetachmentWeightAuxForExternalAmmoWeapons(BattleSideEnum side)
	{
		if (IsDisabledForBattleSideAI(side))
		{
			return float.MinValue;
		}
		UsableStandingPoints.Clear();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = !base.PilotStandingPoint.HasUser && !base.PilotStandingPoint.HasAIMovingTo && (ReloaderAgent == null || ReloaderAgentOriginalPoint != base.PilotStandingPoint);
		int num = -1;
		StandingPoint standingPoint = null;
		bool flag4 = false;
		for (int i = 0; i < base.StandingPoints.Count; i++)
		{
			StandingPoint standingPoint2 = base.StandingPoints[i];
			if (!standingPoint2.GameEntity.HasTag("can_pick_up_ammo"))
			{
				continue;
			}
			if (ReloaderAgent == null || standingPoint2 != ReloaderAgentOriginalPoint)
			{
				if (standingPoint2.IsUsableBySide(side))
				{
					if (!standingPoint2.HasAIMovingTo)
					{
						if (!flag2)
						{
							UsableStandingPoints.Clear();
							num = -1;
						}
						flag2 = true;
					}
					else if (flag2 || standingPoint2.MovingAgent.Formation.Team.Side != side)
					{
						continue;
					}
					flag = true;
					UsableStandingPoints.Add((i, standingPoint2));
					if (flag3 && base.PilotStandingPoint == standingPoint2)
					{
						num = UsableStandingPoints.Count - 1;
					}
				}
				else if (flag3 && standingPoint2.HasAIUser && (standingPoint == null || PilotReservePriorityValues[standingPoint2] > PilotReservePriorityValues[standingPoint] || flag4))
				{
					standingPoint = standingPoint2;
					flag4 = false;
				}
			}
			else if (flag3 && standingPoint == null)
			{
				standingPoint = standingPoint2;
				flag4 = true;
			}
		}
		if (standingPoint != null)
		{
			if (flag4)
			{
				ReloaderAgentOriginalPoint = base.PilotStandingPoint;
			}
			else
			{
				Agent userAgent = standingPoint.UserAgent;
				userAgent.StopUsingGameObjectMT(isSuccessful: true, Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
				userAgent.AIMoveToGameObjectEnable(base.PilotStandingPoint, this, base.Ai.GetScriptedFrameFlags(userAgent));
			}
			if (num != -1)
			{
				UsableStandingPoints.RemoveAt(num);
			}
		}
		AreUsableStandingPointsVacant = flag2;
		if (!flag)
		{
			return float.MinValue;
		}
		if (flag2)
		{
			return 1f;
		}
		if (base.IsDetachmentRecentlyEvaluated)
		{
			return 0.01f;
		}
		return 0.1f;
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTick(float dt)
	{
		base.OnTick(dt);
		if (!base.GameEntity.IsVisibleIncludeParents())
		{
			return;
		}
		if (!GameNetwork.IsClientOrReplay)
		{
			UpdateState(dt);
			if (base.PilotAgent != null && !base.PilotAgent.IsInBeingStruckAction)
			{
				if (base.PilotAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.AttackMask))
				{
					if (State == WeaponState.Idle)
					{
						_aiRequestsShoot = false;
						Shoot();
					}
					else if (State == WeaponState.WaitingAfterShooting && AttackClickWillReload)
					{
						_aiRequestsManualReload = false;
						ManualReload();
					}
				}
				if (_aiRequestsManualReload)
				{
					ManualReload();
				}
				if (_aiRequestsShoot)
				{
					Shoot();
				}
			}
			_aiRequestsShoot = false;
			_aiRequestsManualReload = false;
		}
		HandleUserAiming(dt);
	}

	protected static bool ApproachToAngle(ref float angle, float angleToApproach, bool isMouse, float speed_limit, float dt, float sensitivity)
	{
		speed_limit = TaleWorlds.Library.MathF.Abs(speed_limit);
		if (angle != angleToApproach)
		{
			float num = sensitivity * dt;
			float num2 = TaleWorlds.Library.MathF.Abs(angle - angleToApproach);
			if (isMouse)
			{
				num *= TaleWorlds.Library.MathF.Max(num2 * 8f, 0.15f);
			}
			if (speed_limit > 0f)
			{
				num = TaleWorlds.Library.MathF.Min(num, speed_limit * dt);
			}
			if (num2 <= num)
			{
				angle = angleToApproach;
			}
			else
			{
				angle += num * (float)TaleWorlds.Library.MathF.Sign(angleToApproach - angle);
			}
			return true;
		}
		return false;
	}

	protected virtual void HandleUserAiming(float dt)
	{
		bool flag = false;
		float horizontalAimSensitivity = HorizontalAimSensitivity;
		float verticalAimSensitivity = VerticalAimSensitivity;
		bool flag2 = false;
		if (_cameraState != CameraState.DoNotMove)
		{
			if (_inputGiven)
			{
				flag2 = true;
				if (CanRotate())
				{
					if (_inputX != 0f)
					{
						TargetDirection += horizontalAimSensitivity * dt * _inputX;
						TargetDirection = MBMath.WrapAngle(TargetDirection);
						TargetDirection = MBMath.ClampAngle(TargetDirection, CurrentDirection, 0.7f);
						TargetDirection = MBMath.ClampAngle(TargetDirection, 0f, DirectionRestriction);
					}
					if (_inputY != 0f)
					{
						TargetReleaseAngle += verticalAimSensitivity * dt * _inputY;
						TargetReleaseAngle = MBMath.ClampAngle(TargetReleaseAngle, CurrentReleaseAngle + 0.049999997f, 0.6f);
						TargetReleaseAngle = MBMath.ClampAngle(TargetReleaseAngle, ReleaseAngleRestrictionCenter, ReleaseAngleRestrictionAngle);
					}
				}
				_inputGiven = false;
				_inputX = 0f;
				_inputY = 0f;
			}
			else if (_exactInputGiven)
			{
				bool flag3 = false;
				if (CanRotate())
				{
					if (TargetDirection != _inputTargetX)
					{
						float num = horizontalAimSensitivity * dt;
						if (TaleWorlds.Library.MathF.Abs(TargetDirection - _inputTargetX) < num)
						{
							TargetDirection = _inputTargetX;
						}
						else if (TargetDirection < _inputTargetX)
						{
							TargetDirection += num;
							flag3 = true;
						}
						else
						{
							TargetDirection -= num;
							flag3 = true;
						}
						TargetDirection = MBMath.WrapAngle(TargetDirection);
						TargetDirection = MBMath.ClampAngle(TargetDirection, CurrentDirection, 0.7f);
						TargetDirection = MBMath.ClampAngle(TargetDirection, 0f, DirectionRestriction);
					}
					if (TargetReleaseAngle != _inputTargetY)
					{
						float num2 = verticalAimSensitivity * dt;
						if (TaleWorlds.Library.MathF.Abs(TargetReleaseAngle - _inputTargetY) < num2)
						{
							TargetReleaseAngle = _inputTargetY;
						}
						else if (TargetReleaseAngle < _inputTargetY)
						{
							TargetReleaseAngle += num2;
							flag3 = true;
						}
						else
						{
							TargetReleaseAngle -= num2;
							flag3 = true;
						}
						TargetReleaseAngle = MBMath.ClampAngle(TargetReleaseAngle, CurrentReleaseAngle + 0.049999997f, 0.6f);
						TargetReleaseAngle = MBMath.ClampAngle(TargetReleaseAngle, ReleaseAngleRestrictionCenter, ReleaseAngleRestrictionAngle);
					}
				}
				else
				{
					flag3 = true;
				}
				if (!flag3)
				{
					_exactInputGiven = false;
				}
			}
		}
		switch (_cameraState)
		{
		case CameraState.StickToWeapon:
			flag = ApproachToAngle(ref CurrentDirection, TargetDirection, UsesMouseForAiming, -1f, dt, horizontalAimSensitivity);
			flag = ApproachToAngle(ref CurrentReleaseAngle, TargetReleaseAngle, UsesMouseForAiming, -1f, dt, verticalAimSensitivity) || flag;
			CameraDirection = CurrentDirection;
			CameraReleaseAngle = CurrentReleaseAngle;
			break;
		case CameraState.DoNotMove:
			DontMoveTimer -= dt;
			if (DontMoveTimer < 0f)
			{
				if (!AttackClickWillReload && WeaponMovesDownToReload)
				{
					_cameraState = CameraState.MoveDownToReload;
					MaxRotateSpeed = 0f;
					ReloadTargetReleaseAngle = MBMath.ClampAngle((TaleWorlds.Library.MathF.Abs(CurrentReleaseAngle) > 0.17453292f) ? 0f : CurrentReleaseAngle, CurrentReleaseAngle - 0.049999997f, 0.6f);
					TargetDirection = CameraDirection;
					CameraReleaseAngle = TargetReleaseAngle;
				}
				else
				{
					_cameraState = CameraState.StickToWeapon;
				}
			}
			break;
		case CameraState.MoveDownToReload:
			MaxRotateSpeed += dt * 1.2f;
			MaxRotateSpeed = TaleWorlds.Library.MathF.Min(MaxRotateSpeed, 1f);
			flag = ApproachToAngle(ref CurrentReleaseAngle, ReloadTargetReleaseAngle, UsesMouseForAiming, 0.4f + MaxRotateSpeed, dt, verticalAimSensitivity);
			flag = ApproachToAngle(ref CameraDirection, TargetDirection, UsesMouseForAiming, -1f, dt, horizontalAimSensitivity) || flag;
			flag = ApproachToAngle(ref CameraReleaseAngle, ReloadTargetReleaseAngle, UsesMouseForAiming, 0.5f + MaxRotateSpeed, dt, verticalAimSensitivity) || flag;
			if (!flag)
			{
				_cameraState = CameraState.RememberLastShotDirection;
			}
			break;
		case CameraState.RememberLastShotDirection:
			if (State == WeaponState.Idle || flag2)
			{
				_cameraState = CameraState.FreeMove;
				this.OnReloadDone?.Invoke();
			}
			break;
		case CameraState.FreeMove:
			flag = ApproachToAngle(ref CameraDirection, TargetDirection, UsesMouseForAiming, -1f, dt, horizontalAimSensitivity);
			flag = ApproachToAngle(ref CameraReleaseAngle, TargetReleaseAngle, UsesMouseForAiming, -1f, dt, verticalAimSensitivity) || flag;
			MaxRotateSpeed = 0f;
			break;
		case CameraState.ApproachToCamera:
			MaxRotateSpeed += 0.9f * dt + MaxRotateSpeed * 2f * dt;
			flag = ApproachToAngle(ref CameraDirection, TargetDirection, UsesMouseForAiming, -1f, dt, horizontalAimSensitivity);
			flag = ApproachToAngle(ref CameraReleaseAngle, TargetReleaseAngle, UsesMouseForAiming, -1f, dt, verticalAimSensitivity) || flag;
			flag = ApproachToAngle(ref CurrentDirection, TargetDirection, UsesMouseForAiming, MaxRotateSpeed, dt, horizontalAimSensitivity) || flag;
			flag = ApproachToAngle(ref CurrentReleaseAngle, TargetReleaseAngle, UsesMouseForAiming, MaxRotateSpeed, dt, verticalAimSensitivity) || flag;
			if (!flag)
			{
				_cameraState = CameraState.StickToWeapon;
			}
			break;
		}
		if (CameraHolder != null)
		{
			MatrixFrame frame = _cameraHolderInitialFrame;
			frame.rotation.RotateAboutForward(CameraDirection - CurrentDirection);
			frame.rotation.RotateAboutSide(CameraReleaseAngle - CurrentReleaseAngle);
			CameraHolder.SetFrame(ref frame);
			frame = CameraHolder.GetGlobalFrame();
			frame.rotation.s.z = 0f;
			frame.rotation.s.Normalize();
			frame.rotation.u = Vec3.CrossProduct(frame.rotation.s, frame.rotation.f);
			frame.rotation.u.Normalize();
			frame.rotation.f = Vec3.CrossProduct(frame.rotation.u, frame.rotation.s);
			frame.rotation.f.Normalize();
			if (base.PilotAgent == null)
			{
				_cameraMoveBackFactor = ((1f - _cameraMoveBackFactor > 1E-05f) ? MBMath.LerpFPSIndependent(_cameraMoveBackFactor, 1f, dt * 8f) : 1f);
			}
			else
			{
				_cameraMoveBackFactor = ((_cameraMoveBackFactor > 1E-05f) ? MBMath.LerpFPSIndependent(_cameraMoveBackFactor, 0f, dt * 8f) : 0f);
			}
			if (_cameraMoveBackFactor > 0f)
			{
				frame.origin += frame.rotation.u * _cameraMoveBackFactor * 3f + frame.rotation.f * _cameraMoveBackFactor * 0.3f;
			}
			CameraHolder.SetGlobalFrame(in frame);
		}
		else
		{
			_cameraMoveBackFactor = ((base.PilotAgent == null) ? 1f : 0f);
		}
		if (flag && !_hasFrameChangedInPreviousFrame)
		{
			OnRotationStarted();
		}
		else if (!flag && _hasFrameChangedInPreviousFrame)
		{
			OnRotationStopped();
		}
		_hasFrameChangedInPreviousFrame = flag;
		if ((flag && GameNetwork.IsClient && base.PilotAgent == Agent.Main) || GameNetwork.IsServerOrRecorder)
		{
			float num3 = ((GameNetwork.IsClient && base.PilotAgent == Agent.Main) ? 0.0001f : 0.02f);
			if (_syncTimer > 0.2f && (TaleWorlds.Library.MathF.Abs(CurrentDirection - _lastSyncedDirection) > num3 || TaleWorlds.Library.MathF.Abs(CurrentReleaseAngle - _lastSyncedReleaseAngle) > num3))
			{
				_lastSyncedDirection = CurrentDirection;
				_lastSyncedReleaseAngle = CurrentReleaseAngle;
				MissionLobbyComponent missionBehavior = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();
				if ((missionBehavior == null || missionBehavior.CurrentMultiplayerState != MissionLobbyComponent.MultiplayerGameState.Ending) && GameNetwork.IsClient && base.PilotAgent == Agent.Main)
				{
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new SetMachineRotation(base.Id, CurrentDirection, CurrentReleaseAngle));
					GameNetwork.EndModuleEventAsClient();
				}
				if (GameNetwork.IsServerOrRecorder)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SetMachineTargetRotation(base.Id, CurrentDirection, CurrentReleaseAngle));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeTargetPlayer | GameNetwork.EventBroadcastFlags.AddToMissionRecord, base.PilotAgent?.MissionPeer?.GetNetworkPeer());
				}
			}
		}
		_syncTimer += dt;
		if (_syncTimer >= 1f)
		{
			_syncTimer -= 1f;
		}
		if (flag)
		{
			ApplyAimChange();
		}
	}

	public void GiveInput(float inputX, float inputY)
	{
		_exactInputGiven = false;
		_inputGiven = true;
		_inputX = inputX;
		_inputY = inputY;
		_inputX = MBMath.ClampFloat(_inputX, -1f, 1f);
		_inputY = MBMath.ClampFloat(_inputY, -1f, 1f);
	}

	public void GiveExactInput(float targetX, float targetY)
	{
		_exactInputGiven = true;
		_inputGiven = false;
		_inputTargetX = MBMath.ClampAngle(targetX, 0f, DirectionRestriction);
		_inputTargetY = MBMath.ClampAngle(targetY, ReleaseAngleRestrictionCenter, ReleaseAngleRestrictionAngle);
	}

	protected virtual bool CanRotate()
	{
		return State == WeaponState.Idle;
	}

	protected virtual void ApplyAimChange()
	{
		if (CanRotate())
		{
			ApplyCurrentDirectionToEntity();
			return;
		}
		TargetDirection = CurrentDirection;
		TargetReleaseAngle = CurrentReleaseAngle;
	}

	protected virtual void ApplyCurrentDirectionToEntity()
	{
		MatrixFrame frame = _rotationObjectInitialFrame;
		frame.rotation.RotateAboutUp(CurrentDirection);
		RotationObject.GameEntity.SetFrame(ref frame);
	}

	public virtual float GetTargetReleaseAngle(Vec3 target)
	{
		return Mission.GetMissileVerticalAimCorrection(target - MissileStartingGlobalPositionForSimulation, ShootingSpeed, ref OriginalMissileWeaponStatsDataForTargeting, ItemObject.GetAirFrictionConstant(OriginalMissileItem.PrimaryWeapon.WeaponClass, OriginalMissileItem.PrimaryWeapon.WeaponFlags));
	}

	private void CalculateLocalAnglesFromGlobalDirection(Vec3 globalDirection, out float localTargetDirection, out float localTargetAngle)
	{
		globalDirection.Normalize();
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		if (!globalFrame.rotation.IsUnit())
		{
			globalFrame.rotation.Orthonormalize();
		}
		globalFrame.rotation.RotateAboutAnArbitraryVector(in globalFrame.rotation.u, System.MathF.PI);
		Vec3 vec = globalFrame.rotation.TransformToLocal(in globalDirection);
		localTargetDirection = vec.AsVec2.RotationInRadians;
		localTargetAngle = TaleWorlds.Library.MathF.Atan2(vec.z, TaleWorlds.Library.MathF.Sqrt(vec.x * vec.x + vec.y * vec.y));
	}

	private void CalculateLocalDirectionAndLocalAngleToShootTarget(Vec3 target, out float localTargetDirection, out float localTargetAngle)
	{
		float targetReleaseAngle = GetTargetReleaseAngle(target);
		if (targetReleaseAngle > System.MathF.PI / 2f)
		{
			localTargetDirection = System.MathF.PI;
			localTargetAngle = System.MathF.PI;
			return;
		}
		Vec3 globalDirection = new Vec3((target - MissileStartingGlobalPositionForSimulation).AsVec2).NormalizedCopy();
		globalDirection += new Vec3(0f, 0f, TaleWorlds.Library.MathF.Sin(targetReleaseAngle));
		globalDirection.Normalize();
		Vec3 globalVelocity = GetGlobalVelocity();
		globalDirection *= ShootingSpeed;
		globalDirection -= new Vec3(globalVelocity.AsVec2);
		globalDirection.Normalize();
		CalculateLocalAnglesFromGlobalDirection(globalDirection, out localTargetDirection, out localTargetAngle);
	}

	public virtual bool AimAtThreat(Threat threat)
	{
		Vec3 estimatedTargetGlobalPoint = GetEstimatedTargetGlobalPoint(threat);
		return AimAtTarget(estimatedTargetGlobalPoint);
	}

	public bool AimAtTarget(Vec3 target)
	{
		CalculateLocalDirectionAndLocalAngleToShootTarget(target, out var localTargetDirection, out var localTargetAngle);
		if (localTargetDirection >= System.MathF.PI)
		{
			return false;
		}
		if (!_exactInputGiven || localTargetDirection != _inputTargetX || localTargetAngle != _inputTargetY)
		{
			GiveExactInput(localTargetDirection, localTargetAngle);
		}
		return CheckIsTargetReached(target);
	}

	public virtual bool CheckIsTargetReached(Vec3 target)
	{
		if (TaleWorlds.Library.MathF.Abs(CurrentDirection - _inputTargetX) < 0.001f)
		{
			return TaleWorlds.Library.MathF.Abs(CurrentReleaseAngle - _inputTargetY) < 0.001f;
		}
		return false;
	}

	public Vec3 GetEstimatedTargetGlobalPoint(Threat threat)
	{
		Vec3 targetingPosition = threat.TargetingPosition;
		return targetingPosition + GetEstimatedTargetMovementVector(targetingPosition, threat.GetGlobalVelocity());
	}

	public Vec3 GetEstimatedTargetGlobalPointForAgent(Agent agent)
	{
		return agent.CollisionCapsuleCenter + GetEstimatedTargetMovementVector(agent.CollisionCapsuleCenter, agent.GetAverageRealGlobalVelocity());
	}

	public virtual void AimAtRotation(float horizontalRotation, float verticalRotation)
	{
		horizontalRotation = MBMath.ClampFloat(horizontalRotation, -System.MathF.PI, System.MathF.PI);
		verticalRotation = MBMath.ClampFloat(verticalRotation, -System.MathF.PI, System.MathF.PI);
		horizontalRotation = MBMath.ClampAngle(horizontalRotation, 0f, DirectionRestriction);
		verticalRotation = MBMath.ClampAngle(verticalRotation, ReleaseAngleRestrictionCenter, ReleaseAngleRestrictionAngle);
		if (!_exactInputGiven || horizontalRotation != _inputTargetX || verticalRotation != _inputTargetY)
		{
			GiveExactInput(horizontalRotation, verticalRotation);
		}
	}

	protected void OnLoadingAmmoPointUsingCancelled(Agent agent, bool isCanceledBecauseOfAnimation)
	{
		if (agent.IsAIControlled)
		{
			if (isCanceledBecauseOfAnimation)
			{
				SendAgentToAmmoPickup(agent);
			}
			else
			{
				SendReloaderAgentToOriginalPoint();
			}
		}
	}

	protected void OnAmmoPickupUsingCancelled(Agent agent, bool isCanceledBecauseOfAnimation)
	{
		if (agent.IsAIControlled)
		{
			SendAgentToAmmoPickup(agent);
		}
	}

	protected void SendAgentToAmmoPickup(Agent agent)
	{
		ReloaderAgent = agent;
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		if (primaryWieldedItemIndex != EquipmentIndex.None && agent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.WeaponClass == OriginalMissileItem.PrimaryWeapon.WeaponClass)
		{
			agent.AIMoveToGameObjectEnable(LoadAmmoStandingPoint, this, base.Ai.GetScriptedFrameFlags(agent));
			return;
		}
		StandingPoint standingPoint = base.AmmoPickUpPoints.FirstOrDefault((StandingPoint x) => !x.HasUser);
		if (standingPoint != null)
		{
			agent.AIMoveToGameObjectEnable(standingPoint, this, base.Ai.GetScriptedFrameFlags(agent));
		}
		else
		{
			SendReloaderAgentToOriginalPoint();
		}
	}

	protected void SendReloaderAgentToOriginalPoint()
	{
		if (ReloaderAgent == null)
		{
			return;
		}
		if (ReloaderAgentOriginalPoint != null && !ReloaderAgentOriginalPoint.HasAIMovingTo && !ReloaderAgentOriginalPoint.HasUser)
		{
			if (ReloaderAgent.InteractingWithAnyGameObject())
			{
				ReloaderAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.None);
			}
			ReloaderAgent.AIMoveToGameObjectEnable(ReloaderAgentOriginalPoint, this, base.Ai.GetScriptedFrameFlags(ReloaderAgent));
		}
		else if (ReloaderAgentOriginalPoint == null || (ReloaderAgentOriginalPoint.MovingAgent != ReloaderAgent && ReloaderAgentOriginalPoint.UserAgent != ReloaderAgent))
		{
			if (ReloaderAgent.IsUsingGameObject)
			{
				ReloaderAgent.StopUsingGameObject();
			}
			ReloaderAgent = null;
		}
	}

	private void UpdateState(float dt)
	{
		if (LoadAmmoStandingPoint != null)
		{
			if (ReloaderAgent != null)
			{
				if (!ReloaderAgent.IsActive() || ReloaderAgent.Detachment != this)
				{
					ReloaderAgent = null;
				}
				else if (ReloaderAgentOriginalPoint.UserAgent == ReloaderAgent)
				{
					ReloaderAgent = null;
				}
			}
			if (State == WeaponState.LoadingAmmo && ReloaderAgent == null && !LoadAmmoStandingPoint.HasUser)
			{
				SortCanPickUpAmmoStandingPoints();
				StandingPoint standingPoint = null;
				StandingPoint standingPoint2 = null;
				foreach (StandingPoint canPickUpAmmoStandingPoint in CanPickUpAmmoStandingPoints)
				{
					if (canPickUpAmmoStandingPoint.HasUser && canPickUpAmmoStandingPoint.UserAgent.IsAIControlled)
					{
						if (canPickUpAmmoStandingPoint != base.PilotStandingPoint)
						{
							standingPoint = canPickUpAmmoStandingPoint;
							break;
						}
						standingPoint2 = canPickUpAmmoStandingPoint;
					}
				}
				if (standingPoint == null && standingPoint2 != null)
				{
					standingPoint = standingPoint2;
				}
				if (standingPoint != null)
				{
					if (HasAmmo)
					{
						Agent userAgent = standingPoint.UserAgent;
						userAgent.StopUsingGameObject(isSuccessful: true, Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
						ReloaderAgentOriginalPoint = standingPoint;
						SendAgentToAmmoPickup(userAgent);
					}
					else
					{
						base.IsDisabledForAI = true;
					}
				}
			}
		}
		switch (State)
		{
		case WeaponState.Reloading:
		{
			int num = 0;
			if (ReloadStandingPoints.Count == 0)
			{
				if (base.PilotAgent != null && !base.PilotAgent.IsInBeingStruckAction)
				{
					num = 1;
				}
			}
			else
			{
				foreach (StandingPoint reloadStandingPoint in ReloadStandingPoints)
				{
					if (reloadStandingPoint.HasUser && !reloadStandingPoint.UserAgent.IsInBeingStruckAction)
					{
						num++;
					}
				}
			}
			if (num == 0)
			{
				State = WeaponState.ReloadingPaused;
				break;
			}
			if (_currentReloaderCount != num)
			{
				_currentReloaderCount = num;
				float animationSpeed = TaleWorlds.Library.MathF.Sqrt(_currentReloaderCount);
				for (int j = 0; j < SkeletonOwnerObjects.Length; j++)
				{
					float animationParameterAtChannel2 = SkeletonOwnerObjects[j].GameEntity.Skeleton.GetAnimationParameterAtChannel(0);
					SkeletonOwnerObjects[j].SetAnimationAtChannelSynched(SetUpAnimations[j], 0, animationSpeed);
					if (animationParameterAtChannel2 > 0f)
					{
						SkeletonOwnerObjects[j].SetAnimationChannelParameterSynched(0, animationParameterAtChannel2);
					}
				}
			}
			for (int k = 0; k < Skeletons.Length; k++)
			{
				int animationIndexAtChannel2 = Skeletons[k].GetAnimationIndexAtChannel(0);
				float animationParameterAtChannel3 = Skeletons[k].GetAnimationParameterAtChannel(0);
				Skeletons[k].SetAnimationSpeedAtChannel(0, FinalReloadSpeed * ReloadSpeedMultiplier);
				if (animationIndexAtChannel2 == SetUpAnimationIndices[k] && animationParameterAtChannel3 >= 0.9999f)
				{
					State = WeaponState.LoadingAmmo;
					_animationTimeElapsed = 0f;
				}
			}
			break;
		}
		case WeaponState.ReloadingPaused:
			if (ReloadStandingPoints.Count == 0)
			{
				if (base.PilotAgent != null && !base.PilotAgent.IsInBeingStruckAction)
				{
					State = WeaponState.Reloading;
				}
				break;
			}
			{
				foreach (StandingPoint reloadStandingPoint2 in ReloadStandingPoints)
				{
					if (reloadStandingPoint2.HasUser && !reloadStandingPoint2.UserAgent.IsInBeingStruckAction)
					{
						State = WeaponState.Reloading;
						break;
					}
				}
				break;
			}
		case WeaponState.WaitingBeforeReloading:
			_animationTimeElapsed += dt;
			if (!(_animationTimeElapsed >= TimeGapBetweenShootingEndAndReloadingStart) || (_cameraState != CameraState.RememberLastShotDirection && _cameraState != CameraState.FreeMove && _cameraState != CameraState.StickToWeapon && !(CameraHolder == null)))
			{
				break;
			}
			if (ReloadStandingPoints.Count == 0)
			{
				if (base.PilotAgent != null && !base.PilotAgent.IsInBeingStruckAction)
				{
					State = WeaponState.Reloading;
				}
				break;
			}
			{
				foreach (StandingPoint reloadStandingPoint3 in ReloadStandingPoints)
				{
					if (reloadStandingPoint3.HasUser && !reloadStandingPoint3.UserAgent.IsInBeingStruckAction)
					{
						State = WeaponState.Reloading;
						break;
					}
				}
				break;
			}
		case WeaponState.WaitingBeforeProjectileLeaving:
			_animationTimeElapsed += dt;
			if (_animationTimeElapsed >= TimeGapBetweenShootActionAndProjectileLeaving)
			{
				State = WeaponState.Shooting;
			}
			break;
		case WeaponState.Shooting:
		{
			for (int i = 0; i < Skeletons.Length; i++)
			{
				int animationIndexAtChannel = Skeletons[i].GetAnimationIndexAtChannel(0);
				float animationParameterAtChannel = Skeletons[i].GetAnimationParameterAtChannel(0);
				if (animationIndexAtChannel == FireAnimationIndices[i] && animationParameterAtChannel >= 0.9999f)
				{
					State = ((!AttackClickWillReload) ? WeaponState.WaitingBeforeReloading : WeaponState.WaitingAfterShooting);
					_animationTimeElapsed = 0f;
				}
			}
			break;
		}
		default:
			Debug.FailedAssert("Invalid WeaponState.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Objects\\Siege\\RangedSiegeWeapon.cs", "UpdateState", 2000);
			break;
		case WeaponState.Idle:
		case WeaponState.WaitingAfterShooting:
		case WeaponState.LoadingAmmo:
		case WeaponState.WaitingBeforeIdle:
			break;
		}
	}

	public bool Shoot()
	{
		LastShooterAgent = base.PilotAgent;
		if (State == WeaponState.Idle)
		{
			State = WeaponState.WaitingBeforeProjectileLeaving;
			if (!GameNetwork.IsClientOrReplay)
			{
				_animationTimeElapsed = 0f;
			}
			return true;
		}
		return false;
	}

	public void ManualReload()
	{
		if (AttackClickWillReload)
		{
			State = WeaponState.WaitingBeforeReloading;
		}
	}

	public void AiRequestsShoot()
	{
		_aiRequestsShoot = true;
	}

	public void AiRequestsManualReload()
	{
		_aiRequestsManualReload = true;
	}

	private Vec3 GetBallisticErrorAppliedDirection(float BallisticErrorAmount)
	{
		Mat3 mat = new Mat3
		{
			f = ShootingDirection,
			u = Vec3.Up
		};
		mat.Orthonormalize();
		float a = MBRandom.RandomFloat * (System.MathF.PI * 2f);
		mat.RotateAboutForward(a);
		float f = BallisticErrorAmount * MBRandom.RandomFloat;
		mat.RotateAboutSide(f.ToRadians());
		return mat.f;
	}

	protected void ShootProjectile()
	{
		if (LoadedMissileItem.StringId == MultipleProjectileId)
		{
			ItemObject missileItem = Game.Current.ObjectManager.GetObject<ItemObject>(MultipleProjectileFlyingId);
			for (int i = 0; i < MultipleProjectileCount; i++)
			{
				ShootProjectileAux(missileItem, randomizeMissileSpeed: true);
			}
		}
		else if (LoadedMissileItem.StringId == MultipleFireProjectileId)
		{
			ItemObject missileItem2 = Game.Current.ObjectManager.GetObject<ItemObject>(MultipleFireProjectileFlyingId);
			for (int j = 0; j < MultipleProjectileCount; j++)
			{
				ShootProjectileAux(missileItem2, randomizeMissileSpeed: true);
			}
		}
		else if (LoadedMissileItem.StringId == SingleProjectileId)
		{
			ShootProjectileAux(Game.Current.ObjectManager.GetObject<ItemObject>(SingleProjectileFlyingId), randomizeMissileSpeed: false);
		}
		else if (LoadedMissileItem.StringId == SingleFireProjectileId)
		{
			ShootProjectileAux(Game.Current.ObjectManager.GetObject<ItemObject>(SingleFireProjectileFlyingId), randomizeMissileSpeed: false);
		}
		else
		{
			ShootProjectileAux(LoadedMissileItem, randomizeMissileSpeed: false);
		}
		LastShooterAgent = null;
	}

	protected virtual Mission.Missile ShootProjectileAux(ItemObject missileItem, bool randomizeMissileSpeed)
	{
		SetupProjectileToShoot(randomizeMissileSpeed, out var direction, out var orientation, out var missileBaseSpeed, out var missileShootingSpeed);
		MissionObject missionObjectToIgnore = base.GameEntity.Root.GetFirstScriptOfType<MissionObject>() ?? this;
		return Mission.Current.AddCustomMissile(LastShooterAgent, new MissionWeapon(missileItem, null, LastShooterAgent.Origin?.Banner, 1), ProjectileEntityCurrentGlobalPosition, direction, orientation, missileShootingSpeed, missileBaseSpeed, addRigidBody: false, missionObjectToIgnore);
	}

	protected void SetupProjectileToShoot(bool randomizeMissileSpeed, out Vec3 direction, out Mat3 orientation, out float missileBaseSpeed, out float missileShootingSpeed)
	{
		orientation = Mat3.Identity;
		Vec3 globalVelocity = GetGlobalVelocity();
		if (randomizeMissileSpeed)
		{
			float num = ShootingSpeed * MBRandom.RandomFloatRanged(0.9f, 1.1f);
			orientation.f = GetBallisticErrorAppliedDirection(2.5f);
			orientation.Orthonormalize();
			direction = num * orientation.f + globalVelocity;
			missileShootingSpeed = direction.Normalize();
			missileBaseSpeed = num;
		}
		else
		{
			orientation.f = GetBallisticErrorAppliedDirection(MaximumBallisticError);
			orientation.Orthonormalize();
			direction = ShootingSpeed * orientation.f + globalVelocity;
			missileShootingSpeed = direction.Normalize();
			missileBaseSpeed = ShootingSpeed;
		}
	}

	protected void OnRotationStarted()
	{
		if (MoveSound == null || !MoveSound.IsValid)
		{
			MoveSound = SoundEvent.CreateEvent(MoveSoundIndex, base.Scene);
			MoveSound.PlayInPosition(RotationObject.GameEntity.GlobalPosition);
		}
	}

	protected void OnRotationStopped()
	{
		MoveSound.Stop();
		MoveSound = null;
	}

	public abstract override SiegeEngineType GetSiegeEngineType();

	public bool CanShootAtThreat(Threat threat, int attemptCount = 5)
	{
		WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
		if (threat.TargetableObject != null)
		{
			weakGameEntity = threat.TargetableObject.GetTargetEntity();
		}
		(Vec3, Vec3) tuple = threat.ComputeGlobalTargetingBoundingBoxMinMax();
		Vec3 item = tuple.Item1;
		Vec3 item2 = tuple.Item2;
		Vec3 vec = (item2 + item) / 2f;
		Vec3 v = new Vec3(vec.AsVec2, item.z);
		Vec3 v2 = new Vec3(vec.AsVec2, item2.z);
		for (int i = 0; i < attemptCount; i++)
		{
			Vec3 targetPoint = Vec3.Lerp(v, v2, (float)i / (float)(attemptCount - 1));
			if (!base.Scene.RayCastForClosestEntityOrTerrainIgnoreEntity(MissileStartingGlobalPositionForSimulation, in targetPoint, base.GameEntity.Root, out var collisionDistance, out var collidedEntity, _projectileRadiusCached, BodyFlags.CommonCollisionExcludeFlagsForMissile) || (!(collidedEntity == null) && !(collidedEntity.Root != weakGameEntity.Root)))
			{
				Vec3 estimatedTargetMovementVector = GetEstimatedTargetMovementVector(targetPoint, threat.GetGlobalVelocity());
				targetPoint += estimatedTargetMovementVector;
				if ((!base.Scene.RayCastForClosestEntityOrTerrainIgnoreEntity(MissileStartingGlobalPositionForSimulation, in targetPoint, base.GameEntity.Root, out collisionDistance, out collidedEntity, _projectileRadiusCached, BodyFlags.CommonCollisionExcludeFlagsForMissile) || (!(collidedEntity == null) && !(collidedEntity.Root != weakGameEntity.Root))) && CanShootAtPoint(targetPoint))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CanShootAtAgent(Agent agent, int attemptCount = 5)
	{
		WeakGameEntity root = base.GameEntity.Root;
		(Vec3, Vec3) boxMinMax = agent.CollisionCapsule.GetBoxMinMax();
		Vec3 item = boxMinMax.Item1;
		Vec3 item2 = boxMinMax.Item2;
		Vec3 vec = (item2 + item) / 2f;
		Vec3 v = new Vec3(vec.AsVec2, item.z);
		Vec3 v2 = new Vec3(vec.AsVec2, item2.z);
		for (int i = 0; i < attemptCount; i++)
		{
			Vec3 targetPoint = Vec3.Lerp(v, v2, (float)i / (float)(attemptCount - 1));
			if (!base.Scene.RayCastForClosestEntityOrTerrainIgnoreEntity(MissileStartingGlobalPositionForSimulation, in targetPoint, root, out var collisionDistance, out var collidedEntity, _projectileRadiusCached, BodyFlags.CommonCollisionExcludeFlagsForMissile))
			{
				Vec3 targetVelocity = new Vec3(agent.GetAverageRealGlobalVelocity().AsVec2);
				Vec3 estimatedTargetMovementVector = GetEstimatedTargetMovementVector(targetPoint, targetVelocity);
				targetPoint += estimatedTargetMovementVector;
				if (!base.Scene.RayCastForClosestEntityOrTerrainIgnoreEntity(MissileStartingGlobalPositionForSimulation, in targetPoint, root, out collisionDistance, out collidedEntity, _projectileRadiusCached, BodyFlags.CommonCollisionExcludeFlagsForMissile) && CanShootAtPoint(targetPoint))
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual Vec3 GetEstimatedTargetMovementVector(Vec3 targetCurrentPosition, Vec3 targetVelocity)
	{
		if (targetVelocity != Vec3.Zero)
		{
			return targetVelocity * ((base.GameEntity.GlobalPosition - targetCurrentPosition).Length / ShootingSpeed + TimeGapBetweenShootActionAndProjectileLeaving);
		}
		return Vec3.Zero;
	}

	public bool CanShootAtPoint(Vec3 target)
	{
		CalculateLocalDirectionAndLocalAngleToShootTarget(target, out var localTargetDirection, out var localTargetAngle);
		if (localTargetAngle < BottomReleaseAngleRestriction || localTargetAngle > TopReleaseAngleRestriction)
		{
			return false;
		}
		if (DirectionRestriction / 2f - TaleWorlds.Library.MathF.Abs(localTargetDirection) < 0f)
		{
			return false;
		}
		if (CheckFriendlyFireForObjects(target))
		{
			return false;
		}
		Vec3 missileStartingGlobalPositionForSimulation = MissileStartingGlobalPositionForSimulation;
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 v = globalFrame.rotation.u;
		if (!v.IsUnit)
		{
			v.Normalize();
		}
		globalFrame.rotation.RotateAboutAnArbitraryVector(in v, System.MathF.PI + localTargetDirection);
		Vec3 v2 = globalFrame.rotation.s;
		if (!v2.IsUnit)
		{
			v2.Normalize();
		}
		globalFrame.rotation.RotateAboutAnArbitraryVector(in v2, localTargetAngle);
		float x = globalFrame.rotation.GetEulerAngles().x;
		Vec3 vec = ((MissileStartingPositionEntityForSimulation == null) ? CanShootAtPointCheckingOffset : Vec3.Zero);
		return CanShootPointBallistic(missileStartingGlobalPositionForSimulation + vec, x, ShootingSpeed, target);
	}

	private bool CanShootPointBallistic(Vec3 startGlobalPos, float verticalAngle, float shootingSpeed, Vec3 targetGlobalPos)
	{
		float num = shootingSpeed * TaleWorlds.Library.MathF.Sin(verticalAngle) / 9.806f;
		float z = 4.903f * num * num;
		Vec3 targetPoint = (startGlobalPos + targetGlobalPos) / 2f + new Vec3(0f, 0f, z);
		float projectileRadiusCached = _projectileRadiusCached;
		float collisionDistance;
		if (verticalAngle <= 0f)
		{
			Agent agent = Mission.Current.RayCastForClosestAgent(startGlobalPos, targetGlobalPos, -1, projectileRadiusCached, out collisionDistance);
			if (agent != null && !agent.IsEnemyOf(base.PilotAgent))
			{
				return false;
			}
		}
		else
		{
			if (base.Scene.RayCastForClosestEntityOrTerrainIgnoreEntity(in startGlobalPos, in targetPoint, base.GameEntity.Root, out collisionDistance, out var _, projectileRadiusCached, BodyFlags.CommonCollisionExcludeFlagsForMissile))
			{
				return false;
			}
			Agent agent2 = Mission.Current.RayCastForClosestAgent(startGlobalPos, targetPoint, -1, projectileRadiusCached, out collisionDistance);
			if (agent2 != null && !agent2.IsEnemyOf(base.PilotAgent))
			{
				return false;
			}
			agent2 = Mission.Current.RayCastForClosestAgent(targetPoint, targetGlobalPos, -1, projectileRadiusCached * 2f, out collisionDistance);
			if (agent2 != null && !agent2.IsEnemyOf(base.PilotAgent))
			{
				return false;
			}
		}
		return true;
	}

	protected virtual bool CheckFriendlyFireForObjects(Vec3 target)
	{
		if (Side == BattleSideEnum.Attacker)
		{
			foreach (SiegeWeapon item in Mission.Current.GetAttackerWeaponsForFriendlyFirePreventing())
			{
				if (item.GameEntity != null && item.GameEntity.IsVisibleIncludeParents())
				{
					Vec3 point = item.GameEntity.ComputeGlobalPhysicsBoundingBoxCenter();
					if ((MBMath.GetClosestPointOnLineSegmentToPoint(MissileStartingGlobalPositionForSimulation, in target, in point) - point).LengthSquared < 100f)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	protected internal virtual bool IsTargetValid(ITargetable target)
	{
		return true;
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		if (!base.IsDestroyed)
		{
			if (Side != side)
			{
				return OrderType.AttackEntity;
			}
			return OrderType.Use;
		}
		return OrderType.None;
	}

	protected override WeakGameEntity GetEntityToAttachNavMeshFaces()
	{
		return RotationObject.GameEntity;
	}

	public abstract float ProcessTargetValue(float baseValue, TargetFlags flags);

	public override void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
	{
		base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord, allowVisibilityUpdate);
		RangedSiegeWeaponRecord rangedSiegeWeaponRecord = (RangedSiegeWeaponRecord)(object)synchedMissionObjectReadableRecord.Item2;
		_state = (WeaponState)rangedSiegeWeaponRecord.State;
		TargetDirection = rangedSiegeWeaponRecord.TargetDirection;
		TargetReleaseAngle = MBMath.ClampFloat(rangedSiegeWeaponRecord.TargetReleaseAngle, BottomReleaseAngleRestriction, TopReleaseAngleRestriction);
		AmmoCount = rangedSiegeWeaponRecord.AmmoCount;
		CurrentDirection = TargetDirection;
		CurrentReleaseAngle = TargetReleaseAngle;
		CurrentDirection = TargetDirection;
		CurrentReleaseAngle = TargetReleaseAngle;
		ApplyCurrentDirectionToEntity();
		CheckAmmo();
		UpdateAmmoMesh();
		ChangeProjectileEntityClient(rangedSiegeWeaponRecord.ProjectileIndex);
	}

	protected virtual void UpdateAmmoMesh()
	{
		WeakGameEntity weakGameEntity = base.AmmoPickUpPoints[0].GameEntity;
		int num = StartingAmmoCount - AmmoCount;
		while (weakGameEntity.Parent.IsValid)
		{
			for (int i = 0; i < weakGameEntity.MultiMeshComponentCount; i++)
			{
				MetaMesh metaMesh = weakGameEntity.GetMetaMesh(i);
				for (int j = 0; j < metaMesh.MeshCount; j++)
				{
					metaMesh.GetMeshAtIndex(j).SetVectorArgument(0f, num, 0f, 0f);
				}
			}
			weakGameEntity = weakGameEntity.Parent;
		}
	}

	protected override bool IsAnyUserBelongsToFormation(Formation formation)
	{
		return base.IsAnyUserBelongsToFormation(formation) | (ReloaderAgent?.Formation == formation);
	}

	public virtual Vec3 GetGlobalVelocity()
	{
		return Vec3.Zero;
	}

	private float ComputeProjectileCapsuleRadius()
	{
		float result = 0.01f;
		if (LoadedMissileItem.BodyName != null)
		{
			PhysicsShape fromResource = PhysicsShape.GetFromResource(LoadedMissileItem.BodyName);
			BoundingBox boundingBox = new BoundingBox(in Vec3.Zero);
			fromResource.GetBoundingBox(out boundingBox);
			result = (boundingBox.max.AsVec2 - boundingBox.min.AsVec2).Length / 2f;
		}
		return result;
	}

	private void OnLoadedMissileItemChanged()
	{
		if (!LoadedMissileItem.StringId.Equals(_lastLoadedMissileItemId))
		{
			_projectileRadiusCached = ComputeProjectileCapsuleRadius();
			_lastLoadedMissileItemId = LoadedMissileItem.StringId;
		}
	}

	public void SetPlayerForceUse(bool value)
	{
		PlayerForceUse = value;
	}

	protected override bool ShouldDisableTickIfMachineDisabled()
	{
		return base.AmmoPickUpPoints.Count == 0;
	}

	public override void OnShipCaptured(BattleSideEnum newDefaultSide)
	{
		base.OnShipCaptured(newDefaultSide);
		DefaultSide = newDefaultSide;
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		(base.Ai as RangedSiegeWeaponAi).InitializeThreatSeeker();
	}
}

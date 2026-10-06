using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "sail_visual")]
public class SailVisual : ScriptComponentBehavior
{
	internal struct BurningRecord
	{
		internal List<BurningSystem> SailFires;

		internal BurningSystem MastFire;

		internal float SailLengthZ;

		internal BurningSystem YardLeftFire;

		internal float FireDt;

		internal BurningSystem YardRightFire;

		internal float YardFireStartDt;

		internal List<BurningSystem> RotatorFires;

		internal float RotatorFireStartDt;

		internal Color InitialYardMastColor;

		internal List<BurningSystem> StabilizerFires;

		internal bool BurningFinished;

		internal List<BurningSystem> FoldFires;

		internal List<BurningSystem> StaticRopeFires;

		internal float SailLengthX;

		internal BurningRecord(bool _ = false)
		{
			SailFires = new List<BurningSystem>();
			MastFire = null;
			YardLeftFire = null;
			YardRightFire = null;
			RotatorFires = new List<BurningSystem>();
			StabilizerFires = new List<BurningSystem>();
			FoldFires = new List<BurningSystem>();
			StaticRopeFires = new List<BurningSystem>();
			SailLengthX = 0f;
			SailLengthZ = 0f;
			FireDt = 0f;
			YardFireStartDt = 0f;
			RotatorFireStartDt = 0f;
			InitialYardMastColor = Color.White;
			BurningFinished = false;
		}
	}

	internal struct SailFoldProgress
	{
		internal const float FoldUnfoldSoundEventAnimationDxStopThreshold = 0.875f;

		internal float CurrentProgress;

		internal float RealProgress;

		internal bool FoldIsOngoing;

		internal bool UnfoldIsOngoing;

		internal int NumberOfMorphKeys;

		internal Vec3[] LeftVertexPositions;

		internal Vec3[] RightVertexPositions;

		internal Vec3[] CenterVertexPositions;

		internal Vec3 CurrentLeftFreeBonePosition;

		internal Vec3 CurrentRightFreeBonePosition;

		internal Vec3 CurrentCenterFreeBonePosition;

		internal SoundEvent FoldUnfoldSoundEvent;

		internal bool ShouldMakeFoldUnfoldSound;

		internal bool ShouldStopFoldUnfoldSound;
	}

	internal struct LateenSailData
	{
		internal GameEntity RollRotationEntity;

		internal GameEntity YardShiftEntity;

		internal float LastYawSection;

		internal float RollRotationAnimProgress;

		internal float RollRotationRealDt;

		internal bool RollRotationInProgress;

		internal float RollRotationInitial;

		internal float RollRotationTarget;

		internal float YardShiftInitial;

		internal float YardShiftTarget;

		internal SoundEvent RollAnimationSoundEvent;
	}

	internal struct PulleyDataCache
	{
		internal GameEntity Entity;

		internal PulleySystem PulleySystem;
	}

	internal struct SimpleRopeRecord
	{
		internal GameEntity ParentEntity;

		internal GameEntity RopeEntity;

		internal GameEntity TargetEntity;

		internal RopeSegment RopeSegment;

		internal bool StartPointAttachedToYard;

		internal bool EndPointAttachedToYard;

		internal bool IsBigRope;
	}

	internal struct KnobConnectionPoint
	{
		internal Vec3 ShipLocalPosition;

		internal Vec3 GlobalPosition;

		internal bool IsFixed;

		internal bool RightOfYard;

		internal void UpdateGlobalPosition(Vec3 pos)
		{
			GlobalPosition = pos;
		}

		internal void UpdateRightOfYard(bool value)
		{
			RightOfYard = value;
		}
	}

	internal class FreeBoneRecord
	{
		internal MatrixFrame InitialLocalFrame;

		internal MatrixFrame CurrentLocalFrame;

		internal Vec3 CurrentFrameWithoutRandomWind;

		internal GameEntity Entity;

		internal PulleyDataCache FoldSailPulley;

		internal List<PulleyDataCache> RotatorPulleys;

		internal List<PulleyDataCache> StabilityPulleys;

		internal List<SimpleRopeRecord> StabilityRopes;

		internal sbyte BoneIndex;

		internal FreeBoneConnectionType ConnectionType;

		internal FreeBoneType BoneType;
	}

	internal class FlagCaptureAnimation
	{
		internal bool AnimationInProgress;

		internal Texture NewBannerTexture;

		internal float DtTillStart;

		internal bool MaterialSet;

		internal float BannerWindFactor;
	}

	internal enum FreeBoneConnectionType
	{
		All,
		Closest,
		ClosestTwo
	}

	public enum SailType
	{
		SquareSail,
		LateenSail
	}

	internal enum KnobTypeEnum
	{
		Bollard,
		Cleat,
		Belaying
	}

	internal enum FreeBoneType
	{
		Left,
		Right,
		Center
	}

	internal enum LevelForEditor
	{
		None,
		Lvl1,
		Lvl2,
		Lvl3
	}

	private const string SailMeshEntityTag = "sail_mesh_entity";

	private const string StaticFoldedSailMeshEntityTag = "folded_static_entity";

	private const string SailTopBannerTag = "bd_banner_b";

	private const string FreeBoneTag = "free_bone";

	private const string RollRotationEntityTag = "roll_rotation_entity";

	private const string YawRotationEntityTag = "yaw_rotation_entity";

	private const string YardShiftEntityTag = "yard_shift";

	private const string SailYardEntityTag = "sail_yard";

	private const string PulleySystemsParentTag = "pulley_systems_parent";

	private const string FoldPulleysParentTag = "sail_fold_pulleys";

	private const string RotatePulleysParentTag = "sail_rotate_pulleys";

	private const string StabilityRopesParentTag = "stability_ropes_parent";

	private const string StaticRopesParentTag = "static_ropes_parent";

	private const string MastEntityTag = "mast_entity";

	private const string SimpleRopeTag = "simple_rope";

	private const string SimpleRopeStartTag = "simple_rope_start";

	private const string SimpleRopeEndTag = "simple_rope_end";

	private const string AttachedToYardTag = "attached_to_yard";

	private const string KnobPointsParentTag = "knob_points_parent";

	private const string KnobPointTag = "knot_point";

	private const string KnobPointDynamicTag = "dynamic_knob";

	private const string YardMeshEntity = "yard_mesh";

	private const string SailMeshBurningEntity = "sail_mesh_free_entity";

	private const string SquareSailLvl3ShiftEntityTag = "lvl3_shift_entity";

	private const string SquareSailLvl3Visibilitytag = "lvl3_lateens";

	private const string SquareSailLvl3MeshHoldertag = "lvl3_lateens_entity";

	private const string SquareSailLvl3FoldedParentTag = "lvl3_lateens_folded";

	private const string BallistaVisibilityRopeTag = "ballista_visibility";

	private const string TopFlagRopeTag = "flag_capture_rope";

	private static readonly string[] ClothFragmentPrefabs = new string[8] { "cloth_fragment_a", "cloth_fragment_b", "cloth_fragment_c", "cloth_fragment_e", "cloth_fragment_g", "cloth_fragment_i", "cloth_fragment_d", "cloth_fragment_h" };

	private const float InvisibleDistanceSquared = 22500f;

	private const float LinearDistanceSquared = 2025f;

	private static readonly int SailUnfoldSoundEventId = SoundManager.GetEventGlobalIndex("event:/mission/movement/vessel/sail/sail_open");

	private static readonly int SailFoldSoundEventId = SoundManager.GetEventGlobalIndex("event:/mission/movement/vessel/sail/sail_close");

	private static readonly int LateenSailRollSoundEventId = SoundManager.GetEventGlobalIndex("event:/mission/movement/vessel/sail/lateen_rotation");

	private List<KnobConnectionPoint> _knobConnectionPoints = new List<KnobConnectionPoint>();

	[EditableScriptComponentVariable(true, "Fold Sail Duration")]
	private float _foldSailDuration = 3f;

	[EditableScriptComponentVariable(true, "Folded Sail Transition Duration")]
	private float _foldedSailTransitionDuration = 0.5f;

	[EditableScriptComponentVariable(true, "Fold Free Bone Reset Duration")]
	private float _foldFreeBoneResetDuration = 1.2f;

	[EditableScriptComponentVariable(true, "Unfold Sail Duration")]
	private float _unfoldSailDuration = 4f;

	[EditableScriptComponentVariable(true, "Fold Sail Step Multiplier")]
	private float _foldSailStepMultiplier = 2f;

	[EditableScriptComponentVariable(true, "Lateen Yard Shift")]
	private float _lateenYardShift;

	[EditableScriptComponentVariable(true, "Lateen Roll Change Degree Limit")]
	private float _lateenRollChangeDegreeLimit = 20f;

	[EditableScriptComponentVariable(true, "Lateen Roll Change Animation Duration")]
	private float _lateenRollChangeAnimationDuration = 3f;

	[EditableScriptComponentVariable(true, "Lateen Roll Change Animation Step Multiplier")]
	private float _lateenRollChangeAnimationStepMultiplier = 1f;

	[EditableScriptComponentVariable(true, "Lateen Roll Change Yard Shift Start")]
	private float _lateenRollChangeYardShiftStart = 3f;

	[EditableScriptComponentVariable(true, "Lateen Roll Change Yard Shift Duration")]
	private float _lateenRollChangeYardShiftDuration = 4f;

	[EditableScriptComponentVariable(true, "Lateen Roll Change Yard Shift Acceleration")]
	private float _lateenRollChangeYardShiftAcceleration = 8f;

	[EditableScriptComponentVariable(true, "Lateen Roll Degrees")]
	private float _lateenRollDegrees = 45f;

	[EditableScriptComponentVariable(true, "Rope Connection Max Distance")]
	private float _ropeConnectionMaxDistance = 7f;

	[EditableScriptComponentVariable(true, "Knob Type")]
	private KnobTypeEnum _knobType;

	[EditableScriptComponentVariable(true, "Place Knobs")]
	private SimpleButton _placeKnobButton = new SimpleButton();

	[EditableScriptComponentVariable(true, "Knob Color")]
	private Color _placeKnobColor = Color.White;

	[EditableScriptComponentVariable(true, "Start Fire")]
	private SimpleButton _startFireButton = new SimpleButton();

	[EditableScriptComponentVariable(true, "Place Cloth Fragments")]
	private SimpleButton _placeClothFragments = new SimpleButton();

	[EditableScriptComponentVariable(true, "Sail Type")]
	private SailType _sailType;

	[EditableScriptComponentVariable(true, "Burning Animation Duration")]
	private float _burningAnimationDuration = 20f;

	private LateenSailData _lateenSailData;

	[EditableScriptComponentVariable(true, "Square Lvl3 Mast Shift")]
	private float _squareLvl3MastShift;

	[EditableScriptComponentVariable(true, "Editor Only Level Selection")]
	private LevelForEditor _editorOnlyLevelSelection;

	[EditableScriptComponentVariable(true, "Top Lateen Fire Material")]
	private Material _topLateenFireMaterial;

	[EditableScriptComponentVariable(true, "Editor Only Ship Health")]
	private float _editorOnlyShipHealth = 1f;

	[EditableScriptComponentVariable(true, "Top Flag Rope Position")]
	private float _topFlagRopePosition = 0.8f;

	[EditableScriptComponentVariable(true, "Capture Flag Bottom Rope Position")]
	private float _captureTheFlagBottomPosition = 0.25f;

	[EditableScriptComponentVariable(true, "Start Capture The Flag Animation")]
	private SimpleButton _startCaptureTheFlagAnimation = new SimpleButton();

	private SailFoldProgress _ongoingAnimationData;

	private readonly List<FreeBoneRecord> _freeBones = new List<FreeBoneRecord>();

	private readonly List<SimpleRopeRecord> _simpleRopes = new List<SimpleRopeRecord>();

	private readonly List<SimpleRopeRecord> _mastRopes = new List<SimpleRopeRecord>();

	private Skeleton _sailSkeleton;

	private float _totalFoldDuration;

	private float _totalUnfoldDuration;

	private float _mastClipDistanceFromOrigin = 100f;

	private GameEntity _mastEntity;

	private GameEntity _yardEntity;

	private Mesh _foldedStaticSailMesh;

	private GameEntity _foldedStaticSailEntity;

	private GameEntity _knobParent;

	private SimpleRopeRecord _topFlagRope;

	private GameEntity _burningSailEntity;

	private Mesh _burningSailMesh;

	private Vec3 _currentFrameGlobalWind = Vec3.Zero;

	private Mesh _yardMesh;

	private MatrixFrame _previousYawEntityFrame = MatrixFrame.Identity;

	private MatrixFrame _previousSailYardFrame = MatrixFrame.Identity;

	private float _cumulativeDt;

	private int _resetClothMeshFrameCounter;

	private bool _ropesAreInvisibleThisFrame;

	private bool _ropesWereInvisibleLastFrame;

	private bool _ropesWereLinearLastFrame;

	private bool _lodCheckFirstFrame = true;

	private List<WeakGameEntity> _topLateenSails = new List<WeakGameEntity>();

	private List<WeakGameEntity> _topLateenFoldedSails = new List<WeakGameEntity>();

	private List<WeakGameEntity> _ballistaVisibilityRopes = new List<WeakGameEntity>();

	private int _ballistaRopeEnableFrameCounter;

	private int _currentSailLevelUsed = -1;

	private BurningRecord _burningRecord;

	private bool _isBurning;

	private float _sailEntityAlpha = 1f;

	private float _lastMorphAnimKeySet = -1f;

	private int _remainingFramesForAnimation = 3;

	private float _foldAnimWindReductionFactor = 1f;

	private FlagCaptureAnimation _captureTheFlagAnimation;

	public float TotalFoldDuration => _totalFoldDuration;

	public float TotalUnfoldDuration => _totalUnfoldDuration;

	public ClothSimulatorComponent SailClothComponent { get; private set; }

	public GameEntity SailSkeletonEntity { get; private set; }

	public GameEntity SailYawRotationEntity { get; private set; }

	public ClothSimulatorComponent SailTopBannerClothComponent { get; private set; }

	public GameEntity SailTopBannerEntity { get; private set; }

	public SailType Type => _sailType;

	public ShipVisual ShipVisual { get; private set; }

	public bool SailEnabled { get; set; } = true;

	public bool SoundsEnabled { get; set; }

	public bool FoldAnimationEnabled { get; set; } = true;

	internal SailVisual()
	{
		_ongoingAnimationData.CurrentProgress = 0f;
		_ongoingAnimationData.RealProgress = 0f;
		_ongoingAnimationData.FoldIsOngoing = false;
		_ongoingAnimationData.UnfoldIsOngoing = false;
		_ongoingAnimationData.LeftVertexPositions = null;
		_ongoingAnimationData.RightVertexPositions = null;
		_ongoingAnimationData.CenterVertexPositions = null;
		_ongoingAnimationData.NumberOfMorphKeys = -1;
		_ongoingAnimationData.CurrentLeftFreeBonePosition = Vec3.Zero;
		_ongoingAnimationData.CurrentRightFreeBonePosition = Vec3.Zero;
		_ongoingAnimationData.CurrentCenterFreeBonePosition = Vec3.Zero;
		_ongoingAnimationData.FoldUnfoldSoundEvent = null;
		_ongoingAnimationData.ShouldMakeFoldUnfoldSound = false;
		_ongoingAnimationData.ShouldStopFoldUnfoldSound = false;
		_lateenSailData = default(LateenSailData);
		_lateenSailData.RollRotationEntity = null;
		_lateenSailData.YardShiftEntity = null;
		_lateenSailData.LastYawSection = 0f;
		_lateenSailData.RollRotationAnimProgress = 0f;
		_lateenSailData.RollRotationRealDt = 0f;
		_lateenSailData.RollRotationInProgress = false;
		_lateenSailData.RollRotationInitial = 0f;
		_lateenSailData.RollRotationTarget = 0f;
		_lateenSailData.YardShiftInitial = 0f;
		_lateenSailData.YardShiftTarget = 0f;
		_lateenSailData.RollAnimationSoundEvent = null;
		_captureTheFlagAnimation = new FlagCaptureAnimation();
		_captureTheFlagAnimation.AnimationInProgress = false;
		_captureTheFlagAnimation.NewBannerTexture = null;
		_captureTheFlagAnimation.DtTillStart = 0f;
		_captureTheFlagAnimation.MaterialSet = false;
		_captureTheFlagAnimation.BannerWindFactor = 1f;
		_topFlagRope = default(SimpleRopeRecord);
	}

	protected override void OnEditorInit()
	{
		SoundsEnabled = true;
		_editorOnlyLevelSelection = LevelForEditor.None;
		_editorOnlyShipHealth = 1f;
		FetchEntities();
		UpdatePreviousYardFrame();
		if (_yardEntity != null)
		{
			MatrixFrame frame = _yardEntity.GetGlobalFrame();
			_previousSailYardFrame = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame);
		}
		if (_sailType == SailType.LateenSail)
		{
			InitLateenSailData();
		}
		InitSailFoldAnimationResources();
		if (_sailSkeleton != null)
		{
			_sailSkeleton.EnableScriptDrivenPostIntegrateCallback();
		}
		SailTopBannerClothComponent?.SetForcedGustStrength(0f);
		SailClothComponent?.SetForcedGustStrength(0f);
		UpdateTotalFoldDuration();
		UpdateTotalUnfoldDuration();
		ComputeMastClipPlane();
		PlaceClothFragmentsRandomly((int)(Time.ApplicationTime * 100f));
		PlaceTopFlag(_topFlagRopePosition);
	}

	protected override void OnEditorTick(float dt)
	{
		_cumulativeDt += dt;
		FetchEntities();
		if (_ongoingAnimationData.NumberOfMorphKeys == -1)
		{
			InitSailFoldAnimationResources();
		}
		ComputeMastClipPlane();
		if (!_isBurning)
		{
			HandleLOD();
		}
		CheckFoldAnimationState(dt);
		if (_sailType == SailType.LateenSail)
		{
			TickLateenSail(dt);
		}
		if (SailSkeletonEntity != null)
		{
			SetButtomRopePositions(dt, disableWind: false);
		}
		if (!_ropesAreInvisibleThisFrame)
		{
			TickRopesAndPulleys();
		}
		if (Input.IsKeyReleased(InputKey.F3))
		{
			SailEnabled = false;
		}
		else if (Input.IsKeyReleased(InputKey.F4))
		{
			SailEnabled = true;
		}
		FoldUnfoldSoundEventTick();
		if (_editorOnlyLevelSelection == LevelForEditor.None)
		{
			int num = FetchSailLevel();
			if (num != _currentSailLevelUsed)
			{
				AdjustLevelOfSail(num);
				_currentSailLevelUsed = num;
			}
		}
		CheckClothResetTimer();
		if (_sailSkeleton != null)
		{
			_sailSkeleton.EnableScriptDrivenPostIntegrateCallback();
		}
		if (_isBurning && !_burningRecord.BurningFinished)
		{
			TickFire(dt);
		}
		UpdateMastClipPlane();
		if (_captureTheFlagAnimation.AnimationInProgress)
		{
			TickFlagCaptureAnimation(dt);
		}
	}

	protected override void OnInit()
	{
		ShipVisual = base.GameEntity.Root.GetFirstScriptOfType<ShipVisual>();
		ShipVisual?.SailVisuals?.Add(this);
		_editorOnlyLevelSelection = LevelForEditor.None;
		_editorOnlyShipHealth = 1f;
		FetchEntities();
		UpdatePreviousYardFrame();
		if (_yardEntity != null)
		{
			MatrixFrame frame = _yardEntity.GetGlobalFrame();
			_previousSailYardFrame = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame);
		}
		if (_sailType == SailType.LateenSail)
		{
			InitLateenSailData();
		}
		InitSailFoldAnimationResources();
		int num = FetchSailLevel();
		AdjustLevelOfSail(num);
		_currentSailLevelUsed = num;
		if (_sailSkeleton != null)
		{
			_sailSkeleton.EnableScriptDrivenPostIntegrateCallback();
		}
		SailTopBannerClothComponent?.SetForcedGustStrength(0f);
		SailClothComponent?.SetForcedGustStrength(0f);
		UpdateTotalFoldDuration();
		UpdateTotalUnfoldDuration();
		ComputeMastClipPlane();
		int seed = (int)(Time.ApplicationTime * 100f);
		if (ShipVisual != null)
		{
			seed = ShipVisual.Seed;
		}
		PlaceClothFragmentsRandomly(seed);
	}

	protected override void OnTickParallel(float dt)
	{
		_cumulativeDt += dt;
		if (_ongoingAnimationData.NumberOfMorphKeys == -1)
		{
			InitSailFoldAnimationResources();
		}
		HandleLOD();
		if (_remainingFramesForAnimation == 0)
		{
			CheckFoldAnimationState(dt);
		}
		else
		{
			_remainingFramesForAnimation--;
		}
		if (_sailType == SailType.LateenSail)
		{
			TickLateenSail(dt);
		}
		if (SailSkeletonEntity != null)
		{
			SetButtomRopePositions(dt, disableWind: false);
		}
		if (!_ropesAreInvisibleThisFrame)
		{
			TickRopesAndPulleys();
		}
		CheckClothResetTimer();
		if (_isBurning && !_burningRecord.BurningFinished)
		{
			TickFire(dt);
		}
		UpdateMastClipPlane();
		if (_ballistaRopeEnableFrameCounter > 0)
		{
			_ballistaRopeEnableFrameCounter--;
			if (_ballistaRopeEnableFrameCounter == 0)
			{
				foreach (WeakGameEntity ballistaVisibilityRope in _ballistaVisibilityRopes)
				{
					ballistaVisibilityRope.SetVisibilityExcludeParents(visible: true);
				}
			}
		}
		if (_captureTheFlagAnimation.AnimationInProgress)
		{
			TickFlagCaptureAnimation(dt);
		}
	}

	protected override void OnTick(float dt)
	{
		FoldUnfoldSoundEventTick();
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
	}

	protected override bool SkeletonPostIntegrateCallback(AnimResult result)
	{
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.BoneIndex != -1)
			{
				Vec3 origin = freeBone.CurrentLocalFrame.origin;
				sbyte parentBoneIndex = _sailSkeleton.GetParentBoneIndex(freeBone.BoneIndex);
				Transformation entitialOutTransform = result.GetEntitialOutTransform(parentBoneIndex, _sailSkeleton);
				result.SetOutBoneDisplacement(freeBone.BoneIndex, entitialOutTransform.TransformToLocal(origin), _sailSkeleton);
			}
		}
		if (_sailType == SailType.LateenSail && _mastEntity != null)
		{
			sbyte boneIndex = 2;
			MatrixFrame globalFrame = SailSkeletonEntity.GetGlobalFrame();
			MatrixFrame m = _mastEntity.GetGlobalFrame();
			Vec3 u = m.rotation.u;
			MBMath.FindPlaneLineIntersectionPointWithNormal(m.origin, u, globalFrame.origin, globalFrame.origin - u * 100f, out var _);
			MatrixFrame globalFrame2 = _yardEntity.GetGlobalFrame();
			m.origin += globalFrame2.rotation.f * 0.25f * globalFrame.rotation.f.Length;
			MatrixFrame matrixFrame = globalFrame;
			matrixFrame.rotation.MakeUnit();
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = globalFrame.TransformToLocalNonOrthogonal(in m.origin);
			identity.rotation = matrixFrame.TransformToLocal(in m).rotation;
			sbyte parentBoneIndex2 = _sailSkeleton.GetParentBoneIndex(boneIndex);
			Transformation transformation = result.GetEntitialOutTransform(parentBoneIndex2, _sailSkeleton).TransformToLocal(Transformation.CreateFromMatrixFrame(identity));
			transformation.Rotate(-System.MathF.PI / 2f, Vec3.Forward);
			result.SetOutBoneDisplacement(boneIndex, transformation.Origin, _sailSkeleton);
			result.SetOutQuat(boneIndex, transformation.Rotation, _sailSkeleton);
		}
		return true;
	}

	protected override void OnBoundingBoxValidate()
	{
		if (_yardEntity == null || _sailSkeleton == null)
		{
			return;
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		BoundingBox bb = default(BoundingBox);
		bb.BeginRelaxation();
		BoundingBox localBoundingBox = _yardEntity.GetLocalBoundingBox();
		Vec3 v = _yardEntity.GetGlobalFrame().origin;
		v = globalFrame.TransformToLocalNonOrthogonal(in v);
		float num = localBoundingBox.radius * 1.1f;
		bb.RelaxMinMaxWithPoint(v + Vec3.One * num);
		bb.RelaxMinMaxWithPoint(v - Vec3.One * num);
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.FoldSailPulley.PulleySystem != null)
			{
				freeBone.FoldSailPulley.PulleySystem.ApplyBoundingBox(globalFrame, ref bb);
			}
			if (freeBone.RotatorPulleys != null)
			{
				foreach (PulleyDataCache rotatorPulley in freeBone.RotatorPulleys)
				{
					if (rotatorPulley.PulleySystem != null)
					{
						rotatorPulley.PulleySystem.ApplyBoundingBox(globalFrame, ref bb);
					}
				}
			}
			if (freeBone.StabilityPulleys != null)
			{
				foreach (PulleyDataCache stabilityPulley in freeBone.StabilityPulleys)
				{
					if (stabilityPulley.PulleySystem != null)
					{
						stabilityPulley.PulleySystem.ApplyBoundingBox(globalFrame, ref bb);
					}
				}
			}
			if (freeBone.StabilityRopes == null)
			{
				continue;
			}
			foreach (SimpleRopeRecord stabilityRope in freeBone.StabilityRopes)
			{
				if (stabilityRope.RopeSegment != null)
				{
					stabilityRope.RopeSegment.ApplyBoundingBox(globalFrame, ref bb);
				}
			}
		}
		if (_simpleRopes != null)
		{
			foreach (SimpleRopeRecord simpleRope in _simpleRopes)
			{
				if (simpleRope.RopeSegment != null)
				{
					simpleRope.RopeSegment.ApplyBoundingBox(globalFrame, ref bb);
				}
			}
		}
		base.GameEntity.RelaxLocalBoundingBox(in bb);
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		if (_lateenSailData.RollAnimationSoundEvent != null)
		{
			_lateenSailData.RollAnimationSoundEvent.Stop();
			_lateenSailData.RollAnimationSoundEvent = null;
		}
		_ongoingAnimationData.LeftVertexPositions = null;
		_ongoingAnimationData.RightVertexPositions = null;
		_ongoingAnimationData.CenterVertexPositions = null;
		_ongoingAnimationData.ShouldMakeFoldUnfoldSound = false;
		_ongoingAnimationData.ShouldStopFoldUnfoldSound = true;
		if (_ongoingAnimationData.FoldUnfoldSoundEvent != null)
		{
			_ongoingAnimationData.FoldUnfoldSoundEvent.Stop();
			_ongoingAnimationData.FoldUnfoldSoundEvent = null;
		}
		_freeBones.Clear();
		_simpleRopes.Clear();
		_mastRopes.Clear();
		bool flag = base.GameEntity.IsGhostObject();
		if (_sailSkeleton != null && !flag)
		{
			base.GameEntity.Scene.RemoveAlwaysRenderedSkeleton(_sailSkeleton);
		}
		_sailSkeleton = null;
		_yardEntity = null;
		_foldedStaticSailEntity = null;
		SailClothComponent = null;
		SailTopBannerClothComponent = null;
		SailTopBannerEntity = null;
		SailSkeletonEntity = null;
		SailYawRotationEntity = null;
		_mastEntity = null;
		_isBurning = false;
	}

	protected override bool OnCheckForProblems()
	{
		return CheckForProblemsInternal();
	}

	protected override void OnSaveAsPrefab()
	{
		CheckForProblemsInternal();
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | TickRequirement.TickParallel;
	}

	public void RefreshSailVisual()
	{
		int num = FetchSailLevel();
		AdjustLevelOfSail(num);
		_currentSailLevelUsed = num;
	}

	public void UpdateForcedWindOfSailsAndTopBanner(float dt, Vec3 globalBannerRelativeWindVelocity, in Vec3 sailRelativeGlobalWindVelocity, in Vec3 globalSailForce)
	{
		if (globalBannerRelativeWindVelocity.LengthSquared >= 100f)
		{
			globalBannerRelativeWindVelocity = globalBannerRelativeWindVelocity.NormalizedCopy() * 10f;
		}
		globalBannerRelativeWindVelocity /= Scene.MaximumWindSpeed;
		globalBannerRelativeWindVelocity *= _captureTheFlagAnimation.BannerWindFactor;
		SailTopBannerClothComponent?.SetForcedWind(globalBannerRelativeWindVelocity, isLocal: false);
		Vec3 vec = globalSailForce.RotateVectorToXYPlane().NormalizedCopy();
		Vec3 v = sailRelativeGlobalWindVelocity.AsVec2.Length * vec * _foldAnimWindReductionFactor;
		v *= 2f;
		_currentFrameGlobalWind = Vec3.Lerp(_currentFrameGlobalWind, v, dt);
		if (_currentFrameGlobalWind.LengthSquared >= 100f)
		{
			_currentFrameGlobalWind = _currentFrameGlobalWind.NormalizedCopy() * 10f;
		}
		SailClothComponent.SetForcedWind(_currentFrameGlobalWind / Scene.MaximumWindSpeed, isLocal: false);
	}

	public void SetFoldSailDuration(float foldSailDuration)
	{
		_foldSailDuration = foldSailDuration;
		UpdateTotalFoldDuration();
	}

	public void SetFoldSailStepMultiplier(float foldSailStepMultiplier)
	{
		_foldSailStepMultiplier = foldSailStepMultiplier;
		UpdateTotalFoldDuration();
	}

	public void SetUnfoldSailDuration(float unfoldSailDuration)
	{
		_unfoldSailDuration = unfoldSailDuration;
		UpdateTotalUnfoldDuration();
	}

	public void SetSailEntityAlpha(float alpha)
	{
		_sailEntityAlpha = alpha;
		base.GameEntity.SetAlpha(alpha);
	}

	public void InstantCloseSails()
	{
		SailEnabled = false;
		_ongoingAnimationData.FoldIsOngoing = true;
		_ongoingAnimationData.CurrentProgress = _foldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration;
		_ongoingAnimationData.UnfoldIsOngoing = false;
	}

	private bool CheckForProblemsInternal()
	{
		bool result = true;
		if (SailTopBannerClothComponent != null)
		{
			MetaMesh firstMetaMesh = SailTopBannerClothComponent.GetFirstMetaMesh();
			for (int i = 0; i < firstMetaMesh.MeshCount; i++)
			{
				Mesh meshAtIndex = firstMetaMesh.GetMeshAtIndex(i);
				if (meshAtIndex.HasCloth() && meshAtIndex.GetClothLinearVelocityMultiplier() != 0f)
				{
					string text = ((base.GameEntity.Root != base.GameEntity) ? (base.GameEntity.Root.Name + "|" + base.GameEntity.Name) : base.GameEntity.Name);
					string msg = "Top banner (" + meshAtIndex.Name + ") of Sail Entity (" + text + ") has non-zero linear velocity cloth parameter.";
					MBEditor.AddEntityWarning(SailTopBannerClothComponent.GetEntity(), msg);
					result = false;
				}
			}
		}
		return result;
	}

	private void PlaceKnobs()
	{
		string prefabName = "";
		if (_knobType == KnobTypeEnum.Bollard)
		{
			prefabName = "bollard_a";
		}
		else if (_knobType == KnobTypeEnum.Cleat)
		{
			prefabName = "cleat_a";
		}
		else if (_knobType == KnobTypeEnum.Belaying)
		{
			prefabName = "belaying_pins_a";
		}
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		List<WeakGameEntity> list2 = new List<WeakGameEntity>();
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.RotatorPulleys != null)
			{
				foreach (PulleyDataCache rotatorPulley in freeBone.RotatorPulleys)
				{
					WeakGameEntity firstFixedEntity = rotatorPulley.PulleySystem.FirstFixedEntity;
					if (firstFixedEntity.IsValid)
					{
						list2.Add(firstFixedEntity);
					}
				}
			}
			if (freeBone.StabilityPulleys != null)
			{
				foreach (PulleyDataCache stabilityPulley in freeBone.StabilityPulleys)
				{
					WeakGameEntity firstFixedEntity2 = stabilityPulley.PulleySystem.FirstFixedEntity;
					if (firstFixedEntity2.IsValid)
					{
						list2.Add(firstFixedEntity2);
					}
				}
			}
			if (freeBone.StabilityRopes == null)
			{
				continue;
			}
			foreach (SimpleRopeRecord stabilityRope in freeBone.StabilityRopes)
			{
				if (stabilityRope.RopeEntity != null)
				{
					list2.Add(stabilityRope.RopeEntity.WeakEntity);
				}
			}
		}
		foreach (WeakGameEntity item in list2)
		{
			int num = item.ChildCount - 1;
			while (num >= 0 && num < item.ChildCount)
			{
				WeakGameEntity child = item.GetChild(num);
				if (!child.HasScriptComponent("rope_segment_cosmetics"))
				{
					item.RemoveChild(child, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: true, 37);
				}
				num--;
			}
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName, callScriptCallbacks: true);
			if (!(gameEntity != null))
			{
				continue;
			}
			item.AddChild(gameEntity.WeakEntity);
			list.Clear();
			foreach (GameEntity child2 in gameEntity.GetChildren())
			{
				if (child2.HasTag("knot_point"))
				{
					list.Add(child2.WeakEntity);
				}
			}
			if (list.Count > 0)
			{
				MatrixFrame frame = list[MBRandom.RandomInt(list.Count)].GetFrame();
				frame.Fill();
				MatrixFrame frame2 = frame.Inverse();
				item.SetFrame(ref frame2);
			}
			foreach (Mesh item2 in item.GetAllMeshesWithTag("auto_factor_color"))
			{
				item2.Color = _placeKnobColor.ToUnsignedInteger();
			}
		}
	}

	private void SetKnobColors()
	{
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.RotatorPulleys != null)
			{
				foreach (PulleyDataCache rotatorPulley in freeBone.RotatorPulleys)
				{
					WeakGameEntity firstFixedEntity = rotatorPulley.PulleySystem.FirstFixedEntity;
					if (firstFixedEntity != null)
					{
						list.Add(firstFixedEntity);
					}
				}
			}
			if (freeBone.StabilityPulleys != null)
			{
				foreach (PulleyDataCache stabilityPulley in freeBone.StabilityPulleys)
				{
					WeakGameEntity firstFixedEntity2 = stabilityPulley.PulleySystem.FirstFixedEntity;
					if (firstFixedEntity2 != null)
					{
						list.Add(firstFixedEntity2);
					}
				}
			}
			if (freeBone.StabilityRopes == null)
			{
				continue;
			}
			foreach (SimpleRopeRecord stabilityRope in freeBone.StabilityRopes)
			{
				if (stabilityRope.RopeEntity != null)
				{
					list.Add(stabilityRope.RopeEntity.WeakEntity);
				}
			}
		}
		foreach (WeakGameEntity item in list)
		{
			foreach (Mesh item2 in item.GetAllMeshesWithTag("auto_factor_color"))
			{
				item2.Color = _placeKnobColor.ToUnsignedInteger();
			}
		}
	}

	private int FetchSailLevel()
	{
		int num = -1;
		WeakGameEntity firstChildEntityWithTagRecursive = base.GameEntity.GetFirstChildEntityWithTagRecursive("upgrade_slot");
		if (firstChildEntityWithTagRecursive != null)
		{
			foreach (WeakGameEntity child in firstChildEntityWithTagRecursive.GetChildren())
			{
				if (child.GetVisibilityExcludeParents())
				{
					if (child.HasTag("base"))
					{
						if (num != -1)
						{
							return -1;
						}
						num = 1;
					}
					else if (child.HasTag("lvl2"))
					{
						if (num != -1)
						{
							return -1;
						}
						num = 2;
					}
					else if (child.HasTag("lvl3"))
					{
						if (num != -1)
						{
							return -1;
						}
						num = 3;
					}
				}
			}
			return num;
		}
		return 1;
	}

	private void CheckClothResetTimer()
	{
		if (_resetClothMeshFrameCounter > 0)
		{
			_resetClothMeshFrameCounter--;
			if (_resetClothMeshFrameCounter == 0 && SailClothComponent != null)
			{
				SailClothComponent.SetResetRequired();
			}
		}
	}

	private void SetSailMaterialWrtLevel(Mesh mesh, int sailLevel, bool isEditorScene)
	{
		if (sailLevel == -1 && !isEditorScene)
		{
			return;
		}
		List<string> list = new List<string>();
		WeakGameEntity root = base.GameEntity.Root;
		int num = 0;
		num = ((!isEditorScene) ? (ShipVisual?.Seed ?? ((int)((ulong)root.Pointer & 0xFFFFFFFFu))) : (num + (int)(_cumulativeDt * 5f)));
		float num2 = 1f;
		if (ShipVisual != null)
		{
			num2 = ShipVisual.Health;
		}
		else if (isEditorScene)
		{
			num2 = _editorOnlyShipHealth;
		}
		Material material = null;
		if (ShipVisual != null && !string.IsNullOrEmpty(ShipVisual.CustomSailPatternId))
		{
			material = Material.GetFromResource(ShipVisual.CustomSailPatternId);
		}
		if (material == null)
		{
			Random random = new Random(num);
			if (_sailType == SailType.SquareSail)
			{
				switch (sailLevel)
				{
				case 1:
					list.Add("00");
					break;
				case 2:
					list.Add("04");
					list.Add("05");
					list.Add("06");
					list.Add("10");
					break;
				case 3:
					list.Add("01");
					list.Add("02");
					list.Add("03");
					list.Add("07");
					list.Add("08");
					list.Add("09");
					list.Add("11");
					break;
				}
			}
			else
			{
				switch (sailLevel)
				{
				case 1:
					list.Add("00");
					break;
				case 2:
					list.Add("04");
					list.Add("06");
					break;
				case 3:
					list.Add("01");
					list.Add("02");
					list.Add("03");
					list.Add("05");
					list.Add("07");
					list.Add("08");
					list.Add("09");
					break;
				}
			}
			string text = "generated_";
			text += ((_sailType == SailType.SquareSail) ? "square_" : "lateen_");
			text = ((sailLevel == 1) ? ((num2 > 0.75f) ? (text + "l1_h4_") : ((num2 > 0.5f) ? (text + "l1_h3_") : ((!(num2 > 0.25f)) ? (text + "l1_h1_") : (text + "l1_h2_")))) : ((num2 > 0.75f) ? (text + "_h4_") : ((num2 > 0.5f) ? (text + "_h3_") : ((!(num2 > 0.25f)) ? (text + "_h1_") : (text + "_h2_")))));
			if (list.Count > 0)
			{
				text += list[random.Next(list.Count)];
			}
			material = Material.GetFromResource(text);
		}
		if (mesh.HasTag("faction_color"))
		{
			if (material != null)
			{
				mesh.SetMaterial(material);
			}
			if (ShipVisual != null)
			{
				mesh.Color = ShipVisual.SailColors.sailColor1;
				mesh.Color2 = ShipVisual.SailColors.sailColor2;
			}
		}
	}

	private void AdjustSquareSailSpecificLevelData(int sailLevel, bool isEditorScene)
	{
		bool flag = sailLevel == 3;
		if ((flag && _currentSailLevelUsed != 3) || (!flag && _currentSailLevelUsed == 3))
		{
			float num = _squareLvl3MastShift;
			if (_currentSailLevelUsed == 3 && !flag)
			{
				num *= -1f;
			}
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			base.GameEntity.GetChildrenWithTagRecursive(list, "lvl3_shift_entity");
			foreach (WeakGameEntity item in list)
			{
				MatrixFrame frame = item.GetFrame();
				frame.origin.z += num;
				item.SetLocalFrame(ref frame, isTeleportation: true);
			}
		}
		List<WeakGameEntity> list2 = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenWithTagRecursive(list2, "lvl3_lateens");
		foreach (WeakGameEntity item2 in list2)
		{
			item2.SetDoNotCheckVisibility(value: true);
			item2.SetVisibilityExcludeParents(flag);
		}
		foreach (WeakGameEntity topLateenSail in _topLateenSails)
		{
			topLateenSail.SetDoNotCheckVisibility(value: true);
			foreach (Mesh item3 in topLateenSail.GetAllMeshesWithTag("faction_color"))
			{
				SetSailMaterialWrtLevel(item3, sailLevel, isEditorScene);
			}
		}
		foreach (WeakGameEntity topLateenFoldedSail in _topLateenFoldedSails)
		{
			topLateenFoldedSail.SetDoNotCheckVisibility(value: true);
			foreach (Mesh item4 in topLateenFoldedSail.GetAllMeshesWithTag("faction_color"))
			{
				SetSailMaterialWrtLevel(item4, sailLevel, isEditorScene);
			}
		}
	}

	private void AdjustLevelOfSail(int sailLevel)
	{
		if (_sailSkeleton == null)
		{
			return;
		}
		bool isEditorScene = base.GameEntity.Scene.IsEditorScene();
		foreach (Mesh allMesh in _sailSkeleton.GetAllMeshes())
		{
			if (allMesh.HasTag("faction_color"))
			{
				SetSailMaterialWrtLevel(allMesh, sailLevel, isEditorScene);
			}
		}
		if (_sailType == SailType.SquareSail)
		{
			AdjustSquareSailSpecificLevelData(sailLevel, isEditorScene);
		}
		if (!(_foldedStaticSailEntity != null))
		{
			return;
		}
		foreach (Mesh item in _foldedStaticSailEntity.GetAllMeshesWithTag("faction_color"))
		{
			SetSailMaterialWrtLevel(item, sailLevel, isEditorScene);
		}
	}

	private void ApplyRandomWindToRope(ref Vec3 position, float factor)
	{
		Vec3 vec = new Vec3((float)Math.Cos(position.x * 2.5f + _cumulativeDt * 4.5f), (float)Math.Cos(position.y * 1.2f + _cumulativeDt * 6.5f), (float)Math.Cos(position.z * 3.5f + _cumulativeDt * 3.5f));
		position += vec * 0.1f * factor;
	}

	private void SetButtomRopePositions(float dt, bool disableWind)
	{
		Vec2 globalWindVelocityOfScene = base.GameEntity.GetGlobalWindVelocityOfScene();
		float num = TaleWorlds.Library.MathF.Min(globalWindVelocityOfScene.Normalize(), 8f);
		float num2 = TaleWorlds.Library.MathF.Min(num, 4f);
		float num3 = (float)Math.Pow(num / 8f, 0.44999998807907104) * 8f;
		num2 = (float)Math.Pow(num / 4f, 0.44999998807907104) * 4f;
		Vec3 v = new Vec3(globalWindVelocityOfScene);
		MatrixFrame globalFrame = SailSkeletonEntity.GetGlobalFrame();
		globalFrame.rotation.Orthonormalize();
		Vec3 vec = globalFrame.rotation.TransformToLocal(in v);
		if (vec.Length > 0f)
		{
			vec.Normalize();
		}
		if (_yardEntity != null)
		{
			Vec3 f = _yardEntity.GetGlobalFrame().rotation.f;
			f.Normalize();
			float num4 = TaleWorlds.Library.MathF.Clamp(Vec3.DotProduct(v, f), 0f, 1f);
			num3 *= 0.5f + 0.5f * num4;
			num2 *= 0.5f + 0.5f * num4;
		}
		float amount = 0f;
		if (_ongoingAnimationData.FoldIsOngoing)
		{
			amount = _ongoingAnimationData.CurrentProgress / _foldFreeBoneResetDuration;
			amount = TaleWorlds.Library.MathF.Clamp(amount, 0f, 1f);
			num3 = TaleWorlds.Library.MathF.Lerp(num3, 0f, amount);
		}
		if (_ongoingAnimationData.UnfoldIsOngoing)
		{
			amount = TaleWorlds.Library.MathF.Clamp((_ongoingAnimationData.CurrentProgress - (_unfoldSailDuration + _foldedSailTransitionDuration)) / _foldFreeBoneResetDuration, 0f, 1f);
			num3 = TaleWorlds.Library.MathF.Lerp(0f, num3, amount);
		}
		Vec3 vec2 = globalFrame.rotation.TransformToLocal(-Vec3.Up);
		vec2.Normalize();
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			MatrixFrame initialLocalFrame = freeBone.InitialLocalFrame;
			Vec3 origin = freeBone.InitialLocalFrame.origin;
			if (!disableWind && freeBone.BoneIndex != -1)
			{
				initialLocalFrame.origin += vec * num3 * 0.07f;
				if (_sailType == SailType.SquareSail)
				{
					num2 = TaleWorlds.Library.MathF.Lerp(num2, 0f, amount);
					initialLocalFrame.origin += vec2 * num2 * 0.08f;
				}
				origin = initialLocalFrame.origin;
				ApplyRandomWindToRope(ref initialLocalFrame.origin, 0.1f);
			}
			if (freeBone.BoneIndex != -1)
			{
				bool flag = false;
				if (_ongoingAnimationData.FoldIsOngoing && _ongoingAnimationData.CurrentProgress > _foldFreeBoneResetDuration)
				{
					flag = true;
				}
				else if (_ongoingAnimationData.UnfoldIsOngoing && _ongoingAnimationData.CurrentProgress < _unfoldSailDuration)
				{
					flag = true;
				}
				else if (_sailType == SailType.LateenSail && _lateenSailData.RollRotationInProgress)
				{
					flag = true;
				}
				if (flag)
				{
					if (_sailType == SailType.SquareSail)
					{
						if (freeBone.BoneType == FreeBoneType.Left)
						{
							initialLocalFrame.origin = _ongoingAnimationData.CurrentLeftFreeBonePosition;
						}
						else
						{
							initialLocalFrame.origin = _ongoingAnimationData.CurrentRightFreeBonePosition;
						}
					}
					else if (freeBone.BoneType == FreeBoneType.Left)
					{
						initialLocalFrame.origin = _ongoingAnimationData.CurrentLeftFreeBonePosition;
					}
					else if (freeBone.BoneType == FreeBoneType.Right)
					{
						initialLocalFrame.origin = _ongoingAnimationData.CurrentRightFreeBonePosition;
					}
					else
					{
						initialLocalFrame.origin = _ongoingAnimationData.CurrentCenterFreeBonePosition;
					}
					origin = initialLocalFrame.origin;
				}
			}
			freeBone.CurrentLocalFrame = initialLocalFrame;
			freeBone.CurrentFrameWithoutRandomWind = origin;
		}
	}

	private void FoldUnfoldSoundEventTick()
	{
		if (_ongoingAnimationData.FoldUnfoldSoundEvent != null && _ongoingAnimationData.FoldUnfoldSoundEvent.IsPlaying())
		{
			_ongoingAnimationData.FoldUnfoldSoundEvent.SetPosition(base.GameEntity.GetGlobalFrame().origin);
		}
		if (_ongoingAnimationData.ShouldMakeFoldUnfoldSound)
		{
			int soundCodeId = (_ongoingAnimationData.UnfoldIsOngoing ? SailUnfoldSoundEventId : SailFoldSoundEventId);
			if (_ongoingAnimationData.FoldUnfoldSoundEvent != null)
			{
				_ongoingAnimationData.FoldUnfoldSoundEvent.Stop();
				_ongoingAnimationData.FoldUnfoldSoundEvent = null;
			}
			_ongoingAnimationData.ShouldMakeFoldUnfoldSound = false;
			_ongoingAnimationData.FoldUnfoldSoundEvent = SoundEvent.CreateEvent(soundCodeId, base.GameEntity.Scene);
			_ongoingAnimationData.FoldUnfoldSoundEvent.SetPosition(base.GameEntity.GetGlobalFrame().origin);
			_ongoingAnimationData.FoldUnfoldSoundEvent.Play();
		}
		if (_ongoingAnimationData.ShouldStopFoldUnfoldSound)
		{
			_ongoingAnimationData.FoldUnfoldSoundEvent.Stop();
			_ongoingAnimationData.FoldUnfoldSoundEvent = null;
			_ongoingAnimationData.ShouldStopFoldUnfoldSound = false;
		}
	}

	private void TickRopesAndPulleys()
	{
		MatrixFrame frame = ((_yardEntity != null) ? _yardEntity.GetGlobalFrame() : MatrixFrame.Identity);
		Vec2 globalWindVelocityOfScene = base.GameEntity.GetGlobalWindVelocityOfScene();
		globalWindVelocityOfScene.Normalize();
		bool flag = false;
		MatrixFrame globalFrame = base.GameEntity.Root.GetGlobalFrame();
		flag = Vec2.DotProduct(globalWindVelocityOfScene, frame.rotation.f.AsVec2) < 0f;
		if (_knobParent != null)
		{
			MatrixFrame globalFrame2 = _knobParent.GetGlobalFrame();
			for (int i = 0; i < _knobConnectionPoints.Count; i++)
			{
				KnobConnectionPoint value = _knobConnectionPoints[i];
				Vec3 vec = globalFrame2.TransformToParent(in value.ShipLocalPosition);
				value.UpdateGlobalPosition(vec);
				bool value2 = Vec3.DotProduct(vec - frame.origin, frame.rotation.f) > 0f;
				value.UpdateRightOfYard(value2);
				_knobConnectionPoints[i] = value;
			}
		}
		if (SailSkeletonEntity != null && _yardEntity != null)
		{
			if (!SailYawRotationEntity.GetLocalFrame().NearlyEquals(_previousYawEntityFrame, 0.0001f))
			{
				MatrixFrame globalFrame3 = base.GameEntity.GetGlobalFrame();
				MatrixFrame previousSailYardFrame = globalFrame3.TransformToLocalNonOrthogonal(in frame);
				foreach (SimpleRopeRecord simpleRope in _simpleRopes)
				{
					if (simpleRope.StartPointAttachedToYard)
					{
						MatrixFrame frame2 = simpleRope.RopeEntity.GetGlobalFrame();
						Vec3 v = globalFrame3.TransformToLocalNonOrthogonal(in frame2.origin);
						frame2.origin = previousSailYardFrame.TransformToParent(_previousSailYardFrame.TransformToLocalNonOrthogonal(in v));
						frame2.origin = globalFrame3.TransformToParent(in frame2.origin);
						simpleRope.RopeEntity.SetGlobalFrame(in frame2, isTeleportation: false);
					}
					if (simpleRope.EndPointAttachedToYard)
					{
						MatrixFrame frame3 = simpleRope.TargetEntity.GetGlobalFrame();
						Vec3 v2 = globalFrame3.TransformToLocalNonOrthogonal(in frame3.origin);
						frame3.origin = previousSailYardFrame.TransformToParent(_previousSailYardFrame.TransformToLocalNonOrthogonal(in v2));
						frame3.origin = globalFrame3.TransformToParent(in frame3.origin);
						simpleRope.TargetEntity.SetGlobalFrame(in frame3, isTeleportation: false);
					}
				}
				foreach (FreeBoneRecord freeBone in _freeBones)
				{
					if (freeBone.FoldSailPulley.PulleySystem == null)
					{
						continue;
					}
					foreach (RopeSegment tiedToYardSegment in freeBone.FoldSailPulley.PulleySystem.TiedToYardSegments)
					{
						MatrixFrame frame4 = tiedToYardSegment.GameEntity.GetGlobalFrame();
						Vec3 v3 = globalFrame3.TransformToLocalNonOrthogonal(in frame4.origin);
						frame4.origin = globalFrame3.TransformToParent(previousSailYardFrame.TransformToParent(_previousSailYardFrame.TransformToLocalNonOrthogonal(in v3)));
						tiedToYardSegment.GameEntity.SetGlobalFrame(in frame4, isTeleportation: false);
					}
				}
				_previousSailYardFrame = previousSailYardFrame;
			}
			bool flag2 = !_ongoingAnimationData.FoldIsOngoing && !_ongoingAnimationData.UnfoldIsOngoing;
			MatrixFrame globalFrame4 = SailSkeletonEntity.GetGlobalFrame();
			foreach (FreeBoneRecord freeBone2 in _freeBones)
			{
				Vec3 v4 = globalFrame4.TransformToParent(in freeBone2.CurrentLocalFrame).origin;
				Vec3 vec2 = globalFrame4.TransformToParent(in freeBone2.CurrentFrameWithoutRandomWind);
				Vec3 shipLocalPosition = globalFrame.TransformToLocalNonOrthogonal(in v4);
				if (freeBone2.ConnectionType == FreeBoneConnectionType.Closest)
				{
					_ = 1;
				}
				else
					_ = freeBone2.ConnectionType == FreeBoneConnectionType.ClosestTwo;
				if (freeBone2.FoldSailPulley.PulleySystem != null)
				{
					freeBone2.FoldSailPulley.PulleySystem.SetEndTargetPosition(v4);
					if (_ongoingAnimationData.FoldIsOngoing)
					{
						float num = TaleWorlds.Library.MathF.Min(_ongoingAnimationData.CurrentProgress / _foldFreeBoneResetDuration, 1f);
						freeBone2.FoldSailPulley.PulleySystem.SetRuntimeLooseMultiplier(1f - num);
					}
					else if (_ongoingAnimationData.UnfoldIsOngoing)
					{
						float num2 = TaleWorlds.Library.MathF.Clamp((_ongoingAnimationData.CurrentProgress - _unfoldSailDuration) / _foldFreeBoneResetDuration, 0f, 1f);
						freeBone2.FoldSailPulley.PulleySystem.SetRuntimeLooseMultiplier(1f - num2);
					}
					else
					{
						freeBone2.FoldSailPulley.PulleySystem.SetRuntimeLooseMultiplier(1f);
					}
				}
				if (freeBone2.RotatorPulleys != null)
				{
					foreach (PulleyDataCache rotatorPulley in freeBone2.RotatorPulleys)
					{
						if (rotatorPulley.PulleySystem != null)
						{
							rotatorPulley.PulleySystem.SetEndTargetPosition(v4);
						}
					}
					if (_knobConnectionPoints.Count > 1 && flag2)
					{
						if (freeBone2.RotatorPulleys.Count > 0)
						{
							(int, int) tuple = FindClosestTwoKnobPoint(vec2, shipLocalPosition, _knobConnectionPoints, sideOfYard: true);
							if (tuple.Item1 != -1)
							{
								freeBone2.RotatorPulleys[0].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[tuple.Item1].GlobalPosition);
							}
							else
							{
								int num3 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num3 != -1)
								{
									freeBone2.RotatorPulleys[0].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[num3].GlobalPosition);
								}
							}
							if (tuple.Item2 != -1)
							{
								freeBone2.RotatorPulleys[0].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[tuple.Item2].GlobalPosition);
							}
							else
							{
								int num4 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num4 != -1)
								{
									freeBone2.RotatorPulleys[0].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[num4].GlobalPosition);
								}
							}
						}
						if (freeBone2.RotatorPulleys.Count > 1)
						{
							(int, int) tuple2 = FindClosestTwoKnobPoint(vec2, shipLocalPosition, _knobConnectionPoints, sideOfYard: false);
							if (tuple2.Item1 != -1)
							{
								freeBone2.RotatorPulleys[1].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[tuple2.Item1].GlobalPosition);
							}
							else
							{
								int num5 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num5 != -1)
								{
									freeBone2.RotatorPulleys[1].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[num5].GlobalPosition);
								}
							}
							if (tuple2.Item2 != -1)
							{
								freeBone2.RotatorPulleys[1].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[tuple2.Item2].GlobalPosition);
							}
							else
							{
								int num6 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num6 != -1)
								{
									freeBone2.RotatorPulleys[1].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[num6].GlobalPosition);
								}
							}
							int num7 = ((!flag) ? 1 : 0);
							freeBone2.RotatorPulleys[num7].PulleySystem.SetRuntimeLooseMultiplier(0.0023f);
							freeBone2.RotatorPulleys[(num7 + 1) % 2].PulleySystem.SetRuntimeLooseMultiplier(0.1f);
						}
					}
				}
				if (freeBone2.StabilityPulleys != null)
				{
					foreach (PulleyDataCache stabilityPulley in freeBone2.StabilityPulleys)
					{
						if (stabilityPulley.PulleySystem != null)
						{
							stabilityPulley.PulleySystem.SetEndTargetPosition(vec2);
							if (_ongoingAnimationData.FoldIsOngoing)
							{
								float num8 = TaleWorlds.Library.MathF.Clamp(_ongoingAnimationData.CurrentProgress / (_foldFreeBoneResetDuration + _foldSailDuration), 0f, 1f);
								stabilityPulley.PulleySystem.SetRuntimeLooseMultiplier(0.5f * num8);
							}
							else if (_ongoingAnimationData.UnfoldIsOngoing)
							{
								float num9 = TaleWorlds.Library.MathF.Clamp(_ongoingAnimationData.CurrentProgress / (_foldFreeBoneResetDuration + _unfoldSailDuration), 0f, 1f);
								stabilityPulley.PulleySystem.SetRuntimeLooseMultiplier(0.5f * (1f - num9));
							}
							else
							{
								stabilityPulley.PulleySystem.SetRuntimeLooseMultiplier(0.05f);
							}
						}
					}
					if (_knobConnectionPoints.Count > 1 && flag2)
					{
						if (freeBone2.StabilityPulleys.Count > 0)
						{
							(int, int) tuple3 = FindClosestTwoKnobPoint(vec2, shipLocalPosition, _knobConnectionPoints, sideOfYard: true);
							if (tuple3.Item1 != -1)
							{
								freeBone2.StabilityPulleys[0].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[tuple3.Item1].GlobalPosition);
							}
							else
							{
								int num10 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num10 != -1)
								{
									freeBone2.StabilityPulleys[0].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[num10].GlobalPosition);
								}
							}
							if (tuple3.Item2 != -1)
							{
								freeBone2.StabilityPulleys[0].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[tuple3.Item2].GlobalPosition);
							}
							else
							{
								int num11 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num11 != -1)
								{
									freeBone2.StabilityPulleys[0].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[num11].GlobalPosition);
								}
							}
						}
						if (freeBone2.StabilityPulleys.Count > 1)
						{
							(int, int) tuple4 = FindClosestTwoKnobPoint(vec2, shipLocalPosition, _knobConnectionPoints, sideOfYard: false);
							if (tuple4.Item1 != -1)
							{
								freeBone2.StabilityPulleys[1].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[tuple4.Item1].GlobalPosition);
							}
							else
							{
								int num12 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num12 != -1)
								{
									freeBone2.StabilityPulleys[1].PulleySystem.SetFirstFixedGlobalPosition(_knobConnectionPoints[num12].GlobalPosition);
								}
							}
							if (tuple4.Item2 != -1)
							{
								freeBone2.StabilityPulleys[1].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[tuple4.Item2].GlobalPosition);
							}
							else
							{
								int num13 = FindClosestPointFallback(vec2, _knobConnectionPoints);
								if (num13 != -1)
								{
									freeBone2.StabilityPulleys[1].PulleySystem.SetFirstFreeGlobalPosition(_knobConnectionPoints[num13].GlobalPosition);
								}
							}
							int num14 = ((!flag) ? 1 : 0);
							freeBone2.StabilityPulleys[num14].PulleySystem.SetRuntimeLooseMultiplier(0.0023f);
							freeBone2.StabilityPulleys[(num14 + 1) % 2].PulleySystem.SetRuntimeLooseMultiplier(0.1f);
						}
					}
				}
				if (freeBone2.StabilityRopes == null)
				{
					continue;
				}
				foreach (SimpleRopeRecord stabilityRope in freeBone2.StabilityRopes)
				{
					MatrixFrame frame5 = stabilityRope.TargetEntity.GetGlobalFrame();
					frame5.origin = v4;
					stabilityRope.TargetEntity.SetGlobalFrame(in frame5, isTeleportation: false);
				}
				if (!(_knobConnectionPoints.Count > 0 && flag2))
				{
					continue;
				}
				if (freeBone2.StabilityRopes.Count > 0)
				{
					int num15 = FindClosestKnobPoint(vec2, shipLocalPosition, _knobConnectionPoints, sideOfYard: true);
					if (num15 != -1)
					{
						MatrixFrame frame6 = freeBone2.StabilityRopes[0].RopeEntity.GetGlobalFrame();
						frame6.origin = _knobConnectionPoints[num15].GlobalPosition;
						freeBone2.StabilityRopes[0].RopeEntity.SetGlobalFrame(in frame6);
					}
					else
					{
						int num16 = FindClosestPointFallback(vec2, _knobConnectionPoints);
						if (num16 != -1)
						{
							MatrixFrame frame7 = freeBone2.StabilityRopes[0].RopeEntity.GetGlobalFrame();
							frame7.origin = _knobConnectionPoints[num16].GlobalPosition;
							freeBone2.StabilityRopes[0].RopeEntity.SetGlobalFrame(in frame7);
						}
					}
				}
				if (freeBone2.StabilityRopes.Count <= 1)
				{
					continue;
				}
				int num17 = FindClosestKnobPoint(vec2, shipLocalPosition, _knobConnectionPoints, sideOfYard: false);
				if (num17 != -1)
				{
					MatrixFrame frame8 = freeBone2.StabilityRopes[1].RopeEntity.GetGlobalFrame();
					frame8.origin = _knobConnectionPoints[num17].GlobalPosition;
					freeBone2.StabilityRopes[1].RopeEntity.SetGlobalFrame(in frame8);
				}
				else
				{
					int num18 = FindClosestPointFallback(vec2, _knobConnectionPoints);
					if (num18 != -1)
					{
						MatrixFrame frame9 = freeBone2.StabilityRopes[1].RopeEntity.GetGlobalFrame();
						frame9.origin = _knobConnectionPoints[num18].GlobalPosition;
						freeBone2.StabilityRopes[1].RopeEntity.SetGlobalFrame(in frame9);
					}
				}
				int num19 = ((!flag) ? 1 : 0);
				freeBone2.StabilityRopes[num19].RopeSegment.SetRuntimeLooseMultiplier(0.005f);
				freeBone2.StabilityRopes[(num19 + 1) % 2].RopeSegment.SetRuntimeLooseMultiplier(0.2f);
			}
		}
		UpdatePreviousYardFrame();
	}

	private int FindClosestPointFallback(Vec3 position, List<KnobConnectionPoint> records)
	{
		int result = -1;
		float num = 1E+12f;
		for (int i = 0; i < records.Count; i++)
		{
			float lengthSquared = (position - records[i].GlobalPosition).AsVec2.LengthSquared;
			if (lengthSquared < num)
			{
				result = i;
				num = lengthSquared;
			}
		}
		return result;
	}

	private int FindClosestKnobPointWind(Vec3 position, Vec3 shipLocalPosition, List<KnobConnectionPoint> records, bool sideOfYard, Vec2 windDirection)
	{
		int result = -1;
		float num = 0f;
		for (int i = 0; i < records.Count; i++)
		{
			if (records[i].RightOfYard == sideOfYard && (TaleWorlds.Library.MathF.Sign(records[i].ShipLocalPosition.x) == TaleWorlds.Library.MathF.Sign(shipLocalPosition.x) || _sailType == SailType.LateenSail))
			{
				Vec3 vec = position - records[i].GlobalPosition;
				float length = vec.AsVec2.Length;
				vec.Normalize();
				float num2 = TaleWorlds.Library.MathF.Abs(Vec2.DotProduct(vec.AsVec2, windDirection));
				if (num2 > num && length < _ropeConnectionMaxDistance)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	private (int, int) FindClosestTwoKnobPointWind(Vec3 position, Vec3 shipLocalPosition, List<KnobConnectionPoint> records, bool sideOfYard, Vec2 windDirection)
	{
		(int, int) result = (-1, -1);
		(float, float) tuple = (0f, 0f);
		for (int i = 0; i < records.Count; i++)
		{
			if (records[i].RightOfYard != sideOfYard || (TaleWorlds.Library.MathF.Sign(records[i].ShipLocalPosition.x) != TaleWorlds.Library.MathF.Sign(shipLocalPosition.x) && _sailType != SailType.LateenSail))
			{
				continue;
			}
			Vec3 vec = position - records[i].GlobalPosition;
			float length = vec.AsVec2.Length;
			vec.Normalize();
			float num = TaleWorlds.Library.MathF.Abs(Vec2.DotProduct(vec.AsVec2, windDirection));
			if (length < _ropeConnectionMaxDistance)
			{
				if (num > tuple.Item1)
				{
					tuple.Item2 = tuple.Item1;
					result.Item2 = result.Item1;
					tuple.Item1 = num;
					result.Item1 = i;
				}
				else if (num > tuple.Item2)
				{
					tuple.Item2 = num;
					result.Item2 = i;
				}
			}
		}
		return result;
	}

	private int FindClosestKnobPoint(Vec3 position, Vec3 shipLocalPosition, List<KnobConnectionPoint> records, bool sideOfYard)
	{
		float num = _ropeConnectionMaxDistance * _ropeConnectionMaxDistance;
		int result = -1;
		float num2 = 1E+12f;
		for (int i = 0; i < records.Count; i++)
		{
			if (records[i].RightOfYard == sideOfYard && (TaleWorlds.Library.MathF.Sign(records[i].ShipLocalPosition.x) == TaleWorlds.Library.MathF.Sign(shipLocalPosition.x) || _sailType == SailType.LateenSail))
			{
				Vec3 vec = position - records[i].GlobalPosition;
				float lengthSquared = vec.LengthSquared;
				float lengthSquared2 = vec.AsVec2.LengthSquared;
				if (lengthSquared < num2 && lengthSquared < num)
				{
					result = i;
					num2 = lengthSquared2;
				}
			}
		}
		return result;
	}

	private (int, int) FindClosestTwoKnobPoint(Vec3 position, Vec3 shipLocalPosition, List<KnobConnectionPoint> records, bool sideOfYard)
	{
		float num = _ropeConnectionMaxDistance * _ropeConnectionMaxDistance;
		(int, int) result = (-1, -1);
		(float, float) tuple = (1E+12f, 1E+12f);
		for (int i = 0; i < records.Count; i++)
		{
			if (records[i].RightOfYard != sideOfYard || (TaleWorlds.Library.MathF.Sign(records[i].ShipLocalPosition.x) != TaleWorlds.Library.MathF.Sign(shipLocalPosition.x) && _sailType != SailType.LateenSail))
			{
				continue;
			}
			Vec3 vec = position - records[i].GlobalPosition;
			float lengthSquared = vec.LengthSquared;
			if (vec.AsVec2.LengthSquared < num)
			{
				if (lengthSquared < tuple.Item1)
				{
					tuple.Item2 = tuple.Item1;
					result.Item2 = result.Item1;
					tuple.Item1 = lengthSquared;
					result.Item1 = i;
				}
				else if (lengthSquared < tuple.Item2)
				{
					tuple.Item2 = lengthSquared;
					result.Item2 = i;
				}
			}
		}
		return result;
	}

	private void CheckFoldAnimationState(float dt)
	{
		if (!_ongoingAnimationData.FoldIsOngoing && !_ongoingAnimationData.UnfoldIsOngoing && !SailEnabled)
		{
			StartFoldAnimation();
		}
		if (HasFoldFinished() && !_ongoingAnimationData.UnfoldIsOngoing && SailEnabled)
		{
			StartUnfoldAnimation();
		}
		if (_ongoingAnimationData.FoldIsOngoing)
		{
			if (!HasFoldFinished() && SailEnabled)
			{
				CancelAnimation();
				TickUnfoldAnimation(dt);
			}
			else
			{
				TickFoldAnimation(dt);
			}
		}
		else if (_ongoingAnimationData.UnfoldIsOngoing)
		{
			if (!SailEnabled)
			{
				CancelAnimation();
				TickFoldAnimation(dt);
			}
			else
			{
				TickUnfoldAnimation(dt);
			}
		}
	}

	private void DisableMorphAnimation()
	{
		if (SailClothComponent != null)
		{
			SailClothComponent.DisableMorphAnimation();
		}
		_lastMorphAnimKeySet = -1f;
	}

	private void SetMorphAnimToCloth(float currentMorphKey)
	{
		if (_lastMorphAnimKeySet == currentMorphKey)
		{
			return;
		}
		if (SailClothComponent != null)
		{
			SailClothComponent.SetMorphBuffer(currentMorphKey);
			int num = (int)currentMorphKey;
			int num2 = Math.Min(num + 1, _ongoingAnimationData.NumberOfMorphKeys - 1);
			float alpha = currentMorphKey - (float)num;
			if (_sailType == SailType.LateenSail)
			{
				if (_ongoingAnimationData.CenterVertexPositions != null)
				{
					Vec3 v = _ongoingAnimationData.CenterVertexPositions[num];
					Vec3 v2 = _ongoingAnimationData.CenterVertexPositions[num2];
					_ongoingAnimationData.CurrentCenterFreeBonePosition = Vec3.Lerp(v, v2, alpha);
				}
			}
			else if (_sailType == SailType.SquareSail)
			{
				if (_ongoingAnimationData.LeftVertexPositions != null)
				{
					Vec3 v3 = _ongoingAnimationData.LeftVertexPositions[num];
					Vec3 v4 = _ongoingAnimationData.LeftVertexPositions[num2];
					_ongoingAnimationData.CurrentLeftFreeBonePosition = Vec3.Lerp(v3, v4, alpha);
				}
				if (_ongoingAnimationData.RightVertexPositions != null)
				{
					Vec3 v5 = _ongoingAnimationData.RightVertexPositions[num];
					Vec3 v6 = _ongoingAnimationData.RightVertexPositions[num2];
					_ongoingAnimationData.CurrentRightFreeBonePosition = Vec3.Lerp(v5, v6, alpha);
				}
			}
		}
		_lastMorphAnimKeySet = currentMorphKey;
	}

	private void TickLateenSail(float dt)
	{
		if (_lateenRollDegrees < 1E-06f && _lateenYardShift < 1E-06f)
		{
			if (_lateenSailData.RollRotationEntity != null)
			{
				MatrixFrame frame = _lateenSailData.RollRotationEntity.GetFrame();
				if (Math.Abs(frame.rotation.GetEulerAngles().y * 57.29578f - _lateenRollDegrees) > 0.001f)
				{
					float y = _lateenRollDegrees * (System.MathF.PI / 180f);
					frame.rotation = Mat3.Identity;
					frame.rotation.ApplyEulerAngles(new Vec3(0f, y));
					_lateenSailData.RollRotationEntity.SetFrame(ref frame);
				}
			}
			if (_lateenSailData.YardShiftEntity != null)
			{
				MatrixFrame frame2 = _lateenSailData.YardShiftEntity.GetFrame();
				if (Math.Abs(frame2.origin.x - _lateenYardShift) > 0.001f)
				{
					frame2.origin.x = _lateenYardShift;
					_lateenSailData.YardShiftEntity.SetFrame(ref frame2);
				}
			}
		}
		else
		{
			if (!(_lateenSailData.RollRotationEntity != null) || !(SailYawRotationEntity != null) || !(_lateenSailData.YardShiftEntity != null))
			{
				return;
			}
			if (_lateenSailData.RollRotationInProgress)
			{
				_lateenSailData.RollRotationRealDt += dt;
				float num = _lateenSailData.RollRotationRealDt * _lateenRollChangeAnimationStepMultiplier;
				num -= (float)(int)num;
				float val = TaleWorlds.Library.MathF.Lerp(0.35f, 2f, (float)Math.Pow(num, 1.5));
				val = Math.Min(val, 1f);
				val = TaleWorlds.Library.MathF.Clamp(val - 0.2f, 0f, 1f) * 1.6f;
				_lateenSailData.RollRotationAnimProgress += dt * val / _lateenRollChangeAnimationStepMultiplier;
				float amount = TaleWorlds.Library.MathF.Clamp(_lateenSailData.RollRotationAnimProgress / _lateenRollChangeAnimationDuration, 0f, 1f);
				float y2 = TaleWorlds.Library.MathF.Lerp(_lateenSailData.RollRotationInitial, _lateenSailData.RollRotationTarget, amount);
				MatrixFrame frame3 = _lateenSailData.RollRotationEntity.GetFrame();
				frame3.rotation = Mat3.Identity;
				frame3.rotation.ApplyEulerAngles(new Vec3(0f, y2));
				_lateenSailData.RollRotationEntity.SetFrame(ref frame3);
				float num2 = _lateenRollChangeAnimationDuration - _lateenRollChangeYardShiftStart;
				float num3 = (float)Math.Pow(TaleWorlds.Library.MathF.Clamp((_lateenSailData.RollRotationRealDt - num2) / _lateenRollChangeYardShiftDuration, 0f, 1f), _lateenRollChangeYardShiftAcceleration);
				float x = TaleWorlds.Library.MathF.Lerp(_lateenSailData.YardShiftInitial, _lateenSailData.YardShiftTarget, num3);
				MatrixFrame frame4 = _lateenSailData.YardShiftEntity.GetFrame();
				frame4.origin.x = x;
				_lateenSailData.YardShiftEntity.SetFrame(ref frame4);
				if (_lateenSailData.RollRotationAnimProgress >= _lateenRollChangeAnimationDuration && num3 >= 1f)
				{
					_lateenSailData.RollRotationInProgress = false;
				}
				if (_lateenSailData.RollAnimationSoundEvent != null)
				{
					if (_lateenSailData.RollRotationAnimProgress >= _lateenRollChangeAnimationDuration * 0.9f && num3 >= 0.1f)
					{
						_lateenSailData.RollAnimationSoundEvent.Stop();
						_lateenSailData.RollAnimationSoundEvent = null;
					}
					else
					{
						_lateenSailData.RollAnimationSoundEvent.SetPosition(base.GameEntity.GetGlobalFrame().origin);
					}
				}
				_ = _ongoingAnimationData;
				if (!_lateenSailData.RollRotationInProgress)
				{
					DisableMorphAnimation();
				}
				return;
			}
			float num4 = _lateenRollDegrees * (System.MathF.PI / 180f);
			float num5 = 0f;
			float num6;
			for (num6 = SailYawRotationEntity.GetFrame().rotation.GetEulerAngles().z * 57.29578f; num6 > 180f; num6 -= 180f)
			{
			}
			for (; num6 < -180f; num6 += 180f)
			{
			}
			float num7 = _lateenRollChangeDegreeLimit - 90f;
			float num8 = 0f - _lateenRollChangeDegreeLimit - 90f;
			float num9 = 0f - _lateenRollChangeDegreeLimit + 90f;
			float num10 = _lateenRollChangeDegreeLimit + 90f;
			if (num6 < num8 || num6 > num10)
			{
				num5 = -1f;
			}
			else if (num6 > num7 && num6 < num9)
			{
				num5 = 1f;
			}
			float num11 = _lateenSailData.RollRotationEntity.GetFrame().rotation.GetEulerAngles().y * 57.29578f;
			float num12 = ((num11 > 0f) ? 1f : (-1f));
			if (num5 != 0f && num12 != num5)
			{
				_lateenSailData.RollRotationInProgress = true;
				_lateenSailData.RollRotationInitial = num11 * (System.MathF.PI / 180f);
				_lateenSailData.RollRotationTarget = num5 * num4;
				_lateenSailData.YardShiftInitial = _lateenSailData.YardShiftEntity.GetFrame().origin.x;
				_lateenSailData.YardShiftTarget = _lateenYardShift * num5;
				_lateenSailData.RollRotationAnimProgress = 0f;
				_lateenSailData.RollRotationRealDt = 0f;
				if (SoundsEnabled)
				{
					_lateenSailData.RollAnimationSoundEvent = SoundEvent.CreateEvent(LateenSailRollSoundEventId, base.GameEntity.Scene);
					_lateenSailData.RollAnimationSoundEvent.Play();
				}
			}
		}
	}

	private void SetClothMeshMaxDistance(float value)
	{
		if (SailClothComponent != null)
		{
			SailClothComponent.SetMaxDistanceMultiplier(value);
		}
	}

	private void TickFoldAnimation(float dt)
	{
		if (_ongoingAnimationData.CurrentProgress < _foldFreeBoneResetDuration || _ongoingAnimationData.CurrentProgress > _foldSailDuration + _foldFreeBoneResetDuration)
		{
			_ongoingAnimationData.CurrentProgress += dt;
		}
		else
		{
			_ongoingAnimationData.RealProgress += dt;
			if (_sailType == SailType.LateenSail || !FoldAnimationEnabled)
			{
				_ongoingAnimationData.CurrentProgress += dt;
			}
			else
			{
				_ongoingAnimationData.CurrentProgress += dt * ComputeSquareSailProgressMultiplier(_ongoingAnimationData.RealProgress);
			}
		}
		_ongoingAnimationData.CurrentProgress = Math.Min(_ongoingAnimationData.CurrentProgress, _foldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration);
		if (_ongoingAnimationData.CurrentProgress < _foldFreeBoneResetDuration)
		{
			return;
		}
		float value = (_ongoingAnimationData.CurrentProgress - _foldFreeBoneResetDuration) / _foldSailDuration;
		value = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
		float morphAnimToCloth = value * (float)(_ongoingAnimationData.NumberOfMorphKeys - 1);
		SetMorphAnimToCloth(morphAnimToCloth);
		float num = 0f;
		float num2 = 1f;
		float value2 = 1f - (value - num) / TaleWorlds.Library.MathF.Max(num2 - num, 0.01f);
		value2 = TaleWorlds.Library.MathF.Clamp(value2, 0f, 1f);
		SetClothMeshMaxDistance(value2);
		float num3 = 0f;
		float num4 = 0.75f;
		float value3 = 1f - (value - num3) / TaleWorlds.Library.MathF.Max(num4 - num3, 0.01f);
		value3 = TaleWorlds.Library.MathF.Clamp(value3, 0f, 1f);
		_foldAnimWindReductionFactor = value3;
		if (_ongoingAnimationData.FoldUnfoldSoundEvent != null && value > 0.875f)
		{
			_ongoingAnimationData.ShouldStopFoldUnfoldSound = true;
		}
		if (_ongoingAnimationData.CurrentProgress > _foldSailDuration + _foldFreeBoneResetDuration)
		{
			float value4 = (_ongoingAnimationData.CurrentProgress - (_foldSailDuration + _foldFreeBoneResetDuration)) / _foldedSailTransitionDuration;
			value4 = TaleWorlds.Library.MathF.Clamp(value4, 0f, 1f);
			if (_foldedStaticSailEntity != null)
			{
				if (!_isBurning)
				{
					_foldedStaticSailEntity.SetVisibilityExcludeParents(visible: true);
				}
				_foldedStaticSailEntity.SetAlpha(value4 * _sailEntityAlpha);
				SailSkeletonEntity.SetAlpha(value4);
				if (_foldedStaticSailMesh != null && !_isBurning)
				{
					_foldedStaticSailMesh.SetVectorArgument(1f, 0f, 0f, 0f);
				}
				SailClothComponent.SetVectorArgument(-1f, 0f, 0f, 0f);
				if (value4 >= 0.99999f)
				{
					SailSkeletonEntity.SetVisibilityExcludeParents(visible: false);
				}
			}
			if (_currentSailLevelUsed != 3 || _sailType != SailType.SquareSail)
			{
				return;
			}
			if (value4 < 0.99999f)
			{
				foreach (WeakGameEntity topLateenSail in _topLateenSails)
				{
					topLateenSail.SetVisibilityExcludeParents(visible: true);
					topLateenSail.SetAlpha(1f - value4);
				}
			}
			else
			{
				foreach (WeakGameEntity topLateenSail2 in _topLateenSails)
				{
					topLateenSail2.SetVisibilityExcludeParents(visible: false);
				}
			}
			{
				foreach (WeakGameEntity topLateenFoldedSail in _topLateenFoldedSails)
				{
					topLateenFoldedSail.SetVisibilityExcludeParents(visible: true);
					topLateenFoldedSail.SetAlpha(value4);
				}
				return;
			}
		}
		foreach (WeakGameEntity topLateenSail3 in _topLateenSails)
		{
			topLateenSail3.SetVisibilityExcludeParents(visible: true);
			topLateenSail3.SetAlpha(1f);
		}
		foreach (WeakGameEntity topLateenFoldedSail2 in _topLateenFoldedSails)
		{
			topLateenFoldedSail2.SetVisibilityExcludeParents(visible: false);
			topLateenFoldedSail2.SetAlpha(0f);
		}
	}

	private void TickUnfoldAnimation(float dt)
	{
		_ongoingAnimationData.CurrentProgress += dt;
		_ongoingAnimationData.CurrentProgress = TaleWorlds.Library.MathF.Min(_ongoingAnimationData.CurrentProgress, _unfoldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration);
		if (HasUnfoldFinished())
		{
			_ongoingAnimationData.CurrentProgress = 0f;
			_ongoingAnimationData.RealProgress = 0f;
			_ongoingAnimationData.UnfoldIsOngoing = false;
			_foldAnimWindReductionFactor = 1f;
			DisableMorphAnimation();
			SetClothMeshMaxDistance(1f);
			return;
		}
		if (_ongoingAnimationData.CurrentProgress < _foldedSailTransitionDuration)
		{
			float value = _ongoingAnimationData.CurrentProgress / _foldedSailTransitionDuration;
			value = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f) * _sailEntityAlpha;
			SailSkeletonEntity.SetVisibilityExcludeParents(visible: true);
			SailSkeletonEntity.SetAlpha(value);
			SailClothComponent.SetVectorArgument(1f, 0f, 0f, 0f);
			if (_foldedStaticSailEntity != null)
			{
				_foldedStaticSailEntity.SetVisibilityExcludeParents(visible: true);
				_foldedStaticSailEntity.SetAlpha(value);
				if (_foldedStaticSailMesh != null)
				{
					_foldedStaticSailMesh.SetVectorArgument(-1f, 0f, 0f, 0f);
				}
			}
			if (_currentSailLevelUsed != 3 || _sailType != SailType.SquareSail)
			{
				return;
			}
			if (value < 0.99999f)
			{
				foreach (WeakGameEntity topLateenSail in _topLateenSails)
				{
					topLateenSail.SetVisibilityExcludeParents(visible: true);
					topLateenSail.SetAlpha(value);
				}
				{
					foreach (WeakGameEntity topLateenFoldedSail in _topLateenFoldedSails)
					{
						topLateenFoldedSail.SetVisibilityExcludeParents(visible: true);
						topLateenFoldedSail.SetAlpha(1f - value);
					}
					return;
				}
			}
			foreach (WeakGameEntity topLateenSail2 in _topLateenSails)
			{
				topLateenSail2.SetVisibilityExcludeParents(visible: false);
			}
			{
				foreach (WeakGameEntity topLateenFoldedSail2 in _topLateenFoldedSails)
				{
					topLateenFoldedSail2.SetVisibilityExcludeParents(visible: true);
					topLateenFoldedSail2.SetAlpha(1f);
				}
				return;
			}
		}
		if (_foldedStaticSailEntity != null)
		{
			_foldedStaticSailEntity.SetVisibilityExcludeParents(visible: false);
		}
		foreach (WeakGameEntity topLateenFoldedSail3 in _topLateenFoldedSails)
		{
			topLateenFoldedSail3.SetVisibilityExcludeParents(visible: false);
			topLateenFoldedSail3.SetAlpha(0f);
		}
		foreach (WeakGameEntity topLateenSail3 in _topLateenSails)
		{
			topLateenSail3.SetVisibilityExcludeParents(visible: true);
			topLateenSail3.SetAlpha(1f);
		}
		SailSkeletonEntity.SetAlpha(_sailEntityAlpha);
		float num = TaleWorlds.Library.MathF.Clamp((_ongoingAnimationData.CurrentProgress - _foldedSailTransitionDuration) / _unfoldSailDuration, 0f, 1f);
		if (num >= 1f)
		{
			if (_ongoingAnimationData.FoldUnfoldSoundEvent != null)
			{
				_ongoingAnimationData.ShouldStopFoldUnfoldSound = true;
			}
			DisableMorphAnimation();
		}
		else
		{
			float morphAnimToCloth = (1f - num) * (float)(_ongoingAnimationData.NumberOfMorphKeys - 1);
			SetMorphAnimToCloth(morphAnimToCloth);
		}
		float num2 = 0f;
		float num3 = 1f;
		float value2 = 1f - (1f - num - num2) / TaleWorlds.Library.MathF.Max(num3 - num2, 0.01f);
		value2 = TaleWorlds.Library.MathF.Clamp(value2, 0f, 1f);
		SetClothMeshMaxDistance(value2);
		float num4 = 0.25f;
		float num5 = 1f;
		float value3 = 1f - (1f - num - num4) / TaleWorlds.Library.MathF.Max(num5 - num4, 0.01f);
		value3 = TaleWorlds.Library.MathF.Clamp(value3, 0f, 1f);
		_foldAnimWindReductionFactor = value3;
	}

	private void InitSailFoldAnimationResources()
	{
		if (!(SailClothComponent != null))
		{
			return;
		}
		_ongoingAnimationData.NumberOfMorphKeys = SailClothComponent.GetNumberOfMorphKeys();
		if (_ongoingAnimationData.NumberOfMorphKeys > 0)
		{
			if (_sailType == SailType.SquareSail)
			{
				_ongoingAnimationData.LeftVertexPositions = new Vec3[_ongoingAnimationData.NumberOfMorphKeys];
				SailClothComponent.GetMorphAnimLeftPoints(_ongoingAnimationData.LeftVertexPositions);
				_ongoingAnimationData.RightVertexPositions = new Vec3[_ongoingAnimationData.NumberOfMorphKeys];
				SailClothComponent.GetMorphAnimRightPoints(_ongoingAnimationData.RightVertexPositions);
			}
			else
			{
				_ongoingAnimationData.CenterVertexPositions = new Vec3[_ongoingAnimationData.NumberOfMorphKeys];
				SailClothComponent.GetMorphAnimCenterPoints(_ongoingAnimationData.CenterVertexPositions);
			}
		}
	}

	private void StartFoldAnimation()
	{
		_ongoingAnimationData.CurrentProgress = 0f;
		_ongoingAnimationData.RealProgress = 0f;
		_ongoingAnimationData.FoldIsOngoing = true;
		_ongoingAnimationData.UnfoldIsOngoing = false;
		if (SoundsEnabled)
		{
			_ongoingAnimationData.ShouldMakeFoldUnfoldSound = true;
		}
	}

	private void StartUnfoldAnimation()
	{
		_ongoingAnimationData.CurrentProgress = 0f;
		_ongoingAnimationData.RealProgress = 0f;
		_ongoingAnimationData.FoldIsOngoing = false;
		_ongoingAnimationData.UnfoldIsOngoing = true;
		if (SoundsEnabled)
		{
			_ongoingAnimationData.ShouldMakeFoldUnfoldSound = true;
		}
	}

	private void CancelAnimation()
	{
		if (_ongoingAnimationData.UnfoldIsOngoing)
		{
			float num = 0f;
			if (_ongoingAnimationData.CurrentProgress < _foldedSailTransitionDuration)
			{
				num = _foldSailDuration + _foldFreeBoneResetDuration + (_foldedSailTransitionDuration - _ongoingAnimationData.CurrentProgress);
			}
			else if (_ongoingAnimationData.CurrentProgress < _foldedSailTransitionDuration + _unfoldSailDuration)
			{
				float num2 = (_ongoingAnimationData.CurrentProgress - _foldedSailTransitionDuration) / _unfoldSailDuration;
				num = _foldFreeBoneResetDuration + _foldSailDuration * (1f - num2);
			}
			else
			{
				num = (_unfoldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration - _ongoingAnimationData.CurrentProgress) * _foldFreeBoneResetDuration / _foldedSailTransitionDuration;
			}
			StartFoldAnimation();
			_ongoingAnimationData.CurrentProgress = num;
		}
		else if (_ongoingAnimationData.FoldIsOngoing)
		{
			float num3 = 0f;
			if (_ongoingAnimationData.CurrentProgress < _foldFreeBoneResetDuration)
			{
				num3 = _unfoldSailDuration + _foldFreeBoneResetDuration + (_foldedSailTransitionDuration - _ongoingAnimationData.CurrentProgress);
			}
			else if (_ongoingAnimationData.CurrentProgress < _foldSailDuration + _foldFreeBoneResetDuration)
			{
				float num4 = (_ongoingAnimationData.CurrentProgress - _foldFreeBoneResetDuration) / _foldSailDuration;
				num3 = _foldedSailTransitionDuration + _unfoldSailDuration * (1f - num4);
			}
			else
			{
				num3 = (_foldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration - _ongoingAnimationData.CurrentProgress) * _foldedSailTransitionDuration / _foldFreeBoneResetDuration;
			}
			StartUnfoldAnimation();
			_ongoingAnimationData.CurrentProgress = num3;
		}
	}

	private bool HasFoldFinished()
	{
		return _ongoingAnimationData.CurrentProgress >= _foldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration;
	}

	private bool HasUnfoldFinished()
	{
		return _ongoingAnimationData.CurrentProgress >= _unfoldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration;
	}

	private void UpdateTotalFoldDuration()
	{
		_totalFoldDuration = _foldFreeBoneResetDuration + _foldedSailTransitionDuration;
		if (_sailType == SailType.LateenSail)
		{
			_totalFoldDuration += _foldSailDuration;
			return;
		}
		float num = EstimateSquareSailFoldAnimationDuration();
		_totalFoldDuration += num;
	}

	private void UpdateTotalUnfoldDuration()
	{
		_totalUnfoldDuration = _unfoldSailDuration + _foldFreeBoneResetDuration + _foldedSailTransitionDuration;
	}

	private void HandleLOD()
	{
		Vec3 lastFinalRenderCameraPositionOfScene = base.GameEntity.GetLastFinalRenderCameraPositionOfScene();
		Vec3 origin = base.GameEntity.GetGlobalFrame().origin;
		float num = lastFinalRenderCameraPositionOfScene.DistanceSquared(origin);
		_ropesAreInvisibleThisFrame = num > 22500f;
		bool flag = num > 2025f;
		if (_ropesWereInvisibleLastFrame != _ropesAreInvisibleThisFrame || _lodCheckFirstFrame)
		{
			foreach (FreeBoneRecord freeBone in _freeBones)
			{
				if (freeBone.FoldSailPulley.Entity != null)
				{
					freeBone.FoldSailPulley.Entity.SetVisibilityExcludeParents(!_ropesAreInvisibleThisFrame);
				}
				if (freeBone.RotatorPulleys != null)
				{
					foreach (PulleyDataCache rotatorPulley in freeBone.RotatorPulleys)
					{
						rotatorPulley.Entity.SetVisibilityExcludeParents(!_ropesAreInvisibleThisFrame);
					}
				}
				if (freeBone.StabilityPulleys != null)
				{
					foreach (PulleyDataCache stabilityPulley in freeBone.StabilityPulleys)
					{
						stabilityPulley.Entity.SetVisibilityExcludeParents(!_ropesAreInvisibleThisFrame);
					}
				}
				if (freeBone.StabilityRopes == null)
				{
					continue;
				}
				foreach (SimpleRopeRecord stabilityRope in freeBone.StabilityRopes)
				{
					stabilityRope.ParentEntity.SetVisibilityExcludeParents(!_ropesAreInvisibleThisFrame);
				}
			}
			foreach (SimpleRopeRecord simpleRope in _simpleRopes)
			{
				if (!simpleRope.IsBigRope)
				{
					simpleRope.RopeEntity.SetVisibilityExcludeParents(!_ropesAreInvisibleThisFrame);
				}
			}
			foreach (SimpleRopeRecord mastRope in _mastRopes)
			{
				if (!mastRope.IsBigRope)
				{
					mastRope.RopeEntity.SetVisibilityExcludeParents(!_ropesAreInvisibleThisFrame);
				}
			}
		}
		if (_ropesWereLinearLastFrame != flag || _lodCheckFirstFrame)
		{
			foreach (FreeBoneRecord freeBone2 in _freeBones)
			{
				if (freeBone2.FoldSailPulley.Entity != null)
				{
					freeBone2.FoldSailPulley.PulleySystem.SetLinearMode(flag);
				}
				if (freeBone2.RotatorPulleys != null)
				{
					foreach (PulleyDataCache rotatorPulley2 in freeBone2.RotatorPulleys)
					{
						rotatorPulley2.PulleySystem.SetLinearMode(flag);
					}
				}
				if (freeBone2.StabilityPulleys != null)
				{
					foreach (PulleyDataCache stabilityPulley2 in freeBone2.StabilityPulleys)
					{
						stabilityPulley2.PulleySystem.SetLinearMode(flag);
					}
				}
				if (freeBone2.StabilityRopes == null)
				{
					continue;
				}
				foreach (SimpleRopeRecord stabilityRope2 in freeBone2.StabilityRopes)
				{
					stabilityRope2.RopeSegment.SetLinearMode(flag);
				}
			}
			foreach (SimpleRopeRecord simpleRope2 in _simpleRopes)
			{
				simpleRope2.RopeSegment.SetLinearMode(flag);
			}
			foreach (SimpleRopeRecord mastRope2 in _mastRopes)
			{
				mastRope2.RopeSegment.SetLinearMode(flag);
			}
		}
		_ropesWereInvisibleLastFrame = _ropesAreInvisibleThisFrame;
		_ropesWereLinearLastFrame = flag;
		_lodCheckFirstFrame = false;
	}

	private float ComputeSquareSailProgressMultiplier(float progress)
	{
		float num = progress * _foldSailStepMultiplier;
		num -= (float)(int)num;
		return TaleWorlds.Library.MathF.Clamp(Math.Min(TaleWorlds.Library.MathF.Lerp(0f, 1f, num), 1f) - 0.2f, 0f, 1f) * 1.6f / _foldSailStepMultiplier;
	}

	public float EstimateSquareSailFoldAnimationDuration()
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0.01f;
		while (num < _foldSailDuration)
		{
			num += num3 * ComputeSquareSailProgressMultiplier(num2);
			num2 += num3;
			if (num2 > _foldSailDuration * 10f)
			{
				break;
			}
		}
		return num2;
	}

	private SimpleRopeRecord FillSimpleRopeRecord(WeakGameEntity parentEntity)
	{
		SimpleRopeRecord result = new SimpleRopeRecord
		{
			StartPointAttachedToYard = false,
			EndPointAttachedToYard = false,
			ParentEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(parentEntity),
			RopeSegment = null,
			IsBigRope = parentEntity.HasTag("big_rope"),
			RopeEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(parentEntity.GetFirstChildEntityWithTagRecursive("simple_rope_start"))
		};
		if (result.RopeEntity != null)
		{
			result.StartPointAttachedToYard = result.RopeEntity.HasTag("attached_to_yard");
			result.RopeSegment = result.RopeEntity.GetFirstScriptOfType<RopeSegment>();
		}
		result.TargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(parentEntity.GetFirstChildEntityWithTagRecursive("simple_rope_end"));
		if (result.TargetEntity != null)
		{
			result.EndPointAttachedToYard = result.TargetEntity.HasTag("attached_to_yard");
		}
		if (result.RopeSegment != null)
		{
			result.RopeSegment.SetUseDistanceAsRopeLength();
		}
		return result;
	}

	private void PlaceClothFragmentsRandomly(int seed)
	{
		MBFastRandom mBFastRandom = new MBFastRandom();
		mBFastRandom.SetSeed((uint)seed, 0u);
		List<RopeSegment> segments = new List<RopeSegment>();
		float num = 0.04f;
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.FoldSailPulley.PulleySystem != null)
			{
				freeBone.FoldSailPulley.PulleySystem.GetAllRopeSegments(ref segments, num);
			}
			if (freeBone.RotatorPulleys != null)
			{
				foreach (PulleyDataCache rotatorPulley in freeBone.RotatorPulleys)
				{
					rotatorPulley.PulleySystem.GetAllRopeSegments(ref segments, num);
				}
			}
			if (freeBone.StabilityPulleys != null)
			{
				foreach (PulleyDataCache stabilityPulley in freeBone.StabilityPulleys)
				{
					stabilityPulley.PulleySystem.GetAllRopeSegments(ref segments, num);
				}
			}
			if (freeBone.StabilityRopes == null)
			{
				continue;
			}
			foreach (SimpleRopeRecord stabilityRope in freeBone.StabilityRopes)
			{
				if (stabilityRope.RopeSegment.RopeMesh != null && stabilityRope.RopeSegment.RopeMesh.GetVectorArgument().w < num)
				{
					segments.Add(stabilityRope.RopeSegment);
				}
			}
		}
		foreach (SimpleRopeRecord simpleRope in _simpleRopes)
		{
			if (simpleRope.RopeSegment.RopeMesh != null && simpleRope.RopeSegment.RopeMesh.GetVectorArgument().w < num)
			{
				segments.Add(simpleRope.RopeSegment);
			}
		}
		for (int num2 = TaleWorlds.Library.MathF.Min(6, segments.Count); num2 > 0; num2--)
		{
			int index = mBFastRandom.Next(0, segments.Count);
			RopeSegment ropeSegment = segments[index];
			int num3 = 2 + (int)(mBFastRandom.NextFloat() * 1.5f);
			for (int i = 0; i < num3; i++)
			{
				string prefabName = ClothFragmentPrefabs[mBFastRandom.Next(0, ClothFragmentPrefabs.Count() - 1)];
				GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName, callScriptCallbacks: true);
				ropeSegment.GameEntity.AddChild(gameEntity.WeakEntity);
				gameEntity.EntityFlags |= EntityFlags.DontSaveToScene;
				float scaleAmount = 1f + mBFastRandom.NextFloat() * 1f;
				MatrixFrame frame = MatrixFrame.Identity;
				frame.rotation.ApplyScaleLocal(scaleAmount);
				gameEntity.SetLocalFrame(ref frame, isTeleportation: false);
				RopeSegmentCosmetics firstScriptOfType = gameEntity.GetFirstScriptOfType<RopeSegmentCosmetics>();
				if (firstScriptOfType != null)
				{
					firstScriptOfType.RopeLocalPosition = mBFastRandom.NextFloat();
				}
			}
			segments[index] = segments[segments.Count - 1];
			segments.RemoveAt(segments.Count - 1);
		}
	}

	private void FetchEntities()
	{
		bool flag = _yardEntity == null;
		bool flag2 = base.GameEntity.Scene.IsEditorScene();
		_freeBones.Clear();
		_simpleRopes.Clear();
		_mastRopes.Clear();
		_knobConnectionPoints.Clear();
		SailSkeletonEntity = null;
		SailClothComponent = null;
		_sailSkeleton = null;
		_yardEntity = null;
		_foldedStaticSailEntity = null;
		_foldedStaticSailMesh = null;
		_burningSailEntity = null;
		_burningSailMesh = null;
		_topLateenSails.Clear();
		_topLateenFoldedSails.Clear();
		_ballistaVisibilityRopes.Clear();
		SailYawRotationEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTag("yaw_rotation_entity"));
		if (SailYawRotationEntity != null)
		{
			WeakGameEntity weakEntity = SailYawRotationEntity.WeakEntity;
			if (_sailType == SailType.LateenSail)
			{
				_lateenSailData.RollRotationEntity = SailYawRotationEntity.GetFirstChildEntityWithTagRecursive("roll_rotation_entity");
				if (_lateenSailData.RollRotationEntity != null)
				{
					_lateenSailData.YardShiftEntity = _lateenSailData.RollRotationEntity.GetFirstChildEntityWithTag("yard_shift");
					if (_lateenSailData.YardShiftEntity != null)
					{
						weakEntity = _lateenSailData.YardShiftEntity.WeakEntity;
					}
				}
			}
			WeakGameEntity firstChildEntityWithTagRecursive = base.GameEntity.GetFirstChildEntityWithTagRecursive("mast_entity");
			_mastEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(firstChildEntityWithTagRecursive);
			if (firstChildEntityWithTagRecursive.IsValid && firstChildEntityWithTagRecursive.Parent.IsValid)
			{
				foreach (WeakGameEntity child in firstChildEntityWithTagRecursive.Parent.GetChildren())
				{
					if (child.HasTag("simple_rope"))
					{
						SimpleRopeRecord item = FillSimpleRopeRecord(child);
						_mastRopes.Add(item);
					}
				}
			}
			SailSkeletonEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity.GetFirstChildEntityWithTag("sail_mesh_entity"));
			if (SailSkeletonEntity != null)
			{
				_sailSkeleton = SailSkeletonEntity.Skeleton;
				if (_sailSkeleton != null)
				{
					SailClothComponent = _sailSkeleton.GetComponentAtIndex(TaleWorlds.Engine.GameEntity.ComponentType.ClothSimulator, 0) as ClothSimulatorComponent;
					_sailSkeleton.EnableScriptDrivenPostIntegrateCallback();
					if (_sailSkeleton != null)
					{
						base.GameEntity.Scene.AddAlwaysRenderedSkeleton(_sailSkeleton);
					}
				}
			}
			_burningSailEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity.GetFirstChildEntityWithTag("sail_mesh_free_entity"));
			if (_burningSailEntity != null && _burningSailEntity.Skeleton != null)
			{
				ClothSimulatorComponent clothSimulatorComponent = _burningSailEntity.Skeleton.GetComponentAtIndex(TaleWorlds.Engine.GameEntity.ComponentType.ClothSimulator, 0) as ClothSimulatorComponent;
				if (clothSimulatorComponent != null)
				{
					_burningSailMesh = clothSimulatorComponent.GetFirstMetaMesh().GetMeshAtIndex(0);
				}
			}
			SailTopBannerEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.GetFirstChildEntityWithTagRecursive("bd_banner_b"));
			if (SailTopBannerEntity != null)
			{
				SailTopBannerClothComponent = SailTopBannerEntity.GetClothSimulator(0);
				SailTopBannerEntity.SetDoNotCheckVisibility(value: true);
			}
			_yardEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity.GetFirstChildEntityWithTag("sail_yard"));
			_foldedStaticSailEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity.GetFirstChildEntityWithTag("folded_static_entity"));
			weakEntity.GetChildrenWithTagRecursive(_topLateenSails, "lvl3_lateens_entity");
			weakEntity.GetChildrenWithTagRecursive(_topLateenFoldedSails, "lvl3_lateens_folded");
			if (_foldedStaticSailEntity != null)
			{
				_foldedStaticSailMesh = _foldedStaticSailEntity.GetFirstMesh();
			}
			if (_yardEntity != null)
			{
				WeakGameEntity firstChildEntityWithTagRecursive2 = _yardEntity.WeakEntity.GetFirstChildEntityWithTagRecursive("yard_mesh");
				_yardMesh = ((firstChildEntityWithTagRecursive2 != null) ? firstChildEntityWithTagRecursive2.GetFirstMesh() : null);
			}
			if (flag && _yardEntity != null)
			{
				UpdatePreviousYardFrame();
				MatrixFrame frame = _yardEntity.GetGlobalFrame();
				_previousSailYardFrame = base.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame);
			}
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		if (SailSkeletonEntity != null)
		{
			Skeleton skeleton = SailSkeletonEntity.Skeleton;
			foreach (GameEntity child2 in SailSkeletonEntity.GetChildren())
			{
				if (!child2.HasTag("free_bone") || dictionary.ContainsKey(child2.Name))
				{
					continue;
				}
				FreeBoneRecord freeBoneRecord = new FreeBoneRecord();
				freeBoneRecord.InitialLocalFrame = child2.GetFrame();
				freeBoneRecord.CurrentLocalFrame = freeBoneRecord.InitialLocalFrame;
				freeBoneRecord.BoneIndex = -1;
				freeBoneRecord.ConnectionType = FreeBoneConnectionType.All;
				freeBoneRecord.Entity = child2;
				freeBoneRecord.FoldSailPulley.Entity = null;
				freeBoneRecord.FoldSailPulley.PulleySystem = null;
				if (child2.HasTag("closest_pulley"))
				{
					freeBoneRecord.ConnectionType = FreeBoneConnectionType.Closest;
				}
				else if (child2.HasTag("closest_two_pulleys"))
				{
					freeBoneRecord.ConnectionType = FreeBoneConnectionType.ClosestTwo;
				}
				if (_sailType == SailType.SquareSail)
				{
					if (child2.Name.Contains("_l"))
					{
						freeBoneRecord.BoneType = FreeBoneType.Left;
					}
					else if (child2.Name.Contains("_r"))
					{
						freeBoneRecord.BoneType = FreeBoneType.Right;
					}
				}
				else if (_sailType == SailType.LateenSail)
				{
					if (child2.Name.Contains("_l"))
					{
						freeBoneRecord.BoneType = FreeBoneType.Left;
					}
					else if (child2.Name.Contains("_r"))
					{
						freeBoneRecord.BoneType = FreeBoneType.Right;
					}
					else if (child2.Name.Contains("_c"))
					{
						freeBoneRecord.BoneType = FreeBoneType.Center;
					}
				}
				if (skeleton != null)
				{
					string name = child2.Name;
					for (int i = 0; i < skeleton.GetBoneCount(); i++)
					{
						if (skeleton.GetBoneName((sbyte)i) == name)
						{
							freeBoneRecord.InitialLocalFrame = skeleton.GetBoneEntitialRestFrame((sbyte)i, useBoneMapping: false);
							freeBoneRecord.BoneIndex = (sbyte)i;
							break;
						}
					}
				}
				dictionary.Add(child2.Name, _freeBones.Count);
				_freeBones.Add(freeBoneRecord);
			}
		}
		WeakGameEntity firstChildEntityWithTag = base.GameEntity.GetFirstChildEntityWithTag("pulley_systems_parent");
		if (firstChildEntityWithTag != null)
		{
			firstChildEntityWithTag.SetDoNotCheckVisibility(value: true);
			WeakGameEntity firstChildEntityWithTag2 = firstChildEntityWithTag.GetFirstChildEntityWithTag("sail_fold_pulleys");
			if (firstChildEntityWithTag2 != null)
			{
				firstChildEntityWithTag2.SetDoNotCheckVisibility(value: true);
				foreach (WeakGameEntity child3 in firstChildEntityWithTag2.GetChildren())
				{
					string[] tags = child3.Tags;
					foreach (string text in tags)
					{
						if (text != "fold_pulley_system")
						{
							int value = -1;
							if (dictionary.TryGetValue(text, out value))
							{
								_freeBones[value].FoldSailPulley.Entity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child3);
								_freeBones[value].FoldSailPulley.PulleySystem = child3.GetFirstScriptOfType<PulleySystem>();
								break;
							}
						}
					}
				}
			}
			WeakGameEntity firstChildEntityWithTag3 = firstChildEntityWithTag.GetFirstChildEntityWithTag("sail_rotate_pulleys");
			if (firstChildEntityWithTag3 != null)
			{
				firstChildEntityWithTag3.SetDoNotCheckVisibility(value: true);
				foreach (WeakGameEntity child4 in firstChildEntityWithTag3.GetChildren())
				{
					string[] tags = child4.Tags;
					foreach (string text2 in tags)
					{
						if (!(text2 != "pulley_system"))
						{
							continue;
						}
						int value2 = -1;
						if (dictionary.TryGetValue(text2, out value2))
						{
							PulleyDataCache item2 = new PulleyDataCache
							{
								Entity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child4),
								PulleySystem = child4.GetFirstScriptOfType<PulleySystem>()
							};
							if (_freeBones[value2].RotatorPulleys == null)
							{
								_freeBones[value2].RotatorPulleys = new List<PulleyDataCache>();
							}
							_freeBones[value2].RotatorPulleys.Add(item2);
							break;
						}
					}
				}
			}
			WeakGameEntity firstChildEntityWithTag4 = firstChildEntityWithTag.GetFirstChildEntityWithTag("stability_ropes_parent");
			if (firstChildEntityWithTag4 != null)
			{
				firstChildEntityWithTag4.SetDoNotCheckVisibility(value: true);
				foreach (WeakGameEntity child5 in firstChildEntityWithTag4.GetChildren())
				{
					bool flag3 = child5.HasTag("simple_rope");
					string text3 = (flag3 ? "simple_rope" : "pulley_system");
					string[] tags = child5.Tags;
					foreach (string text4 in tags)
					{
						if (!(text4 != text3))
						{
							continue;
						}
						int value3 = -1;
						if (!dictionary.TryGetValue(text4, out value3))
						{
							continue;
						}
						if (flag3)
						{
							SimpleRopeRecord item3 = FillSimpleRopeRecord(child5);
							if (item3.RopeSegment != null)
							{
								item3.RopeSegment.SetAsDynamic();
							}
							if (_freeBones[value3].StabilityRopes == null)
							{
								_freeBones[value3].StabilityRopes = new List<SimpleRopeRecord>();
							}
							_freeBones[value3].StabilityRopes.Add(item3);
						}
						else
						{
							PulleyDataCache item4 = new PulleyDataCache
							{
								Entity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child5),
								PulleySystem = child5.GetFirstScriptOfType<PulleySystem>()
							};
							if (_freeBones[value3].StabilityPulleys == null)
							{
								_freeBones[value3].StabilityPulleys = new List<PulleyDataCache>();
							}
							_freeBones[value3].StabilityPulleys.Add(item4);
						}
						break;
					}
				}
			}
			WeakGameEntity firstChildEntityWithTag5 = firstChildEntityWithTag.GetFirstChildEntityWithTag("static_ropes_parent");
			if (firstChildEntityWithTag5 != null)
			{
				firstChildEntityWithTag5.SetDoNotCheckVisibility(value: true);
				foreach (WeakGameEntity child6 in firstChildEntityWithTag5.GetChildren())
				{
					if (child6.HasTag("simple_rope"))
					{
						SimpleRopeRecord item5 = FillSimpleRopeRecord(child6);
						if (item5.RopeSegment != null)
						{
							item5.RopeSegment.SetUseDistanceAsRopeLength();
						}
						_simpleRopes.Add(item5);
					}
					if (child6.HasTag("ballista_visibility"))
					{
						_ballistaVisibilityRopes.Add(child6);
					}
				}
			}
		}
		WeakGameEntity root = base.GameEntity.Root;
		_knobParent = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(root.GetFirstChildEntityWithTagRecursive("knob_points_parent"));
		if (_knobParent != null)
		{
			MatrixFrame globalFrame = root.GetGlobalFrame();
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			_knobParent.WeakEntity.GetChildrenWithTagRecursive(list, "knot_point");
			foreach (WeakGameEntity item7 in list)
			{
				KnobConnectionPoint item6 = default(KnobConnectionPoint);
				item6.GlobalPosition = item7.GetGlobalFrame().origin;
				item6.ShipLocalPosition = globalFrame.TransformToLocalNonOrthogonal(in item6.GlobalPosition);
				item6.IsFixed = item7.HasTag("dynamic_knob");
				_knobConnectionPoints.Add(item6);
				if (!flag2)
				{
					item7.Remove(79);
				}
			}
		}
		WeakGameEntity firstChildEntityWithTagRecursive3 = base.GameEntity.GetFirstChildEntityWithTagRecursive("flag_capture_rope");
		if (firstChildEntityWithTagRecursive3 != null)
		{
			_topFlagRope = FillSimpleRopeRecord(firstChildEntityWithTagRecursive3);
		}
		base.GameEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
		base.GameEntity.SetBoundingboxDirty();
	}

	private void InitLateenSailData()
	{
		if (!(SailYawRotationEntity == null) && !(_lateenSailData.RollRotationEntity == null) && !(_lateenSailData.YardShiftEntity == null))
		{
			float z = SailYawRotationEntity.GetFrame().rotation.GetEulerAngles().z;
			if (z > _lateenRollChangeDegreeLimit)
			{
				float num = ((z > 0.01f) ? 1f : (-1f));
				float num2 = _lateenRollDegrees * (System.MathF.PI / 180f);
				float y = num * num2;
				MatrixFrame frame = _lateenSailData.RollRotationEntity.GetFrame();
				frame.rotation = Mat3.Identity;
				frame.rotation.ApplyEulerAngles(new Vec3(0f, y));
				_lateenSailData.RollRotationEntity.SetFrame(ref frame);
				MatrixFrame frame2 = _lateenSailData.YardShiftEntity.GetFrame();
				frame2.origin.x = num * _lateenYardShift;
				_lateenSailData.YardShiftEntity.SetFrame(ref frame2);
			}
		}
	}

	private void UpdatePreviousYardFrame()
	{
		_previousYawEntityFrame = SailYawRotationEntity.GetLocalFrame();
	}

	private void TickFire(float dt)
	{
		_burningRecord.FireDt += dt;
		float num = _burningRecord.FireDt / _burningAnimationDuration;
		bool burningFinished = true;
		foreach (BurningSystem sailFire in _burningRecord.SailFires)
		{
			sailFire.Tick(dt);
		}
		if (SailClothComponent != null)
		{
			float num2 = 0.99f + TaleWorlds.Library.MathF.Min(num, 1f) * 0.55f;
			if (SailEnabled)
			{
				if (_burningSailMesh != null)
				{
					_burningSailMesh.SetVectorArgument(num2, 0f, 0f, 0f);
				}
			}
			else
			{
				_foldedStaticSailMesh.SetVectorArgument(num2, 0f, 0f, 0f);
			}
			if (num2 < 1.52f)
			{
				burningFinished = false;
			}
		}
		if (_topLateenFireMaterial != null && _currentSailLevelUsed == 3 && _sailType == SailType.SquareSail)
		{
			float vectorArgument = 0.99f + num * 1.01f;
			foreach (WeakGameEntity topLateenSail in _topLateenSails)
			{
				foreach (Mesh item in topLateenSail.GetAllMeshesWithTag("faction_color"))
				{
					item.SetVectorArgument(vectorArgument, 0f, 0f, 0f);
				}
			}
			foreach (WeakGameEntity topLateenFoldedSail in _topLateenFoldedSails)
			{
				topLateenFoldedSail.SetDoNotCheckVisibility(value: true);
				foreach (Mesh item2 in topLateenFoldedSail.GetAllMeshesWithTag("faction_color"))
				{
					item2.SetVectorArgument(vectorArgument, 0f, 0f, 0f);
				}
			}
		}
		foreach (BurningSystem sailFire2 in _burningRecord.SailFires)
		{
			MBReadOnlyList<BurningNode> burningNodes = sailFire2.BurningNodes;
			int num3 = (int)((num - 0.2f) * (float)burningNodes.Count);
			for (int i = 0; i < num3 && i < burningNodes.Count; i++)
			{
				burningNodes[i].CurrentFireProgress = 0f;
			}
		}
		if (_burningRecord.MastFire != null)
		{
			_burningRecord.MastFire.Tick(dt);
			float flameProgress = _burningRecord.MastFire.GetFlameProgress();
			Color color = Color.Lerp(_burningRecord.InitialYardMastColor, _burningRecord.InitialYardMastColor * 0.75f, flameProgress);
			color.Alpha = 1f;
			_mastEntity.SetFactorColor(color.ToUnsignedInteger());
			float num4 = _burningAnimationDuration * 9f;
			float num5 = _burningRecord.FireDt / num4;
			float num6 = TaleWorlds.Library.MathF.Clamp(1f - (num5 - 0.75f) * 4f, 0f, 1f);
			_burningRecord.MastFire.SetExternalFlameMultiplier(num6);
			if (num6 > 0f)
			{
				_burningRecord.MastFire.CheckWater();
				burningFinished = false;
			}
		}
		if (_burningRecord.FireDt > _burningAnimationDuration * 0.5f)
		{
			if (_burningRecord.YardFireStartDt == 0f)
			{
				_burningRecord.YardFireStartDt = _burningRecord.FireDt;
			}
			float num7 = 0f;
			if (_burningRecord.YardRightFire != null)
			{
				_burningRecord.YardRightFire.Tick(dt);
				num7 = Math.Max(num7, _burningRecord.YardRightFire.GetFlameProgress());
				float num8 = _burningAnimationDuration * 9f;
				float num9 = (_burningRecord.FireDt - _burningRecord.YardFireStartDt) / num8;
				float num10 = TaleWorlds.Library.MathF.Clamp(1f - (num9 - 0.75f) * 4f, 0f, 1f);
				_burningRecord.YardRightFire.SetExternalFlameMultiplier(num10);
				if (num10 > 0f)
				{
					_burningRecord.YardRightFire.CheckWater();
					burningFinished = false;
				}
			}
			if (_burningRecord.YardLeftFire != null)
			{
				_burningRecord.YardLeftFire.Tick(dt);
				num7 = Math.Max(num7, _burningRecord.YardLeftFire.GetFlameProgress());
				float num11 = _burningAnimationDuration * 9f;
				float num12 = (_burningRecord.FireDt - _burningRecord.YardFireStartDt) / num11;
				float num13 = TaleWorlds.Library.MathF.Clamp(1f - (num12 - 0.75f) * 4f, 0f, 1f);
				_burningRecord.YardLeftFire.SetExternalFlameMultiplier(num13);
				if (num13 > 0f)
				{
					_burningRecord.YardLeftFire.CheckWater();
					burningFinished = false;
				}
			}
			foreach (BurningSystem staticRopeFire in _burningRecord.StaticRopeFires)
			{
				staticRopeFire.Tick(dt);
				if (staticRopeFire.BurnedRope != null)
				{
					float num14 = (_burningRecord.FireDt - _burningRecord.YardFireStartDt) / staticRopeFire.GetBurningAnimationDuration();
					float num15 = 1f - (num14 - 0.4f);
					staticRopeFire.BurnedRope.SetAlpha(TaleWorlds.Library.MathF.Max(num15, 0.01f));
					staticRopeFire.SetExternalFlameMultiplier(num15);
					if (num15 > 0f)
					{
						burningFinished = false;
						staticRopeFire.CheckWater();
					}
				}
			}
			if (_yardMesh != null)
			{
				Color color2 = Color.Lerp(_burningRecord.InitialYardMastColor, _burningRecord.InitialYardMastColor * 0.75f, num7);
				color2.Alpha = 1f;
				_yardMesh.Color = color2.ToUnsignedInteger();
			}
		}
		if (_burningRecord.FireDt > _burningAnimationDuration * 0.25f)
		{
			foreach (BurningSystem rotatorFire in _burningRecord.RotatorFires)
			{
				rotatorFire.Tick(dt);
				if (rotatorFire.BurnedPulley != null)
				{
					float num16 = _burningRecord.FireDt / rotatorFire.GetBurningAnimationDuration();
					float num17 = 1f - (num16 - 0.4f);
					rotatorFire.BurnedPulley.SetAlpha(TaleWorlds.Library.MathF.Max(num17, 0.01f));
					rotatorFire.SetExternalFlameMultiplier(num17);
					if (num17 > 0f)
					{
						rotatorFire.CheckWater();
						burningFinished = false;
					}
				}
			}
		}
		foreach (BurningSystem foldFire in _burningRecord.FoldFires)
		{
			foldFire.Tick(dt);
			if (foldFire.BurnedPulley != null)
			{
				float num18 = _burningRecord.FireDt / foldFire.GetBurningAnimationDuration();
				float num19 = 1f - (num18 - 0.4f);
				foldFire.BurnedPulley.SetAlpha(TaleWorlds.Library.MathF.Max(num19, 0.01f));
				foldFire.SetExternalFlameMultiplier(num19);
				if (num19 > 0f)
				{
					foldFire.CheckWater();
					burningFinished = false;
				}
			}
		}
		foreach (BurningSystem stabilizerFire in _burningRecord.StabilizerFires)
		{
			stabilizerFire.Tick(dt);
			if (stabilizerFire.BurnedRope != null)
			{
				float num20 = _burningRecord.FireDt / stabilizerFire.GetBurningAnimationDuration();
				float num21 = 1f - (num20 - 0.4f);
				stabilizerFire.BurnedRope.SetAlpha(TaleWorlds.Library.MathF.Max(num21, 0.01f));
				stabilizerFire.SetExternalFlameMultiplier(num21);
				if (num21 > 0f)
				{
					stabilizerFire.CheckWater();
					burningFinished = false;
				}
			}
		}
		_burningRecord.BurningFinished = burningFinished;
	}

	private void PositionSailFireParticles()
	{
		Vec3 vec = Vec3.Zero;
		Vec3 vec2 = Vec3.Zero;
		Vec3 vec3 = Vec3.Zero;
		Vec3 vec4 = Vec3.Zero;
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.BoneIndex != -1)
			{
				if (freeBone.BoneType == FreeBoneType.Left)
				{
					vec = freeBone.CurrentLocalFrame.origin;
					vec3 = freeBone.InitialLocalFrame.origin;
				}
				else if (freeBone.BoneType == FreeBoneType.Right)
				{
					vec2 = freeBone.CurrentLocalFrame.origin;
					vec4 = freeBone.InitialLocalFrame.origin;
				}
			}
		}
		MatrixFrame globalFrame = SailSkeletonEntity.GetGlobalFrame();
		Vec3 vec5 = new Vec3((0f - _burningRecord.SailLengthX) * 0.5f, 0f, _burningRecord.SailLengthZ * 0.5f);
		Vec3 vec6 = new Vec3(_burningRecord.SailLengthX * 0.5f, 0f, _burningRecord.SailLengthZ * 0.5f);
		Vec3 vec7 = vec5 - vec3;
		Vec3 vec8 = vec6 - vec5;
		Vec3 v = vec - vec3;
		Vec3 v2 = vec2 - vec4;
		_ = 1f / (float)_burningRecord.SailFires.Count;
		float num = 3.45f;
		float num2 = 0.62f;
		foreach (BurningSystem sailFire in _burningRecord.SailFires)
		{
			foreach (BurningNode burningNode in sailFire.BurningNodes)
			{
				Vec3 v3 = vec3;
				Vec2 sailStripLocation = burningNode.SailStripLocation;
				v3 += vec8 * sailStripLocation.x;
				v3 += vec7 * sailStripLocation.y;
				sailStripLocation.y = 1f - sailStripLocation.y;
				Vec3 vec9 = Vec3.Lerp(v, v2, sailStripLocation.x);
				Vec3 zero = Vec3.Zero;
				if (sailStripLocation.y > num2)
				{
					float num3 = 1f - (sailStripLocation.y - num2) / TaleWorlds.Library.MathF.Max(1f - num2, 0.01f);
					zero += vec9 * (1f + num * num3);
				}
				else
				{
					float num4 = sailStripLocation.y / TaleWorlds.Library.MathF.Max(num2, 0.01f);
					zero += vec9 * num * num4;
				}
				if (sailStripLocation.x > 0.5f)
				{
					float num5 = 1f - (sailStripLocation.x - 0.5f) / 0.5f;
					zero += vec9 * 1.83f * num5;
				}
				else
				{
					float num6 = sailStripLocation.x / 0.5f;
					zero += vec9 * 1.83f * num6;
				}
				v3 += zero;
				MatrixFrame frame = burningNode.GameEntity.GetGlobalFrame();
				frame.origin = globalFrame.TransformToParent(in v3);
				burningNode.GameEntity.SetGlobalFrame(in frame);
			}
		}
	}

	private void PlaceTopFlag(float dx)
	{
		if (_topFlagRope.RopeSegment != null && SailTopBannerEntity != null)
		{
			Vec3 origin = _topFlagRope.RopeEntity.GetGlobalFrame().origin;
			Vec3 origin2 = _topFlagRope.TargetEntity.GetGlobalFrame().origin;
			Vec3 v = RopeSegment.CalculateAutoCurvePosition(origin, origin2, _topFlagRope.RopeSegment.CurrentRopeLength, dx);
			MatrixFrame frame = SailTopBannerEntity.GetLocalFrame();
			Vec3 vec = SailTopBannerEntity.Parent.GetGlobalFrame().TransformToLocalNonOrthogonal(in v);
			frame.origin.z = vec.z;
			SailTopBannerEntity.SetLocalFrame(ref frame, isTeleportation: false);
		}
	}

	private void TickFlagCaptureAnimation(float dt)
	{
		_captureTheFlagAnimation.DtTillStart += dt;
		if (_captureTheFlagAnimation.DtTillStart < 4f)
		{
			float amount = TaleWorlds.Library.MathF.Clamp(_captureTheFlagAnimation.DtTillStart / 4f, 0f, 1f);
			float dx = TaleWorlds.Library.MathF.Lerp(_topFlagRopePosition, _captureTheFlagBottomPosition, amount);
			PlaceTopFlag(dx);
			_captureTheFlagAnimation.BannerWindFactor = 0.15f;
			if (base.GameEntity.IsInEditorScene())
			{
				Vec3 windVector = new Vec3(base.Scene.GetGlobalWindVelocity() * 0.15f) / Scene.MaximumWindSpeed;
				SailTopBannerClothComponent.SetForcedWind(windVector, isLocal: false);
			}
		}
		else if (_captureTheFlagAnimation.DtTillStart < 5f)
		{
			if (SailTopBannerClothComponent != null && !_captureTheFlagAnimation.MaterialSet)
			{
				Mesh meshAtIndex = SailTopBannerClothComponent.GetFirstMetaMesh().GetMeshAtIndex(0);
				Material material = meshAtIndex.GetMaterial();
				material = material.CreateCopy();
				material.SetTexture(Material.MBTextureType.DiffuseMap2, _captureTheFlagAnimation.NewBannerTexture);
				meshAtIndex.SetMaterial(material);
				_captureTheFlagAnimation.MaterialSet = true;
			}
			_captureTheFlagAnimation.BannerWindFactor = 0.15f;
			if (base.GameEntity.IsInEditorScene())
			{
				Vec3 windVector2 = new Vec3(base.Scene.GetGlobalWindVelocity() * 0.15f) / Scene.MaximumWindSpeed;
				SailTopBannerClothComponent.SetForcedWind(windVector2, isLocal: false);
			}
		}
		else if (_captureTheFlagAnimation.DtTillStart < 9f)
		{
			float num = TaleWorlds.Library.MathF.Clamp((_captureTheFlagAnimation.DtTillStart - 5f) / 4f, 0f, 1f);
			float dx2 = TaleWorlds.Library.MathF.Lerp(_captureTheFlagBottomPosition, _topFlagRopePosition, num);
			PlaceTopFlag(dx2);
			float num2 = TaleWorlds.Library.MathF.Clamp((num - 0.8f) / 0.2f, 0.15f, 1f);
			_captureTheFlagAnimation.BannerWindFactor = num2;
			if (base.GameEntity.IsInEditorScene())
			{
				Vec3 windVector3 = new Vec3(base.Scene.GetGlobalWindVelocity() * num2) / Scene.MaximumWindSpeed;
				SailTopBannerClothComponent.SetForcedWind(windVector3, isLocal: false);
			}
		}
		else
		{
			_captureTheFlagAnimation.AnimationInProgress = false;
			_captureTheFlagAnimation.BannerWindFactor = 1f;
			SailTopBannerClothComponent.DisableForcedWind();
		}
	}

	public bool IsBurningFinished()
	{
		return _burningRecord.BurningFinished;
	}

	public bool IsBurning()
	{
		return _isBurning;
	}

	public void StartFire()
	{
		if (_isBurning)
		{
			foreach (BurningSystem sailFire in _burningRecord.SailFires)
			{
				sailFire.Remove();
			}
			if (_burningRecord.YardLeftFire != null)
			{
				_burningRecord.YardLeftFire.Remove();
			}
			if (_burningRecord.YardRightFire != null)
			{
				_burningRecord.YardRightFire.Remove();
			}
			if (_burningRecord.MastFire != null)
			{
				_burningRecord.MastFire.Remove();
			}
			foreach (BurningSystem rotatorFire in _burningRecord.RotatorFires)
			{
				rotatorFire.Remove();
			}
			foreach (BurningSystem stabilizerFire in _burningRecord.StabilizerFires)
			{
				stabilizerFire.Remove();
			}
			foreach (BurningSystem foldFire in _burningRecord.FoldFires)
			{
				foldFire.Remove();
			}
			foreach (BurningSystem staticRopeFire in _burningRecord.StaticRopeFires)
			{
				staticRopeFire.Remove();
			}
		}
		_isBurning = true;
		_burningRecord = new BurningRecord(_: true);
		Scene scene = base.GameEntity.Scene;
		bool flag = false;
		if (SailSkeletonEntity != null && _sailSkeleton != null && flag)
		{
			MatrixFrame globalFrame = SailSkeletonEntity.GetGlobalFrame();
			Mesh mesh = null;
			using (IEnumerator<Mesh> enumerator2 = _sailSkeleton.GetAllMeshes().GetEnumerator())
			{
				if (enumerator2.MoveNext())
				{
					mesh = enumerator2.Current;
				}
			}
			if (mesh != null)
			{
				int num = 6;
				Vec3 boundingBoxMax = mesh.GetBoundingBoxMax();
				Vec3 boundingBoxMin = mesh.GetBoundingBoxMin();
				Vec3 vec = boundingBoxMax - boundingBoxMin;
				float num2 = 1f / (float)num;
				float num3 = 1f / (float)num;
				_burningRecord.SailLengthX = vec.x;
				_burningRecord.SailLengthZ = vec.z;
				string prefabName = "burning_node";
				float num4 = _burningAnimationDuration / (float)num;
				for (int i = 0; i < num; i++)
				{
					GameEntity gameEntity = TaleWorlds.Engine.GameEntity.CreateEmpty(scene);
					gameEntity.Name = $"sail_strip_root_{i}";
					SailSkeletonEntity.AddChild(gameEntity);
					BurningSystem burningSystem = new BurningSystem(gameEntity, 1f / num4);
					for (int j = 0; j < num; j++)
					{
						Vec2 zero = Vec2.Zero;
						zero.x = ((float)i + 0.1f + MBRandom.RandomFloat * 0.8f) * num2;
						zero.y = ((float)j + 0.1f + MBRandom.RandomFloat * 0.8f) * num3;
						float x = zero.x * vec.x + boundingBoxMin.x;
						float z = zero.y * (0f - vec.z) + boundingBoxMax.z;
						GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName, callScriptCallbacks: true);
						gameEntity2.EntityFlags |= EntityFlags.DontSaveToScene;
						gameEntity.AddChild(gameEntity2);
						MatrixFrame m = MatrixFrame.Identity;
						m.origin.x = x;
						m.origin.z = z;
						gameEntity2.SetGlobalFrame(globalFrame.TransformToParent(in m));
						gameEntity2.UpdateTriadFrameForEditor();
						BurningNode firstScriptOfType = gameEntity2.GetFirstScriptOfType<BurningNode>();
						if (firstScriptOfType != null)
						{
							firstScriptOfType.SetSailStripLocation(zero);
							burningSystem.AddNewNode(firstScriptOfType);
							if (MBRandom.RandomFloat > 0.82f)
							{
								firstScriptOfType.EnableSparks();
							}
						}
					}
					_burningRecord.SailFires.Add(burningSystem);
				}
			}
		}
		if (_mastEntity != null)
		{
			MatrixFrame globalFrame2 = _mastEntity.GetGlobalFrame();
			Mesh firstMesh = _mastEntity.GetFirstMesh();
			if (firstMesh != null)
			{
				GameEntity gameEntity3 = TaleWorlds.Engine.GameEntity.CreateEmpty(scene);
				gameEntity3.Name = "mastFireRoot";
				_mastEntity.AddChild(gameEntity3);
				float num5 = _burningAnimationDuration * 0.25f;
				_burningRecord.InitialYardMastColor = Color.FromUint(firstMesh.Color);
				_burningRecord.InitialYardMastColor.Alpha = 1f;
				Vec3 boundingBoxMin2 = firstMesh.GetBoundingBoxMin();
				Vec3 v = new Vec3(0f, 0f, firstMesh.GetBoundingBoxMax().z);
				Vec3 v2 = new Vec3(0f, 0f, boundingBoxMin2.z);
				float num6 = v2.z + 4.35f;
				WeakGameEntity firstChildEntityWithTagRecursive = base.GameEntity.Root.GetFirstChildEntityWithTagRecursive("body_mesh");
				if (firstChildEntityWithTagRecursive != null)
				{
					Vec3 vec2 = globalFrame2.TransformToParent(in v);
					Vec3 vec3 = globalFrame2.TransformToParent(in v2);
					Vec3 vec4 = vec2 - vec3;
					float maxLength = vec4.Normalize();
					float resultLength = -1f;
					if (firstChildEntityWithTagRecursive.RayHitEntity(v, vec4, maxLength, ref resultLength))
					{
						v2 = globalFrame2.TransformToLocalNonOrthogonal(v + vec4 * resultLength);
						num6 = v2.z + 3f;
					}
				}
				float num7 = (v2 - v).Normalize();
				float num8 = 2f;
				int num9 = (int)(num7 / num8);
				num9 = TaleWorlds.Library.MathF.Max(0, num9 - 2);
				float num10 = num5 / (float)num9;
				_burningRecord.MastFire = new BurningSystem(gameEntity3, 1f / num10);
				string prefabName2 = "burning_node_yard";
				for (int k = 0; k < num9; k++)
				{
					GameEntity gameEntity4 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName2, callScriptCallbacks: true);
					if (!(gameEntity4 == null))
					{
						gameEntity3.AddChild(gameEntity4);
						BurningNode firstScriptOfType2 = gameEntity4.GetFirstScriptOfType<BurningNode>();
						if (firstScriptOfType2 != null)
						{
							_burningRecord.MastFire.AddNewNode(firstScriptOfType2);
						}
						if (MBRandom.RandomFloat > 0.82f)
						{
							firstScriptOfType2.EnableSparks();
						}
						MatrixFrame frame = MatrixFrame.Identity;
						frame.origin.z = num6 + (float)k * num8;
						frame.rotation.RotateAboutForward(System.MathF.PI / 2f);
						gameEntity4.SetFrame(ref frame);
					}
				}
			}
		}
		if (_yardMesh != null)
		{
			_mastEntity.GetGlobalFrame();
			Vec3 boundingBoxMin3 = _yardMesh.GetBoundingBoxMin();
			Vec3 boundingBoxMax2 = _yardMesh.GetBoundingBoxMax();
			Vec3 vec5 = (boundingBoxMin3 + boundingBoxMax2) * 0.5f;
			string prefabName3 = "burning_node_yard";
			GameEntity gameEntity5 = TaleWorlds.Engine.GameEntity.CreateEmpty(scene);
			gameEntity5.Name = "mastFireRootLeft";
			if (_sailType == SailType.LateenSail)
			{
				_lateenSailData.RollRotationEntity.AddChild(gameEntity5);
			}
			else
			{
				_yardEntity.AddChild(gameEntity5);
			}
			float num11 = 2f;
			int num12 = (int)((vec5.x - boundingBoxMin3.x) / num11);
			float num13 = _burningAnimationDuration * 0.25f / (float)num12;
			_burningRecord.YardLeftFire = new BurningSystem(gameEntity5, 1f / num13);
			float y = vec5.y;
			for (int l = 0; l < num12; l++)
			{
				GameEntity gameEntity6 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName3, callScriptCallbacks: true);
				if (!(gameEntity6 == null))
				{
					gameEntity5.AddChild(gameEntity6);
					BurningNode firstScriptOfType3 = gameEntity6.GetFirstScriptOfType<BurningNode>();
					if (firstScriptOfType3 != null)
					{
						_burningRecord.YardLeftFire.AddNewNode(firstScriptOfType3);
					}
					if (MBRandom.RandomFloat > 0.62f)
					{
						firstScriptOfType3.EnableSparks();
					}
					MatrixFrame frame2 = MatrixFrame.Identity;
					frame2.origin.x = y - (float)l * num11;
					gameEntity6.SetFrame(ref frame2);
				}
			}
			GameEntity gameEntity7 = TaleWorlds.Engine.GameEntity.CreateEmpty(scene);
			gameEntity7.Name = "mastFireRootRight";
			if (_sailType == SailType.LateenSail)
			{
				_lateenSailData.RollRotationEntity.AddChild(gameEntity7);
			}
			else
			{
				_yardEntity.AddChild(gameEntity7);
			}
			float num14 = 2f;
			int num15 = (int)((boundingBoxMax2.x - vec5.x) / num14);
			float num16 = _burningAnimationDuration * 0.25f / (float)num15;
			_burningRecord.YardRightFire = new BurningSystem(gameEntity7, 1f / num16);
			float y2 = vec5.y;
			for (int n = 0; n < num15; n++)
			{
				GameEntity gameEntity8 = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName3, callScriptCallbacks: true);
				if (!(gameEntity8 == null))
				{
					gameEntity7.AddChild(gameEntity8);
					BurningNode firstScriptOfType4 = gameEntity8.GetFirstScriptOfType<BurningNode>();
					if (firstScriptOfType4 != null)
					{
						_burningRecord.YardRightFire.AddNewNode(firstScriptOfType4);
					}
					if (MBRandom.RandomFloat > 0.62f)
					{
						firstScriptOfType4.EnableSparks();
					}
					MatrixFrame frame3 = MatrixFrame.Identity;
					frame3.origin.x = y2 + (float)n * num14;
					gameEntity8.SetFrame(ref frame3);
				}
			}
		}
		_burningRecord.RotatorFires = new List<BurningSystem>();
		foreach (FreeBoneRecord freeBone in _freeBones)
		{
			if (freeBone.RotatorPulleys != null)
			{
				foreach (PulleyDataCache rotatorPulley in freeBone.RotatorPulleys)
				{
					BurningSystem burningSystem2 = new BurningSystem(null, 2.7f, rotatorPulley.PulleySystem);
					_burningRecord.RotatorFires.Add(burningSystem2);
					rotatorPulley.PulleySystem.FillBurningRecord(burningSystem2);
					float num17 = _burningAnimationDuration * 0.5f / (float)burningSystem2.BurningNodes.Count;
					burningSystem2.SpreadRate = 1f / num17;
				}
			}
			if (freeBone.StabilityPulleys != null)
			{
				foreach (PulleyDataCache stabilityPulley in freeBone.StabilityPulleys)
				{
					BurningSystem burningSystem3 = new BurningSystem(null, 2.7f, stabilityPulley.PulleySystem);
					_burningRecord.StabilizerFires.Add(burningSystem3);
					stabilityPulley.PulleySystem.FillBurningRecord(burningSystem3);
					float num18 = _burningAnimationDuration * 0.5f / (float)burningSystem3.BurningNodes.Count;
					burningSystem3.SpreadRate = 1f / num18;
				}
			}
			if (freeBone.FoldSailPulley.PulleySystem != null)
			{
				BurningSystem burningSystem4 = new BurningSystem(null, 4.7f, freeBone.FoldSailPulley.PulleySystem);
				_burningRecord.FoldFires.Add(burningSystem4);
				freeBone.FoldSailPulley.PulleySystem.FillBurningRecord(burningSystem4);
				float num19 = _burningAnimationDuration * 0.5f / (float)burningSystem4.BurningNodes.Count;
				burningSystem4.SpreadRate = 1f / num19;
			}
			if (freeBone.StabilityRopes == null)
			{
				continue;
			}
			float nodeLength = 2f;
			string prefabName4 = "burning_node_rope";
			foreach (SimpleRopeRecord stabilityRope in freeBone.StabilityRopes)
			{
				BurningSystem burningSystem5 = new BurningSystem(null, 1.2f, stabilityRope.RopeSegment);
				stabilityRope.RopeSegment.FillBurningRecordForSegment(burningSystem5, prefabName4, nodeLength, reversePlacement: true);
				stabilityRope.RopeSegment.BurnedClipReverseMode = true;
				if (burningSystem5.BurningNodes.Count > 0)
				{
					_burningRecord.StabilizerFires.Add(burningSystem5);
					float num20 = _burningAnimationDuration * 0.5f / (float)burningSystem5.BurningNodes.Count;
					burningSystem5.SpreadRate = 1f / num20;
				}
			}
		}
		float nodeLength2 = 2f;
		string prefabName5 = "burning_node_rope";
		foreach (SimpleRopeRecord simpleRope in _simpleRopes)
		{
			if (!(MBRandom.RandomFloat < 0.3f))
			{
				BurningSystem burningSystem6 = new BurningSystem(null, 1.4f, simpleRope.RopeSegment);
				simpleRope.RopeSegment.FillBurningRecordForSegment(burningSystem6, prefabName5, nodeLength2, reversePlacement: false);
				if (burningSystem6.BurningNodes.Count > 0)
				{
					_burningRecord.StaticRopeFires.Add(burningSystem6);
					float num21 = _burningAnimationDuration * 0.5f / (float)burningSystem6.BurningNodes.Count;
					burningSystem6.SpreadRate = 1f / num21;
				}
			}
		}
		if (SailEnabled)
		{
			if (_burningSailEntity != null)
			{
				_burningSailEntity.SetVisibilityExcludeParents(visible: true);
				SailSkeletonEntity.SetVisibilityExcludeParents(visible: false);
			}
		}
		else if (_burningSailMesh != null)
		{
			_foldedStaticSailMesh.SetMaterial(_burningSailMesh.GetMaterial());
			foreach (Mesh item in _foldedStaticSailEntity.GetAllMeshesWithTag("static_ropes"))
			{
				item.SetVisibilityMask((VisibilityMaskFlags)0u);
			}
		}
		if (!(_topLateenFireMaterial != null) || _currentSailLevelUsed != 3 || _sailType != SailType.SquareSail)
		{
			return;
		}
		foreach (WeakGameEntity topLateenSail in _topLateenSails)
		{
			topLateenSail.SetDoNotCheckVisibility(value: true);
			foreach (Mesh item2 in topLateenSail.GetAllMeshesWithTag("faction_color"))
			{
				item2.SetMaterial(_topLateenFireMaterial);
			}
		}
		foreach (WeakGameEntity topLateenFoldedSail in _topLateenFoldedSails)
		{
			topLateenFoldedSail.SetDoNotCheckVisibility(value: true);
			foreach (Mesh item3 in topLateenFoldedSail.GetAllMeshesWithTag("faction_color"))
			{
				item3.SetMaterial(_topLateenFireMaterial);
			}
		}
	}

	private void ComputeMastClipPlane()
	{
		WeakGameEntity firstChildEntityWithTagRecursive = base.GameEntity.Root.GetFirstChildEntityWithTagRecursive("body_mesh");
		if (firstChildEntityWithTagRecursive != null && _mastEntity != null)
		{
			float num = 30f;
			MatrixFrame globalFrame = _mastEntity.GetGlobalFrame();
			Vec3 u = globalFrame.rotation.u;
			Vec3 rayOrigin = globalFrame.origin - num * u;
			float resultLength = -1f;
			if (firstChildEntityWithTagRecursive.RayHitEntity(rayOrigin, u, num * 2f, ref resultLength))
			{
				_mastClipDistanceFromOrigin = num - resultLength;
			}
		}
	}

	private void UpdateMastClipPlane()
	{
		if (_mastEntity != null)
		{
			MatrixFrame globalFrame = _mastEntity.GetGlobalFrame();
			Vec3 clipPosition = globalFrame.origin - globalFrame.rotation.u * _mastClipDistanceFromOrigin;
			_mastEntity.SetCustomClipPlane(clipPosition, globalFrame.rotation.u, setForChildren: false);
		}
	}

	public void GetDimensions(in MatrixFrame shipFrame, bool isLateen, out float width, out float height, out Vec3 center)
	{
		MatrixFrame frame = SailSkeletonEntity.GetGlobalFrame();
		Vec3 scaleVector = frame.rotation.GetScaleVector();
		BoundingBox boundingBox = SailClothComponent.GetFirstMetaMesh().GetBoundingBox();
		Vec3 vec = boundingBox.max - boundingBox.min;
		width = vec.x * scaleVector.x;
		height = vec.z * scaleVector.z;
		if (isLateen)
		{
			height = TaleWorlds.Library.MathF.Sqrt(width * width + height * height) * 0.88f;
		}
		center = shipFrame.TransformToLocalNonOrthogonal(in frame).TransformToParent(in boundingBox.center);
	}

	public void SetBallistaRopeVisibility(bool value)
	{
		if (value)
		{
			_ballistaRopeEnableFrameCounter = 2;
			return;
		}
		foreach (WeakGameEntity ballistaVisibilityRope in _ballistaVisibilityRopes)
		{
			ballistaVisibilityRope.SetVisibilityExcludeParents(value);
		}
		_ballistaRopeEnableFrameCounter = 0;
	}

	public void StartFlagCaptureAnimation(Texture newTexture)
	{
		if (SailTopBannerClothComponent != null && SailTopBannerClothComponent.GetFirstMetaMesh().GetMeshAtIndex(0).GetMaterial()
			.GetTexture(Material.MBTextureType.DiffuseMap2) != newTexture)
		{
			_captureTheFlagAnimation.AnimationInProgress = true;
			_captureTheFlagAnimation.NewBannerTexture = newTexture;
			_captureTheFlagAnimation.DtTillStart = 0f;
			_captureTheFlagAnimation.MaterialSet = false;
		}
	}
}

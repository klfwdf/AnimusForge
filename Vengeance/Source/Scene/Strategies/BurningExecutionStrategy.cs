using System;
using System.Collections.Generic;
using RichExecutions.Core;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Ground-level burning pipeline. A dense, collision-free mixture of roots,
/// loose sticks, brushwood and single logs surrounds one central native wood
/// heap. Every piece uses an independent random angle, radius, height and
/// rotation so the pyre does not expose circular or grid-like placement. Sixteen
/// actual wood positions are distributed across all five layers for direct
/// Native campfire particles, which catch in nearest-first order before a larger
/// mesh-free core reaches the prisoner's body.
/// </summary>
internal sealed class BurningExecutionStrategy : IExecutionMethodStrategy
{
    private const string BurningCrucifixDeathAction = "act_ver_burning_crucifix_death";
    private const string CentralWoodHeapPrefab = "bd_wood_heap_f";
    private const string PyreFireAndSmokeParticleSystem = "psys_game_campfire";
    private const string PyreFireFallbackParticleSystem = "psys_torch_fire_moving";
    private const string KindlingSparkParticleSystem = "psys_campfire_sparks";
    private const int RootKindlingCount = 56;
    private const int StickKindlingCount = 20;
    private const int BrushwoodKindlingCount = 16;
    private const int LogKindlingCount = 12;
    private const int BottomFillKindlingCount = 40;
    private const int OuterRingKindlingCount = 80;
    private const int RaisedCoreFillKindlingCount = 72;
    private const int LeftFillKindlingCount = 12;
    private const int KindlingCount =
        RootKindlingCount + StickKindlingCount + BrushwoodKindlingCount + LogKindlingCount;
    private const int KindlingFireCount = 15;
    private const int PureFlameCount = 8;
    private const float CentralWoodHeapScale = 0.58f;
    private const float CentralWoodHeapForwardOffset = -0.42f;
    private const float GroundFireMinimumScale = 1.30f;
    private const float GroundFireScaleRange = 0.45f;
    private const float CentralFireScale = 2.60f;
    private const uint CharredVisualColor = 0xFF2B211Au;
    private const uint CharredKindlingColor = 0xFF10100Eu;
    private const float VictimRaiseHeight = 2.20f;
    private const float PyreMaximumVisibleHeightAboveGround = 1.55f;
    private const float RaisedCoreMaximumVisibleHeightAboveGround = 2.25f;
    private const float IgnitionSparkLeadSeconds = 0.18f;
    private const float GroundFireSpreadSeconds = 0.14f;
    private const float CoreFireBuildSeconds = 0.38f;
    private const float ActorReleaseRetrySeconds = 0.25f;
    private const float BurningLegsSeconds = 1.25f;
    private const float BurningWaistSeconds = 1.25f;
    private const float BurningTorsoSeconds = 1.50f;
    private const float BurningDeathPoseBlendSeconds = 0.40f;
    private const float BurningLethalDelaySeconds = 3.00f;
    private const float KindlingCharDurationSeconds = 18.00f;
    private const float RaisedVictimProbeDelaySeconds = 0.35f;

    private static readonly int[] ConeLayerCounts = { 32, 26, 20, 16, 10 };
    private static readonly int[] FireCountByLayer = { 6, 4, 3, 1, 1 };
    private static readonly (float Right, float Forward, float Up, float Scale)[] PureFlameOffsets =
    {
        (-0.46f, 0.04f, 0.42f, 1.45f),
        (0.44f, -0.02f, 0.48f, 1.55f),
        (-0.34f, -0.10f, 0.78f, 1.60f),
        (0.36f, 0.08f, 0.86f, 1.70f),
        (-0.28f, 0.06f, 1.10f, 1.65f),
        (0.30f, -0.08f, 1.18f, 1.75f),
        (-0.18f, -0.02f, 1.42f, 1.55f),
        (0.20f, 0.04f, 1.50f, 1.65f)
    };
    private static readonly string[] RootKindlingPrefabs =
    {
        "tree_root_a_pine",
        "tree_root_b_pine",
        "tree_root_a_dry",
        "tree_root_b_dry",
        "tree_root_a_acacia",
        "tree_root_b_acacia",
        "tree_root_a_aspen",
        "tree_root_b_aspen",
        "tree_root_a_beech_b",
        "tree_root_b_beech_b"
    };
    private static readonly string[] StickKindlingPrefabs =
    {
        "bd_wood_stick_a",
        "bd_wood_stick_b",
        "bd_wood_stick_c"
    };
    private static readonly string[] BrushwoodKindlingPrefabs =
    {
        "brushwood_a",
        "brushwood_b",
        "brushwood_c",
        "brushwood_d",
        "brushwood_e",
        "brushwood_f"
    };
    private static readonly string[] LogKindlingPrefabs =
    {
        "log_pine_a",
        "log_beech_a",
        "log_pine_b",
        "log_beech_b",
        "log_pine_c",
        "log_beech_c"
    };

    private enum KindlingVisualKind
    {
        Root = 0,
        Stick = 1,
        Brushwood = 2,
        Log = 3
    }

    private readonly struct KindlingVisualSpec
    {
        public KindlingVisualSpec(
            string prefabName,
            KindlingVisualKind kind,
            float scaleMultiplier,
            float pitchRangeDegrees,
            float rollRangeDegrees)
        {
            PrefabName = prefabName;
            Kind = kind;
            ScaleMultiplier = scaleMultiplier;
            PitchRangeDegrees = pitchRangeDegrees;
            RollRangeDegrees = rollRangeDegrees;
        }

        public string PrefabName { get; }
        public KindlingVisualKind Kind { get; }
        public float ScaleMultiplier { get; }
        public float PitchRangeDegrees { get; }
        public float RollRangeDegrees { get; }
    }

    private readonly struct KindlingFireCandidate
    {
        public KindlingFireCandidate(Vec3 position, int layer)
        {
            Position = position;
            Layer = layer;
        }

        public Vec3 Position { get; }
        public int Layer { get; }
    }

    private readonly struct EntityFactorColorState
    {
        public EntityFactorColorState(GameEntity entity, uint factorColor)
        {
            Entity = entity;
            FactorColor = factorColor;
        }

        public GameEntity Entity { get; }
        public uint FactorColor { get; }
    }

    private enum BurningPhase
    {
        None = 0,
        IgnitionContact = 1,
        GroundSpread = 2,
        CoreBurning = 3,
        Legs = 4,
        Waist = 5,
        Torso = 6,
        LethalDelay = 7,
        DeathTransition = 8,
        Completed = 9
    }

    private IExecutionSceneHost _host = null!;
    private GameEntity? _pyreUsePointHost;
    private HangingHoistUsePoint? _pyreUsePoint;
    private BurningPhase _phase;
    private float _phaseElapsed;
    private bool _releaseFrameLogged;
    private bool _kindlingSpawnAttempted;
    private bool _deathPoseStarted;
    private bool _deathPoseFrozen;
    private bool _victimMortalityCaptured;
    private bool _executionActorReleased;
    private bool _charredAppearanceApplied;
    private bool _kindlingCharredAppearanceApplied;
    private bool _kindlingCharringStarted;
    private bool _pureFlamesIgnited;
    private bool _victimFactorColorsCaptured;
    private bool _victimHeightRaised;
    private bool _raisedVictimFailureLogged;
    private bool _pyreUseStateLogged;
    private bool _pyreUseStateFailureLogged;
    private bool _raisedVictimProbeLogged;
    private float _actorReleaseRetryElapsed;
    private float _kindlingCharElapsed;
    private float _raisedVictimProbeElapsed;
    private int _nextGroundFireIndex;
    private int _charredKindlingGroupCount;
    private int _pyreParticleSpawned;
    private int _kindlingHeightClampCount;
    private float _maximumKindlingHeightCorrection;
    private Agent.MortalityState _victimMortalityBeforeFreeze;
    private readonly List<KindlingFireCandidate> _kindlingFireCandidates = new();
    private readonly List<EntityFactorColorState> _victimFactorColors = new();
    private readonly List<EntityFactorColorState> _kindlingFactorColors = new();
    private readonly List<List<EntityFactorColorState>> _kindlingFactorColorGroups = new();
    private readonly List<GameEntity> _pyreParticleEntities = new();
    private Vec3[] _groundFireOrder = Array.Empty<Vec3>();
    private Vec3 _kindlingSparkPosition = Vec3.Invalid;
    private Vec3 _groundVictimPosition = Vec3.Invalid;

    public string MethodId => ExecutionMethodRules.Burning;

    public bool UsesStagedExecution => true;

    public bool RequiresExecutionAxe => false;

    public bool DefersVictimDeathToMissionExit => true;

    public void AttachHost(IExecutionSceneHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public void SetupSceneVisuals()
    {
        if (_kindlingSpawnAttempted)
        {
            return;
        }

        _kindlingSpawnAttempted = true;
        var placement = _host.Placement;
        if (placement is null)
        {
            _host.LogError("The burning ground placement was unavailable while scattering kindling.");
            return;
        }

        _groundVictimPosition = _host.VictimPosition;
        if (_groundVictimPosition.IsValid)
        {
            var raisedVictimPosition = _groundVictimPosition;
            raisedVictimPosition.z += VictimRaiseHeight;
            _host.VictimPosition = raisedVictimPosition;
            _victimHeightRaised = true;
            MaintainRaisedVictim(_host.VictimAgent, placement, "initial cone-top placement");
            _host.LogInfo(
                $"Raised the original prisoner ritual point by {VictimRaiseHeight:0.00}m from " +
                $"z={_groundVictimPosition.z:0.00} to z={raisedVictimPosition.z:0.00}; " +
                "the original Agent now uses a hidden native StandingPoint, parent attachment and target-Z " +
                "instead of competing TeleportToPosition writes.");
        }
        else
        {
            _host.LogError(
                "The original prisoner ground point was invalid; the conical pyre will still spawn " +
                "but the prisoner cannot be raised this tick.");
        }

        var centralHeapPosition = placement.Offset(
            0.08f,
            CentralWoodHeapForwardOffset,
            -0.08f);
        if (_groundVictimPosition.IsValid)
        {
            centralHeapPosition.x += _groundVictimPosition.x - placement.Origin.x;
            centralHeapPosition.y += _groundVictimPosition.y - placement.Origin.y;
            centralHeapPosition.z += _groundVictimPosition.z - placement.Origin.z;
        }

        var centralHeap = _host.SpawnPrefab(
            CentralWoodHeapPrefab,
            centralHeapPosition,
            createPhysics: false,
            callScriptCallbacks: false,
            yawDegrees: MBRandom.RandomFloat * 360f,
            pitchDegrees: 0f,
            rollDegrees: 0f,
            uniformScale: CentralWoodHeapScale);
        var centralHeapSpawned = centralHeap is not null;
        if (centralHeapSpawned)
        {
            ClampKindlingVisibleHeight(centralHeap!, _groundVictimPosition.z);
            CaptureKindlingFactorColors(centralHeap!);
            _host.LogInfo(
                $"Added one collision-free central '{CentralWoodHeapPrefab}' at " +
                $"({centralHeapPosition.x:0.00},{centralHeapPosition.y:0.00},{centralHeapPosition.z:0.00}), " +
                $"scale={CentralWoodHeapScale:0.00}; it participates in final charring.");
        }
        else
        {
            _host.LogWarning(
                $"The central '{CentralWoodHeapPrefab}' could not be created; " +
                "the mixed five-layer pyre remains active without ending the mission.");
        }

        var kindlingPlan = BuildKindlingVisualPlan();
        var spawnedByKind = new int[4];
        var kindlingPlanIndex = 0;
        var spawnedCount = 0;
        for (var layer = 0; layer < ConeLayerCounts.Length; layer++)
        {
            var piecesInLayer = ConeLayerCounts[layer];
            for (var layerIndex = 0; layerIndex < piecesInLayer; layerIndex++)
            {
                var kindlingSpec = kindlingPlan[kindlingPlanIndex++];
                // Do not reserve equally spaced angular slots. Even a jittered
                // slot layout still reads as a man-made ring in-game. Each
                // piece instead samples its own full angle and annulus radius.
                var angle = MBRandom.RandomFloat * MathF.PI * 2f;
                var radius = layer switch
                {
                    0 => RandomRadius(0.48f, 1.48f),
                    1 => RandomRadius(0.30f, 1.28f),
                    2 => RandomRadius(0.16f, 1.08f),
                    3 => RandomRadius(0.06f, 0.86f),
                    _ => RandomRadius(0.02f, 0.62f)
                };
                var layerBaseUp = layer switch
                {
                    0 => 0.04f,
                    1 => 0.34f,
                    2 => 0.65f,
                    3 => 0.96f,
                    _ => 1.24f
                };
                var verticalScatter = layer switch
                {
                    0 => 0.34f,
                    1 => 0.46f,
                    2 => 0.54f,
                    3 => 0.58f,
                    _ => 0.50f
                };
                // Wide overlapping height bands break the previous stepped
                // silhouette. The slight downward bias keeps most long roots
                // buried while still allowing irregular pieces to bridge layers.
                var up = layerBaseUp +
                         (MBRandom.RandomFloat - 0.58f) * verticalScatter;
                var layerScale = layer switch
                {
                    0 => 0.28f + MBRandom.RandomFloat * 0.64f,
                    1 => 0.23f + MBRandom.RandomFloat * 0.59f,
                    2 => 0.19f + MBRandom.RandomFloat * 0.54f,
                    3 => 0.15f + MBRandom.RandomFloat * 0.49f,
                    _ => 0.12f + MBRandom.RandomFloat * 0.43f
                };
                var scale = layerScale * kindlingSpec.ScaleMultiplier;
                var right = MathF.Cos(angle) * radius +
                            (MBRandom.RandomFloat - 0.5f) * 0.36f;
                var forward = MathF.Sin(angle) * radius +
                              (MBRandom.RandomFloat - 0.5f) * 0.36f;
                var position = placement.Offset(right, forward, up);
                if (_groundVictimPosition.IsValid)
                {
                    position.x += _groundVictimPosition.x - placement.Origin.x;
                    position.y += _groundVictimPosition.y - placement.Origin.y;
                    position.z += _groundVictimPosition.z - placement.Origin.z;
                }

                var yaw = MBRandom.RandomFloat * 360f;
                var upperRotationScale = layer switch
                {
                    3 => 0.48f,
                    4 => 0.26f,
                    _ => 1f
                };
                var pitch = (MBRandom.RandomFloat - 0.5f) *
                            kindlingSpec.PitchRangeDegrees * upperRotationScale;
                var roll = (MBRandom.RandomFloat - 0.5f) *
                           kindlingSpec.RollRangeDegrees * upperRotationScale;
                var kindlingEntity = _host.SpawnPrefab(
                    kindlingSpec.PrefabName,
                    position,
                    createPhysics: false,
                    callScriptCallbacks: false,
                    yawDegrees: yaw,
                    pitchDegrees: pitch,
                    rollDegrees: roll,
                    uniformScale: scale);
                if (kindlingEntity is not null)
                {
                    var heightCorrection = ClampKindlingVisibleHeight(
                        kindlingEntity,
                        _groundVictimPosition.z);
                    spawnedCount++;
                    spawnedByKind[(int)kindlingSpec.Kind]++;
                    CaptureKindlingFactorColors(kindlingEntity);
                    // Mixed wood origins sit inside their visible bounds. Move
                    // every blaze to the exposed outer surface and progressively
                    // closer to the cone apex instead of hiding flame sheets
                    // inside roots, brushwood or logs.
                    var radialLength = MathF.Sqrt(right * right + forward * forward);
                    var radialExposure = layer switch
                    {
                        0 => 0.14f,
                        1 => 0.10f,
                        2 => 0.07f,
                        3 => 0.04f,
                        _ => 0.02f
                    };
                    var fireRight = right;
                    var fireForward = forward;
                    if (radialLength > 0.001f)
                    {
                        fireRight += right / radialLength * radialExposure;
                        fireForward += forward / radialLength * radialExposure;
                    }

                    var surfaceLift = layer switch
                    {
                        0 => 0.10f + scale * 0.22f,
                        1 => 0.10f + scale * 0.22f,
                        2 => 0.08f + scale * 0.22f,
                        3 => 0.06f + scale * 0.22f,
                        _ => 0.04f + scale * 0.22f
                    };
                    var firePosition = placement.Offset(
                        fireRight,
                        fireForward,
                        up + surfaceLift - heightCorrection);
                    if (_groundVictimPosition.IsValid)
                    {
                        firePosition.x += _groundVictimPosition.x - placement.Origin.x;
                        firePosition.y += _groundVictimPosition.y - placement.Origin.y;
                        firePosition.z += _groundVictimPosition.z - placement.Origin.z;
                    }

                    _kindlingFireCandidates.Add(new KindlingFireCandidate(firePosition, layer));
                }
            }
        }

        SpawnBottomFillKindling(
            placement,
            spawnedByKind,
            ref spawnedCount);
        SpawnOuterRingKindling(
            placement,
            spawnedByKind,
            ref spawnedCount);
        SpawnRaisedCoreFillKindling(
            placement,
            spawnedByKind,
            ref spawnedCount);
        SpawnLeftFillKindling(
            placement,
            spawnedByKind,
            ref spawnedCount);

        SelectRandomKindlingFirePositions();
        _host.LogInfo(
            $"Built {spawnedCount}/{KindlingCount + BottomFillKindlingCount + OuterRingKindlingCount + RaisedCoreFillKindlingCount + LeftFillKindlingCount} collision-free mixed wood pieces as a fully randomized five-layer cone " +
            $"(roots={spawnedByKind[(int)KindlingVisualKind.Root]}, " +
            $"sticks={spawnedByKind[(int)KindlingVisualKind.Stick]}, " +
            $"brushwood={spawnedByKind[(int)KindlingVisualKind.Brushwood]}, " +
            $"logs={spawnedByKind[(int)KindlingVisualKind.Log]}, " +
            $"bottomFill={BottomFillKindlingCount}, outerRing={OuterRingKindlingCount}, raisedCoreFill={RaisedCoreFillKindlingCount}, leftFill={LeftFillKindlingCount}, " +
            $"centralHeap={(centralHeapSpawned ? 1 : 0)}/1, " +
            $"centralHeapForward={CentralWoodHeapForwardOffset:0.00}m; " +
            $"layerCounts={string.Join("/", ConeLayerCounts)}, overlappingRadiusHeightBands=true, " +
            $"independentAnglePositionFullAxisRotationWideScale=true, woodApex≈1.49m, " +
            $"prisonerLift={VictimRaiseHeight:0.00}m; " +
            $"selected {_groundFireOrder.Length}/{KindlingFireCount} actual wood positions for visible flames.");
    }

    private void SpawnBottomFillKindling(
        ExecutionScenePlacement placement,
        int[] spawnedByKind,
        ref int spawnedCount)
    {
        for (var index = 0; index < BottomFillKindlingCount; index++)
        {
            var selector = MBRandom.RandomFloat;
            var kind = selector < 0.50f
                ? KindlingVisualKind.Root
                : selector < 0.76f
                    ? KindlingVisualKind.Brushwood
                    : KindlingVisualKind.Stick;
            var prefabs = kind switch
            {
                KindlingVisualKind.Brushwood => BrushwoodKindlingPrefabs,
                KindlingVisualKind.Stick => StickKindlingPrefabs,
                _ => RootKindlingPrefabs
            };
            var prefab = prefabs[RandomIndex(prefabs.Length)];
            var angle = MBRandom.RandomFloat * MathF.PI * 2f;
            var radius = RandomRadius(0.30f, 1.24f);
            var right = MathF.Cos(angle) * radius;
            var forward = MathF.Sin(angle) * radius;
            var up = -0.24f + MBRandom.RandomFloat * 0.54f;
            var position = placement.Offset(right, forward, up);
            if (_groundVictimPosition.IsValid)
            {
                position.x += _groundVictimPosition.x - placement.Origin.x;
                position.y += _groundVictimPosition.y - placement.Origin.y;
                position.z += _groundVictimPosition.z - placement.Origin.z;
            }

            var scale = kind switch
            {
                KindlingVisualKind.Brushwood => 0.24f + MBRandom.RandomFloat * 0.58f,
                KindlingVisualKind.Stick => 0.22f + MBRandom.RandomFloat * 0.55f,
                _ => 0.20f + MBRandom.RandomFloat * 0.62f
            };
            var entity = _host.SpawnPrefab(
                prefab,
                position,
                createPhysics: false,
                callScriptCallbacks: false,
                yawDegrees: MBRandom.RandomFloat * 360f,
                pitchDegrees: (MBRandom.RandomFloat - 0.5f) * 300f,
                rollDegrees: (MBRandom.RandomFloat - 0.5f) * 330f,
                uniformScale: scale);
            if (entity is null)
            {
                continue;
            }

            position.z -= ClampKindlingVisibleHeight(entity, _groundVictimPosition.z);
            spawnedCount++;
            spawnedByKind[(int)kind]++;
            CaptureKindlingFactorColors(entity);
            var firePosition = position;
            firePosition.z += 0.10f + scale * 0.20f;
            _kindlingFireCandidates.Add(new KindlingFireCandidate(firePosition, layer: 0));
        }
    }

    private void SpawnOuterRingKindling(
        ExecutionScenePlacement placement,
        int[] spawnedByKind,
        ref int spawnedCount)
    {
        for (var index = 0; index < OuterRingKindlingCount; index++)
        {
            // Add a broad, irregular annulus outside the original cone. Every
            // piece samples its angle and radius independently so the extra
            // layer closes the silhouette without becoming a regular fence.
            var selector = MBRandom.RandomFloat;
            var kind = selector < 0.60f
                ? KindlingVisualKind.Root
                : selector < 0.88f
                    ? KindlingVisualKind.Brushwood
                    : KindlingVisualKind.Stick;
            var prefabs = kind switch
            {
                KindlingVisualKind.Brushwood => BrushwoodKindlingPrefabs,
                KindlingVisualKind.Stick => StickKindlingPrefabs,
                _ => RootKindlingPrefabs
            };
            var prefab = prefabs[RandomIndex(prefabs.Length)];
            var angle = MBRandom.RandomFloat * MathF.PI * 2f;
            var radius = RandomRadius(1.30f, 2.05f);
            var right = MathF.Cos(angle) * radius +
                        (MBRandom.RandomFloat - 0.5f) * 0.28f;
            var forward = MathF.Sin(angle) * radius +
                          (MBRandom.RandomFloat - 0.5f) * 0.28f;
            var up = -0.38f + MBRandom.RandomFloat * 0.58f;
            var position = placement.Offset(right, forward, up);
            if (_groundVictimPosition.IsValid)
            {
                position.x += _groundVictimPosition.x - placement.Origin.x;
                position.y += _groundVictimPosition.y - placement.Origin.y;
                position.z += _groundVictimPosition.z - placement.Origin.z;
            }

            var scale = kind switch
            {
                KindlingVisualKind.Brushwood => 0.22f + MBRandom.RandomFloat * 0.48f,
                KindlingVisualKind.Stick => 0.20f + MBRandom.RandomFloat * 0.44f,
                _ => 0.19f + MBRandom.RandomFloat * 0.52f
            };
            var entity = _host.SpawnPrefab(
                prefab,
                position,
                createPhysics: false,
                callScriptCallbacks: false,
                yawDegrees: MBRandom.RandomFloat * 360f,
                pitchDegrees: (MBRandom.RandomFloat - 0.5f) * 24f,
                rollDegrees: (MBRandom.RandomFloat - 0.5f) * 16f,
                uniformScale: scale);
            if (entity is null)
            {
                continue;
            }

            position.z -= ClampKindlingVisibleHeight(entity, _groundVictimPosition.z);
            spawnedCount++;
            spawnedByKind[(int)kind]++;
            CaptureKindlingFactorColors(entity);
            var firePosition = position;
            firePosition.z += 0.08f + scale * 0.18f;
            _kindlingFireCandidates.Add(new KindlingFireCandidate(firePosition, layer: 0));
        }
    }

    private void SpawnLeftFillKindling(
        ExecutionScenePlacement placement,
        int[] spawnedByKind,
        ref int spawnedCount)
    {
        for (var index = 0; index < LeftFillKindlingCount; index++)
        {
            // The prisoner faces placement.Forward and the player approaches
            // from that side, so positive placement.Right appears on the
            // player's screen-left. Fill the sparse outer-left arc without
            // changing the collision-free cone or blocking the ignition actor.
            var right = 0.55f + MBRandom.RandomFloat * 1.05f;
            var forward = -1.20f + MBRandom.RandomFloat * 2.40f;
            var up = -0.22f + MBRandom.RandomFloat * 0.82f;
            var useBrushwood = MBRandom.RandomFloat < 0.34f;
            var prefabs = useBrushwood
                ? BrushwoodKindlingPrefabs
                : RootKindlingPrefabs;
            var prefab = prefabs[RandomIndex(prefabs.Length)];
            var kind = useBrushwood
                ? KindlingVisualKind.Brushwood
                : KindlingVisualKind.Root;
            var position = placement.Offset(right, forward, up);
            if (_groundVictimPosition.IsValid)
            {
                position.x += _groundVictimPosition.x - placement.Origin.x;
                position.y += _groundVictimPosition.y - placement.Origin.y;
                position.z += _groundVictimPosition.z - placement.Origin.z;
            }

            var scale = useBrushwood
                ? 0.24f + MBRandom.RandomFloat * 0.64f
                : 0.21f + MBRandom.RandomFloat * 0.66f;
            var entity = _host.SpawnPrefab(
                prefab,
                position,
                createPhysics: false,
                callScriptCallbacks: false,
                yawDegrees: MBRandom.RandomFloat * 360f,
                pitchDegrees: (MBRandom.RandomFloat - 0.5f) * 280f,
                rollDegrees: (MBRandom.RandomFloat - 0.5f) * 320f,
                uniformScale: scale);
            if (entity is null)
            {
                continue;
            }

            position.z -= ClampKindlingVisibleHeight(entity, _groundVictimPosition.z);
            spawnedCount++;
            spawnedByKind[(int)kind]++;
            CaptureKindlingFactorColors(entity);
            var firePosition = position;
            firePosition.z += 0.12f + scale * 0.20f;
            _kindlingFireCandidates.Add(
                new KindlingFireCandidate(firePosition, up > 0.09f ? 1 : 0));
        }
    }

    private void SpawnRaisedCoreFillKindling(
        ExecutionScenePlacement placement,
        int[] spawnedByKind,
        ref int spawnedCount)
    {
        for (var index = 0; index < RaisedCoreFillKindlingCount; index++)
        {
            // Fill the open chimney directly below the raised prisoner and
            // between the four crucifix braces. These pieces are deliberately
            // narrower than the outer cone and may rise above the normal
            // knee-height clamp, while remaining below the prisoner's waist.
            var selector = MBRandom.RandomFloat;
            var kind = selector < 0.55f
                ? KindlingVisualKind.Root
                : selector < 0.82f
                    ? KindlingVisualKind.Brushwood
                    : KindlingVisualKind.Stick;
            var prefabs = kind switch
            {
                KindlingVisualKind.Brushwood => BrushwoodKindlingPrefabs,
                KindlingVisualKind.Stick => StickKindlingPrefabs,
                _ => RootKindlingPrefabs
            };
            var prefab = prefabs[RandomIndex(prefabs.Length)];
            var angle = MBRandom.RandomFloat * MathF.PI * 2f;
            var radius = RandomRadius(0.10f, 0.78f);
            var right = MathF.Cos(angle) * radius;
            var forward = MathF.Sin(angle) * radius;
            var up = 0.65f + MBRandom.RandomFloat * 1.55f;
            var position = placement.Offset(right, forward, up);
            if (_groundVictimPosition.IsValid)
            {
                position.x += _groundVictimPosition.x - placement.Origin.x;
                position.y += _groundVictimPosition.y - placement.Origin.y;
                position.z += _groundVictimPosition.z - placement.Origin.z;
            }

            var scale = kind switch
            {
                KindlingVisualKind.Brushwood => 0.22f + MBRandom.RandomFloat * 0.42f,
                KindlingVisualKind.Stick => 0.20f + MBRandom.RandomFloat * 0.40f,
                _ => 0.18f + MBRandom.RandomFloat * 0.46f
            };
            var entity = _host.SpawnPrefab(
                prefab,
                position,
                createPhysics: false,
                callScriptCallbacks: false,
                yawDegrees: MBRandom.RandomFloat * 360f,
                pitchDegrees: (MBRandom.RandomFloat - 0.5f) * 320f,
                rollDegrees: (MBRandom.RandomFloat - 0.5f) * 340f,
                uniformScale: scale);
            if (entity is null)
            {
                continue;
            }

            position.z -= ClampKindlingVisibleHeight(
                entity,
                _groundVictimPosition.z,
                RaisedCoreMaximumVisibleHeightAboveGround);
            spawnedCount++;
            spawnedByKind[(int)kind]++;
            CaptureKindlingFactorColors(entity);
            var firePosition = position;
            firePosition.z += 0.10f + scale * 0.22f;
            _kindlingFireCandidates.Add(
                new KindlingFireCandidate(firePosition, up > 1.05f ? 2 : 1));
        }
    }

    private static float RandomRadius(float minimum, float maximum)
    {
        var minimumSquared = minimum * minimum;
        var maximumSquared = maximum * maximum;
        return MathF.Sqrt(
            minimumSquared + MBRandom.RandomFloat * (maximumSquared - minimumSquared));
    }

    private float ClampKindlingVisibleHeight(
        GameEntity entity,
        float groundHeight,
        float maximumVisibleHeightAboveGround = PyreMaximumVisibleHeightAboveGround)
    {
        if (float.IsNaN(groundHeight) || float.IsInfinity(groundHeight))
        {
            return 0f;
        }

        try
        {
            var bounds = entity.GetGlobalBoundingBox();
            var maximumHeight = groundHeight + maximumVisibleHeightAboveGround;
            if (float.IsNaN(bounds.max.z) || float.IsInfinity(bounds.max.z) ||
                bounds.max.z <= maximumHeight)
            {
                return 0f;
            }

            var correction = bounds.max.z - maximumHeight;
            var frame = entity.GetGlobalFrame();
            frame.origin.z -= correction;
            entity.SetGlobalFrame(in frame, isTeleportation: true);
            _kindlingHeightClampCount++;
            _maximumKindlingHeightCorrection = MathF.Max(
                _maximumKindlingHeightCorrection,
                correction);
            return correction;
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                "A pyre wood piece could not be clamped below knee height; it remains visible. " +
                exception.Message);
            return 0f;
        }
    }

    private static int RandomIndex(int count) =>
        Math.Min(count - 1, (int)(MBRandom.RandomFloat * count));

    private static List<KindlingVisualSpec> BuildKindlingVisualPlan()
    {
        var plan = new List<KindlingVisualSpec>(KindlingCount);
        AddKindlingVisuals(
            plan,
            RootKindlingPrefabs,
            RootKindlingCount,
            KindlingVisualKind.Root,
            scaleMultiplier: 1.00f,
            pitchRangeDegrees: 300f,
            rollRangeDegrees: 330f);
        AddKindlingVisuals(
            plan,
            StickKindlingPrefabs,
            StickKindlingCount,
            KindlingVisualKind.Stick,
            scaleMultiplier: 0.92f,
            pitchRangeDegrees: 340f,
            rollRangeDegrees: 350f);
        AddKindlingVisuals(
            plan,
            BrushwoodKindlingPrefabs,
            BrushwoodKindlingCount,
            KindlingVisualKind.Brushwood,
            scaleMultiplier: 1.05f,
            pitchRangeDegrees: 260f,
            rollRangeDegrees: 300f);
        AddKindlingVisuals(
            plan,
            LogKindlingPrefabs,
            LogKindlingCount,
            KindlingVisualKind.Log,
            scaleMultiplier: 0.72f,
            pitchRangeDegrees: 320f,
            rollRangeDegrees: 340f);

        // Shuffle the exact 56/20/16/12 quota before assigning pieces to the
        // denser 32/26/20/16/10 cone slots. This avoids category rings while
        // preserving both the total count and the established fire distribution.
        for (var index = plan.Count - 1; index > 0; index--)
        {
            var randomIndex = Math.Min(
                index,
                (int)(MBRandom.RandomFloat * (index + 1)));
            var temporary = plan[index];
            plan[index] = plan[randomIndex];
            plan[randomIndex] = temporary;
        }

        return plan;
    }

    private static void AddKindlingVisuals(
        List<KindlingVisualSpec> plan,
        IReadOnlyList<string> prefabNames,
        int count,
        KindlingVisualKind kind,
        float scaleMultiplier,
        float pitchRangeDegrees,
        float rollRangeDegrees)
    {
        for (var index = 0; index < count; index++)
        {
            plan.Add(
                new KindlingVisualSpec(
                    prefabNames[index % prefabNames.Count],
                    kind,
                    scaleMultiplier,
                    pitchRangeDegrees,
                    rollRangeDegrees));
        }
    }

    private void SelectRandomKindlingFirePositions()
    {
        var candidatesByLayer = new List<KindlingFireCandidate>[ConeLayerCounts.Length];
        for (var layer = 0; layer < candidatesByLayer.Length; layer++)
        {
            candidatesByLayer[layer] = new List<KindlingFireCandidate>();
        }

        foreach (var candidate in _kindlingFireCandidates)
        {
            if (candidate.Layer >= 0 && candidate.Layer < candidatesByLayer.Length)
            {
                candidatesByLayer[candidate.Layer].Add(candidate);
            }
        }

        var selected = new List<Vec3>(KindlingFireCount);
        for (var layer = 0; layer < candidatesByLayer.Length; layer++)
        {
            TakeRandomFirePositions(
                candidatesByLayer[layer],
                Math.Min(FireCountByLayer[layer], candidatesByLayer[layer].Count),
                selected);
        }

        if (selected.Count < KindlingFireCount)
        {
            var remaining = new List<KindlingFireCandidate>();
            foreach (var layerCandidates in candidatesByLayer)
            {
                remaining.AddRange(layerCandidates);
            }

            TakeRandomFirePositions(
                remaining,
                Math.Min(KindlingFireCount - selected.Count, remaining.Count),
                selected);
        }

        _groundFireOrder = selected.ToArray();
    }

    private static void TakeRandomFirePositions(
        List<KindlingFireCandidate> candidates,
        int count,
        List<Vec3> selected)
    {
        for (var index = 0; index < count && candidates.Count > 0; index++)
        {
            var randomIndex = Math.Min(
                candidates.Count - 1,
                (int)(MBRandom.RandomFloat * candidates.Count));
            selected.Add(candidates[randomIndex].Position);
            candidates.RemoveAt(randomIndex);
        }
    }

    public void TickSceneVisuals(float dt)
    {
        // The host invokes this once per initialized mission tick, including
        // aftermath, so failed restoration keeps retrying without double dt.
        RetryReleaseExecutionActor(dt);
        var victim = _host.VictimAgent;
        var placement = _host.Placement;
        if (victim is null || placement is null || !victim.IsActive() ||
            _phase == BurningPhase.Completed)
        {
            return;
        }

        MaintainRaisedVictim(victim, placement, "burning scene tick");
        ProbeRaisedVictim(dt, victim);
    }

    private void MaintainRaisedVictim(
        Agent? victim,
        ExecutionScenePlacement placement,
        string purpose)
    {
        var raisedPosition = _host.VictimPosition;
        if (!_victimHeightRaised || victim is null || !victim.IsActive() || !raisedPosition.IsValid)
        {
            return;
        }

        try
        {
            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            EnsureNativePyreUseState(victim, purpose);
            victim.SetIsPhysicsForceClosed(false);
            victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
            var direction = placement.Forward.IsNonZero()
                ? placement.Forward.Normalized()
                : Vec2.Forward;
            var direction3D = new Vec3(direction.x, direction.y, 0f);
            var targetUp = new Vec3(0f, 0f, 1f);
            victim.SetTargetZ(raisedPosition.z);
            victim.SetTargetPositionAndDirection(raisedPosition.AsVec2, direction3D);
            victim.SetTargetUp(in targetUp);
        }
        catch (Exception exception)
        {
            if (_raisedVictimFailureLogged)
            {
                return;
            }

            _raisedVictimFailureLogged = true;
            _host.LogError(
                $"Could not maintain the original prisoner at the raised cone apex during {purpose}; " +
                "the burning scene continues and later ticks may recover.",
                exception);
        }
    }

    private bool EnsureNativePyreUseState(Agent victim, string purpose)
    {
        if (!TryCreatePyreUsePoint(victim))
        {
            if (!_pyreUseStateFailureLogged)
            {
                _pyreUseStateFailureLogged = true;
                _host.LogWarning(
                    $"Could not create the hidden native pyre StandingPoint during {purpose}; " +
                    "target-Z driving continues and the mission remains active.");
            }

            return false;
        }

        try
        {
            if (!_pyreUsePoint!.IsUsedBy(victim))
            {
                if (victim.CurrentlyUsedGameObject is not null)
                {
                    victim.StopUsingGameObject(isSuccessful: false);
                }

                victim.DisableScriptedMovement();
                victim.UseGameObject(_pyreUsePoint);
            }

            var used = _pyreUsePoint.IsUsedBy(victim);
            var parentMatched = _pyreUsePointHost is not null &&
                                victim.IsAgentParentEntitySameAs(_pyreUsePointHost);
            if (!parentMatched && _pyreUsePointHost is not null)
            {
                victim.SetForceAttachedEntity(_pyreUsePointHost.WeakEntity);
                parentMatched = victim.IsAgentParentEntitySameAs(_pyreUsePointHost);
            }

            if (!_pyreUseStateLogged)
            {
                _pyreUseStateLogged = true;
                _host.LogInfo(
                    $"Original prisoner native pyre use state during {purpose}: " +
                    $"currentlyUsed={victim.CurrentlyUsedGameObject?.GetType().Name ?? "none"}, " +
                    $"standingPointHasUser={_pyreUsePoint.HasUser}, userMatched={used}, " +
                    $"parentMatched={parentMatched}.");
            }

            if ((!used || !parentMatched) && !_pyreUseStateFailureLogged)
            {
                _pyreUseStateFailureLogged = true;
                _host.LogWarning(
                    $"The engine did not retain the original prisoner's complete pyre attachment during {purpose}; " +
                    $"used={used}, parentMatched={parentMatched}. Target-Z driving continues without mission cancellation.");
            }

            return used && parentMatched;
        }
        catch (Exception exception)
        {
            if (!_pyreUseStateFailureLogged)
            {
                _pyreUseStateFailureLogged = true;
                _host.LogWarning(
                    $"Could not establish the original prisoner's native pyre use state during {purpose}; " +
                    $"target-Z driving continues without mission cancellation. {exception.Message}");
            }

            return false;
        }
    }

    private bool TryCreatePyreUsePoint(Agent victim)
    {
        if (_pyreUsePointHost is not null && _pyreUsePoint is not null)
        {
            _pyreUsePoint.Bind(victim);
            return true;
        }

        GameEntity? host = null;
        try
        {
            host = GameEntity.CreateEmpty(
                _host.Mission.Scene,
                isModifiableFromEditor: false,
                createPhysics: false,
                callScriptCallbacks: false);
            if (host is null)
            {
                return false;
            }

            host.Name = "rex_burning_victim_anchor";
            host.SetMobility(GameEntity.Mobility.Stationary);
            var frame = MatrixFrame.Identity;
            frame.origin = _groundVictimPosition.IsValid
                ? _groundVictimPosition
                : victim.Position;
            host.SetGlobalFrame(in frame, isTeleportation: true);
            host.CreateAndAddScriptComponent(
                typeof(HangingHoistUsePoint).Name,
                callScriptCallbacks: true);
            var usePoint = host.GetFirstScriptOfType<HangingHoistUsePoint>();
            if (usePoint is null)
            {
                _host.RemoveSpawnedEntity(host);
                return false;
            }

            usePoint.Bind(victim);
            _pyreUsePointHost = host;
            _pyreUsePoint = usePoint;
            _host.LogInfo(
                $"Created hidden native pyre StandingPoint at " +
                $"({frame.origin.x:0.00}, {frame.origin.y:0.00}, {frame.origin.z:0.00}) " +
                $"for original Agent {victim.Index}.");
            return true;
        }
        catch (Exception exception)
        {
            if (host is not null)
            {
                _host.RemoveSpawnedEntity(host);
            }

            _host.LogWarning(
                $"Could not create the hidden native pyre StandingPoint; " +
                $"the mission remains active. {exception.Message}");
            return false;
        }
    }

    private void ProbeRaisedVictim(float dt, Agent victim)
    {
        if (_raisedVictimProbeLogged || !_victimHeightRaised || !_groundVictimPosition.IsValid)
        {
            return;
        }

        _raisedVictimProbeElapsed += MathF.Max(0f, dt);
        if (_raisedVictimProbeElapsed < RaisedVictimProbeDelaySeconds)
        {
            return;
        }

        _raisedVictimProbeLogged = true;
        try
        {
            var agentRoot = victim.Position;
            var visuals = victim.AgentVisuals;
            var visualRoot = visuals is not null && visuals.IsValid()
                ? visuals.GetGlobalFrame().origin
                : Vec3.Invalid;
            var entityRoot = visuals is not null && visuals.IsValid()
                ? visuals.GetEntity()?.GetGlobalFrame().origin ?? Vec3.Invalid
                : Vec3.Invalid;
            var target = _host.VictimPosition;
            _host.LogInfo(
                $"Burning cone-top lift probe after {_raisedVictimProbeElapsed:0.00}s: " +
                $"groundZ={_groundVictimPosition.z:0.000}, targetZ={target.z:0.000}, " +
                $"agentRoot=({agentRoot.x:0.000},{agentRoot.y:0.000},{agentRoot.z:0.000}), " +
                $"visualRoot=({visualRoot.x:0.000},{visualRoot.y:0.000},{visualRoot.z:0.000}), " +
                $"entityRoot=({entityRoot.x:0.000},{entityRoot.y:0.000},{entityRoot.z:0.000}), " +
                $"actualAgentLift={agentRoot.z - _groundVictimPosition.z:0.000}m, " +
                $"used={_pyreUsePoint?.IsUsedBy(victim) == true}, " +
                $"parentMatched={_pyreUsePointHost is not null && victim.IsAgentParentEntitySameAs(_pyreUsePointHost)}.");
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not read the burning cone-top lift probe; the scene continues. {exception.Message}");
        }
    }

    public void PrefetchActions()
    {
        // The shared scene-act prefetch resolves both the crucifix idle and
        // crucifix death actions without visibly playing either one. The shared
        // entry path then binds the idle before the first rendered mission tick.
    }

    public void StartExecutionAction()
    {
        _host.StartStagedMethodExecutionAction();
    }

    public void TickExecution(float dt)
    {
        if (_phase != BurningPhase.None)
        {
            return;
        }

        _host.ExecutionActionAttemptElapsed += MathF.Max(0f, dt);
        var actionAgent = _host.GetActiveExecutionActor();
        var progress = 0f;
        if (actionAgent is not null && actionAgent.IsActive())
        {
            _host.ReassertExecutionActionRoot(actionAgent, "burning torch-action tick");
            progress = _host.ObserveExecutionAction(actionAgent);
        }

        if (progress >= 0.01f)
        {
            _host.MaximumExecutionActionProgress = MathF.Max(
                _host.MaximumExecutionActionProgress,
                progress);
        }

        var release = _host.SceneAct?.LethalProgress ?? 0.40f;
        if (progress >= release || _host.MaximumExecutionActionProgress >= release)
        {
            if (!_releaseFrameLogged)
            {
                _releaseFrameLogged = true;
                _host.LogInfo(
                    $"Burning torch action reached its release frame " +
                    $"(progress={MathF.Max(progress, _host.MaximumExecutionActionProgress):0.000}); " +
                    "starting the spark-to-edge-to-core pyre spread.");
            }

            BeginMethodSequence("burning torch release frame");
            return;
        }

        if (_host.ExecutionActionAttemptElapsed >= (_host.SceneAct?.MaximumActionSeconds ?? 4.5f))
        {
            _host.LogWarning(
                $"Burning torch action stalled at progress {MathF.Max(progress, _host.MaximumExecutionActionProgress):0.000}; " +
                "starting the pyre sequence by timeout.");
            BeginMethodSequence("burning action timeout");
        }
    }

    public void BeginMethodSequence(string trigger)
    {
        if (_phase != BurningPhase.None)
        {
            return;
        }

        ResolveGroundIgnitionOrder();
        _phase = BurningPhase.IgnitionContact;
        _phaseElapsed = 0f;
        _nextGroundFireIndex = 0;
        _executionActorReleased = _host.ReleaseExecutionActorControl(
            $"{trigger}; pyre ignition has started");
        _actorReleaseRetryElapsed = 0f;
        _host.TryPlayWorldParticleBurst(
            KindlingSparkParticleSystem,
            _kindlingSparkPosition,
            "torch contact with the nearest pyre edge");
        TryPlayVictimVoice(
            SkinVoiceManager.VoiceType.Fear,
            "immediate fear scream when the pyre catches");
        _host.LogInfo(
            $"Began the ground pyre after {trigger}: sparkLead={IgnitionSparkLeadSeconds:0.00}s, " +
            $"groundZones={_groundFireOrder.Length}, zoneInterval={GroundFireSpreadSeconds:0.00}s, " +
            $"groundEffect='{PyreFireAndSmokeParticleSystem}' direct particles, meshFreeCore=true, " +
            $"coreBuild={CoreFireBuildSeconds:0.00}s, " +
            $"legs={BurningLegsSeconds:0.00}s, waist={BurningWaistSeconds:0.00}s, " +
            $"torso={BurningTorsoSeconds:0.00}s, lethalDelay={BurningLethalDelaySeconds:0.00}s, " +
            $"deathBlend={BurningDeathPoseBlendSeconds:0.00}s.");
    }

    public void TickMethodSequence(float dt)
    {
        var safeDt = MathF.Max(0f, dt);
        TickKindlingCharring(safeDt);
        if (_phase is BurningPhase.None or BurningPhase.Completed)
        {
            return;
        }

        _phaseElapsed += safeDt;
        switch (_phase)
        {
            case BurningPhase.IgnitionContact when _phaseElapsed >= IgnitionSparkLeadSeconds:
                _phase = BurningPhase.GroundSpread;
                _phaseElapsed = 0f;
                IgniteNextGroundFire();
                break;

            case BurningPhase.GroundSpread when _phaseElapsed >= GroundFireSpreadSeconds:
                _phaseElapsed = 0f;
                if (_nextGroundFireIndex < _groundFireOrder.Length)
                {
                    IgniteNextGroundFire();
                }
                else
                {
                    _phase = BurningPhase.CoreBurning;
                    TryIgniteCoreFireAndSmoke();
                    TryIgnitePureBodyFlames();
                }
                break;

            case BurningPhase.CoreBurning when _phaseElapsed >= CoreFireBuildSeconds:
                _phase = BurningPhase.Legs;
                _phaseElapsed = 0f;
                _host.TryIgniteExecutionEffectRoles(
                    "prisoner lower and upper legs",
                    "pyre_fire_left_leg",
                    "pyre_fire_right_leg",
                    "pyre_fire_left_thigh",
                    "pyre_fire_right_thigh");
                TryPlayVictimVoice(SkinVoiceManager.VoiceType.Fear, "fear scream at leg ignition");
                break;

            case BurningPhase.Legs when _phaseElapsed >= BurningLegsSeconds:
                _phase = BurningPhase.Waist;
                _phaseElapsed = 0f;
                _host.TryIgniteExecutionEffectRoles("prisoner waist", "pyre_fire_waist");
                TryPlayVictimVoice(SkinVoiceManager.VoiceType.Pain, "pain scream at waist ignition");
                break;

            case BurningPhase.Waist when _phaseElapsed >= BurningWaistSeconds:
                _phase = BurningPhase.Torso;
                _phaseElapsed = 0f;
                _host.TryIgniteExecutionEffectRoles("prisoner torso", "pyre_fire_torso");
                TryPlayVictimVoice(SkinVoiceManager.VoiceType.Yell, "scream at torso ignition");
                break;

            case BurningPhase.Torso when _phaseElapsed >= BurningTorsoSeconds:
                _phase = BurningPhase.LethalDelay;
                _phaseElapsed = 0f;
                _host.LogInfo(
                    $"The prisoner remains in the crucifix-bound idle for the " +
                    $"{BurningLethalDelaySeconds:0.00}s lethal delay.");
                break;

            case BurningPhase.LethalDelay when _phaseElapsed >= BurningLethalDelaySeconds:
                _phase = BurningPhase.DeathTransition;
                _phaseElapsed = 0f;
                TryStartVictimPose(
                    BurningCrucifixDeathAction,
                    BurningDeathPoseBlendSeconds,
                    "burning crucifix death transition at the lethal trigger",
                    ref _deathPoseStarted);
                TryApplyCharredAppearance("the fire reached the final death pose");
                TryPlayVictimVoice(SkinVoiceManager.VoiceType.Death, "burning death cry");
                break;

            case BurningPhase.DeathTransition when _phaseElapsed >= BurningDeathPoseBlendSeconds:
                _phase = BurningPhase.Completed;
                _phaseElapsed = 0f;
                _host.LogInfo(
                    $"Completed the {BurningDeathPoseBlendSeconds:0.00}s crucifix idle-to-death blend; " +
                    "the final death pose will now be frozen at the stake without ragdoll.");
                _host.CompleteMethodExecutionSequence("custom crucifix burning death pose");
                break;
        }
    }

    public void ApplyLethalFrameEffects()
    {
        // Every fire stage and the 0.40-second crucifix death crossfade complete
        // before the shared lethal flow freezes the original prisoner.
    }

    private void ResolveGroundIgnitionOrder()
    {
        var placement = _host.Placement;
        if (_groundFireOrder.Length == 0 && placement is not null)
        {
            _groundFireOrder = CreateFallbackGroundFirePositions(placement);
            _host.LogWarning(
                "No successfully spawned root position was available for ignition; " +
                "fifteen fallback positions inside the pyre footprint will continue.");
        }

        if (_groundFireOrder.Length == 0)
        {
            _kindlingSparkPosition = _host.VictimPosition;
            if (_kindlingSparkPosition.IsValid)
            {
                _kindlingSparkPosition.z += 0.10f;
            }

            _host.LogWarning(
                "No root-referenced fire position could be resolved; ground flames are unavailable " +
                "but the central and body fire stages will continue.");
            return;
        }

        var actorPosition = _host.GetExecutionActorPosition();
        if (!actorPosition.IsValid)
        {
            actorPosition = _host.GetActiveExecutionActor()?.Position ?? Vec3.Invalid;
        }

        var distances = new float[_groundFireOrder.Length];
        for (var index = 0; index < _groundFireOrder.Length; index++)
        {
            distances[index] = actorPosition.IsValid
                ? actorPosition.AsVec2.Distance(_groundFireOrder[index].AsVec2)
                : index;
        }

        for (var left = 0; left < _groundFireOrder.Length - 1; left++)
        {
            var nearest = left;
            for (var right = left + 1; right < _groundFireOrder.Length; right++)
            {
                if (distances[right] < distances[nearest])
                {
                    nearest = right;
                }
            }

            if (nearest == left)
            {
                continue;
            }

            (_groundFireOrder[left], _groundFireOrder[nearest]) =
                (_groundFireOrder[nearest], _groundFireOrder[left]);
            (distances[left], distances[nearest]) = (distances[nearest], distances[left]);
        }

        _kindlingSparkPosition = _groundFireOrder[0];
        _host.LogInfo(
            $"Resolved {_groundFireOrder.Length} randomized root-referenced fire/smoke positions from the active actor; " +
            $"first=({_groundFireOrder[0].x:0.00},{_groundFireOrder[0].y:0.00},{_groundFireOrder[0].z:0.00}), " +
            $"then a direct mesh-free '{PyreFireAndSmokeParticleSystem}' core at the prisoner base.");
    }

    private static Vec3[] CreateFallbackGroundFirePositions(ExecutionScenePlacement placement)
    {
        var positions = new Vec3[KindlingFireCount];
        for (var index = 0; index < positions.Length; index++)
        {
            var angle = index * (MathF.PI * 2f / positions.Length) +
                        (MBRandom.RandomFloat - 0.5f) * 0.35f;
            var radius = 0.38f + MBRandom.RandomFloat * 0.26f;
            positions[index] = placement.Offset(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius,
                0.08f + MBRandom.RandomFloat * 0.12f);
        }

        return positions;
    }

    private void IgniteNextGroundFire()
    {
        if (_nextGroundFireIndex >= _groundFireOrder.Length)
        {
            return;
        }

        var position = _groundFireOrder[_nextGroundFireIndex];
        _nextGroundFireIndex++;
        var scale = GroundFireMinimumScale + MBRandom.RandomFloat * GroundFireScaleRange;
        var fireCreated = TryCreatePersistentPyreParticle(
            position,
            scale,
            $"root fire/smoke {_nextGroundFireIndex}/{_groundFireOrder.Length}");
        if (fireCreated && !_kindlingCharringStarted)
        {
            _kindlingCharringStarted = true;
            _kindlingCharElapsed = 0f;
            _host.LogInfo(
                $"Kindling charring began with the first visible ground fire and will spread over " +
                $"{KindlingCharDurationSeconds:0.00}s instead of following the fast fire-point spawn rate.");
        }
    }

    private void TryIgniteCoreFireAndSmoke()
    {
        var position = _groundVictimPosition.IsValid
            ? _groundVictimPosition
            : _host.VictimPosition;
        if (position.IsValid)
        {
            // The direct effect has no burned-log mesh or internal 0.327m
            // prefab offset. Lift its origin into the cone so the flame and
            // light smoke emerge through the upper branches.
            position.z += 0.44f;
        }

        TryCreatePersistentPyreParticle(
            position,
            CentralFireScale,
            "mesh-free central fire/smoke core");
    }

    private void TryIgnitePureBodyFlames()
    {
        if (_pureFlamesIgnited || PureFlameOffsets.Length != PureFlameCount || !_host.VictimPosition.IsValid)
        {
            return;
        }

        _pureFlamesIgnited = true;
        var placement = _host.Placement;
        var right = placement?.Right ?? new Vec2(1f, 0f);
        var forward = placement?.Forward ?? Vec2.Forward;
        for (var index = 0; index < PureFlameOffsets.Length; index++)
        {
            var offset = PureFlameOffsets[index];
            var position = _host.VictimPosition +
                           new Vec3(
                               right.x * offset.Right + forward.x * offset.Forward,
                               right.y * offset.Right + forward.y * offset.Forward,
                               offset.Up);
            TryCreatePersistentPyreParticle(
                position,
                offset.Scale,
                $"pure body flame {index + 1}/{PureFlameCount}",
                PyreFireFallbackParticleSystem);
        }
    }

    private bool TryCreatePersistentPyreParticle(
        Vec3 position,
        float scale,
        string purpose,
        string preferredParticleSystem = PyreFireAndSmokeParticleSystem)
    {
        GameEntity? entity = null;
        try
        {
            if (!position.IsValid)
            {
                _host.LogWarning(
                    $"Could not create direct pyre particles for {purpose}; the world position was invalid.");
                return false;
            }

            var particleSystemName = preferredParticleSystem;
            var runtimeId = ParticleSystemManager.GetRuntimeIdByName(particleSystemName);
            if (runtimeId < 0)
            {
                particleSystemName = string.Equals(
                    preferredParticleSystem,
                    PyreFireFallbackParticleSystem,
                    StringComparison.OrdinalIgnoreCase)
                    ? PyreFireAndSmokeParticleSystem
                    : PyreFireFallbackParticleSystem;
                runtimeId = ParticleSystemManager.GetRuntimeIdByName(particleSystemName);
                _host.LogWarning(
                    $"Direct pyre particle '{preferredParticleSystem}' was unavailable for {purpose}; " +
                    $"falling back to '{particleSystemName}' without spawning a prefab.");
            }

            if (runtimeId < 0)
            {
                _host.LogWarning(
                    $"Could not resolve either direct pyre particle system for {purpose}; the sequence continues.");
                return false;
            }

            entity = GameEntity.CreateEmpty(
                _host.Mission.Scene,
                isModifiableFromEditor: false,
                createPhysics: false,
                callScriptCallbacks: false);
            if (entity is null)
            {
                _host.LogWarning(
                    $"Could not create the empty scene entity for direct pyre particles during {purpose}.");
                return false;
            }

            var frame = MatrixFrame.Identity;
            frame.origin = position;
            frame.rotation.ApplyScaleLocal(scale);
            entity.SetGlobalFrame(in frame, isTeleportation: true);
            entity.SetMobility(GameEntity.Mobility.Stationary);

            var localFrame = MatrixFrame.Identity;
            var particle = ParticleSystem.CreateParticleSystemAttachedToEntity(
                runtimeId,
                entity,
                ref localFrame);
            if (particle is null)
            {
                entity.AddParticleSystemComponent(particleSystemName);
            }
            else
            {
                particle.SetDontRemoveFromEntity(true);
                particle.SetRuntimeEmissionRateMultiplier(1f);
                particle.Restart();
            }

            _pyreParticleEntities.Add(entity);
            entity = null;
            _pyreParticleSpawned++;
            _host.LogInfo(
                $"Created direct pyre particle {_pyreParticleSpawned} for {purpose}: " +
                $"system='{particleSystemName}', position=({position.x:0.00},{position.y:0.00},{position.z:0.00}), " +
                $"scale={scale:0.00}, prefab=false.");
            return true;
        }
        catch (Exception exception)
        {
            if (entity is not null)
            {
                _host.RemoveSpawnedEntity(entity);
            }

            _host.LogWarning(
                $"Direct pyre particles failed during {purpose}; the remaining sequence continues. " +
                exception.Message);
            return false;
        }
    }

    private void RetryReleaseExecutionActor(float dt)
    {
        if (_executionActorReleased || _phase == BurningPhase.None)
        {
            return;
        }

        _actorReleaseRetryElapsed += MathF.Max(0f, dt);
        if (_actorReleaseRetryElapsed < ActorReleaseRetrySeconds)
        {
            return;
        }

        _actorReleaseRetryElapsed = 0f;
        _executionActorReleased = _host.ReleaseExecutionActorControl(
            "burning sequence release retry");
    }

    public bool ApplyDeath()
    {
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive() || !_host.VictimPosition.IsValid)
        {
            _host.LogError(
                "The original prisoner Agent is unavailable and cannot be frozen in the burning death pose.");
            return false;
        }

        try
        {
            TryApplyCharredAppearance("burning death application");
            if (!_victimMortalityCaptured)
            {
                _victimMortalityBeforeFreeze = victim.CurrentMortalityState;
                _victimMortalityCaptured = true;
            }

            victim.SetMortalityState(Agent.MortalityState.Invulnerable);
            _deathPoseFrozen = _host.FreezeVictimForSuspension(
                _host.VictimPosition,
                "original-prisoner burning death pose at the stake");
            if (_deathPoseFrozen)
            {
                MaintainFrozenDeathPose();
                _host.LogInfo(
                    $"Original prisoner Agent {victim.Index} remains upright in the final burning pose; " +
                    "no RegisterBlow, native death action, corpse pool or ragdoll was entered. " +
                    "Campaign death remains deferred until mission exit.");
            }

            return _deathPoseFrozen;
        }
        catch (Exception exception)
        {
            _host.LogError(
                "Could not freeze the original prisoner in the final burning pose; no battlefield-death fallback is attempted.",
                exception);
            return false;
        }
    }

    public void TickAfterDeath(float dt)
    {
        TickKindlingCharring(MathF.Max(0f, dt));
        MaintainFrozenDeathPose();
    }

    public void Cleanup()
    {
        _deathPoseFrozen = false;
        for (var index = _pyreParticleEntities.Count - 1; index >= 0; index--)
        {
            _host.RemoveSpawnedEntity(_pyreParticleEntities[index]);
        }

        _pyreParticleEntities.Clear();
        var victim = _host.VictimAgent;
        if (_victimMortalityCaptured && victim is not null && victim.IsActive())
        {
            try
            {
                victim.SetMortalityState(_victimMortalityBeforeFreeze);
            }
            catch (Exception exception)
            {
                _host.LogWarning(
                    $"Could not restore the original prisoner's mortality state during burning cleanup. {exception.Message}");
            }
        }

        RestoreVictimFactorColors();
        RestoreKindlingFactorColors();
        _kindlingCharringStarted = false;
        _kindlingCharElapsed = 0f;
        RestoreRaisedVictimState();
    }

    private void RestoreRaisedVictimState()
    {
        if (!_victimHeightRaised)
        {
            ReleaseNativePyreUseState(_host.VictimAgent);
            return;
        }

        var victim = _host.VictimAgent;
        try
        {
            if (victim is not null && victim.IsActive())
            {
                victim.SetCurrentActionSpeed(0, 1f);
                victim.ClearTargetFrame();
                var zeroUp = Vec3.Zero;
                victim.SetTargetUp(in zeroUp);
                victim.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: false);
                victim.SetForceAttachedEntity(WeakGameEntity.Invalid);
                if (_pyreUsePoint is not null &&
                    ReferenceEquals(victim.CurrentlyUsedGameObject, _pyreUsePoint))
                {
                    victim.StopUsingGameObject(isSuccessful: false);
                }
                victim.SetIsPhysicsForceClosed(false);
                victim.SetIsAIPaused(false);
                if (_groundVictimPosition.IsValid)
                {
                    var direction = _host.Placement?.Forward ?? Vec2.Forward;
                    _host.PoseAgentAt(victim, _groundVictimPosition, direction);
                }
            }

            if (_groundVictimPosition.IsValid)
            {
                _host.VictimPosition = _groundVictimPosition;
            }

            _host.LogInfo(
                "Restored the original prisoner from the temporary 1.00m cone-top presentation during cleanup.");
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                "Could not completely restore the temporary cone-top prisoner presentation during cleanup. " +
                exception.Message);
        }
        finally
        {
            _victimHeightRaised = false;
            ReleaseNativePyreUseState(victim);
        }
    }

    private void ReleaseNativePyreUseState(Agent? victim)
    {
        try
        {
            if (victim is not null && victim.IsActive())
            {
                if (_pyreUsePoint is not null &&
                    ReferenceEquals(victim.CurrentlyUsedGameObject, _pyreUsePoint))
                {
                    victim.StopUsingGameObject(isSuccessful: false);
                }

                victim.SetForceAttachedEntity(WeakGameEntity.Invalid);
            }
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                $"Could not completely release the original prisoner's native pyre attachment. {exception.Message}");
        }

        _pyreUsePoint?.Unbind();
        _pyreUsePoint = null;
        if (_pyreUsePointHost is not null)
        {
            _host.RemoveSpawnedEntity(_pyreUsePointHost);
            _pyreUsePointHost = null;
        }
    }

    private bool TryApplyCharredAppearance(string purpose)
    {
        if (_charredAppearanceApplied)
        {
            return true;
        }

        if (!_victimFactorColorsCaptured && !TryCaptureVictimFactorColors())
        {
            return false;
        }

        var appliedCount = 0;
        foreach (var state in _victimFactorColors)
        {
            try
            {
                state.Entity.SetFactorColor(CharredVisualColor);
                appliedCount++;
            }
            catch (Exception exception)
            {
                _host.LogWarning(
                    $"Could not char one prisoner visual entity during {purpose}; remaining visuals continue. " +
                    exception.Message);
            }
        }

        _charredAppearanceApplied = appliedCount > 0;
        if (_charredAppearanceApplied)
        {
            _host.LogInfo(
                $"Applied charred factor color 0x{CharredVisualColor:X8} to " +
                $"{appliedCount}/{_victimFactorColors.Count} original-prisoner visual entities during {purpose}.");
        }
        else
        {
            _host.LogWarning(
                $"No original-prisoner visual entity accepted the charred factor color during {purpose}; " +
                "the burning sequence continues.");
        }

        return _charredAppearanceApplied;
    }

    private bool TryCaptureVictimFactorColors()
    {
        var visuals = _host.VictimAgent?.AgentVisuals;
        if (visuals is null || !visuals.IsValid())
        {
            _host.LogWarning(
                "Could not capture the original prisoner's visual colors because AgentVisuals is unavailable.");
            return false;
        }

        try
        {
            var root = visuals.GetEntity();
            if (root is null)
            {
                _host.LogWarning(
                    "Could not capture the original prisoner's visual colors because the visual root is unavailable.");
                return false;
            }

            var entities = new List<GameEntity> { root };
            var children = new List<GameEntity>();
            root.GetChildrenRecursive(ref children);
            entities.AddRange(children);

            _victimFactorColors.Clear();
            foreach (var entity in entities)
            {
                try
                {
                    _victimFactorColors.Add(
                        new EntityFactorColorState(entity, entity.GetFactorColor()));
                }
                catch (Exception exception)
                {
                    _host.LogWarning(
                        "Could not read one original-prisoner visual factor color; remaining entities continue. " +
                        exception.Message);
                }
            }

            _victimFactorColorsCaptured = _victimFactorColors.Count > 0;
            return _victimFactorColorsCaptured;
        }
        catch (Exception exception)
        {
            _host.LogError(
                "Could not enumerate original-prisoner visuals for the charred appearance; the execution continues.",
                exception);
            return false;
        }
    }

    private void RestoreVictimFactorColors()
    {
        if (!_victimFactorColorsCaptured)
        {
            return;
        }

        var restoredCount = 0;
        foreach (var state in _victimFactorColors)
        {
            try
            {
                state.Entity.SetFactorColor(state.FactorColor);
                restoredCount++;
            }
            catch (Exception exception)
            {
                _host.LogWarning(
                    "Could not restore one original-prisoner visual factor color during cleanup. " +
                    exception.Message);
            }
        }

        _host.LogInfo(
            $"Restored {restoredCount}/{_victimFactorColors.Count} original-prisoner visual factor colors during cleanup.");
        _victimFactorColors.Clear();
        _victimFactorColorsCaptured = false;
        _charredAppearanceApplied = false;
    }

    private void CaptureKindlingFactorColors(GameEntity root)
    {
        try
        {
            var group = new List<EntityFactorColorState>();
            var entities = new List<GameEntity> { root };
            var children = new List<GameEntity>();
            root.GetChildrenRecursive(ref children);
            entities.AddRange(children);
            foreach (var entity in entities)
            {
                try
                {
                    var state = new EntityFactorColorState(entity, entity.GetFactorColor());
                    _kindlingFactorColors.Add(state);
                    group.Add(state);
                }
                catch (Exception exception)
                {
                    _host.LogWarning(
                        "Could not capture one kindling factor color; remaining roots continue. " +
                        exception.Message);
                }
            }

            if (group.Count > 0)
            {
                _kindlingFactorColorGroups.Add(group);
            }
        }
        catch (Exception exception)
        {
            _host.LogWarning(
                "Could not enumerate one kindling prefab for later charring; remaining roots continue. " +
                exception.Message);
        }
    }

    private void TickKindlingCharring(float dt)
    {
        if (!_kindlingCharringStarted ||
            _kindlingCharredAppearanceApplied ||
            _kindlingFactorColorGroups.Count == 0)
        {
            return;
        }

        _kindlingCharElapsed = MathF.Min(
            KindlingCharDurationSeconds,
            _kindlingCharElapsed + MathF.Max(0f, dt));
        var progress = KindlingCharDurationSeconds <= 0f
            ? 1f
            : _kindlingCharElapsed / KindlingCharDurationSeconds;
        var targetGroupCount = Math.Min(
            _kindlingFactorColorGroups.Count,
            (int)Math.Floor(_kindlingFactorColorGroups.Count * progress));
        if (targetGroupCount > _charredKindlingGroupCount)
        {
            TryApplyKindlingCharredProgress(
                targetGroupCount,
                $"time-based pyre spread at {_kindlingCharElapsed:0.00}/{KindlingCharDurationSeconds:0.00}s");
        }
    }

    private bool TryApplyKindlingCharredProgress(int requestedGroupCount, string purpose)
    {
        var targetGroupCount = Math.Max(
            _charredKindlingGroupCount,
            Math.Min(requestedGroupCount, _kindlingFactorColorGroups.Count));
        if (targetGroupCount <= _charredKindlingGroupCount)
        {
            return _charredKindlingGroupCount > 0;
        }

        var appliedEntityCount = 0;
        var attemptedGroupCount = 0;
        for (var groupIndex = _charredKindlingGroupCount;
             groupIndex < targetGroupCount;
             groupIndex++)
        {
            attemptedGroupCount++;
            foreach (var state in _kindlingFactorColorGroups[groupIndex])
            {
                try
                {
                    state.Entity.SetFactorColor(CharredKindlingColor);
                    appliedEntityCount++;
                }
                catch (Exception exception)
                {
                    _host.LogWarning(
                        $"Could not char one kindling entity during {purpose}; remaining roots continue. " +
                        exception.Message);
                }
            }
        }

        _charredKindlingGroupCount = targetGroupCount;
        _kindlingCharredAppearanceApplied =
            _kindlingFactorColorGroups.Count > 0 &&
            _charredKindlingGroupCount >= _kindlingFactorColorGroups.Count;
        if (appliedEntityCount > 0)
        {
            _host.LogInfo(
                $"Progressively charred {attemptedGroupCount} wood prefab group(s), " +
                $"{appliedEntityCount} entity instance(s), during {purpose}; " +
                $"progress={_charredKindlingGroupCount}/{_kindlingFactorColorGroups.Count}.");
        }
        else
        {
            _host.LogWarning(
                $"No kindling entity accepted the near-black factor color during {purpose}; " +
                "the burning sequence continues.");
        }

        return _charredKindlingGroupCount > 0;
    }

    private void RestoreKindlingFactorColors()
    {
        if (_kindlingFactorColors.Count == 0)
        {
            return;
        }

        var restoredCount = 0;
        foreach (var state in _kindlingFactorColors)
        {
            try
            {
                state.Entity.SetFactorColor(state.FactorColor);
                restoredCount++;
            }
            catch (Exception exception)
            {
                _host.LogWarning(
                    "Could not restore one kindling factor color during cleanup. " +
                    exception.Message);
            }
        }

        _host.LogInfo(
            $"Restored {restoredCount}/{_kindlingFactorColors.Count} kindling factor colors during cleanup.");
        _kindlingFactorColors.Clear();
        _kindlingFactorColorGroups.Clear();
        _charredKindlingGroupCount = 0;
        _kindlingCharredAppearanceApplied = false;
    }

    private void MaintainFrozenDeathPose()
    {
        if (!_deathPoseFrozen)
        {
            return;
        }

        var victim = _host.VictimAgent;
        var placement = _host.Placement;
        var position = _host.VictimPosition;
        if (victim is null || placement is null || !position.IsValid || !victim.IsActive())
        {
            _deathPoseFrozen = false;
            _host.LogWarning(
                "Stopped maintaining the burning death pose because the original prisoner became unavailable.");
            return;
        }

        try
        {
            if (victim.State == AgentState.Killed ||
                victim.IsAddedAsCorpse() ||
                victim.IsFadingOut())
            {
                _deathPoseFrozen = false;
                _host.LogWarning(
                    "Stopped maintaining the burning death pose because another runtime path took over native death/corpse presentation.");
                return;
            }

            victim.Controller = AgentControllerType.None;
            victim.SetIsAIPaused(true);
            victim.SetCurrentActionSpeed(0, 0f);
            victim.SetIsPhysicsForceClosed(false);
            victim.SetExcludedFromGravity(exclude: true, applyAverageGlobalVelocity: false);
            EnsureNativePyreUseState(victim, "maintained burning death pose");
            var direction = placement.Forward.IsNonZero()
                ? placement.Forward.Normalized()
                : Vec2.Forward;
            var direction3D = new Vec3(direction.x, direction.y, 0f);
            var targetUp = new Vec3(0f, 0f, 1f);
            victim.SetTargetZ(position.z);
            victim.SetTargetPositionAndDirection(position.AsVec2, direction3D);
            victim.SetTargetUp(in targetUp);
        }
        catch (Exception exception)
        {
            _deathPoseFrozen = false;
            _host.LogWarning(
                $"Stopped maintaining the burning death pose after an error. {exception.Message}");
        }
    }

    private bool TryStartVictimPose(
        string actionName,
        float blendSeconds,
        string purpose,
        ref bool started)
    {
        if (started)
        {
            return true;
        }

        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return false;
        }

        try
        {
            var action = ActionIndexCache.Create(actionName);
            if (action == ActionIndexCache.act_none)
            {
                _host.LogError($"Custom action '{actionName}' is unavailable for {purpose}.");
                return false;
            }

            started = victim.SetActionChannel(
                0,
                in action,
                ignorePriority: true,
                additionalFlags: AnimFlags.anf_restart |
                                 AnimFlags.anf_lock_movement |
                                 AnimFlags.anf_enforce_all |
                                 AnimFlags.anf_enforce_root_rotation |
                                 AnimFlags.anf_disable_foot_ik |
                                 AnimFlags.anf_disable_agent_agent_collisions,
                blendWithNextActionFactor: 0f,
                actionSpeed: 1f,
                blendInPeriod: blendSeconds,
                blendOutPeriodToNoAnim: 0f,
                startProgress: 0f);
            _host.LogInfo(
                $"Started {purpose} on channel 0: requested='{actionName}', " +
                $"started={started}, current={victim.GetCurrentAction(0).Index}, blend={blendSeconds:0.00}s.");
            return started;
        }
        catch (Exception exception)
        {
            _host.LogError($"Could not start {purpose}; the burning sequence continues.", exception);
            return false;
        }
    }

    private void TryPlayVictimVoice(
        SkinVoiceManager.SkinVoiceType voice,
        string purpose)
    {
        var victim = _host.VictimAgent;
        if (victim is null || !victim.IsActive())
        {
            return;
        }

        try
        {
            victim.MakeVoice(
                voice,
                SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
            _host.LogInfo($"Requested prisoner voice '{voice.TypeID}' for {purpose}.");
        }
        catch (Exception exception)
        {
            _host.LogError($"Could not play prisoner {purpose}; the fire sequence continues.", exception);
        }
    }
}

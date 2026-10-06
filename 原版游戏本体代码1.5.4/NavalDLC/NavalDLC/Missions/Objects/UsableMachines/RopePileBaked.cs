using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class RopePileBaked : ScriptComponentBehavior
{
	private enum RopeLod
	{
		Near,
		Medium,
		Far,
		Culled
	}

	public enum RopeSlackPolicy
	{
		Natural,
		Taut,
		Collapsed
	}

	public const float HookLength = 0.5f;

	private const int NumberOfPoints = 12;

	private const int NumberOfDataPerFrame = 4;

	private const int BakedFrameCount = 3;

	private const int AdditionalBoneFrameCount = 4;

	private static readonly float[] PointParam = ComputePointParams();

	private static readonly float[] SegmentFraction = ComputeSegmentFractions();

	private const float LodNearDistance = 30f;

	private const float LodMediumDistance = 100f;

	private const float LodCullDistance = 300f;

	private const float LodFrustumCosine = 0.35f;

	private const float LodBoundingRadiusPadding = 2f;

	private const int VerletIterationsNear = 4;

	private const int VerletIterationsMedium = 3;

	private const int WaterSamplesNear = 8;

	private const int WaterSamplesMedium = 4;

	private const float WindForceScale = 0.4f;

	private const float WaterFloatOffset = 0.05f;

	private const float RestLengthSmoothing = 3f;

	private const float MaxVerletDt = 0.05f;

	private const float DefaultVerletRestLength = 40f;

	public const float SagCoef = 0.25f;

	private Mesh _ropeMesh;

	private Vec3[] _posWorld;

	private Vec3[] _prevPosWorld;

	private Vec3[] _wobbleImpulseWorld;

	private bool _verletInitialized;

	private bool _lastLodWasFarOrCulled;

	private float _lastVerletTime;

	private float _verletRestLength = 40f;

	private float _targetRestLength = 40f;

	private float _meshUnfurlOverride = -1f;

	private bool _sourcePinned = true;

	private bool _targetPinned = true;

	private float _lifetimeRemaining = -1f;

	private float _fadeDuration;

	private float _lifetimeTotal;

	private GameEntity _sourceAnchorTracker;

	private GameEntity _targetAnchorTracker;

	private Vec3 _sourceAnchorLocalOffset = Vec3.Zero;

	private Vec3 _targetAnchorLocalOffset = Vec3.Zero;

	private float _dampingOverride = -1f;

	private float _dampingTargetOverride = -1f;

	private Vec3 _driftAcceleration = Vec3.Zero;

	private GameEntity _clipPlaneSrcEntity;

	private GameEntity _clipPlaneTgtEntity;

	private bool _applyClipPlanes;

	private const float DampingConvergenceRate = 0.5f;

	private BoundingBox _localUpdatedBoundingBox;

	private BoundingBox _ropePileBaseBoundingBox;

	private const int DebugMaterialPointCount = 24;

	private const float DebugMaxRopeLength = 40f;

	private static float[] ComputePointParams()
	{
		float[] array = new float[12];
		for (int i = 0; i < 12; i++)
		{
			array[i] = 0.5f * (1f - TaleWorlds.Library.MathF.Cos(System.MathF.PI * (float)i / 11f));
		}
		return array;
	}

	private static float[] ComputeSegmentFractions()
	{
		float[] array = new float[11];
		for (int i = 0; i < 11; i++)
		{
			array[i] = PointParam[i + 1] - PointParam[i];
		}
		return array;
	}

	protected override void OnEditorInit()
	{
		base.OnEditorInit();
		_ropeMesh = base.GameEntity.GetFirstMesh();
	}

	protected override void OnInit()
	{
		base.OnInit();
		_ropeMesh = base.GameEntity.GetFirstMesh();
		_ropeMesh.SetupAdditionalBoneBuffer(4);
		_posWorld = new Vec3[12];
		_prevPosWorld = new Vec3[12];
		_wobbleImpulseWorld = new Vec3[12];
		_ropePileBaseBoundingBox = base.GameEntity.GetLocalBoundingBox();
		_localUpdatedBoundingBox = _ropePileBaseBoundingBox;
		base.GameEntity.SetHasCustomBoundingBoxValidationSystem(hasCustomBoundingBox: true);
	}

	protected override void OnBoundingBoxValidate()
	{
		BoundingBox boundingBox = default(BoundingBox);
		boundingBox.BeginRelaxation();
		if (base.GameEntity.ChildCount > 0)
		{
			boundingBox = base.GameEntity.ComputeBoundingBoxIncludeChildren();
		}
		boundingBox.RelaxWithBoundingBox(_localUpdatedBoundingBox);
		boundingBox.RecomputeRadius();
		base.GameEntity.RelaxLocalBoundingBox(in boundingBox);
	}

	public override TickRequirement GetTickRequirement()
	{
		if (!(_lifetimeRemaining > 0f))
		{
			return TickRequirement.None;
		}
		return TickRequirement.Tick;
	}

	protected override void OnTick(float dt)
	{
		if (_lifetimeRemaining > 0f)
		{
			_lifetimeRemaining -= dt;
			RunChain((_sourceAnchorTracker != null) ? ResolveAnchorPosition(_sourceAnchorTracker, _sourceAnchorLocalOffset) : (_sourcePinned ? base.GameEntity.GlobalPosition : _posWorld[0]), (_targetAnchorTracker != null) ? ResolveAnchorPosition(_targetAnchorTracker, _targetAnchorLocalOffset) : (_targetPinned ? base.GameEntity.GlobalPosition : _posWorld[11]));
			if (_fadeDuration > 0f && _lifetimeRemaining < _fadeDuration)
			{
				uint factorColor = (((uint)(TaleWorlds.Library.MathF.Max(0f, _lifetimeRemaining / _fadeDuration) * 255f) & 0xFF) << 24) | 0xFFFFFF;
				base.GameEntity.SetFactorColor(factorColor);
			}
		}
		else
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: false);
			_lifetimeRemaining = -1f;
			SetScriptComponentToTick(GetTickRequirement());
			base.GameEntity.SetFactorColor(uint.MaxValue);
		}
	}

	public void SetEndPinning(bool sourcePinned, bool targetPinned)
	{
		_sourcePinned = sourcePinned;
		_targetPinned = targetPinned;
	}

	public void SetApplyClipPlanes(bool applyClipPlanes)
	{
		_applyClipPlanes = applyClipPlanes;
	}

	public void SetSourceAnchorTracker(GameEntity tracker, Vec3 localOffset)
	{
		_sourceAnchorTracker = tracker;
		_sourceAnchorLocalOffset = localOffset;
	}

	public void SetTargetAnchorTracker(GameEntity tracker, Vec3 localOffset)
	{
		_targetAnchorTracker = tracker;
		_targetAnchorLocalOffset = localOffset;
	}

	public void ClearAnchorTrackers()
	{
		_sourceAnchorTracker = null;
		_targetAnchorTracker = null;
		_sourceAnchorLocalOffset = Vec3.Zero;
		_targetAnchorLocalOffset = Vec3.Zero;
	}

	private Vec3 ResolveAnchorPosition(GameEntity tracker, Vec3 localOffset)
	{
		if (tracker == null)
		{
			return Vec3.Zero;
		}
		if (localOffset.LengthSquared < 1E-06f)
		{
			return tracker.GlobalPosition;
		}
		return tracker.GetGlobalFrame().TransformToParent(in localOffset);
	}

	public void SetDampingRamp(float initialRate, float targetRate)
	{
		_dampingOverride = initialRate;
		_dampingTargetOverride = targetRate;
	}

	public void ClearDampingRamp()
	{
		_dampingOverride = -1f;
		_dampingTargetOverride = -1f;
	}

	public void SetDriftAcceleration(Vec3 acceleration)
	{
		_driftAcceleration = acceleration;
	}

	public void ClearDriftAcceleration()
	{
		_driftAcceleration = Vec3.Zero;
	}

	public void SetClipPlaneEntities(GameEntity srcHullEntity, GameEntity tgtHullEntity)
	{
		_clipPlaneSrcEntity = srcHullEntity;
		_clipPlaneTgtEntity = tgtHullEntity;
	}

	public void ClearClipPlanes()
	{
		_clipPlaneSrcEntity = null;
		_clipPlaneTgtEntity = null;
	}

	public void StartLifetime(float totalSeconds, float fadeSeconds)
	{
		_lifetimeTotal = totalSeconds;
		_lifetimeRemaining = totalSeconds;
		_fadeDuration = fadeSeconds;
		SetScriptComponentToTick(GetTickRequirement());
	}

	public void SeedChain(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		_verletInitialized = false;
		InitializeChainToStraightLine(in sourceGlobalPosition, in targetGlobalPosition);
		_verletInitialized = true;
		_lastVerletTime = Mission.Current?.CurrentTime ?? 0f;
	}

	public void SeedChainFromSlice(RopePileBaked sourceRope, float startFraction, float endFraction)
	{
		if (sourceRope != null && sourceRope._posWorld != null && sourceRope._verletInitialized)
		{
			startFraction = MBMath.ClampFloat(startFraction, 0f, 1f);
			endFraction = MBMath.ClampFloat(endFraction, 0f, 1f);
			for (int i = 0; i < 12; i++)
			{
				float fraction = startFraction + PointParam[i] * (endFraction - startFraction);
				Vec3 vec = sourceRope.SampleChainAtFraction(fraction);
				_posWorld[i] = vec;
				_prevPosWorld[i] = vec;
				_wobbleImpulseWorld[i] = Vec3.Zero;
			}
			_verletRestLength = _targetRestLength;
			_verletInitialized = true;
			_lastVerletTime = Mission.Current?.CurrentTime ?? 0f;
		}
	}

	public void SetRopeState(RopeSlackPolicy policy, float referenceLength, float tension = 0f)
	{
		_targetRestLength = TaleWorlds.Library.MathF.Max(ComputeRestLength(policy, referenceLength, tension), 0.1f);
	}

	public void SnapRopeState(RopeSlackPolicy policy, float referenceLength, float tension = 0f)
	{
		_targetRestLength = TaleWorlds.Library.MathF.Max(ComputeRestLength(policy, referenceLength, tension), 0.1f);
		_verletRestLength = _targetRestLength;
	}

	private static float ComputeRestLength(RopeSlackPolicy policy, float reference, float tension)
	{
		return policy switch
		{
			RopeSlackPolicy.Natural => reference, 
			RopeSlackPolicy.Taut => reference * (0.9f - 0.1f * TaleWorlds.Library.MathF.Clamp(tension, 0f, 1f)), 
			RopeSlackPolicy.Collapsed => 0.1f, 
			_ => reference, 
		};
	}

	public void SetMeshUnfurlOverride(float unfurlFraction)
	{
		_meshUnfurlOverride = unfurlFraction;
	}

	public void ClearMeshUnfurlOverride()
	{
		_meshUnfurlOverride = -1f;
	}

	public void ResetChain()
	{
		_verletInitialized = false;
	}

	public void ApplyWobble(Vec3 globalHitVector, float intensity, float freqScale = 1f, float hitT = 0.5f)
	{
		if (_wobbleImpulseWorld == null || !(intensity > 0f))
		{
			return;
		}
		float length = globalHitVector.Length;
		if (!(length >= 1E-05f))
		{
			return;
		}
		Vec3 vec = globalHitVector * (intensity / length);
		int num = (int)(MBMath.ClampFloat(hitT, 0f, 1f) * 11f);
		int num2 = (_sourcePinned ? 1 : 0);
		int num3 = (_targetPinned ? 10 : 11);
		for (int i = -3; i <= 3; i++)
		{
			int num4 = num + i;
			if (num4 >= num2 && num4 <= num3)
			{
				float num5 = 1f - (float)TaleWorlds.Library.MathF.Abs(i) / 4f;
				_wobbleImpulseWorld[num4] += vec * num5;
			}
		}
	}

	public MatrixFrame UpdateRopeMeshVisualAccordingToTargetPoint(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition, in Vec3 globalVelocity, float time)
	{
		Vec3 vec = globalVelocity + MBGlobals.GravitationalAcceleration * time;
		float time2 = time - 0.5f / TaleWorlds.Library.MathF.Max(vec.Length, 0.01f);
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = ProjectileSample(in globalVelocity, in sourceGlobalPosition, time2, 1f);
		identity.rotation.f = vec.NormalizedCopy();
		identity.rotation.s = Vec3.CrossProduct(identity.rotation.f, identity.rotation.u).NormalizedCopy();
		identity.rotation.u = Vec3.CrossProduct(identity.rotation.s, identity.rotation.f);
		identity.rotation.RotateAboutSide(-System.MathF.PI / 2f);
		RunChain(in sourceGlobalPosition, in identity.origin);
		return identity;
	}

	public Vec3 UpdateRopeMeshVisualAccordingToTargetPointLinear(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		Vec3 endGlobalPosition = ComputeEndFromTarget(in sourceGlobalPosition, in targetGlobalPosition);
		RunChain(in sourceGlobalPosition, in endGlobalPosition);
		return endGlobalPosition;
	}

	public Vec3 UpdateRopeMeshVisualAccordingToTargetPointLinearNoHookOffset(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		RunChain(in sourceGlobalPosition, in targetGlobalPosition);
		return targetGlobalPosition;
	}

	public Vec3 UpdateRopeMeshVisualAccordingToTargetPointLinearWithoutBoundingBoxUpdate(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		Vec3 endGlobalPosition = ComputeEndFromTarget(in sourceGlobalPosition, in targetGlobalPosition);
		RunChain(in sourceGlobalPosition, in endGlobalPosition, skipBoundingBox: true);
		return endGlobalPosition;
	}

	private void RunChain(in Vec3 sourceGlobalPosition, in Vec3 endGlobalPosition, bool skipBoundingBox = false)
	{
		MatrixFrame ropeGlobalFrame = base.GameEntity.GetGlobalFrame();
		float num = sourceGlobalPosition.Distance(endGlobalPosition);
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		float num2 = 1f - TaleWorlds.Library.MathF.Max((vectorArgument.x - num) / vectorArgument.x, 0f);
		vectorArgument.z = ((_meshUnfurlOverride >= 0f) ? MBMath.ClampFloat(_meshUnfurlOverride, 0f, 1f) : num2);
		_ropeMesh.SetVectorArgument(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
		Vec3 sourceLocal = ropeGlobalFrame.TransformToLocalNonOrthogonal(in sourceGlobalPosition);
		Vec3 endLocal = ropeGlobalFrame.TransformToLocalNonOrthogonal(in endGlobalPosition);
		RopeLod ropeLod = ComputeLod(in sourceLocal, in endLocal);
		switch (ropeLod)
		{
		case RopeLod.Culled:
			_lastLodWasFarOrCulled = true;
			return;
		case RopeLod.Far:
			WriteStraightBake(in sourceLocal, in endLocal);
			_lastLodWasFarOrCulled = true;
			return;
		}
		if (!_verletInitialized || _lastLodWasFarOrCulled)
		{
			InitializeChainToStraightLine(in sourceGlobalPosition, in endGlobalPosition);
			_verletInitialized = true;
			_lastVerletTime = Mission.Current?.CurrentTime ?? 0f;
		}
		_lastLodWasFarOrCulled = false;
		float num3 = Mission.Current?.CurrentTime ?? _lastVerletTime;
		float num4 = TaleWorlds.Library.MathF.Clamp(num3 - _lastVerletTime, 0f, 0.05f);
		_lastVerletTime = num3;
		if (num4 > 0f)
		{
			StepVerlet(in sourceGlobalPosition, in endGlobalPosition, ropeLod, num4);
		}
		else
		{
			_posWorld[0] = sourceGlobalPosition;
			_posWorld[11] = endGlobalPosition;
		}
		BakeChain(in ropeGlobalFrame, in sourceLocal, in endLocal, skipBoundingBox);
	}

	private void DebugRenderPoints(in Vec3 sourceGlobalPosition, in Vec3 endGlobalPosition)
	{
		float num = TaleWorlds.Library.MathF.Max(0f, 40f - _verletRestLength);
		float num2 = TaleWorlds.Library.MathF.Max(_verletRestLength, 0.001f);
		for (int i = 0; i < 24; i++)
		{
			float num3 = (float)i / 23f * 40f;
			if (!(num3 <= num))
			{
				float fraction = (num3 - num) / num2;
				SampleChainAtFraction(fraction);
			}
		}
	}

	private Vec3 SampleChainAtFraction(float fraction)
	{
		float num = MBMath.ClampFloat(fraction, 0f, 1f);
		int num2 = 0;
		int num3 = 11;
		for (int i = 0; i < 11; i++)
		{
			if (PointParam[i + 1] >= num)
			{
				num2 = i;
				num3 = i + 1;
				break;
			}
		}
		float num4 = PointParam[num3] - PointParam[num2];
		float alpha = ((num4 > 1E-05f) ? ((num - PointParam[num2]) / num4) : 0f);
		return Vec3.Lerp(_posWorld[num2], _posWorld[num3], alpha);
	}

	private void StepVerlet(in Vec3 sourceGlobalPosition, in Vec3 endGlobalPosition, RopeLod lod, float dt)
	{
		_verletRestLength = TaleWorlds.Library.MathF.Lerp(_verletRestLength, _targetRestLength, TaleWorlds.Library.MathF.Min(1f, 3f * dt));
		float globalTime = Mission.Current?.CurrentTime ?? 0f;
		Vec2 globalWindVelocityWithGustNoiseOfScene = base.GameEntity.GetGlobalWindVelocityWithGustNoiseOfScene(globalTime);
		Vec3 vec = new Vec3(globalWindVelocityWithGustNoiseOfScene.x * 0.4f, globalWindVelocityWithGustNoiseOfScene.y * 0.4f);
		Vec3 vec2 = MBGlobals.GravitationalAcceleration + vec + _driftAcceleration;
		float num = dt * dt;
		float num2 = ((_dampingOverride >= 0f) ? _dampingOverride : 3f);
		if (_dampingOverride >= 0f && _dampingTargetOverride >= 0f)
		{
			_dampingOverride = TaleWorlds.Library.MathF.Lerp(_dampingOverride, _dampingTargetOverride, TaleWorlds.Library.MathF.Min(1f, 0.5f * dt));
		}
		float num3 = TaleWorlds.Library.MathF.Max(0f, 1f - num2 * dt);
		int num4 = (_sourcePinned ? 1 : 0);
		int num5 = (_targetPinned ? 11 : 12);
		for (int i = num4; i < num5; i++)
		{
			Vec3 vec3 = _posWorld[i];
			Vec3 vec4 = _prevPosWorld[i];
			Vec3 vec5 = vec3 + (vec3 - vec4) * num3 + vec2 * num + _wobbleImpulseWorld[i] * dt;
			_wobbleImpulseWorld[i] = Vec3.Zero;
			_prevPosWorld[i] = vec3;
			_posWorld[i] = vec5;
		}
		if (_sourcePinned)
		{
			_posWorld[0] = sourceGlobalPosition;
			_prevPosWorld[0] = sourceGlobalPosition;
		}
		if (_targetPinned)
		{
			_posWorld[11] = endGlobalPosition;
			_prevPosWorld[11] = endGlobalPosition;
		}
		float verletRestLength = _verletRestLength;
		int num6 = ((lod == RopeLod.Near) ? 4 : 3);
		for (int j = 0; j < num6; j++)
		{
			bool num7 = (j & 1) == 0;
			int num8 = ((!num7) ? 10 : 0);
			int num9 = (num7 ? 11 : (-1));
			int num10 = (num7 ? 1 : (-1));
			for (int k = num8; k != num9; k += num10)
			{
				Vec3 vec6 = _posWorld[k];
				Vec3 vec7 = _posWorld[k + 1];
				Vec3 vec8 = vec7 - vec6;
				float length = vec8.Length;
				if (length < 1E-05f)
				{
					continue;
				}
				float num11 = SegmentFraction[k] * verletRestLength;
				if (length <= num11)
				{
					continue;
				}
				float num12 = (length - num11) * 0.5f / length;
				Vec3 vec9 = vec8 * num12;
				bool flag = k == 0 && _sourcePinned;
				bool flag2 = k + 1 == 11 && _targetPinned;
				if (!(flag && flag2))
				{
					if (flag)
					{
						_posWorld[k + 1] = vec7 - vec8 * (num12 * 2f);
						continue;
					}
					if (flag2)
					{
						_posWorld[k] = vec6 + vec8 * (num12 * 2f);
						continue;
					}
					_posWorld[k] = vec6 + vec9;
					_posWorld[k + 1] = vec7 - vec9;
				}
			}
		}
		if (_applyClipPlanes)
		{
			ApplyClipPlanes();
		}
		ApplyWaterFloor(lod);
	}

	private void ApplyClipPlanes()
	{
		if (!(_clipPlaneSrcEntity != null) && !(_clipPlaneTgtEntity != null))
		{
			return;
		}
		bool flag = _clipPlaneSrcEntity != null;
		bool flag2 = _clipPlaneTgtEntity != null;
		Vec3 vec = Vec3.Zero;
		Vec3 vec2 = Vec3.Zero;
		Vec3 vec3 = Vec3.Zero;
		Vec3 vec4 = Vec3.Zero;
		if (flag)
		{
			MatrixFrame globalFrame = _clipPlaneSrcEntity.GetGlobalFrame();
			vec = globalFrame.origin;
			vec2 = globalFrame.rotation.f.NormalizedCopy();
		}
		if (flag2)
		{
			MatrixFrame globalFrame2 = _clipPlaneTgtEntity.GetGlobalFrame();
			vec3 = globalFrame2.origin;
			vec4 = globalFrame2.rotation.f.NormalizedCopy();
		}
		for (int i = 0; i < 12; i++)
		{
			Vec3 vec5 = _posWorld[i];
			if (flag)
			{
				float num = Vec3.DotProduct(vec5 - vec, vec2);
				if (num < 0f)
				{
					vec5 += vec2 * (0f - num);
				}
			}
			if (flag2)
			{
				float num2 = Vec3.DotProduct(vec5 - vec3, vec4);
				if (num2 < 0f)
				{
					vec5 += vec4 * (0f - num2);
				}
			}
			_posWorld[i] = vec5;
		}
	}

	private void ApplyWaterFloor(RopeLod lod)
	{
		Scene scene = Mission.Current?.Scene;
		if (!(scene != null))
		{
			return;
		}
		int num = ((lod == RopeLod.Near) ? 8 : 4);
		if (num <= 0)
		{
			return;
		}
		int num2 = 10;
		int num3 = TaleWorlds.Library.MathF.Max(1, num2 / num);
		int num4 = (_sourcePinned ? num3 : 0);
		int num5 = (_targetPinned ? 11 : 12);
		int num6 = TaleWorlds.Library.MathF.Max(1, num3);
		float num7 = 0f;
		int num8 = -1;
		for (int i = num4; i < num5; i += num6)
		{
			Vec3 vec = _posWorld[i];
			float num9 = scene.GetWaterLevelAtPosition(vec.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: false) + 0.05f - vec.z;
			float num10 = ((num9 > 0f) ? num9 : 0f);
			if (num10 > 0f)
			{
				_posWorld[i].z += num10;
			}
			if (num8 >= 0 && (num10 > 0f || num7 > 0f))
			{
				for (int j = num8 + 1; j < i; j++)
				{
					float num11 = (float)(j - num8) / (float)(i - num8);
					float num12 = num7 * (1f - num11) + num10 * num11;
					if (num12 > 0f)
					{
						_posWorld[j].z += num12;
					}
				}
			}
			num7 = num10;
			num8 = i;
		}
	}

	private void BakeChain(in MatrixFrame ropeGlobalFrame, in Vec3 sourceLocalPos, in Vec3 endLocalPos, bool skipBoundingBox)
	{
		MatrixFrame frame = new MatrixFrame(new Mat3(in sourceLocalPos, new Vec3(12f), in endLocalPos), in endLocalPos);
		_ropeMesh.SetAdditionalBoneFrame(0, in frame);
		BoundingBox candidateLocalBoundingBox = new BoundingBox(ropeGlobalFrame.TransformToLocalNonOrthogonal(in _posWorld[0]));
		for (int i = 0; i < 3; i++)
		{
			int num = i * 4;
			Vec3 point = ropeGlobalFrame.TransformToLocalNonOrthogonal(in _posWorld[num]);
			Vec3 point2 = ropeGlobalFrame.TransformToLocalNonOrthogonal(in _posWorld[num + 1]);
			Vec3 point3 = ropeGlobalFrame.TransformToLocalNonOrthogonal(in _posWorld[num + 2]);
			Vec3 point4 = ropeGlobalFrame.TransformToLocalNonOrthogonal(in _posWorld[num + 3]);
			candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point);
			candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point2);
			candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point3);
			candidateLocalBoundingBox.RelaxMinMaxWithPoint(in point4);
			MatrixFrame frame2 = new MatrixFrame(new Mat3(in point, in point2, in point3), in point4);
			_ropeMesh.SetAdditionalBoneFrame(i + 1, in frame2);
		}
		if (!skipBoundingBox)
		{
			UpdateRopeLocalBoundingBox(in candidateLocalBoundingBox);
		}
	}

	private void WriteStraightBake(in Vec3 sourceLocalPos, in Vec3 endLocalPos)
	{
		MatrixFrame frame = new MatrixFrame(new Mat3(in sourceLocalPos, new Vec3(2f), in endLocalPos), in endLocalPos);
		_ropeMesh.SetAdditionalBoneFrame(0, in frame);
		MatrixFrame frame2 = new MatrixFrame(new Mat3(in sourceLocalPos, in endLocalPos, in Vec3.Zero), in Vec3.Zero);
		_ropeMesh.SetAdditionalBoneFrame(1, in frame2);
	}

	private void InitializeChainToStraightLine(in Vec3 sourceGlobalPosition, in Vec3 endGlobalPosition)
	{
		_verletRestLength = _targetRestLength;
		for (int i = 0; i < 12; i++)
		{
			Vec3 vec = Vec3.Lerp(sourceGlobalPosition, endGlobalPosition, PointParam[i]);
			_posWorld[i] = vec;
			_prevPosWorld[i] = vec;
		}
	}

	private RopeLod ComputeLod(in Vec3 sourceLocal, in Vec3 endLocal)
	{
		Scene scene = Mission.Current?.Scene;
		if (scene == null || !base.GameEntity.IsVisibleIncludeParents())
		{
			return RopeLod.Culled;
		}
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 vec = globalFrame.TransformToParent(in sourceLocal);
		Vec3 vec2 = globalFrame.TransformToParent(in endLocal);
		Vec3 vec3 = (vec + vec2) * 0.5f;
		MatrixFrame lastFinalRenderCameraFrame = scene.LastFinalRenderCameraFrame;
		float num = vec3.Distance(lastFinalRenderCameraFrame.origin);
		if (num > 300f)
		{
			return RopeLod.Culled;
		}
		Vec3 vec4 = vec3 - lastFinalRenderCameraFrame.origin;
		float length = vec4.Length;
		if (length > 0.001f)
		{
			Vec3 v = -lastFinalRenderCameraFrame.rotation.u;
			float num2 = Vec3.DotProduct(vec4 * (1f / length), v);
			float num3 = vec.Distance(vec2) * 0.6f + 2f;
			float num4 = TaleWorlds.Library.MathF.Min(1f, num3 / TaleWorlds.Library.MathF.Max(length, 0.1f));
			float num5 = TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Max(1f - num4 * num4, 0f));
			float num6 = TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Max(0.8775f, 0f));
			float num7 = 0.35f * num5 - num6 * num4;
			if (num2 < num7)
			{
				return RopeLod.Culled;
			}
		}
		if (num < 30f)
		{
			return RopeLod.Near;
		}
		if (num < 100f)
		{
			return RopeLod.Medium;
		}
		return RopeLod.Far;
	}

	private static Vec3 ComputeEndFromTarget(in Vec3 sourceGlobalPosition, in Vec3 targetGlobalPosition)
	{
		Vec3 vec = targetGlobalPosition - sourceGlobalPosition;
		float length = vec.Length;
		if (length < 0.0001f)
		{
			return targetGlobalPosition;
		}
		return targetGlobalPosition - vec * (0.5f / length);
	}

	private static Vec3 ProjectileSample(in Vec3 globalVelocity, in Vec3 sourceGlobalPosition, float time, float fraction)
	{
		float num = time * MBMath.ClampFloat(fraction, 0f, 1f);
		return sourceGlobalPosition + globalVelocity * num + 0.5f * MBGlobals.GravitationalAcceleration * num * num;
	}

	private void UpdateRopeLocalBoundingBox(in BoundingBox candidateLocalBoundingBox)
	{
		BoundingBox boundingBox = base.GameEntity.GetLocalBoundingBox();
		if (BoundingBox.ArrangeWithAnotherBoundingBox(ref boundingBox, candidateLocalBoundingBox, 10f))
		{
			_localUpdatedBoundingBox = boundingBox;
			base.GameEntity.SetBoundingboxDirty();
			base.GameEntity.Root.GetFirstScriptOfType<MissionShip>()?.InvalidateLocalBoundingBoxCache();
		}
	}

	public void SetRopeBoundingBoxToInitialState()
	{
		base.GameEntity.SetManualLocalBoundingBox(in _ropePileBaseBoundingBox);
		WeakGameEntity parent = base.GameEntity.Parent;
		if (parent.IsValid)
		{
			parent.SetBoundingboxDirty();
		}
	}
}

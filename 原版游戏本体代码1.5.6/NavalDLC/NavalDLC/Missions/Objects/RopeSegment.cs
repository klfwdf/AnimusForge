using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "rope_segment")]
internal class RopeSegment : ScriptComponentBehavior
{
	private const int BridgeCurveLinearSampleCount = 8;

	private const string PhysicsEntityTag = "rope_physics_body";

	private static readonly Comparer<KeyValuePair<float, Vec3>> _cacheCompareDelegate = Comparer<KeyValuePair<float, Vec3>>.Create((KeyValuePair<float, Vec3> x, KeyValuePair<float, Vec3> y) => x.Key.CompareTo(y.Key));

	private static float[] _physicsCheckPoints = new float[3] { 0.05f, 0.5f, 0.93f };

	[EditableScriptComponentVariable(true, "Segment Index")]
	private int _segmentIndex;

	[EditableScriptComponentVariable(true, "Is Fixed")]
	private bool _isFixed;

	[EditableScriptComponentVariable(true, "Loose Amount")]
	private float _looseAmount = 0.1f;

	[EditableScriptComponentVariable(true, "Default Rope Length")]
	private float _defaultRopeLength = 25.9f;

	[EditableScriptComponentVariable(true, "Uses Physics Body")]
	private bool _usesPhysicsBody;

	[EditableScriptComponentVariable(true, "Swing Multiplier")]
	private float _swingMultiplier = 1f;

	private KeyValuePair<float, Vec3>[] _bridgeCurveLinearAccessCache = new KeyValuePair<float, Vec3>[8];

	private bool _firstTick = true;

	private Vec3 _previousPosition = Vec3.Zero;

	private Vec3 _previousVelocity = Vec3.Zero;

	private MatrixFrame _prevParentFrame = MatrixFrame.Identity;

	private float _pendulumVelocity;

	private float _pendulumCurrentRotation;

	private int _tickRemainingForPhysics = 30;

	private GameEntity _endEntity;

	private GameEntity _physicsEntity;

	private Mesh _ropeMesh;

	private bool _externalEndEntitySet;

	private float _cumulativeTime;

	private MatrixFrame _currentFrameSwingFrame = MatrixFrame.Identity;

	private Vec3 _previousChangeDueToShip = Vec3.Zero;

	private List<RopeSegmentCosmetics> _ropeSegmentCosmetics = new List<RopeSegmentCosmetics>();

	private bool _dynamicMode;

	private List<float> _ropeSegmentCosmeticsDxCached = new List<float>();

	public float RuntimeLooseMultiplier { get; private set; }

	public bool UseDistanceAsRopeLength { get; private set; }

	public float BurnedClipFactor { get; set; }

	public bool BurnedClipReverseMode { get; set; }

	public Mesh RopeMesh
	{
		get
		{
			return _ropeMesh;
		}
		private set
		{
			_ropeMesh = value;
		}
	}

	public float CurrentRopeLength { get; private set; }

	public bool LinearMode { get; private set; }

	public float LooseAmount
	{
		get
		{
			return _looseAmount;
		}
		private set
		{
			_looseAmount = value;
		}
	}

	public bool IsFixed
	{
		get
		{
			return _isFixed;
		}
		private set
		{
			_isFixed = value;
		}
	}

	public int SegmentIndex
	{
		get
		{
			return _segmentIndex;
		}
		private set
		{
			_segmentIndex = value;
		}
	}

	public float DefaultRopeLength
	{
		get
		{
			return _defaultRopeLength;
		}
		private set
		{
			_defaultRopeLength = value;
		}
	}

	public WeakGameEntity EndEntity
	{
		get
		{
			return _endEntity.WeakEntity;
		}
		private set
		{
			_endEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(value);
			_externalEndEntitySet = value != null;
		}
	}

	private RopeSegment()
	{
		RuntimeLooseMultiplier = 1f;
		CurrentRopeLength = 12.95f;
		UseDistanceAsRopeLength = false;
		LinearMode = false;
		BurnedClipFactor = 0f;
		BurnedClipReverseMode = false;
	}

	protected override void OnEditorInit()
	{
		FetchEntities();
		if (_usesPhysicsBody)
		{
			_physicsEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.Root.GetFirstChildEntityWithTagRecursive("rope_physics_body"));
		}
	}

	protected override void OnEditorTick(float dt)
	{
		_physicsCheckPoints[0] = 0.15f;
		_physicsCheckPoints[1] = 0.5f;
		_physicsCheckPoints[2] = 0.85f;
		FetchEntities();
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			TickAux(dt);
		}
		else
		{
			_firstTick = true;
		}
	}

	protected override void OnInit()
	{
		FetchEntities();
		if (_usesPhysicsBody)
		{
			_physicsEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.Root.GetFirstChildEntityWithTagRecursive("rope_physics_body"));
		}
	}

	protected override void OnTickParallel3(float dt)
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			TickAux(dt);
		}
		else
		{
			_firstTick = true;
		}
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
		if (variableName == "Default Rope Length")
		{
			CurrentRopeLength = _defaultRopeLength * 0.5f;
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		_endEntity = null;
		_physicsEntity = null;
		_ropeMesh = null;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel3;
	}

	private void FetchEntities()
	{
		_ropeSegmentCosmetics.Clear();
		_physicsEntity = null;
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			RopeSegmentCosmetics firstScriptOfType = child.GetFirstScriptOfType<RopeSegmentCosmetics>();
			if (firstScriptOfType != null)
			{
				_ropeSegmentCosmetics.Add(firstScriptOfType);
				firstScriptOfType.GameEntity.SetDoNotCheckVisibility(value: true);
			}
		}
		if (base.GameEntity.Parent != null && !_externalEndEntitySet)
		{
			_endEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(base.GameEntity.Parent.GetFirstChildEntityWithTag("simple_rope_end"));
		}
		_ropeMesh = base.GameEntity.GetFirstMesh();
		if (_ropeMesh != null)
		{
			_ropeMesh.SetupAdditionalBoneBuffer(2);
		}
		base.GameEntity.SetBoundingboxDirty();
		base.GameEntity.SetDoNotCheckVisibility(value: true);
		if (_ropeMesh != null)
		{
			_ropeMesh.SetAdditionalBoneFrame(1, MatrixFrame.Identity);
		}
		if (!(_ropeMesh != null) || !(_endEntity != null) || _ropeSegmentCosmetics.Count <= 0)
		{
			return;
		}
		_ropeSegmentCosmeticsDxCached.Clear();
		Vec3 plankTargetOrigin = new Vec3(0f, 0f, 0f, -1f);
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		MatrixFrame globalFrame2 = _endEntity.GetGlobalFrame();
		Vec3 plankSourceOrigin = globalFrame.TransformToLocalNonOrthogonal(in globalFrame2.origin);
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		float curvedLength = vectorArgument.x * vectorArgument.z;
		FillBridgeCurveAccessData(in plankTargetOrigin, in plankSourceOrigin, in curvedLength);
		foreach (RopeSegmentCosmetics ropeSegmentCosmetic in _ropeSegmentCosmetics)
		{
			float currentLength = TaleWorlds.Library.MathF.Clamp(ropeSegmentCosmetic.RopeLocalPosition, 0f, 1f) * curvedLength;
			_ropeSegmentCosmeticsDxCached.Add(GetCurveDxFromDt(plankTargetOrigin, currentLength));
		}
	}

	private void TickAux(float dt)
	{
		if (!(_endEntity == null) && !(_ropeMesh == null))
		{
			_cumulativeTime += dt;
			Vec3 vec = new Vec3(0f, 0f, 0f, -1f);
			MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
			MatrixFrame globalFrame2 = _endEntity.GetGlobalFrame();
			Vec3 vec2 = globalFrame.TransformToLocalNonOrthogonal(in globalFrame2.origin);
			SetRopeShaderParams(vec, vec2);
			TickSwingPhysics(dt, vec, vec2);
			TickCosmetics(vec, vec2);
		}
	}

	private void SetRopeShaderParams(Vec3 startPosition, Vec3 endPosition)
	{
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.s = startPosition;
		frame.origin = endPosition;
		float num = (endPosition - startPosition).Normalize();
		_ropeMesh.SetAdditionalBoneFrame(0, in frame);
		float num2 = 0f;
		if (!LinearMode)
		{
			num2 = _looseAmount;
		}
		float x = base.GameEntity.GetGlobalFrame().rotation.GetScaleVector().x;
		num2 = num2 * RuntimeLooseMultiplier * x;
		num2 = TaleWorlds.Library.MathF.Max(0.005f, num2);
		float w = _ropeMesh.GetVectorArgument().w;
		if (_isFixed || UseDistanceAsRopeLength)
		{
			_ropeMesh.SetVectorArgument(num + num2, 25.9f, 1f, w);
			return;
		}
		float num3 = num + num2;
		float num4 = _defaultRopeLength - num3;
		float vectorArgument = 1f - num4 / _defaultRopeLength;
		_ropeMesh.SetVectorArgument(num3, 25.9f, vectorArgument, w);
	}

	private float GetCurveDxFromDt(Vec3 startPosition, float currentLength)
	{
		int num = Array.BinarySearch(_bridgeCurveLinearAccessCache, new KeyValuePair<float, Vec3>(currentLength, Vec3.Zero), _cacheCompareDelegate);
		float num2 = 1f / 7f;
		if (num >= 0)
		{
			return (float)num * num2;
		}
		int num3 = ~num;
		int num4 = num3 - 1;
		KeyValuePair<float, Vec3> keyValuePair = _bridgeCurveLinearAccessCache[num4];
		KeyValuePair<float, Vec3> keyValuePair2 = _bridgeCurveLinearAccessCache[num3];
		return ((currentLength - keyValuePair.Key) / (keyValuePair2.Key - keyValuePair.Key) + (float)num4) * num2;
	}

	private Vec3 GetCurvePositionFromLength(Vec3 startPosition, float currentLength)
	{
		int num = Array.BinarySearch(_bridgeCurveLinearAccessCache, new KeyValuePair<float, Vec3>(currentLength, Vec3.Zero), _cacheCompareDelegate);
		if (num >= 0)
		{
			return _bridgeCurveLinearAccessCache[num].Value;
		}
		int num2 = ~num;
		int num3 = num2 - 1;
		KeyValuePair<float, Vec3> keyValuePair = _bridgeCurveLinearAccessCache[num3];
		KeyValuePair<float, Vec3> keyValuePair2 = _bridgeCurveLinearAccessCache[num2];
		float alpha = (currentLength - keyValuePair.Key) / (keyValuePair2.Key - keyValuePair.Key);
		Vec3 vec = Vec3.Lerp(keyValuePair.Value, keyValuePair2.Value, alpha);
		if (!LinearMode)
		{
			vec = _currentFrameSwingFrame.TransformToLocal(vec - startPosition) + startPosition;
		}
		return vec;
	}

	private void FillBridgeCurveAccessData(in Vec3 plankTargetOrigin, in Vec3 plankSourceOrigin, in float curvedLength)
	{
		_bridgeCurveLinearAccessCache[0] = new KeyValuePair<float, Vec3>(0f, plankTargetOrigin);
		Vec3 v = plankTargetOrigin;
		float num = 1f / 7f;
		float num2 = 0f;
		for (int i = 1; i < 7; i++)
		{
			Vec3 vec = CalculateAutoCurvePosition(plankTargetOrigin, plankSourceOrigin, curvedLength, (float)i * num);
			float num3 = vec.Distance(v);
			num2 += num3;
			_bridgeCurveLinearAccessCache[i] = new KeyValuePair<float, Vec3>(num2, vec);
			v = vec;
		}
		_bridgeCurveLinearAccessCache[7] = new KeyValuePair<float, Vec3>(curvedLength, plankSourceOrigin);
	}

	private void TickCosmetics(Vec3 startPoint, Vec3 endPoint)
	{
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		float curvedLength = vectorArgument.x * vectorArgument.z;
		if (_ropeSegmentCosmetics.Count > 0 && !LinearMode && _dynamicMode)
		{
			FillBridgeCurveAccessData(in startPoint, in endPoint, in curvedLength);
		}
		for (int i = 0; i < _ropeSegmentCosmetics.Count; i++)
		{
			RopeSegmentCosmetics ropeSegmentCosmetics = _ropeSegmentCosmetics[i];
			WeakGameEntity gameEntity = ropeSegmentCosmetics.GameEntity;
			MatrixFrame frame = gameEntity.GetGlobalFrame();
			Vec3 zero = Vec3.Zero;
			if (LinearMode)
			{
				zero = Vec3.Lerp(startPoint, endPoint, TaleWorlds.Library.MathF.Clamp(ropeSegmentCosmetics.RopeLocalPosition, 0f, 1f));
				if (ropeSegmentCosmetics.IsBurningNode)
				{
					Vec3 v = endPoint - startPoint;
					v = base.GameEntity.GetGlobalFrame().rotation.TransformToParent(in v);
					if ((double)v.LengthSquared > 0.0001)
					{
						v.Normalize();
						frame.rotation.s = v;
						frame.rotation.f = -frame.rotation.s.CrossProductWithUp();
						frame.rotation.f.Normalize();
						frame.rotation.u = Vec3.CrossProduct(frame.rotation.s, frame.rotation.f);
					}
				}
			}
			else
			{
				float num = TaleWorlds.Library.MathF.Clamp(ropeSegmentCosmetics.RopeLocalPosition, 0f, 1f) * curvedLength;
				if (_dynamicMode)
				{
					zero = GetCurvePositionFromLength(startPoint, num);
				}
				else
				{
					float dx = _ropeSegmentCosmeticsDxCached[i];
					zero = CalculateAutoCurvePosition(startPoint, endPoint, curvedLength, dx);
					zero = _currentFrameSwingFrame.TransformToLocal(zero - startPoint) + startPoint;
				}
				if (ropeSegmentCosmetics.IsBurningNode)
				{
					Vec3 v2 = GetCurvePositionFromLength(startPoint, TaleWorlds.Library.MathF.Min(num + 0.1f, curvedLength)) - zero;
					v2 = base.GameEntity.GetGlobalFrame().rotation.TransformToParent(in v2);
					if ((double)v2.LengthSquared > 1E-06)
					{
						v2.Normalize();
						frame.rotation.s = v2;
						frame.rotation.f = -frame.rotation.s.CrossProductWithUp();
						frame.rotation.f.Normalize();
						frame.rotation.u = Vec3.CrossProduct(frame.rotation.s, frame.rotation.f);
					}
				}
			}
			frame.origin = base.GameEntity.GetGlobalFrame().TransformToParent(in zero);
			gameEntity.SetGlobalFrame(in frame);
		}
	}

	private bool CheckPhysicsEntity(in Vec3 startPosition, in Vec3 endPosition, float currentRotation, float nextRotation, float ropeLength)
	{
		Vec3 v = endPosition - startPosition;
		v.Normalize();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.rotation.RotateAboutAnArbitraryVector(in v, currentRotation);
		MatrixFrame identity2 = MatrixFrame.Identity;
		identity2.rotation.RotateAboutAnArbitraryVector(in v, nextRotation);
		float[] physicsCheckPoints = _physicsCheckPoints;
		foreach (float dx in physicsCheckPoints)
		{
			Vec3 vec = CalculateAutoCurvePosition(startPosition, endPosition, ropeLength, dx);
			Vec3 vec2 = vec;
			vec = identity.TransformToParent(vec - startPosition) + startPosition;
			vec2 = identity2.TransformToParent(vec2 - startPosition) + startPosition;
			Vec3 rayDirection = vec2 - vec;
			float num = rayDirection.Normalize();
			if (!(num < 0.0001f))
			{
				num += 0.02f;
				float resultLength = 0f;
				if (_physicsEntity.RayHitEntity(base.GameEntity.GetGlobalFrame().TransformToParent(in vec), rayDirection, num, ref resultLength) && num > resultLength)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void TickSwingPhysics(float dt, Vec3 startPoint, Vec3 endPoint)
	{
		if (_ropeMesh == null || _endEntity == null || (double)_looseAmount < 1E-07 || dt == 0f)
		{
			_currentFrameSwingFrame = MatrixFrame.Identity;
			return;
		}
		if (_tickRemainingForPhysics > 0)
		{
			_tickRemainingForPhysics--;
			return;
		}
		WeakGameEntity parent = base.GameEntity.Parent;
		if (parent != null && dt > 0f && !LinearMode)
		{
			MatrixFrame frame = parent.Root.GetGlobalFrame();
			Vec3 vec = endPoint - startPoint;
			Vec3 v = base.GameEntity.GetLocalFrame().TransformToParent(in startPoint);
			vec.Normalize();
			if ((double)vec.Length < 1E-09)
			{
				return;
			}
			Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
			float num = vectorArgument.x * vectorArgument.z;
			bool firstTick = _firstTick;
			if (_firstTick)
			{
				Vec3 previousPosition = v;
				_previousPosition = previousPosition;
				_prevParentFrame = frame;
				_firstTick = false;
			}
			Vec3 v2 = vec.CrossProductWithUp();
			v2.Normalize();
			if (false)
			{
				Vec3 vec2 = _prevParentFrame.TransformToLocalNonOrthogonal(in frame).TransformToParent(in v) - v;
				if (firstTick)
				{
					_previousChangeDueToShip = vec2;
				}
				Vec3 vec3 = (vec2 - _previousChangeDueToShip) * 0.0003f / dt;
				_previousChangeDueToShip = vec2;
				float value = vec3.Normalize();
				value = TaleWorlds.Library.MathF.Clamp(value, 0f, 1f);
				float num2 = Vec3.DotProduct(-vec3, v2);
				if (TaleWorlds.Library.MathF.IsValidValue(num2))
				{
					if (TaleWorlds.Library.MathF.Abs(num2) > 0f)
					{
						float num3 = TaleWorlds.Library.MathF.Sign(num2);
						num2 = TaleWorlds.Library.MathF.Max((TaleWorlds.Library.MathF.Abs(num2) - 0.6f) * 2.5f, 0f) * num3 * value;
					}
					_pendulumVelocity -= num2 * _swingMultiplier * 0.25f;
				}
			}
			if (_pendulumCurrentRotation > 0f)
			{
				float num4 = MBMath.SmoothStep(0f, 0.1f, _pendulumCurrentRotation);
				_pendulumVelocity -= dt * 2f * num4 * 1.027f * 0.3f;
			}
			else
			{
				float num5 = MBMath.SmoothStep(0f, -0.1f, _pendulumCurrentRotation);
				_pendulumVelocity += dt * 2f * num5 * 1.027f * 0.3f;
			}
			float num6 = TaleWorlds.Library.MathF.Lerp(1f, 0.5f, dt * 4f);
			_pendulumVelocity *= num6;
			new Vec3(TaleWorlds.Library.MathF.Pow(TaleWorlds.Library.MathF.Cos(startPoint.x * 0.5f + _cumulativeTime * 0.45f), 10f), TaleWorlds.Library.MathF.Pow(TaleWorlds.Library.MathF.Cos(startPoint.y * 1.2f + _cumulativeTime * 0.65f), 10f), TaleWorlds.Library.MathF.Pow(TaleWorlds.Library.MathF.Cos(startPoint.z * 3.5f + _cumulativeTime * 0.35f), 10f)).Normalize();
			float a = TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Cos(startPoint.x * 0.5f + _cumulativeTime * 2.5f) - 0.95f, 0f, 1f) * 4.5f;
			a = TaleWorlds.Library.MathF.Max(a, 0f);
			float a2 = TaleWorlds.Library.MathF.Clamp(TaleWorlds.Library.MathF.Cos(startPoint.y * 0.9f + _cumulativeTime * 2.5f) - 0.95f, 0f, 1f) * 4.9f;
			a2 = TaleWorlds.Library.MathF.Max(a2, 0f);
			float num7 = 1f + TaleWorlds.Library.MathF.Cos(startPoint.z * 0.3f + _cumulativeTime * 0.345f);
			float num8 = TaleWorlds.Library.MathF.Min(base.GameEntity.GetGlobalWindStrengthVectorOfScene().Length, 5f) * TaleWorlds.Library.MathF.Max(a, a2) * num7 * dt / TaleWorlds.Library.MathF.Max(1f, num);
			_pendulumVelocity += num8 * 6.8f * _swingMultiplier;
			float num9 = _pendulumVelocity * dt * 50f;
			if (_physicsEntity != null && !CheckPhysicsEntity(in startPoint, in endPoint, _pendulumCurrentRotation, _pendulumCurrentRotation + num9, num))
			{
				_pendulumVelocity *= -0.95f;
				num9 *= -1.25f;
			}
			float num10 = TaleWorlds.Library.MathF.Sign(_pendulumVelocity);
			float a3 = TaleWorlds.Library.MathF.Abs(_pendulumVelocity);
			a3 = TaleWorlds.Library.MathF.Min(a3, 0.06f);
			_pendulumVelocity = a3 * num10;
			_pendulumCurrentRotation += num9;
			if (!TaleWorlds.Library.MathF.IsValidValue(_pendulumCurrentRotation))
			{
				_pendulumCurrentRotation = 0f;
			}
			if (!TaleWorlds.Library.MathF.IsValidValue(_pendulumVelocity))
			{
				_pendulumVelocity = 0f;
			}
			while (true)
			{
				if (_pendulumCurrentRotation > System.MathF.PI)
				{
					_pendulumCurrentRotation -= System.MathF.PI * 2f;
					continue;
				}
				if (!(_pendulumCurrentRotation < -System.MathF.PI))
				{
					break;
				}
				_pendulumCurrentRotation += System.MathF.PI * 2f;
			}
			_previousVelocity = (startPoint - _previousPosition) / dt;
			_previousPosition = startPoint;
			_prevParentFrame = frame;
			Vec3 v3 = startPoint - endPoint;
			v3.Normalize();
			_currentFrameSwingFrame = MatrixFrame.Identity;
			_currentFrameSwingFrame.rotation.RotateAboutAnArbitraryVector(in v3, _pendulumCurrentRotation);
		}
		else
		{
			_currentFrameSwingFrame = MatrixFrame.Identity;
		}
		_ropeMesh.SetAdditionalBoneFrame(1, in _currentFrameSwingFrame);
	}

	public void ShiftRope(float meters)
	{
		Vec3 vectorArgument = _ropeMesh.GetVectorArgument();
		float num = vectorArgument.z * vectorArgument.x;
		if (!(num > 0f))
		{
			return;
		}
		float num2 = meters / num;
		foreach (RopeSegmentCosmetics ropeSegmentCosmetic in _ropeSegmentCosmetics)
		{
			ropeSegmentCosmetic.RopeLocalPosition += num2;
		}
	}

	public void ApplyBoundingBox(MatrixFrame parentFrame, ref BoundingBox bb)
	{
		MatrixFrame globalFrame = base.GameEntity.GetGlobalFrame();
		Vec3 vec = parentFrame.TransformToLocalNonOrthogonal(in globalFrame.origin);
		bb.RelaxMinMaxWithPoint(vec + Vec3.One * 0.25f);
		bb.RelaxMinMaxWithPoint(vec - Vec3.One * 0.25f);
		if (_endEntity != null)
		{
			globalFrame = _endEntity.GetGlobalFrame();
			Vec3 vec2 = parentFrame.TransformToLocalNonOrthogonal(in globalFrame.origin);
			bb.RelaxMinMaxWithPoint(vec2 + Vec3.One * 0.25f);
			bb.RelaxMinMaxWithPoint(vec2 - Vec3.One * 0.25f);
		}
	}

	public void SetUseDistanceAsRopeLength()
	{
		UseDistanceAsRopeLength = true;
	}

	public void SetEndEntity(WeakGameEntity entity)
	{
		_endEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(entity);
		_externalEndEntitySet = entity != null;
	}

	public void SetAsFixedEntity()
	{
		_isFixed = true;
	}

	public void AddRope(float value)
	{
		CurrentRopeLength += value;
	}

	public void SetLinearMode(bool value)
	{
		LinearMode = value;
	}

	public void SetRuntimeLooseMultiplier(float value)
	{
		RuntimeLooseMultiplier = value;
	}

	public void FillBurningRecordForSegment(BurningSystem system, string prefabName, float nodeLength, bool reversePlacement)
	{
		float num = base.GameEntity.GetGlobalFrame().origin.Distance(_endEntity.GetGlobalFrame().origin);
		int num2 = (int)(num / nodeLength);
		float num3 = nodeLength / (num * 2f);
		for (int i = 0; i < num2; i++)
		{
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(base.GameEntity.Scene, prefabName, callScriptCallbacks: true);
			if (!(gameEntity == null))
			{
				base.GameEntity.AddChild(gameEntity.WeakEntity);
				BurningNode firstScriptOfType = gameEntity.GetFirstScriptOfType<BurningNode>();
				if (firstScriptOfType != null)
				{
					system.AddNewNode(firstScriptOfType);
				}
				if (MBRandom.RandomFloat > 0.82f)
				{
					firstScriptOfType.EnableSparks();
				}
				gameEntity.CreateAndAddScriptComponent("rope_segment_cosmetics", callScriptCallbacks: true);
				RopeSegmentCosmetics firstScriptOfType2 = gameEntity.GetFirstScriptOfType<RopeSegmentCosmetics>();
				firstScriptOfType2.RopeLocalPosition = num3 + (float)i * nodeLength / num;
				_ropeSegmentCosmetics.Add(firstScriptOfType2);
				if (reversePlacement)
				{
					firstScriptOfType2.RopeLocalPosition = 1f - firstScriptOfType2.RopeLocalPosition;
				}
			}
		}
		_dynamicMode = true;
	}

	public bool DeregisterRopeSegmentCosmetics(RopeSegmentCosmetics cosmetics)
	{
		if (_ropeSegmentCosmetics.IndexOf(cosmetics) != -1)
		{
			_ropeSegmentCosmetics.Remove(cosmetics);
			return true;
		}
		return false;
	}

	public void SetAsDynamic()
	{
		_dynamicMode = true;
	}

	public void SetAlpha(float value)
	{
		if (_ropeMesh != null)
		{
			if (value <= 0f)
			{
				base.GameEntity.SetVisibilityExcludeParents(visible: false);
				return;
			}
			base.GameEntity.SetVisibilityExcludeParents(visible: true);
			_ropeMesh.SetColorAlpha((uint)(TaleWorlds.Library.MathF.Clamp(value, 0f, 1f) * 255f));
		}
	}

	public static Vec3 CalculateAutoCurvePosition(Vec3 startPos, Vec3 endPos, float ropeLength, float dx)
	{
		Vec2 vec = startPos.AsVec2 - endPos.AsVec2;
		float num = TaleWorlds.Library.MathF.Clamp((vec.Length - 0.4f) / 0.2f, 0f, 1f);
		Vec3 vec2 = Vec3.Lerp(startPos, endPos, dx);
		if (num < 1E-06f)
		{
			return vec2;
		}
		if (startPos.z > endPos.z)
		{
			Vec3 vec3 = startPos;
			startPos = endPos;
			endPos = vec3;
			dx = 1f - dx;
			vec *= -1f;
		}
		ropeLength = TaleWorlds.Library.MathF.Max(ropeLength, vec.Length);
		float length = vec.Length;
		float num2 = (startPos.z - endPos.z) / length;
		ropeLength /= length;
		float num3 = TaleWorlds.Library.MathF.Sqrt(ropeLength * ropeLength - num2 * num2);
		float num4 = 1f;
		for (int i = 0; i < 10; i++)
		{
			float num5 = num4;
			float num6 = (float)Math.Sinh(num5);
			float num7 = (float)Math.Cosh(num5);
			float num8 = num5 - (num3 - num6 / num5) / (num6 / (num5 * num5) - num7 / num5);
			if (!TaleWorlds.Library.MathF.IsValidValue(num8))
			{
				break;
			}
			num4 = num8;
		}
		float num9 = 1f / (2f * num4);
		float num10 = (1f - TaleWorlds.Library.MathF.Log((ropeLength - num2) / (ropeLength + num2)) * num9) * 0.5f;
		float num11 = (0f - Math.Abs(num2)) * 0.5f - ropeLength * 0.5f * (1f / (float)Math.Tanh(num4));
		float num12 = num9 * (float)Math.Cosh((dx - num10) / num9) + num11;
		Vec3 v = Vec3.Lerp(startPos, endPos, dx);
		v.z = endPos.z + num12 * length;
		if (!v.IsValid)
		{
			return vec2;
		}
		return Vec3.Lerp(vec2, v, num);
	}
}

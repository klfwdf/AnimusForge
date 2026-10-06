using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "pulley_system")]
internal class PulleySystem : ScriptComponentBehavior
{
	private struct SegmentData
	{
		internal RopeSegment RopeSegment;

		internal GameEntity RopeEntity;
	}

	private const string PulleyTag = "pulley";

	private const string PulleyWheelTag = "pulley_wheel";

	private const string PulleyLeftPointTag = "pulley_left_point";

	private const string PulleyRightPointTag = "pulley_right_point";

	private const string EndPointRopeTag = "end_point_rope";

	private const string EndPointTargetTag = "end_point_target";

	private const string AttachedToYardTag = "attached_to_yard";

	private const string FreePileTag = "free_pile";

	[EditableScriptComponentVariable(true, "End Rope Length")]
	private float _endRopeLength = 2f;

	private GameEntity _pulleyEntity;

	private GameEntity _pulleyWheelEntity;

	private GameEntity _pulleyLeftRopeConnectionEntity;

	private GameEntity _pulleyRightRopeConnectionEntity;

	private List<RopeSegment> _tiedToYardSegments = new List<RopeSegment>();

	private List<SegmentData> _fixedSegments = new List<SegmentData>();

	private List<SegmentData> _freeSegments = new List<SegmentData>();

	private SegmentData _endPointRope;

	private GameEntity _endTargetEntity;

	private Vec3 _targetPositionLocalPrevFrame = Vec3.Zero;

	private float _endRopeConnectionOffset;

	private float _looseAmountMultiplier;

	private bool _firstTick = true;

	public WeakGameEntity FirstFixedEntity
	{
		get
		{
			if (_fixedSegments.Count > 0)
			{
				return _fixedSegments[0].RopeEntity.WeakEntity;
			}
			return WeakGameEntity.Invalid;
		}
	}

	public List<RopeSegment> TiedToYardSegments => _tiedToYardSegments;

	private PulleySystem()
	{
	}

	protected override void OnEditorInit()
	{
		FetchEntities();
	}

	protected override void OnEditorTick(float dt)
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			FetchEntities();
			TickAux();
		}
	}

	protected override void OnInit()
	{
		FetchEntities();
	}

	protected override void OnTickParallel2(float dt)
	{
		if (base.GameEntity.IsVisibleIncludeParents())
		{
			TickAux();
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		base.OnRemoved(removeReason);
		_pulleyEntity = null;
		_pulleyWheelEntity = null;
		_pulleyLeftRopeConnectionEntity = null;
		_pulleyRightRopeConnectionEntity = null;
		_tiedToYardSegments.Clear();
		_freeSegments.Clear();
		_fixedSegments.Clear();
		_endPointRope.RopeEntity = null;
		_endPointRope.RopeSegment = null;
		_endTargetEntity = null;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel2;
	}

	private void FetchEntities()
	{
		_tiedToYardSegments.Clear();
		FetchRopeSegmentsForSide(base.GameEntity, isFixed: true, ref _fixedSegments);
		FetchRopeSegmentsForSide(base.GameEntity, isFixed: false, ref _freeSegments);
		foreach (SegmentData freeSegment in _freeSegments)
		{
			freeSegment.RopeSegment.SetUseDistanceAsRopeLength();
			freeSegment.RopeSegment.SetAsDynamic();
		}
		foreach (SegmentData fixedSegment in _fixedSegments)
		{
			fixedSegment.RopeSegment.SetAsDynamic();
		}
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		base.GameEntity.GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			if (item.HasTag("pulley"))
			{
				_pulleyEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
			}
			else if (item.HasTag("end_point_rope"))
			{
				_endPointRope.RopeEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
				_endPointRope.RopeSegment = _endPointRope.RopeEntity.GetFirstScriptOfType<RopeSegment>();
			}
			else if (item.HasTag("end_point_target"))
			{
				_endTargetEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(item);
			}
		}
		if (_pulleyEntity != null)
		{
			_pulleyRightRopeConnectionEntity = _pulleyEntity.GetFirstChildEntityWithTag("pulley_right_point");
			_pulleyLeftRopeConnectionEntity = _pulleyEntity.GetFirstChildEntityWithTag("pulley_left_point");
			_pulleyWheelEntity = _pulleyEntity.GetFirstChildEntityWithTag("pulley_wheel");
			Mesh firstMesh = _pulleyEntity.GetFirstMesh();
			if (firstMesh != null)
			{
				_endRopeConnectionOffset = (firstMesh.GetBoundingBoxMax() - firstMesh.GetBoundingBoxMin()).z;
			}
		}
		if (_freeSegments.Count > 0)
		{
			int count = _freeSegments.Count;
			for (int i = 0; i < count - 1; i++)
			{
				_freeSegments[i].RopeSegment.SetEndEntity(_freeSegments[i + 1].RopeEntity.WeakEntity);
			}
			_freeSegments[count - 1].RopeSegment.SetEndEntity(_pulleyLeftRopeConnectionEntity.WeakEntity);
		}
		if (_fixedSegments.Count > 0)
		{
			int count2 = _fixedSegments.Count;
			for (int j = 0; j < count2 - 1; j++)
			{
				_fixedSegments[j].RopeSegment.SetEndEntity(_fixedSegments[j + 1].RopeEntity.WeakEntity);
			}
			_fixedSegments[count2 - 1].RopeSegment.SetEndEntity(_pulleyRightRopeConnectionEntity.WeakEntity);
		}
		if (_endPointRope.RopeSegment != null)
		{
			_endPointRope.RopeSegment.SetEndEntity(_endTargetEntity.WeakEntity);
			_endPointRope.RopeSegment.SetAsFixedEntity();
			_endPointRope.RopeSegment.SetAsDynamic();
		}
		base.GameEntity.SetDoNotCheckVisibility(value: true);
	}

	private void TickAux()
	{
		if (!(_pulleyEntity == null) && _freeSegments.Count != 0 && _fixedSegments.Count != 0 && !(_pulleyLeftRopeConnectionEntity == null) && !(_pulleyRightRopeConnectionEntity == null) && !(_endTargetEntity == null))
		{
			Vec3 v = _endTargetEntity.GetGlobalFrame().origin;
			MatrixFrame globalFrame = base.GameEntity.Root.GetGlobalFrame();
			_ = Vec3.Zero;
			Vec3 v2 = (_freeSegments[_freeSegments.Count - 1].RopeEntity.GetGlobalFrame().origin + _fixedSegments[_fixedSegments.Count - 1].RopeEntity.GetGlobalFrame().origin) * 0.5f - v;
			v2.Normalize();
			MatrixFrame frame = _pulleyEntity.GetGlobalFrame();
			float x = frame.rotation.GetScaleVector().x;
			float num = _endRopeLength * x;
			frame.origin = v + v2 * num;
			_pulleyEntity.SetGlobalFrame(in frame);
			MatrixFrame identity = MatrixFrame.Identity;
			identity.rotation = globalFrame.rotation;
			Vec3 v3 = identity.TransformToLocalNonOrthogonal(in v2);
			Vec3 vec = globalFrame.TransformToLocalNonOrthogonal(in v);
			if (_firstTick)
			{
				_targetPositionLocalPrevFrame = vec;
				_firstTick = false;
			}
			Vec3 v4 = vec - _targetPositionLocalPrevFrame;
			float num2 = v4.Length;
			if (Vec3.DotProduct(v3, v4) < 0f)
			{
				num2 *= -1f;
			}
			float num3 = 0f;
			float num4 = 0f;
			for (int i = 0; i < _freeSegments.Count - 1; i++)
			{
				WeakGameEntity weakEntity = _freeSegments[i + 1].RopeEntity.WeakEntity;
				SetRopeParamsForSegment(weakEntity, _freeSegments[i], isFixed: true, num2 * 2f, moveUV: true, is_end_rope: false);
			}
			num3 += SetRopeParamsForSegment(_pulleyLeftRopeConnectionEntity.WeakEntity, _freeSegments[_freeSegments.Count - 1], isFixed: true, num2 * 2f, moveUV: true, is_end_rope: false);
			for (int j = 0; j < _fixedSegments.Count - 1; j++)
			{
				WeakGameEntity weakEntity2 = _fixedSegments[j + 1].RopeEntity.WeakEntity;
				SetRopeParamsForSegment(weakEntity2, _fixedSegments[j], isFixed: true, num2 * 2f, moveUV: false, is_end_rope: false);
			}
			num4 += SetRopeParamsForSegment(_pulleyLeftRopeConnectionEntity.WeakEntity, _fixedSegments[_fixedSegments.Count - 1], isFixed: true, num2 * 2f, moveUV: false, is_end_rope: false);
			ComputePulleyFrame(0f, (num3 + num4) * 0.5f);
			int num5 = 5;
			for (int k = 0; k < num5; k++)
			{
				SetRopeParamsForSegment(_pulleyLeftRopeConnectionEntity.WeakEntity, _freeSegments[_freeSegments.Count - 1], isFixed: true, 0f, moveUV: false, is_end_rope: false);
				SetRopeParamsForSegment(_pulleyRightRopeConnectionEntity.WeakEntity, _fixedSegments[_fixedSegments.Count - 1], isFixed: true, 0f, moveUV: false, is_end_rope: false);
			}
			if (_endPointRope.RopeEntity != null)
			{
				MatrixFrame globalFrame2 = _pulleyEntity.GetGlobalFrame();
				Vec3 origin = globalFrame2.origin;
				origin += (_endRopeConnectionOffset - 0.165f) * globalFrame2.rotation.u;
				MatrixFrame frame2 = _endPointRope.RopeEntity.GetGlobalFrame();
				frame2.origin = origin;
				_endPointRope.RopeEntity.SetGlobalFrame(in frame2);
				SetRopeParamsForSegment(_endTargetEntity.WeakEntity, _endPointRope, isFixed: true, 0f, moveUV: false, is_end_rope: true);
			}
			_targetPositionLocalPrevFrame = vec;
		}
	}

	private void ComputePulleyFrame(float move_amount, float total_rope_length)
	{
		Vec3 zero = Vec3.Zero;
		float num = 0f;
		int num2 = 0;
		float num3 = 0f;
		RopeSegment ropeSegment = _endPointRope.RopeSegment;
		Vec3 origin = _freeSegments[_freeSegments.Count - 1].RopeEntity.WeakEntity.GetGlobalFrame().origin;
		Vec3 origin2 = _freeSegments[_freeSegments.Count - 1].RopeEntity.WeakEntity.GetGlobalFrame().origin;
		zero += origin;
		RopeSegment ropeSegment2 = _freeSegments[_freeSegments.Count - 1].RopeSegment;
		if (ropeSegment2 != null)
		{
			num += MathF.Max(0.0005f, ropeSegment2.LooseAmount * _looseAmountMultiplier);
			num2++;
		}
		zero += origin2;
		RopeSegment ropeSegment3 = _fixedSegments[_fixedSegments.Count - 1].RopeSegment;
		if (ropeSegment3 != null)
		{
			num += MathF.Max(0.0005f, ropeSegment3.LooseAmount * _looseAmountMultiplier);
			num2++;
		}
		if (ropeSegment != null)
		{
			num3 = MathF.Max(0.0005f, ropeSegment.LooseAmount * _looseAmountMultiplier);
		}
		zero *= 0.5f;
		if (num2 > 0)
		{
			num /= (float)num2;
		}
		Vec3 origin3 = _endTargetEntity.GetGlobalFrame().origin;
		float num4 = zero.Distance(origin3);
		num4 += num + num3;
		float x = _pulleyEntity.GetGlobalFrame().rotation.GetScaleVector().x;
		float num5 = _endRopeLength * x;
		float value = 1f - num5 / num4;
		value = MathF.Clamp(value, 0f, 1f);
		Vec3 vec = RopeSegment.CalculateAutoCurvePosition(zero, origin3, num4, value);
		float dx = MathF.Min(value + 0.01f, 1f);
		Vec3 vec2 = RopeSegment.CalculateAutoCurvePosition(zero, origin3, num4, dx) - vec;
		if (vec2.LengthSquared > 0f)
		{
			vec2.Normalize();
		}
		vec2 = vec2 * 0.5f + (origin3 - vec) * 0.5f;
		vec2.Normalize();
		WeakGameEntity weakEntity = _fixedSegments[_fixedSegments.Count - 1].RopeEntity.WeakEntity;
		WeakGameEntity weakEntity2 = _freeSegments[_freeSegments.Count - 1].RopeEntity.WeakEntity;
		Vec3 s = weakEntity.GetGlobalFrame().origin - weakEntity2.GetGlobalFrame().origin;
		if (!(s.Length < 1E-06f))
		{
			s.Normalize();
			MatrixFrame frame = _pulleyEntity.GetFrame();
			frame.rotation.u = vec2;
			frame.rotation.s = s;
			frame.rotation.f = Vec3.CrossProduct(frame.rotation.s, frame.rotation.u);
			frame.rotation.f.Normalize();
			frame.rotation.s = Vec3.CrossProduct(frame.rotation.f, frame.rotation.u);
			frame.rotation.s.Normalize();
			WeakGameEntity parent = _pulleyEntity.WeakEntity.Parent;
			if (parent != null)
			{
				frame.rotation = parent.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame).rotation;
			}
			frame.rotation.Orthonormalize();
			_pulleyEntity.SetFrame(ref frame);
			MatrixFrame frame2 = _pulleyEntity.GetGlobalFrame();
			frame2.origin = vec;
			_pulleyEntity.SetGlobalFrame(in frame2);
		}
	}

	private float SetRopeParamsForSegment(WeakGameEntity pulleyRopeConnectPoint, SegmentData segmentData, bool isFixed, float pull_amount, bool moveUV, bool is_end_rope)
	{
		pulleyRopeConnectPoint.GetGlobalFrame();
		segmentData.RopeEntity.GetGlobalFrame();
		if (moveUV)
		{
			Vec3 vectorArgument = segmentData.RopeSegment.RopeMesh.GetVectorArgument2();
			vectorArgument.w += pull_amount * 25.9f;
			segmentData.RopeSegment.RopeMesh.SetVectorArgument2(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
		}
		if (!isFixed || moveUV)
		{
			segmentData.RopeSegment.ShiftRope(0f - pull_amount);
		}
		return 25.9f;
	}

	public void SetEndTargetPosition(Vec3 position)
	{
		if (_endTargetEntity != null)
		{
			MatrixFrame frame = _endTargetEntity.GetGlobalFrame();
			frame.origin = position;
			_endTargetEntity.SetGlobalFrame(in frame);
		}
	}

	public void SetLinearMode(bool value)
	{
		foreach (SegmentData freeSegment in _freeSegments)
		{
			freeSegment.RopeSegment.SetLinearMode(value);
		}
		foreach (SegmentData fixedSegment in _fixedSegments)
		{
			fixedSegment.RopeSegment.SetLinearMode(value);
		}
		if (_endPointRope.RopeSegment != null)
		{
			_endPointRope.RopeSegment.SetLinearMode(value);
		}
	}

	public bool DeregisterRopeSegmentCosmetics(RopeSegmentCosmetics cosmetics)
	{
		bool result = false;
		foreach (SegmentData fixedSegment in _fixedSegments)
		{
			if (fixedSegment.RopeSegment.DeregisterRopeSegmentCosmetics(cosmetics))
			{
				result = true;
			}
		}
		foreach (SegmentData freeSegment in _freeSegments)
		{
			if (freeSegment.RopeSegment.DeregisterRopeSegmentCosmetics(cosmetics))
			{
				result = true;
			}
		}
		if (_endPointRope.RopeSegment != null && _endPointRope.RopeSegment.DeregisterRopeSegmentCosmetics(cosmetics))
		{
			result = true;
		}
		return result;
	}

	private void FetchRopeSegmentsForSide(WeakGameEntity parentEntity, bool isFixed, ref List<SegmentData> output)
	{
		output.Clear();
		foreach (WeakGameEntity child in base.GameEntity.GetChildren())
		{
			RopeSegment firstScriptOfType = child.GetFirstScriptOfType<RopeSegment>();
			if (firstScriptOfType != null && firstScriptOfType.IsFixed == isFixed && !child.HasTag("end_point_rope"))
			{
				SegmentData item = new SegmentData
				{
					RopeSegment = firstScriptOfType,
					RopeEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(child)
				};
				output.Add(item);
				if (child.HasTag("attached_to_yard"))
				{
					_tiedToYardSegments.Add(firstScriptOfType);
				}
			}
		}
		output.Sort((SegmentData a, SegmentData b) => a.RopeSegment.SegmentIndex.CompareTo(b.RopeSegment.SegmentIndex));
	}

	public void SetRuntimeLooseMultiplier(float value)
	{
		_looseAmountMultiplier = value;
		foreach (SegmentData freeSegment in _freeSegments)
		{
			freeSegment.RopeSegment.SetRuntimeLooseMultiplier(value);
		}
		foreach (SegmentData fixedSegment in _fixedSegments)
		{
			fixedSegment.RopeSegment.SetRuntimeLooseMultiplier(value);
		}
		_endPointRope.RopeSegment.SetRuntimeLooseMultiplier(value * 0.25f);
	}

	public void ApplyBoundingBox(MatrixFrame parentFrame, ref BoundingBox bb)
	{
		foreach (SegmentData freeSegment in _freeSegments)
		{
			MatrixFrame globalFrame = freeSegment.RopeEntity.GetGlobalFrame();
			Vec3 vec = parentFrame.TransformToLocalNonOrthogonal(in globalFrame.origin);
			bb.RelaxMinMaxWithPoint(vec + Vec3.One * 0.25f);
			bb.RelaxMinMaxWithPoint(vec - Vec3.One * 0.25f);
		}
		foreach (SegmentData fixedSegment in _fixedSegments)
		{
			MatrixFrame globalFrame = fixedSegment.RopeEntity.GetGlobalFrame();
			Vec3 vec2 = parentFrame.TransformToLocalNonOrthogonal(in globalFrame.origin);
			bb.RelaxMinMaxWithPoint(vec2 + Vec3.One * 0.25f);
			bb.RelaxMinMaxWithPoint(vec2 - Vec3.One * 0.25f);
		}
		if (_endTargetEntity != null)
		{
			MatrixFrame globalFrame = _endTargetEntity.GetGlobalFrame();
			Vec3 vec3 = parentFrame.TransformToLocalNonOrthogonal(in globalFrame.origin);
			bb.RelaxMinMaxWithPoint(vec3 + Vec3.One * 0.25f);
			bb.RelaxMinMaxWithPoint(vec3 - Vec3.One * 0.25f);
		}
	}

	public Vec3 GetTiePointCenter()
	{
		if (_freeSegments.Count == 0 || _fixedSegments.Count == 0)
		{
			return Vec3.Zero;
		}
		return (_freeSegments[_freeSegments.Count - 1].RopeEntity.GetGlobalFrame().origin + _fixedSegments[_fixedSegments.Count - 1].RopeEntity.GetGlobalFrame().origin) * 0.5f;
	}

	public void SetFirstFreeGlobalPosition(Vec3 position)
	{
		if (_freeSegments.Count > 0)
		{
			MatrixFrame frame = _freeSegments[0].RopeEntity.GetGlobalFrame();
			frame.origin = position;
			_freeSegments[0].RopeEntity.SetGlobalFrame(in frame);
		}
	}

	public void SetFirstFixedGlobalPosition(Vec3 position)
	{
		if (_fixedSegments.Count > 0)
		{
			MatrixFrame frame = _fixedSegments[0].RopeEntity.GetGlobalFrame();
			frame.origin = position;
			_fixedSegments[0].RopeEntity.SetGlobalFrame(in frame);
		}
	}

	public void FillBurningRecord(BurningSystem system)
	{
		float nodeLength = 2f;
		string prefabName = "burning_node_rope";
		if (_endPointRope.RopeSegment == null)
		{
			return;
		}
		_endPointRope.RopeSegment.FillBurningRecordForSegment(system, prefabName, nodeLength, reversePlacement: true);
		foreach (SegmentData item in (MBRandom.RandomFloat > 0.5f) ? _freeSegments : _fixedSegments)
		{
			item.RopeSegment.FillBurningRecordForSegment(system, prefabName, nodeLength, reversePlacement: true);
		}
	}

	public void SetAlpha(float value)
	{
		if (value <= 0f)
		{
			base.GameEntity.SetVisibilityExcludeParents(visible: false);
			return;
		}
		base.GameEntity.SetVisibilityExcludeParents(visible: true);
		base.GameEntity.SetAlpha(MathF.Clamp(value, 0f, 1f));
	}

	public void GetAllRopeSegments(ref List<RopeSegment> segments, float maximumRopeThickness)
	{
		foreach (SegmentData freeSegment in _freeSegments)
		{
			if (freeSegment.RopeSegment.RopeMesh != null && freeSegment.RopeSegment.RopeMesh.GetVectorArgument().w < maximumRopeThickness)
			{
				segments.Add(freeSegment.RopeSegment);
			}
		}
		foreach (SegmentData fixedSegment in _fixedSegments)
		{
			if (fixedSegment.RopeSegment.RopeMesh != null && fixedSegment.RopeSegment.RopeMesh.GetVectorArgument().w < maximumRopeThickness)
			{
				segments.Add(fixedSegment.RopeSegment);
			}
		}
		if (_endPointRope.RopeSegment != null && _endPointRope.RopeSegment.RopeMesh != null && _endPointRope.RopeSegment.RopeMesh.GetVectorArgument().w < maximumRopeThickness)
		{
			segments.Add(_endPointRope.RopeSegment);
		}
	}
}

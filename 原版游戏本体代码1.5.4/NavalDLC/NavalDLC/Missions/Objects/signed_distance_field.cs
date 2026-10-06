using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects;

[ScriptComponentParams("ship_visual_only", "")]
public class signed_distance_field : ScriptComponentBehavior
{
	[EditableScriptComponentVariable(true, "SDF Texture")]
	private Texture _sdfTexture;

	[EditableScriptComponentVariable(true, "Visualize SDF")]
	private bool _visualizeSDF;

	private int _sdfIndex = -1;

	public void DummyFunc()
	{
		Debug.Print(_visualizeSDF.ToString());
	}

	private signed_distance_field()
	{
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.TickParallel;
	}

	protected override void OnInit()
	{
		if (!base.GameEntity.IsGhostObject())
		{
			_sdfIndex = base.GameEntity.RegisterWaterSDFClip(_sdfTexture);
			SetSDFParams();
		}
	}

	protected override void OnEditorInit()
	{
		if (!base.GameEntity.IsGhostObject())
		{
			_sdfIndex = base.GameEntity.RegisterWaterSDFClip(_sdfTexture);
			SetSDFParams();
		}
	}

	protected override void OnTickParallel(float dt)
	{
		SetSDFParams();
	}

	protected override void OnEditorVariableChanged(string variableName)
	{
	}

	protected override void OnRemoved(int removeReason)
	{
		if (_sdfIndex != -1)
		{
			base.GameEntity.DeRegisterWaterSDFClip(_sdfIndex);
		}
	}

	private MatrixFrame ComputeBBOXFrame(ref Vec3 sdfBBExtend)
	{
		Vec3 min = default(Vec3);
		Vec3 max = default(Vec3);
		_sdfTexture.GetSDFBoundingBoxData(ref min, ref max);
		BoundingBox boundingBox = default(BoundingBox);
		boundingBox.BeginRelaxation();
		boundingBox.RelaxMinMaxWithPoint(in min);
		boundingBox.RelaxMinMaxWithPoint(in max);
		boundingBox.RecomputeRadius();
		MatrixFrame identity = MatrixFrame.Identity;
		identity.origin = boundingBox.center;
		sdfBBExtend = boundingBox.max - boundingBox.min;
		identity.rotation.s *= sdfBBExtend.x * 0.5f;
		identity.rotation.f *= sdfBBExtend.y * 0.5f;
		identity.rotation.u *= sdfBBExtend.z * 0.5f;
		return identity;
	}

	private void SetSDFParams()
	{
		if (_sdfTexture != null && _sdfIndex != -1)
		{
			Vec3 sdfBBExtend = default(Vec3);
			MatrixFrame m = ComputeBBOXFrame(ref sdfBBExtend);
			m = base.GameEntity.GetGlobalFrame().TransformToParent(in m);
			m.Fill();
			MatrixFrame frame = m.Inverse();
			frame.Fill();
			base.GameEntity.SetWaterSDFClipData(_sdfIndex, in frame, base.GameEntity.IsVisibleIncludeParents());
		}
	}
}

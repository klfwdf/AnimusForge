using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class RopePile : ScriptComponentBehavior
{
	public Vec3 point0 = new Vec3(0f, 0f, 0f, 0f);

	public Vec3 point1 = new Vec3(0f, 0f, 0f, 0f);

	public Vec3 point2 = new Vec3(0f, 0f, 0f, 0f);

	public Vec3 point3 = new Vec3(0f, 0f, 0f, 0f);

	public float factor;

	public override TickRequirement GetTickRequirement()
	{
		return base.GetTickRequirement() | TickRequirement.Tick;
	}

	protected override void OnInit()
	{
		SetScriptComponentToTick(GetTickRequirement());
		base.GameEntity.GetFirstMesh().SetupAdditionalBoneBuffer(1);
	}

	protected override void OnTick(float dt)
	{
		Mesh firstMesh = base.GameEntity.GetFirstMesh();
		firstMesh.SetAdditionalBoneFrame(0, new MatrixFrame(new Mat3(in point0, in point1, in point2), in point3));
		Vec3 vectorArgument = firstMesh.GetVectorArgument();
		vectorArgument.z = factor;
		firstMesh.SetVectorArgument(vectorArgument.x, vectorArgument.y, vectorArgument.z, vectorArgument.w);
	}
}

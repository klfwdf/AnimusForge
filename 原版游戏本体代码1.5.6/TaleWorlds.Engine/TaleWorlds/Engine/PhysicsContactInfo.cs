using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineStruct("rglPhysics_contact_info", false, null)]
public struct PhysicsContactInfo
{
	public Vec3 Position;

	public Vec3 Normal;

	public float Penetration;

	public Vec3 Impulse;

	[CustomEngineStructMemberData("physics_material0_index")]
	public PhysicsMaterial PhysicsMaterial0;

	[CustomEngineStructMemberData("physics_material1_index")]
	public PhysicsMaterial PhysicsMaterial1;
}

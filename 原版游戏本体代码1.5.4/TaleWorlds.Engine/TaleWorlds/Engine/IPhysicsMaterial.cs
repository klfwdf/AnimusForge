using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IPhysicsMaterial
{
	[EngineMethod("get_index_with_name", false, null, false)]
	PhysicsMaterial GetIndexWithName(string materialName);

	[EngineMethod("get_material_count", false, null, false)]
	int GetMaterialCount();

	[EngineMethod("get_material_name_at_index", false, null, false)]
	string GetMaterialNameAtIndex(int index);

	[EngineMethod("get_material_flags_at_index", false, null, false)]
	PhysicsMaterialFlags GetFlagsAtIndex(int index);

	[EngineMethod("get_restitution_at_index", false, null, false)]
	float GetRestitutionAtIndex(int index);

	[EngineMethod("get_dynamic_friction_at_index", false, null, false)]
	float GetDynamicFrictionAtIndex(int index);

	[EngineMethod("get_static_friction_at_index", false, null, false)]
	float GetStaticFrictionAtIndex(int index);

	[EngineMethod("get_linear_damping_at_index", false, null, false)]
	float GetLinearDampingAtIndex(int index);

	[EngineMethod("get_angular_damping_at_index", false, null, false)]
	float GetAngularDampingAtIndex(int index);
}

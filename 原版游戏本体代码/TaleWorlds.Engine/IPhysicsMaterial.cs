using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200002E RID: 46
	[ApplicationInterfaceBase]
	internal interface IPhysicsMaterial
	{
		// Token: 0x060004FC RID: 1276
		[EngineMethod("get_index_with_name", false, null, false)]
		PhysicsMaterial GetIndexWithName(string materialName);

		// Token: 0x060004FD RID: 1277
		[EngineMethod("get_material_count", false, null, false)]
		int GetMaterialCount();

		// Token: 0x060004FE RID: 1278
		[EngineMethod("get_material_name_at_index", false, null, false)]
		string GetMaterialNameAtIndex(int index);

		// Token: 0x060004FF RID: 1279
		[EngineMethod("get_material_flags_at_index", false, null, false)]
		PhysicsMaterialFlags GetFlagsAtIndex(int index);

		// Token: 0x06000500 RID: 1280
		[EngineMethod("get_restitution_at_index", false, null, false)]
		float GetRestitutionAtIndex(int index);

		// Token: 0x06000501 RID: 1281
		[EngineMethod("get_dynamic_friction_at_index", false, null, false)]
		float GetDynamicFrictionAtIndex(int index);

		// Token: 0x06000502 RID: 1282
		[EngineMethod("get_static_friction_at_index", false, null, false)]
		float GetStaticFrictionAtIndex(int index);

		// Token: 0x06000503 RID: 1283
		[EngineMethod("get_linear_damping_at_index", false, null, false)]
		float GetLinearDampingAtIndex(int index);

		// Token: 0x06000504 RID: 1284
		[EngineMethod("get_angular_damping_at_index", false, null, false)]
		float GetAngularDampingAtIndex(int index);
	}
}

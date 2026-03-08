using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200001F RID: 31
	[ApplicationInterfaceBase]
	internal interface IDecal
	{
		// Token: 0x060001B6 RID: 438
		[EngineMethod("get_material", false, null, false)]
		Material GetMaterial(UIntPtr decalPointer);

		// Token: 0x060001B7 RID: 439
		[EngineMethod("set_material", false, null, false)]
		void SetMaterial(UIntPtr decalPointer, UIntPtr materialPointer);

		// Token: 0x060001B8 RID: 440
		[EngineMethod("create_decal", false, null, false)]
		Decal CreateDecal(string name);

		// Token: 0x060001B9 RID: 441
		[EngineMethod("override_road_boundary_p0", false, null, false)]
		void OverrideRoadBoundaryP0(UIntPtr decalPointer, in Vec2 data);

		// Token: 0x060001BA RID: 442
		[EngineMethod("override_road_boundary_p1", false, null, false)]
		void OverrideRoadBoundaryP1(UIntPtr decalPointer, in Vec2 data);

		// Token: 0x060001BB RID: 443
		[EngineMethod("get_factor_1", false, null, false)]
		uint GetFactor1(UIntPtr decalPointer);

		// Token: 0x060001BC RID: 444
		[EngineMethod("set_factor_1_linear", false, null, false)]
		void SetFactor1Linear(UIntPtr decalPointer, uint linearFactorColor1);

		// Token: 0x060001BD RID: 445
		[EngineMethod("set_factor_1", false, null, false)]
		void SetFactor1(UIntPtr decalPointer, uint factorColor1);

		// Token: 0x060001BE RID: 446
		[EngineMethod("set_alpha", false, null, true)]
		void SetAlpha(UIntPtr decalPointer, float alpha);

		// Token: 0x060001BF RID: 447
		[EngineMethod("set_vector_argument", false, null, false)]
		void SetVectorArgument(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x060001C0 RID: 448
		[EngineMethod("set_vector_argument_2", false, null, false)]
		void SetVectorArgument2(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x060001C1 RID: 449
		[EngineMethod("get_global_frame", false, null, false)]
		void GetFrame(UIntPtr decalPointer, ref MatrixFrame outFrame);

		// Token: 0x060001C2 RID: 450
		[EngineMethod("set_global_frame", false, null, false)]
		void SetFrame(UIntPtr decalPointer, ref MatrixFrame decalFrame);

		// Token: 0x060001C3 RID: 451
		[EngineMethod("create_copy", false, null, false)]
		Decal CreateCopy(UIntPtr pointer);

		// Token: 0x060001C4 RID: 452
		[EngineMethod("check_and_register_to_decal_set", false, null, false)]
		void CheckAndRegisterToDecalSet(UIntPtr pointer);

		// Token: 0x060001C5 RID: 453
		[EngineMethod("set_is_visible", false, null, true)]
		void SetIsVisible(UIntPtr pointer, bool value);
	}
}

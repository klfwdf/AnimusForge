using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IDecal
{
	[EngineMethod("get_material", false, null, false)]
	Material GetMaterial(UIntPtr decalPointer);

	[EngineMethod("set_material", false, null, false)]
	void SetMaterial(UIntPtr decalPointer, UIntPtr materialPointer);

	[EngineMethod("create_decal", false, null, false)]
	Decal CreateDecal(string name);

	[EngineMethod("override_road_boundary_p0", false, null, false)]
	void OverrideRoadBoundaryP0(UIntPtr decalPointer, in Vec2 data);

	[EngineMethod("override_road_boundary_p1", false, null, false)]
	void OverrideRoadBoundaryP1(UIntPtr decalPointer, in Vec2 data);

	[EngineMethod("get_factor_1", false, null, false)]
	uint GetFactor1(UIntPtr decalPointer);

	[EngineMethod("set_factor_1_linear", false, null, false)]
	void SetFactor1Linear(UIntPtr decalPointer, uint linearFactorColor1);

	[EngineMethod("set_factor_1", false, null, false)]
	void SetFactor1(UIntPtr decalPointer, uint factorColor1);

	[EngineMethod("set_alpha", false, null, true)]
	void SetAlpha(UIntPtr decalPointer, float alpha);

	[EngineMethod("set_vector_argument", false, null, false)]
	void SetVectorArgument(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("set_vector_argument_2", false, null, false)]
	void SetVectorArgument2(UIntPtr decalPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("get_global_frame", false, null, false)]
	void GetFrame(UIntPtr decalPointer, ref MatrixFrame outFrame);

	[EngineMethod("set_global_frame", false, null, false)]
	void SetFrame(UIntPtr decalPointer, ref MatrixFrame decalFrame);

	[EngineMethod("create_copy", false, null, false)]
	Decal CreateCopy(UIntPtr pointer);

	[EngineMethod("check_and_register_to_decal_set", false, null, false)]
	void CheckAndRegisterToDecalSet(UIntPtr pointer);

	[EngineMethod("set_is_visible", false, null, true)]
	void SetIsVisible(UIntPtr pointer, bool value);
}

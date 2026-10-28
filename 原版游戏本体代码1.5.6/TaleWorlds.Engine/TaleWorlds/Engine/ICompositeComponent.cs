using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface ICompositeComponent
{
	[EngineMethod("create_composite_component", false, null, false)]
	CompositeComponent CreateCompositeComponent();

	[EngineMethod("set_material", false, null, false)]
	void SetMaterial(UIntPtr compositeComponentPointer, UIntPtr materialPointer);

	[EngineMethod("create_copy", false, null, false)]
	CompositeComponent CreateCopy(UIntPtr pointer);

	[EngineMethod("add_component", false, null, false)]
	void AddComponent(UIntPtr pointer, UIntPtr componentPointer);

	[EngineMethod("add_prefab_entity", false, null, false)]
	void AddPrefabEntity(UIntPtr pointer, UIntPtr scenePointer, string prefabName);

	[EngineMethod("release", false, null, false)]
	void Release(UIntPtr compositeComponentPointer);

	[EngineMethod("get_factor_1", false, null, false)]
	uint GetFactor1(UIntPtr compositeComponentPointer);

	[EngineMethod("get_factor_2", false, null, false)]
	uint GetFactor2(UIntPtr compositeComponentPointer);

	[EngineMethod("set_factor_1", false, null, false)]
	void SetFactor1(UIntPtr compositeComponentPointer, uint factorColor1);

	[EngineMethod("set_factor_2", false, null, false)]
	void SetFactor2(UIntPtr compositeComponentPointer, uint factorColor2);

	[EngineMethod("set_vector_argument", false, null, false)]
	void SetVectorArgument(UIntPtr compositeComponentPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[EngineMethod("get_frame", false, null, false)]
	void GetFrame(UIntPtr compositeComponentPointer, ref MatrixFrame outFrame);

	[EngineMethod("set_frame", false, null, false)]
	void SetFrame(UIntPtr compositeComponentPointer, ref MatrixFrame meshFrame);

	[EngineMethod("get_vector_user_data", false, null, false)]
	Vec3 GetVectorUserData(UIntPtr compositeComponentPointer);

	[EngineMethod("set_vector_user_data", false, null, false)]
	void SetVectorUserData(UIntPtr compositeComponentPointer, ref Vec3 vectorArg);

	[EngineMethod("get_bounding_box", false, null, false)]
	void GetBoundingBox(UIntPtr compositeComponentPointer, ref BoundingBox outBoundingBox);

	[EngineMethod("set_visibility_mask", false, null, false)]
	void SetVisibilityMask(UIntPtr compositeComponentPointer, VisibilityMaskFlags visibilityMask);

	[EngineMethod("get_first_meta_mesh", false, null, false)]
	MetaMesh GetFirstMetaMesh(UIntPtr compositeComponentPointer);

	[EngineMethod("add_multi_mesh", false, null, false)]
	void AddMultiMesh(UIntPtr compositeComponentPointer, string multiMeshName);

	[EngineMethod("is_visible", false, null, false)]
	bool IsVisible(UIntPtr compositeComponentPointer);

	[EngineMethod("set_visible", false, null, false)]
	void SetVisible(UIntPtr compositeComponentPointer, bool visible);
}

using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000021 RID: 33
	[ApplicationInterfaceBase]
	internal interface ICompositeComponent
	{
		// Token: 0x060001D3 RID: 467
		[EngineMethod("create_composite_component", false, null, false)]
		CompositeComponent CreateCompositeComponent();

		// Token: 0x060001D4 RID: 468
		[EngineMethod("set_material", false, null, false)]
		void SetMaterial(UIntPtr compositeComponentPointer, UIntPtr materialPointer);

		// Token: 0x060001D5 RID: 469
		[EngineMethod("create_copy", false, null, false)]
		CompositeComponent CreateCopy(UIntPtr pointer);

		// Token: 0x060001D6 RID: 470
		[EngineMethod("add_component", false, null, false)]
		void AddComponent(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x060001D7 RID: 471
		[EngineMethod("add_prefab_entity", false, null, false)]
		void AddPrefabEntity(UIntPtr pointer, UIntPtr scenePointer, string prefabName);

		// Token: 0x060001D8 RID: 472
		[EngineMethod("release", false, null, false)]
		void Release(UIntPtr compositeComponentPointer);

		// Token: 0x060001D9 RID: 473
		[EngineMethod("get_factor_1", false, null, false)]
		uint GetFactor1(UIntPtr compositeComponentPointer);

		// Token: 0x060001DA RID: 474
		[EngineMethod("get_factor_2", false, null, false)]
		uint GetFactor2(UIntPtr compositeComponentPointer);

		// Token: 0x060001DB RID: 475
		[EngineMethod("set_factor_1", false, null, false)]
		void SetFactor1(UIntPtr compositeComponentPointer, uint factorColor1);

		// Token: 0x060001DC RID: 476
		[EngineMethod("set_factor_2", false, null, false)]
		void SetFactor2(UIntPtr compositeComponentPointer, uint factorColor2);

		// Token: 0x060001DD RID: 477
		[EngineMethod("set_vector_argument", false, null, false)]
		void SetVectorArgument(UIntPtr compositeComponentPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x060001DE RID: 478
		[EngineMethod("get_frame", false, null, false)]
		void GetFrame(UIntPtr compositeComponentPointer, ref MatrixFrame outFrame);

		// Token: 0x060001DF RID: 479
		[EngineMethod("set_frame", false, null, false)]
		void SetFrame(UIntPtr compositeComponentPointer, ref MatrixFrame meshFrame);

		// Token: 0x060001E0 RID: 480
		[EngineMethod("get_vector_user_data", false, null, false)]
		Vec3 GetVectorUserData(UIntPtr compositeComponentPointer);

		// Token: 0x060001E1 RID: 481
		[EngineMethod("set_vector_user_data", false, null, false)]
		void SetVectorUserData(UIntPtr compositeComponentPointer, ref Vec3 vectorArg);

		// Token: 0x060001E2 RID: 482
		[EngineMethod("get_bounding_box", false, null, false)]
		void GetBoundingBox(UIntPtr compositeComponentPointer, ref BoundingBox outBoundingBox);

		// Token: 0x060001E3 RID: 483
		[EngineMethod("set_visibility_mask", false, null, false)]
		void SetVisibilityMask(UIntPtr compositeComponentPointer, VisibilityMaskFlags visibilityMask);

		// Token: 0x060001E4 RID: 484
		[EngineMethod("get_first_meta_mesh", false, null, false)]
		MetaMesh GetFirstMetaMesh(UIntPtr compositeComponentPointer);

		// Token: 0x060001E5 RID: 485
		[EngineMethod("add_multi_mesh", false, null, false)]
		void AddMultiMesh(UIntPtr compositeComponentPointer, string multiMeshName);

		// Token: 0x060001E6 RID: 486
		[EngineMethod("is_visible", false, null, false)]
		bool IsVisible(UIntPtr compositeComponentPointer);

		// Token: 0x060001E7 RID: 487
		[EngineMethod("set_visible", false, null, false)]
		void SetVisible(UIntPtr compositeComponentPointer, bool visible);
	}
}

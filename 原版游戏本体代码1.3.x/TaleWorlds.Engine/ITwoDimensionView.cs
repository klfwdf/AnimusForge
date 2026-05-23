using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000036 RID: 54
	[ApplicationInterfaceBase]
	internal interface ITwoDimensionView
	{
		// Token: 0x06000569 RID: 1385
		[EngineMethod("create_twodimension_view", false, null, false)]
		TwoDimensionView CreateTwoDimensionView(string viewName);

		// Token: 0x0600056A RID: 1386
		[EngineMethod("begin_frame", false, null, false)]
		void BeginFrame(UIntPtr pointer);

		// Token: 0x0600056B RID: 1387
		[EngineMethod("end_frame", false, null, false)]
		void EndFrame(UIntPtr pointer);

		// Token: 0x0600056C RID: 1388
		[EngineMethod("clear", false, null, false)]
		void Clear(UIntPtr pointer);

		// Token: 0x0600056D RID: 1389
		[EngineMethod("add_new_mesh", false, null, false)]
		void AddNewMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

		// Token: 0x0600056E RID: 1390
		[EngineMethod("add_new_quad_mesh", false, null, false)]
		void AddNewQuadMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

		// Token: 0x0600056F RID: 1391
		[EngineMethod("add_cached_text_mesh", false, null, false)]
		bool AddCachedTextMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

		// Token: 0x06000570 RID: 1392
		[EngineMethod("add_new_text_mesh", false, null, false)]
		void AddNewTextMesh(UIntPtr pointer, float[] vertices, float[] uvs, uint[] indices, int vertexCount, int indexCount, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

		// Token: 0x06000571 RID: 1393
		[EngineMethod("get_or_create_material", false, null, false)]
		UIntPtr GetOrCreateMaterial(UIntPtr pointer, UIntPtr mainTexture, UIntPtr overlayTexture);
	}
}

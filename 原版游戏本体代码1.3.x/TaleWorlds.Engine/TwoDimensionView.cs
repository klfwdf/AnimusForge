using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000096 RID: 150
	[EngineClass("rglTwo_dimension_view")]
	public sealed class TwoDimensionView : View
	{
		// Token: 0x06000D15 RID: 3349 RVA: 0x0000EA04 File Offset: 0x0000CC04
		internal TwoDimensionView(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x06000D16 RID: 3350 RVA: 0x0000EA0D File Offset: 0x0000CC0D
		public static TwoDimensionView CreateTwoDimension(string viewName)
		{
			return EngineApplicationInterface.ITwoDimensionView.CreateTwoDimensionView(viewName);
		}

		// Token: 0x06000D17 RID: 3351 RVA: 0x0000EA1A File Offset: 0x0000CC1A
		public void BeginFrame()
		{
			EngineApplicationInterface.ITwoDimensionView.BeginFrame(base.Pointer);
		}

		// Token: 0x06000D18 RID: 3352 RVA: 0x0000EA2C File Offset: 0x0000CC2C
		public void EndFrame()
		{
			EngineApplicationInterface.ITwoDimensionView.EndFrame(base.Pointer);
		}

		// Token: 0x06000D19 RID: 3353 RVA: 0x0000EA3E File Offset: 0x0000CC3E
		public void Clear()
		{
			EngineApplicationInterface.ITwoDimensionView.Clear(base.Pointer);
		}

		// Token: 0x06000D1A RID: 3354 RVA: 0x0000EA50 File Offset: 0x0000CC50
		public void CreateMeshFromDescription(WeakMaterial material, TwoDimensionMeshDrawData meshDrawData)
		{
			EngineApplicationInterface.ITwoDimensionView.AddNewMesh(base.Pointer, material.Pointer, ref meshDrawData);
		}

		// Token: 0x06000D1B RID: 3355 RVA: 0x0000EA6B File Offset: 0x0000CC6B
		public bool CreateTextMeshFromCache(Material material, TwoDimensionTextMeshDrawData meshDrawData)
		{
			return EngineApplicationInterface.ITwoDimensionView.AddCachedTextMesh(base.Pointer, material.Pointer, ref meshDrawData);
		}

		// Token: 0x06000D1C RID: 3356 RVA: 0x0000EA88 File Offset: 0x0000CC88
		public void CreateTextMeshFromDescription(float[] vertices, float[] uvs, uint[] indices, int indexCount, Material material, TwoDimensionTextMeshDrawData meshDrawData)
		{
			EngineApplicationInterface.ITwoDimensionView.AddNewTextMesh(base.Pointer, vertices, uvs, indices, vertices.Length / 2, indexCount, material.Pointer, ref meshDrawData);
		}

		// Token: 0x06000D1D RID: 3357 RVA: 0x0000EAB8 File Offset: 0x0000CCB8
		public WeakMaterial GetOrCreateMaterial(Texture mainTexture, Texture overlayTexture)
		{
			return new WeakMaterial(EngineApplicationInterface.ITwoDimensionView.GetOrCreateMaterial(base.Pointer, (mainTexture != null) ? mainTexture.Pointer : UIntPtr.Zero, (overlayTexture != null) ? overlayTexture.Pointer : UIntPtr.Zero));
		}
	}
}

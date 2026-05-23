using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000090 RID: 144
	[EngineClass("rglTableau_view")]
	public sealed class TableauView : SceneView
	{
		// Token: 0x06000CD8 RID: 3288 RVA: 0x0000E411 File Offset: 0x0000C611
		internal TableauView(UIntPtr meshPointer)
			: base(meshPointer)
		{
		}

		// Token: 0x06000CD9 RID: 3289 RVA: 0x0000E41A File Offset: 0x0000C61A
		public static TableauView CreateTableauView(string viewName)
		{
			return EngineApplicationInterface.ITableauView.CreateTableauView(viewName);
		}

		// Token: 0x06000CDA RID: 3290 RVA: 0x0000E427 File Offset: 0x0000C627
		public void SetSortingEnabled(bool value)
		{
			EngineApplicationInterface.ITableauView.SetSortingEnabled(base.Pointer, value);
		}

		// Token: 0x06000CDB RID: 3291 RVA: 0x0000E43A File Offset: 0x0000C63A
		public void SetContinuousRendering(bool value)
		{
			EngineApplicationInterface.ITableauView.SetContinousRendering(base.Pointer, value);
		}

		// Token: 0x06000CDC RID: 3292 RVA: 0x0000E44D File Offset: 0x0000C64D
		public void SetDoNotRenderThisFrame(bool value)
		{
			EngineApplicationInterface.ITableauView.SetDoNotRenderThisFrame(base.Pointer, value);
		}

		// Token: 0x06000CDD RID: 3293 RVA: 0x0000E460 File Offset: 0x0000C660
		public void SetDeleteAfterRendering(bool value)
		{
			EngineApplicationInterface.ITableauView.SetDeleteAfterRendering(base.Pointer, value);
		}

		// Token: 0x06000CDE RID: 3294 RVA: 0x0000E473 File Offset: 0x0000C673
		public static Texture AddTableau(string name, RenderTargetComponent.TextureUpdateEventHandler eventHandler, object objectRef, int tableauSizeX, int tableauSizeY)
		{
			Texture texture = Texture.CreateTableauTexture(name, eventHandler, objectRef, tableauSizeX, tableauSizeY);
			texture.TableauView.SetRenderOnDemand(false);
			return texture;
		}
	}
}

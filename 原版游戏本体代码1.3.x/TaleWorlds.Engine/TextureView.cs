using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000092 RID: 146
	[EngineClass("rglTexture_view")]
	public sealed class TextureView : View
	{
		// Token: 0x06000D02 RID: 3330 RVA: 0x0000E767 File Offset: 0x0000C967
		internal TextureView(UIntPtr meshPointer)
			: base(meshPointer)
		{
		}

		// Token: 0x06000D03 RID: 3331 RVA: 0x0000E770 File Offset: 0x0000C970
		public static TextureView CreateTextureView()
		{
			return EngineApplicationInterface.ITextureView.CreateTextureView();
		}

		// Token: 0x06000D04 RID: 3332 RVA: 0x0000E77C File Offset: 0x0000C97C
		public void SetTexture(Texture texture)
		{
			EngineApplicationInterface.ITextureView.SetTexture(base.Pointer, texture.Pointer);
		}
	}
}

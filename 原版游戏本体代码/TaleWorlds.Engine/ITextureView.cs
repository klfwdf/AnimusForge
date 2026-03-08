using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000032 RID: 50
	[ApplicationInterfaceBase]
	internal interface ITextureView
	{
		// Token: 0x06000538 RID: 1336
		[EngineMethod("create_texture_view", false, null, false)]
		TextureView CreateTextureView();

		// Token: 0x06000539 RID: 1337
		[EngineMethod("set_texture", false, null, true)]
		void SetTexture(UIntPtr pointer, UIntPtr texture_ptr);
	}
}

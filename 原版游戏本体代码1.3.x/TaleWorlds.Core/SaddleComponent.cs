using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000C7 RID: 199
	public class SaddleComponent : ItemComponent
	{
		// Token: 0x06000AD1 RID: 2769 RVA: 0x00022B64 File Offset: 0x00020D64
		public SaddleComponent(SaddleComponent saddleComponent)
		{
		}

		// Token: 0x06000AD2 RID: 2770 RVA: 0x00022B6C File Offset: 0x00020D6C
		public override ItemComponent GetCopy()
		{
			return new SaddleComponent(this);
		}
	}
}

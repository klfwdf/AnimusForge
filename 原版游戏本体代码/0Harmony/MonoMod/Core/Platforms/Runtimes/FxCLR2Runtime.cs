using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x02000543 RID: 1347
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class FxCLR2Runtime : FxBaseRuntime
	{
		// Token: 0x06001E2A RID: 7722 RVA: 0x00061D70 File Offset: 0x0005FF70
		public FxCLR2Runtime(ISystem system)
		{
			this.system = system;
			Abi? abiCore = this.AbiCore;
			if (abiCore == null)
			{
				this.AbiCore = system.DefaultAbi;
			}
		}

		// Token: 0x04001277 RID: 4727
		private readonly ISystem system;
	}
}

using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x0200053D RID: 1341
	internal class Core80Runtime : Core70Runtime
	{
		// Token: 0x06001E0A RID: 7690 RVA: 0x00061834 File Offset: 0x0005FA34
		[NullableContext(1)]
		public Core80Runtime(ISystem system, IArchitecture arch)
			: base(system, arch)
		{
		}

		// Token: 0x17000686 RID: 1670
		// (get) Token: 0x06001E0B RID: 7691 RVA: 0x0006183E File Offset: 0x0005FA3E
		protected override Guid ExpectedJitVersion
		{
			get
			{
				return Core80Runtime.JitVersionGuid;
			}
		}

		// Token: 0x17000687 RID: 1671
		// (get) Token: 0x06001E0C RID: 7692 RVA: 0x00061845 File Offset: 0x0005FA45
		protected override int VtableIndexICorJitInfoAllocMem
		{
			get
			{
				return 154;
			}
		}

		// Token: 0x17000688 RID: 1672
		// (get) Token: 0x06001E0D RID: 7693 RVA: 0x0006184C File Offset: 0x0005FA4C
		protected override int ICorJitInfoFullVtableCount
		{
			get
			{
				return 170;
			}
		}

		// Token: 0x0400126F RID: 4719
		private static readonly Guid JitVersionGuid = new Guid(1271838981U, 54608, 19037, 177, 235, 39, 111, byte.MaxValue, 104, 209, 131);
	}
}

using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x0200053C RID: 1340
	internal class Core70Runtime : Core60Runtime
	{
		// Token: 0x06001E05 RID: 7685 RVA: 0x000617CD File Offset: 0x0005F9CD
		[NullableContext(1)]
		public Core70Runtime(ISystem system, IArchitecture arch)
			: base(system, arch)
		{
		}

		// Token: 0x17000683 RID: 1667
		// (get) Token: 0x06001E06 RID: 7686 RVA: 0x000617D7 File Offset: 0x0005F9D7
		protected override Guid ExpectedJitVersion
		{
			get
			{
				return Core70Runtime.JitVersionGuid;
			}
		}

		// Token: 0x17000684 RID: 1668
		// (get) Token: 0x06001E07 RID: 7687 RVA: 0x000617DE File Offset: 0x0005F9DE
		protected override int VtableIndexICorJitInfoAllocMem
		{
			get
			{
				return 159;
			}
		}

		// Token: 0x17000685 RID: 1669
		// (get) Token: 0x06001E08 RID: 7688 RVA: 0x000617E5 File Offset: 0x0005F9E5
		protected override int ICorJitInfoFullVtableCount
		{
			get
			{
				return 175;
			}
		}

		// Token: 0x0400126E RID: 4718
		private static readonly Guid JitVersionGuid = new Guid(1810136669U, 43307, 19734, 146, 128, 246, 61, 246, 70, 173, 164);
	}
}

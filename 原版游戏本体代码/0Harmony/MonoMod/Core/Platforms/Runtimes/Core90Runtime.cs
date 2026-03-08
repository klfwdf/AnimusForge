using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Platforms.Runtimes
{
	// Token: 0x0200053E RID: 1342
	internal class Core90Runtime : Core80Runtime
	{
		// Token: 0x06001E0F RID: 7695 RVA: 0x00061899 File Offset: 0x0005FA99
		[NullableContext(1)]
		public Core90Runtime(ISystem system, IArchitecture arch)
			: base(system, arch)
		{
		}

		// Token: 0x17000689 RID: 1673
		// (get) Token: 0x06001E10 RID: 7696 RVA: 0x000618A3 File Offset: 0x0005FAA3
		protected override Guid ExpectedJitVersion
		{
			get
			{
				return Core90Runtime.JitVersionGuid;
			}
		}

		// Token: 0x1700068A RID: 1674
		// (get) Token: 0x06001E11 RID: 7697 RVA: 0x000618AA File Offset: 0x0005FAAA
		protected override int VtableIndexICorJitInfoAllocMem
		{
			get
			{
				return 158;
			}
		}

		// Token: 0x1700068B RID: 1675
		// (get) Token: 0x06001E12 RID: 7698 RVA: 0x000618B1 File Offset: 0x0005FAB1
		protected override int ICorJitInfoFullVtableCount
		{
			get
			{
				return 174;
			}
		}

		// Token: 0x04001270 RID: 4720
		private static readonly Guid JitVersionGuid = new Guid(3592522360U, 39476, 19567, 141, 181, 7, 122, 6, 2, 47, 174);
	}
}

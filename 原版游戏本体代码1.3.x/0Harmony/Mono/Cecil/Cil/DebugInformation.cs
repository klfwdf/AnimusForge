using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200030E RID: 782
	internal abstract class DebugInformation : ICustomDebugInformationProvider, IMetadataTokenProvider
	{
		// Token: 0x17000527 RID: 1319
		// (get) Token: 0x06001457 RID: 5207 RVA: 0x00040EA3 File Offset: 0x0003F0A3
		// (set) Token: 0x06001458 RID: 5208 RVA: 0x00040EAB File Offset: 0x0003F0AB
		public MetadataToken MetadataToken
		{
			get
			{
				return this.token;
			}
			set
			{
				this.token = value;
			}
		}

		// Token: 0x17000528 RID: 1320
		// (get) Token: 0x06001459 RID: 5209 RVA: 0x00040EB4 File Offset: 0x0003F0B4
		public bool HasCustomDebugInformations
		{
			get
			{
				return !this.custom_infos.IsNullOrEmpty<CustomDebugInformation>();
			}
		}

		// Token: 0x17000529 RID: 1321
		// (get) Token: 0x0600145A RID: 5210 RVA: 0x00040EC4 File Offset: 0x0003F0C4
		public Collection<CustomDebugInformation> CustomDebugInformations
		{
			get
			{
				if (this.custom_infos == null)
				{
					Interlocked.CompareExchange<Collection<CustomDebugInformation>>(ref this.custom_infos, new Collection<CustomDebugInformation>(), null);
				}
				return this.custom_infos;
			}
		}

		// Token: 0x0600145B RID: 5211 RVA: 0x00002B15 File Offset: 0x00000D15
		internal DebugInformation()
		{
		}

		// Token: 0x04000A25 RID: 2597
		internal MetadataToken token;

		// Token: 0x04000A26 RID: 2598
		internal Collection<CustomDebugInformation> custom_infos;
	}
}

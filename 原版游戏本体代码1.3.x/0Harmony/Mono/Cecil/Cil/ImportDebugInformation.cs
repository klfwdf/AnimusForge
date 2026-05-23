using System;
using System.Threading;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000313 RID: 787
	internal sealed class ImportDebugInformation : DebugInformation
	{
		// Token: 0x17000536 RID: 1334
		// (get) Token: 0x06001477 RID: 5239 RVA: 0x00041088 File Offset: 0x0003F288
		public bool HasTargets
		{
			get
			{
				return !this.targets.IsNullOrEmpty<ImportTarget>();
			}
		}

		// Token: 0x17000537 RID: 1335
		// (get) Token: 0x06001478 RID: 5240 RVA: 0x00041098 File Offset: 0x0003F298
		public Collection<ImportTarget> Targets
		{
			get
			{
				if (this.targets == null)
				{
					Interlocked.CompareExchange<Collection<ImportTarget>>(ref this.targets, new Collection<ImportTarget>(), null);
				}
				return this.targets;
			}
		}

		// Token: 0x17000538 RID: 1336
		// (get) Token: 0x06001479 RID: 5241 RVA: 0x000410BA File Offset: 0x0003F2BA
		// (set) Token: 0x0600147A RID: 5242 RVA: 0x000410C2 File Offset: 0x0003F2C2
		public ImportDebugInformation Parent
		{
			get
			{
				return this.parent;
			}
			set
			{
				this.parent = value;
			}
		}

		// Token: 0x0600147B RID: 5243 RVA: 0x000410CB File Offset: 0x0003F2CB
		public ImportDebugInformation()
		{
			this.token = new MetadataToken(TokenType.ImportScope);
		}

		// Token: 0x04000A3C RID: 2620
		internal ImportDebugInformation parent;

		// Token: 0x04000A3D RID: 2621
		internal Collection<ImportTarget> targets;
	}
}

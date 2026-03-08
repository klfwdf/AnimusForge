using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000316 RID: 790
	internal abstract class CustomDebugInformation : DebugInformation
	{
		// Token: 0x1700053B RID: 1339
		// (get) Token: 0x0600147E RID: 5246 RVA: 0x000410E3 File Offset: 0x0003F2E3
		public Guid Identifier
		{
			get
			{
				return this.identifier;
			}
		}

		// Token: 0x1700053C RID: 1340
		// (get) Token: 0x0600147F RID: 5247
		public abstract CustomDebugInformationKind Kind { get; }

		// Token: 0x06001480 RID: 5248 RVA: 0x000410EB File Offset: 0x0003F2EB
		internal CustomDebugInformation(Guid identifier)
		{
			this.identifier = identifier;
			this.token = new MetadataToken(TokenType.CustomDebugInformation);
		}

		// Token: 0x04000A46 RID: 2630
		private Guid identifier;
	}
}

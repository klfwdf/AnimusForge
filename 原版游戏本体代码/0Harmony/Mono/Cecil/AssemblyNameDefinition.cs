using System;

namespace Mono.Cecil
{
	// Token: 0x020001EB RID: 491
	internal sealed class AssemblyNameDefinition : AssemblyNameReference
	{
		// Token: 0x1700023C RID: 572
		// (get) Token: 0x0600097F RID: 2431 RVA: 0x0001EBF9 File Offset: 0x0001CDF9
		public override byte[] Hash
		{
			get
			{
				return Empty<byte>.Array;
			}
		}

		// Token: 0x06000980 RID: 2432 RVA: 0x0001EC00 File Offset: 0x0001CE00
		internal AssemblyNameDefinition()
		{
			this.token = new MetadataToken(TokenType.Assembly, 1);
		}

		// Token: 0x06000981 RID: 2433 RVA: 0x0001EC19 File Offset: 0x0001CE19
		public AssemblyNameDefinition(string name, Version version)
			: base(name, version)
		{
			this.token = new MetadataToken(TokenType.Assembly, 1);
		}
	}
}

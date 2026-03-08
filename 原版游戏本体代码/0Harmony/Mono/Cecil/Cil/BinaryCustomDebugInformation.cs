using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000317 RID: 791
	internal sealed class BinaryCustomDebugInformation : CustomDebugInformation
	{
		// Token: 0x1700053D RID: 1341
		// (get) Token: 0x06001481 RID: 5249 RVA: 0x0004110A File Offset: 0x0003F30A
		// (set) Token: 0x06001482 RID: 5250 RVA: 0x00041112 File Offset: 0x0003F312
		public byte[] Data
		{
			get
			{
				return this.data;
			}
			set
			{
				this.data = value;
			}
		}

		// Token: 0x1700053E RID: 1342
		// (get) Token: 0x06001483 RID: 5251 RVA: 0x0001B69F File Offset: 0x0001989F
		public override CustomDebugInformationKind Kind
		{
			get
			{
				return CustomDebugInformationKind.Binary;
			}
		}

		// Token: 0x06001484 RID: 5252 RVA: 0x0004111B File Offset: 0x0003F31B
		public BinaryCustomDebugInformation(Guid identifier, byte[] data)
			: base(identifier)
		{
			this.data = data;
		}

		// Token: 0x04000A47 RID: 2631
		private byte[] data;
	}
}

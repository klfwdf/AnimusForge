using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000687 RID: 1671
	internal abstract class Op
	{
		// Token: 0x060023DE RID: 9182
		[NullableContext(1)]
		public abstract void Encode(Encoder encoder, in Instruction instruction, int operand);

		// Token: 0x060023DF RID: 9183 RVA: 0x00073D5E File Offset: 0x00071F5E
		public virtual OpKind GetImmediateOpKind()
		{
			return (OpKind)(-1);
		}

		// Token: 0x060023E0 RID: 9184 RVA: 0x00073D5E File Offset: 0x00071F5E
		public virtual OpKind GetNearBranchOpKind()
		{
			return (OpKind)(-1);
		}

		// Token: 0x060023E1 RID: 9185 RVA: 0x00073D5E File Offset: 0x00071F5E
		public virtual OpKind GetFarBranchOpKind()
		{
			return (OpKind)(-1);
		}
	}
}

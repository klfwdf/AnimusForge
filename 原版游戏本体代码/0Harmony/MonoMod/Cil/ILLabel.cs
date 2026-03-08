using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;

namespace MonoMod.Cil
{
	// Token: 0x02000872 RID: 2162
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ILLabel
	{
		// Token: 0x1700082A RID: 2090
		// (get) Token: 0x06002AED RID: 10989 RVA: 0x0008F8FF File Offset: 0x0008DAFF
		// (set) Token: 0x06002AEE RID: 10990 RVA: 0x0008F907 File Offset: 0x0008DB07
		[Nullable(2)]
		public Instruction Target
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			set;
		}

		// Token: 0x1700082B RID: 2091
		// (get) Token: 0x06002AEF RID: 10991 RVA: 0x0008F910 File Offset: 0x0008DB10
		public IEnumerable<Instruction> Branches
		{
			get
			{
				return from i in this.Context.Instrs
					where i.Operand == this
					select i;
			}
		}

		// Token: 0x06002AF0 RID: 10992 RVA: 0x0008F92E File Offset: 0x0008DB2E
		internal ILLabel(ILContext context)
		{
			this.Context = context;
			this.Context._Labels.Add(this);
		}

		// Token: 0x06002AF1 RID: 10993 RVA: 0x0008F94E File Offset: 0x0008DB4E
		internal ILLabel(ILContext context, [Nullable(2)] Instruction target)
			: this(context)
		{
			this.Target = target;
		}

		// Token: 0x04003A4A RID: 14922
		private readonly ILContext Context;
	}
}

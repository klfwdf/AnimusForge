using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000655 RID: 1621
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class InstructionListDebugView
	{
		// Token: 0x06002356 RID: 9046 RVA: 0x00072556 File Offset: 0x00070756
		public InstructionListDebugView(InstructionList list)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			this.list = list;
		}

		// Token: 0x170007F0 RID: 2032
		// (get) Token: 0x06002357 RID: 9047 RVA: 0x00072574 File Offset: 0x00070774
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public Instruction[] Items
		{
			get
			{
				return this.list.ToArray();
			}
		}

		// Token: 0x04002AF4 RID: 10996
		private readonly InstructionList list;
	}
}

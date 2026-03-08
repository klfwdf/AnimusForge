using System;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x0200003A RID: 58
	[NullableContext(2)]
	[Nullable(0)]
	internal sealed class SetStringAction : IAction
	{
		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060001E4 RID: 484 RVA: 0x000081C2 File Offset: 0x000063C2
		[Nullable(1)]
		public IRef Context
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060001E5 RID: 485 RVA: 0x000081CA File Offset: 0x000063CA
		public object Value { get; }

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060001E6 RID: 486 RVA: 0x000081D2 File Offset: 0x000063D2
		public object Original { get; }

		// Token: 0x060001E7 RID: 487 RVA: 0x000081DA File Offset: 0x000063DA
		[NullableContext(1)]
		public SetStringAction(IRef context, string value)
		{
			this.Context = context;
			this.Value = value;
			this.Original = this.Context.Value;
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00008201 File Offset: 0x00006401
		public void DoAction()
		{
			this.Context.Value = this.Value;
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00008214 File Offset: 0x00006414
		public void UndoAction()
		{
			this.Context.Value = this.Original;
		}
	}
}

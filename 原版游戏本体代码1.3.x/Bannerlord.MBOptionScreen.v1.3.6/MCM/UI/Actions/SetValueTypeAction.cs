using System;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x0200003B RID: 59
	[NullableContext(2)]
	[Nullable(0)]
	internal sealed class SetValueTypeAction<[Nullable(0)] T> : IAction where T : struct
	{
		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060001EA RID: 490 RVA: 0x00008227 File Offset: 0x00006427
		[Nullable(1)]
		public IRef Context
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060001EB RID: 491 RVA: 0x0000822F File Offset: 0x0000642F
		public object Value { get; }

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060001EC RID: 492 RVA: 0x00008237 File Offset: 0x00006437
		public object Original { get; }

		// Token: 0x060001ED RID: 493 RVA: 0x0000823F File Offset: 0x0000643F
		[NullableContext(0)]
		public SetValueTypeAction([Nullable(1)] IRef context, T value)
		{
			this.Context = context;
			this.Value = value;
			this.Original = this.Context.Value;
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0000826B File Offset: 0x0000646B
		public void DoAction()
		{
			this.Context.Value = this.Value;
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0000827E File Offset: 0x0000647E
		public void UndoAction()
		{
			this.Context.Value = this.Original;
		}
	}
}

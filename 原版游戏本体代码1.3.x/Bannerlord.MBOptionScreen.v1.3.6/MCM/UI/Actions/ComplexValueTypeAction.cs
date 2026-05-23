using System;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x02000037 RID: 55
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ComplexValueTypeAction<[Nullable(0)] T> : IAction where T : struct
	{
		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x00008036 File Offset: 0x00006236
		public IRef Context { get; }

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060001D2 RID: 466 RVA: 0x0000803E File Offset: 0x0000623E
		[Nullable(2)]
		public object Value
		{
			[NullableContext(2)]
			get
			{
				return this.DoFunction(this.Context.Value as T?);
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060001D3 RID: 467 RVA: 0x00008065 File Offset: 0x00006265
		[Nullable(2)]
		public object Original
		{
			[NullableContext(2)]
			get
			{
				return this.UndoFunction(this.Context.Value as T?);
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060001D4 RID: 468 RVA: 0x0000808C File Offset: 0x0000628C
		[Nullable(new byte[] { 1, 0, 0 })]
		private Func<T?, T?> DoFunction
		{
			[return: Nullable(new byte[] { 1, 0, 0 })]
			get;
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x00008094 File Offset: 0x00006294
		[Nullable(new byte[] { 1, 0, 0 })]
		private Func<T?, T?> UndoFunction
		{
			[return: Nullable(new byte[] { 1, 0, 0 })]
			get;
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0000809C File Offset: 0x0000629C
		public ComplexValueTypeAction(IRef context, [Nullable(new byte[] { 1, 0, 0 })] Func<T?, T?> doFunction, [Nullable(new byte[] { 1, 0, 0 })] Func<T?, T?> undoFunction)
		{
			this.Context = context;
			this.DoFunction = doFunction;
			this.UndoFunction = undoFunction;
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x000080B9 File Offset: 0x000062B9
		public void DoAction()
		{
			this.Context.Value = this.Value;
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x000080CC File Offset: 0x000062CC
		public void UndoAction()
		{
			this.Context.Value = this.Original;
		}
	}
}

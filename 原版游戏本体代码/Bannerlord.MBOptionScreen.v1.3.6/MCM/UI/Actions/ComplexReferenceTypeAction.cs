using System;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x02000036 RID: 54
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class ComplexReferenceTypeAction<T> : IAction where T : class
	{
		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060001C9 RID: 457 RVA: 0x00007F81 File Offset: 0x00006181
		public IRef Context { get; }

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060001CA RID: 458 RVA: 0x00007F89 File Offset: 0x00006189
		[Nullable(2)]
		public object Value
		{
			[NullableContext(2)]
			get
			{
				this.DoFunction(this.Context.Value as T);
				return this.Context.Value;
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060001CB RID: 459 RVA: 0x00007FB6 File Offset: 0x000061B6
		[Nullable(2)]
		public object Original
		{
			[NullableContext(2)]
			get
			{
				this.UndoFunction(this.Context.Value as T);
				return this.Context.Value;
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060001CC RID: 460 RVA: 0x00007FE3 File Offset: 0x000061E3
		[Nullable(new byte[] { 1, 2 })]
		private Action<T> DoFunction
		{
			[return: Nullable(new byte[] { 1, 2 })]
			get;
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060001CD RID: 461 RVA: 0x00007FEB File Offset: 0x000061EB
		[Nullable(new byte[] { 1, 2 })]
		private Action<T> UndoFunction
		{
			[return: Nullable(new byte[] { 1, 2 })]
			get;
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00007FF3 File Offset: 0x000061F3
		public ComplexReferenceTypeAction(IRef context, [Nullable(new byte[] { 1, 2 })] Action<T> doFunction, [Nullable(new byte[] { 1, 2 })] Action<T> undoFunction)
		{
			this.Context = context;
			this.DoFunction = doFunction;
			this.UndoFunction = undoFunction;
		}

		// Token: 0x060001CF RID: 463 RVA: 0x00008010 File Offset: 0x00006210
		public void DoAction()
		{
			this.Context.Value = this.Value;
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00008023 File Offset: 0x00006223
		public void UndoAction()
		{
			this.Context.Value = this.Original;
		}
	}
}

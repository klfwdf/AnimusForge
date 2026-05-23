using System;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x02000039 RID: 57
	[NullableContext(2)]
	[Nullable(0)]
	internal sealed class SetSelectedIndexAction : IAction
	{
		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060001DE RID: 478 RVA: 0x000080DF File Offset: 0x000062DF
		[Nullable(1)]
		public IRef Context
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060001DF RID: 479 RVA: 0x000080E7 File Offset: 0x000062E7
		public object Value { get; }

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060001E0 RID: 480 RVA: 0x000080EF File Offset: 0x000062EF
		public object Original { get; }

		// Token: 0x060001E1 RID: 481 RVA: 0x000080F8 File Offset: 0x000062F8
		[NullableContext(1)]
		public SetSelectedIndexAction(IRef context, object value)
		{
			this.Context = context;
			this.Value = new SelectedIndexWrapper(value).SelectedIndex;
			this.Original = new SelectedIndexWrapper(context.Value).SelectedIndex;
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x0000814C File Offset: 0x0000634C
		public void DoAction()
		{
			SelectedIndexWrapper selectedIndexWrapper;
			selectedIndexWrapper..ctor(this.Context.Value);
			selectedIndexWrapper.SelectedIndex = ((int?)this.Value).GetValueOrDefault(-1);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x00008188 File Offset: 0x00006388
		public void UndoAction()
		{
			SelectedIndexWrapper selectedIndexWrapper;
			selectedIndexWrapper..ctor(this.Context.Value);
			selectedIndexWrapper.SelectedIndex = ((int?)this.Original).GetValueOrDefault(-1);
		}
	}
}

using System;
using System.Runtime.CompilerServices;
using MCM.Common;

namespace MCM.UI.Actions
{
	// Token: 0x02000038 RID: 56
	[NullableContext(2)]
	internal interface IAction
	{
		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060001D9 RID: 473
		[Nullable(1)]
		IRef Context
		{
			[NullableContext(1)]
			get;
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060001DA RID: 474
		object Original { get; }

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060001DB RID: 475
		object Value { get; }

		// Token: 0x060001DC RID: 476
		void DoAction();

		// Token: 0x060001DD RID: 477
		void UndoAction();
	}
}

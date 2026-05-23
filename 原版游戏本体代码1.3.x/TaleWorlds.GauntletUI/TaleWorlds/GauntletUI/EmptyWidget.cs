using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200003B RID: 59
	internal class EmptyWidget : Widget
	{
		// Token: 0x060003ED RID: 1005 RVA: 0x0000FC84 File Offset: 0x0000DE84
		public EmptyWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x0000FC8D File Offset: 0x0000DE8D
		protected override void OnUpdate(float dt)
		{
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x0000FC8F File Offset: 0x0000DE8F
		protected override void OnParallelUpdate(float dt)
		{
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x0000FC91 File Offset: 0x0000DE91
		protected override void OnLateUpdate(float dt)
		{
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x0000FC93 File Offset: 0x0000DE93
		public override void UpdateBrushes(float dt)
		{
		}
	}
}

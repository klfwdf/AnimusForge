using System;

namespace TaleWorlds.Library.Information
{
	// Token: 0x020000AD RID: 173
	public class TooltipTriggerVM : ViewModel
	{
		// Token: 0x06000687 RID: 1671 RVA: 0x00016B2C File Offset: 0x00014D2C
		public TooltipTriggerVM(Type linkedTooltipType, params object[] args)
		{
			this._linkedTooltipType = linkedTooltipType;
			this._args = args;
		}

		// Token: 0x06000688 RID: 1672 RVA: 0x00016B42 File Offset: 0x00014D42
		public void ExecuteBeginHint()
		{
			InformationManager.ShowTooltip(this._linkedTooltipType, this._args);
		}

		// Token: 0x06000689 RID: 1673 RVA: 0x00016B55 File Offset: 0x00014D55
		public void ExecuteEndHint()
		{
			InformationManager.HideTooltip();
		}

		// Token: 0x040001F4 RID: 500
		private Type _linkedTooltipType;

		// Token: 0x040001F5 RID: 501
		private object[] _args;
	}
}

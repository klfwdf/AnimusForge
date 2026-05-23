using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Information
{
	// Token: 0x02000015 RID: 21
	public class HintViewModel : ViewModel
	{
		// Token: 0x06000114 RID: 276 RVA: 0x00004548 File Offset: 0x00002748
		public HintViewModel()
		{
			this.HintText = TextObject.GetEmpty();
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000455B File Offset: 0x0000275B
		public HintViewModel(TextObject hintText, string uniqueName = null)
		{
			this.HintText = hintText;
			this._uniqueName = uniqueName;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00004571 File Offset: 0x00002771
		public void ExecuteBeginHint()
		{
			if (!TextObject.IsNullOrEmpty(this.HintText))
			{
				MBInformationManager.ShowHint(this.HintText.ToString());
			}
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00004590 File Offset: 0x00002790
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x04000075 RID: 117
		public TextObject HintText;

		// Token: 0x04000076 RID: 118
		private readonly string _uniqueName;
	}
}

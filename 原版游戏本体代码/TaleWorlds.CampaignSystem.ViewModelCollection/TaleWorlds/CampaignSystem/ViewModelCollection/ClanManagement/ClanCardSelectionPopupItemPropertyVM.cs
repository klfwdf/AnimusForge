using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200011C RID: 284
	public class ClanCardSelectionPopupItemPropertyVM : ViewModel
	{
		// Token: 0x06001A0C RID: 6668 RVA: 0x0006250F File Offset: 0x0006070F
		public ClanCardSelectionPopupItemPropertyVM(in ClanCardSelectionItemPropertyInfo info)
		{
			this._titleText = info.Title;
			this._valueText = info.Value;
		}

		// Token: 0x06001A0D RID: 6669 RVA: 0x00062530 File Offset: 0x00060730
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject titleText = this._titleText;
			this.Title = ((titleText != null) ? titleText.ToString() : null) ?? string.Empty;
			TextObject valueText = this._valueText;
			this.Value = ((valueText != null) ? valueText.ToString() : null) ?? string.Empty;
		}

		// Token: 0x170008C0 RID: 2240
		// (get) Token: 0x06001A0E RID: 6670 RVA: 0x00062585 File Offset: 0x00060785
		// (set) Token: 0x06001A0F RID: 6671 RVA: 0x0006258D File Offset: 0x0006078D
		[DataSourceProperty]
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				if (value != this._title)
				{
					this._title = value;
					base.OnPropertyChangedWithValue<string>(value, "Title");
				}
			}
		}

		// Token: 0x170008C1 RID: 2241
		// (get) Token: 0x06001A10 RID: 6672 RVA: 0x000625B0 File Offset: 0x000607B0
		// (set) Token: 0x06001A11 RID: 6673 RVA: 0x000625B8 File Offset: 0x000607B8
		[DataSourceProperty]
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				if (value != this._value)
				{
					this._value = value;
					base.OnPropertyChangedWithValue<string>(value, "Value");
				}
			}
		}

		// Token: 0x04000BFC RID: 3068
		private readonly TextObject _titleText;

		// Token: 0x04000BFD RID: 3069
		private readonly TextObject _valueText;

		// Token: 0x04000BFE RID: 3070
		private string _title;

		// Token: 0x04000BFF RID: 3071
		private string _value;
	}
}

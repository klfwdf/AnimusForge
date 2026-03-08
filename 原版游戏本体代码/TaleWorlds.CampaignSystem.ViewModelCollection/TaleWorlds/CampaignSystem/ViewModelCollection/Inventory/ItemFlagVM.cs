using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x0200008F RID: 143
	public class ItemFlagVM : ViewModel
	{
		// Token: 0x06000C64 RID: 3172 RVA: 0x0003285B File Offset: 0x00030A5B
		public ItemFlagVM(string iconName, TextObject hint)
		{
			this.Icon = this.GetIconPath(iconName);
			this.Hint = new HintViewModel(hint, null);
		}

		// Token: 0x06000C65 RID: 3173 RVA: 0x00032880 File Offset: 0x00030A80
		private string GetIconPath(string iconName)
		{
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, "GetIconPath");
			mbstringBuilder.Append<string>("<img src=\"SPGeneral\\");
			mbstringBuilder.Append<string>(iconName);
			mbstringBuilder.Append<string>("\"/>");
			return mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x170003FC RID: 1020
		// (get) Token: 0x06000C66 RID: 3174 RVA: 0x000328CD File Offset: 0x00030ACD
		// (set) Token: 0x06000C67 RID: 3175 RVA: 0x000328D5 File Offset: 0x00030AD5
		[DataSourceProperty]
		public string Icon
		{
			get
			{
				return this._icon;
			}
			set
			{
				if (value != this._icon)
				{
					this._icon = value;
					base.OnPropertyChangedWithValue<string>(value, "Icon");
				}
			}
		}

		// Token: 0x170003FD RID: 1021
		// (get) Token: 0x06000C68 RID: 3176 RVA: 0x000328F8 File Offset: 0x00030AF8
		// (set) Token: 0x06000C69 RID: 3177 RVA: 0x00032900 File Offset: 0x00030B00
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x04000588 RID: 1416
		private string _icon;

		// Token: 0x04000589 RID: 1417
		private HintViewModel _hint;
	}
}

using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000CC RID: 204
	public class EncyclopediaSearchResultVM : ViewModel
	{
		// Token: 0x17000652 RID: 1618
		// (get) Token: 0x06001353 RID: 4947 RVA: 0x0004D8EE File Offset: 0x0004BAEE
		// (set) Token: 0x06001354 RID: 4948 RVA: 0x0004D8F6 File Offset: 0x0004BAF6
		public string OrgNameText { get; private set; }

		// Token: 0x06001355 RID: 4949 RVA: 0x0004D900 File Offset: 0x0004BB00
		public EncyclopediaSearchResultVM(EncyclopediaListItem source, string searchedText, int matchStartIndex)
		{
			this.MatchStartIndex = matchStartIndex;
			this.LinkId = source.Id;
			this.PageType = source.TypeName;
			this.OrgNameText = source.Name;
			this._nameText = source.Name;
			this.UpdateSearchedText(searchedText);
		}

		// Token: 0x06001356 RID: 4950 RVA: 0x0004D95C File Offset: 0x0004BB5C
		public void UpdateSearchedText(string searchedText)
		{
			this._searchedText = searchedText;
			if (string.IsNullOrEmpty(this.OrgNameText))
			{
				return;
			}
			int num = this.OrgNameText.IndexOf(this._searchedText, StringComparison.InvariantCultureIgnoreCase);
			if (num < 0)
			{
				return;
			}
			int num2 = MBMath.ClampInt(this._searchedText.Length, 0, this.OrgNameText.Length - num);
			if (num2 == 0)
			{
				return;
			}
			string text = this.OrgNameText.Substring(num, num2);
			if (!string.IsNullOrEmpty(text))
			{
				this.NameText = this.OrgNameText.Replace(text, "<a>" + text + "</a>");
			}
		}

		// Token: 0x06001357 RID: 4951 RVA: 0x0004D9F1 File Offset: 0x0004BBF1
		public void Execute()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this.PageType, this.LinkId);
		}

		// Token: 0x17000653 RID: 1619
		// (get) Token: 0x06001358 RID: 4952 RVA: 0x0004DA0E File Offset: 0x0004BC0E
		// (set) Token: 0x06001359 RID: 4953 RVA: 0x0004DA16 File Offset: 0x0004BC16
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (this._nameText != value)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x040008D8 RID: 2264
		private string _searchedText;

		// Token: 0x040008DA RID: 2266
		public readonly int MatchStartIndex;

		// Token: 0x040008DB RID: 2267
		public string LinkId = "";

		// Token: 0x040008DC RID: 2268
		public string PageType;

		// Token: 0x040008DD RID: 2269
		public string _nameText;
	}
}

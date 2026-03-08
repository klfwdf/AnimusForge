using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000CA RID: 202
	public class EncyclopediaLinkVM : ViewModel
	{
		// Token: 0x17000641 RID: 1601
		// (get) Token: 0x0600131A RID: 4890 RVA: 0x0004CD90 File Offset: 0x0004AF90
		// (set) Token: 0x0600131B RID: 4891 RVA: 0x0004CD98 File Offset: 0x0004AF98
		[DataSourceProperty]
		public string ActiveLink
		{
			get
			{
				return this._activeLink;
			}
			set
			{
				if (this._activeLink != value)
				{
					this._activeLink = value;
					base.OnPropertyChangedWithValue<string>(value, "ActiveLink");
				}
			}
		}

		// Token: 0x0600131C RID: 4892 RVA: 0x0004CDBB File Offset: 0x0004AFBB
		public void ExecuteActiveLink()
		{
			if (!string.IsNullOrEmpty(this.ActiveLink))
			{
				Campaign.Current.EncyclopediaManager.GoToLink(this.ActiveLink);
			}
		}

		// Token: 0x040008C3 RID: 2243
		private string _activeLink;
	}
}

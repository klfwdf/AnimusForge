using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000C9 RID: 201
	public class EncyclopediaHomeVM : EncyclopediaPageVM
	{
		// Token: 0x0600130F RID: 4879 RVA: 0x0004CBD8 File Offset: 0x0004ADD8
		public EncyclopediaHomeVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this.Lists = new MBBindingList<ListTypeVM>();
			foreach (EncyclopediaPage encyclopediaPage in from p in Campaign.Current.EncyclopediaManager.GetEncyclopediaPages()
				orderby p.HomePageOrderIndex
				select p)
			{
				if (encyclopediaPage.IsRelevant())
				{
					this.Lists.Add(new ListTypeVM(encyclopediaPage));
				}
			}
			this.RefreshValues();
		}

		// Token: 0x06001310 RID: 4880 RVA: 0x0004CC7C File Offset: 0x0004AE7C
		public override void Refresh()
		{
			base.Refresh();
			this.RefreshValues();
		}

		// Token: 0x06001311 RID: 4881 RVA: 0x0004CC8C File Offset: 0x0004AE8C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._baseName = GameTexts.FindText("str_encyclopedia_name", null).ToString();
			this.HomeTitleText = GameTexts.FindText("str_encyclopedia_name", null).ToString();
			this.Lists.ApplyActionOnAllItems(delegate(ListTypeVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06001312 RID: 4882 RVA: 0x0004CCF5 File Offset: 0x0004AEF5
		public override string GetNavigationBarURL()
		{
			return GameTexts.FindText("str_encyclopedia_home", null).ToString() + " \\";
		}

		// Token: 0x06001313 RID: 4883 RVA: 0x0004CD11 File Offset: 0x0004AF11
		public override string GetName()
		{
			return this._baseName;
		}

		// Token: 0x1700063E RID: 1598
		// (get) Token: 0x06001314 RID: 4884 RVA: 0x0004CD19 File Offset: 0x0004AF19
		// (set) Token: 0x06001315 RID: 4885 RVA: 0x0004CD21 File Offset: 0x0004AF21
		[DataSourceProperty]
		public bool IsListActive
		{
			get
			{
				return this._isListActive;
			}
			set
			{
				if (value != this._isListActive)
				{
					this._isListActive = value;
					base.OnPropertyChangedWithValue(value, "IsListActive");
				}
			}
		}

		// Token: 0x1700063F RID: 1599
		// (get) Token: 0x06001316 RID: 4886 RVA: 0x0004CD3F File Offset: 0x0004AF3F
		// (set) Token: 0x06001317 RID: 4887 RVA: 0x0004CD47 File Offset: 0x0004AF47
		[DataSourceProperty]
		public string HomeTitleText
		{
			get
			{
				return this._homeTitleText;
			}
			set
			{
				if (value != this._homeTitleText)
				{
					this._homeTitleText = value;
					base.OnPropertyChangedWithValue<string>(value, "HomeTitleText");
				}
			}
		}

		// Token: 0x17000640 RID: 1600
		// (get) Token: 0x06001318 RID: 4888 RVA: 0x0004CD6A File Offset: 0x0004AF6A
		// (set) Token: 0x06001319 RID: 4889 RVA: 0x0004CD72 File Offset: 0x0004AF72
		[DataSourceProperty]
		public MBBindingList<ListTypeVM> Lists
		{
			get
			{
				return this._lists;
			}
			set
			{
				if (value != this._lists)
				{
					this._lists = value;
					base.OnPropertyChangedWithValue<MBBindingList<ListTypeVM>>(value, "Lists");
				}
			}
		}

		// Token: 0x040008BF RID: 2239
		private string _baseName;

		// Token: 0x040008C0 RID: 2240
		private MBBindingList<ListTypeVM> _lists;

		// Token: 0x040008C1 RID: 2241
		private bool _isListActive;

		// Token: 0x040008C2 RID: 2242
		private string _homeTitleText;
	}
}

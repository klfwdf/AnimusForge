using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages
{
	// Token: 0x020000D0 RID: 208
	[EncyclopediaViewModel(typeof(Concept))]
	public class EncyclopediaConceptPageVM : EncyclopediaContentPageVM
	{
		// Token: 0x0600139F RID: 5023 RVA: 0x0004E884 File Offset: 0x0004CA84
		public EncyclopediaConceptPageVM(EncyclopediaPageArgs args)
			: base(args)
		{
			this._concept = base.Obj as Concept;
			Concept.SetConceptTextLinks();
			base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(this._concept);
			this.RefreshValues();
			this.Refresh();
		}

		// Token: 0x060013A0 RID: 5024 RVA: 0x0004E8DA File Offset: 0x0004CADA
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = this._concept.Title.ToString();
			this.DescriptionText = this._concept.Description.ToString();
			base.UpdateBookmarkHintText();
		}

		// Token: 0x060013A1 RID: 5025 RVA: 0x0004E914 File Offset: 0x0004CB14
		public override void Refresh()
		{
			base.IsLoadingOver = false;
			base.IsLoadingOver = true;
		}

		// Token: 0x060013A2 RID: 5026 RVA: 0x0004E924 File Offset: 0x0004CB24
		public override string GetName()
		{
			return this._concept.Title.ToString();
		}

		// Token: 0x060013A3 RID: 5027 RVA: 0x0004E936 File Offset: 0x0004CB36
		public void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x060013A4 RID: 5028 RVA: 0x0004E948 File Offset: 0x0004CB48
		public override string GetNavigationBarURL()
		{
			return HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home", null).ToString()) + " \\ " + HyperlinkTexts.GetGenericHyperlinkText("ListPage-Concept", GameTexts.FindText("str_encyclopedia_concepts", null).ToString()) + " \\ " + this.GetName();
		}

		// Token: 0x060013A5 RID: 5029 RVA: 0x0004E9B0 File Offset: 0x0004CBB0
		public override void ExecuteSwitchBookmarkedState()
		{
			base.ExecuteSwitchBookmarkedState();
			if (base.IsBookmarked)
			{
				Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(this._concept);
				return;
			}
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(this._concept);
		}

		// Token: 0x17000672 RID: 1650
		// (get) Token: 0x060013A6 RID: 5030 RVA: 0x0004EA00 File Offset: 0x0004CC00
		// (set) Token: 0x060013A7 RID: 5031 RVA: 0x0004EA08 File Offset: 0x0004CC08
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x17000673 RID: 1651
		// (get) Token: 0x060013A8 RID: 5032 RVA: 0x0004EA2B File Offset: 0x0004CC2B
		// (set) Token: 0x060013A9 RID: 5033 RVA: 0x0004EA33 File Offset: 0x0004CC33
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x040008FF RID: 2303
		private Concept _concept;

		// Token: 0x04000900 RID: 2304
		private string _titleText;

		// Token: 0x04000901 RID: 2305
		private string _descriptionText;
	}
}

using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters
{
	// Token: 0x02000130 RID: 304
	public class ClanSupporterGroupVM : ViewModel
	{
		// Token: 0x06001C66 RID: 7270 RVA: 0x00068E4B File Offset: 0x0006704B
		public ClanSupporterGroupVM(TextObject groupName, float influenceBonus, Action<ClanSupporterGroupVM> onSelection)
		{
			this._groupNameText = groupName;
			this._influenceBonus = influenceBonus;
			this._onSelection = onSelection;
			this.Supporters = new MBBindingList<ClanSupporterItemVM>();
			this.RefreshValues();
		}

		// Token: 0x06001C67 RID: 7271 RVA: 0x00068E79 File Offset: 0x00067079
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Refresh();
		}

		// Token: 0x06001C68 RID: 7272 RVA: 0x00068E88 File Offset: 0x00067088
		public void AddSupporter(Hero hero)
		{
			if (!this.Supporters.Any((ClanSupporterItemVM x) => x.Hero.Hero == hero))
			{
				this.Supporters.Add(new ClanSupporterItemVM(hero));
			}
		}

		// Token: 0x06001C69 RID: 7273 RVA: 0x00068ED4 File Offset: 0x000670D4
		public void Refresh()
		{
			TextObject textObject = GameTexts.FindText("str_amount_with_influence_icon", null);
			this.TotalInfluenceBonus = (float)this.Supporters.Count * this._influenceBonus;
			TextObject textObject2 = GameTexts.FindText("str_plus_with_number", null);
			textObject2.SetTextVariable("NUMBER", this.TotalInfluenceBonus.ToString("F2"));
			textObject.SetTextVariable("AMOUNT", textObject2.ToString());
			textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
			this.TotalInfluence = textObject.ToString();
			TextObject textObject3 = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null);
			textObject3.SetTextVariable("RANK", this._groupNameText.ToString());
			textObject3.SetTextVariable("NUMBER", this.Supporters.Count);
			this.Name = textObject3.ToString();
			TextObject textObject4 = new TextObject("{=cZCOa00c}{SUPPORTER_RANK} Supporters ({NUM})", null);
			textObject4.SetTextVariable("SUPPORTER_RANK", this._groupNameText.ToString());
			textObject4.SetTextVariable("NUM", this.Supporters.Count);
			this.TitleText = textObject4.ToString();
			TextObject textObject5 = new TextObject("{=jdbT6nc9}Each {SUPPORTER_RANK} supporter provides {INFLUENCE_BONUS} per day.", null);
			textObject5.SetTextVariable("SUPPORTER_RANK", this._groupNameText.ToString());
			textObject5.SetTextVariable("INFLUENCE_BONUS", this._influenceBonus.ToString("F2") + "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
			this.InfluenceBonusDescription = textObject5.ToString();
		}

		// Token: 0x06001C6A RID: 7274 RVA: 0x00069046 File Offset: 0x00067246
		public void ExecuteSelect()
		{
			Action<ClanSupporterGroupVM> onSelection = this._onSelection;
			if (onSelection == null)
			{
				return;
			}
			onSelection(this);
		}

		// Token: 0x170009AB RID: 2475
		// (get) Token: 0x06001C6B RID: 7275 RVA: 0x00069059 File Offset: 0x00067259
		// (set) Token: 0x06001C6C RID: 7276 RVA: 0x00069061 File Offset: 0x00067261
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

		// Token: 0x170009AC RID: 2476
		// (get) Token: 0x06001C6D RID: 7277 RVA: 0x00069084 File Offset: 0x00067284
		// (set) Token: 0x06001C6E RID: 7278 RVA: 0x0006908C File Offset: 0x0006728C
		[DataSourceProperty]
		public float TotalInfluenceBonus
		{
			get
			{
				return this._totalInfluenceBonus;
			}
			private set
			{
				if (value != this._totalInfluenceBonus)
				{
					this._totalInfluenceBonus = value;
					base.OnPropertyChangedWithValue(value, "TotalInfluenceBonus");
				}
			}
		}

		// Token: 0x170009AD RID: 2477
		// (get) Token: 0x06001C6F RID: 7279 RVA: 0x000690AA File Offset: 0x000672AA
		// (set) Token: 0x06001C70 RID: 7280 RVA: 0x000690B2 File Offset: 0x000672B2
		[DataSourceProperty]
		public string InfluenceBonusDescription
		{
			get
			{
				return this._influenceBonusDescription;
			}
			set
			{
				if (value != this._influenceBonusDescription)
				{
					this._influenceBonusDescription = value;
					base.OnPropertyChangedWithValue<string>(value, "InfluenceBonusDescription");
				}
			}
		}

		// Token: 0x170009AE RID: 2478
		// (get) Token: 0x06001C71 RID: 7281 RVA: 0x000690D5 File Offset: 0x000672D5
		// (set) Token: 0x06001C72 RID: 7282 RVA: 0x000690DD File Offset: 0x000672DD
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x170009AF RID: 2479
		// (get) Token: 0x06001C73 RID: 7283 RVA: 0x00069100 File Offset: 0x00067300
		// (set) Token: 0x06001C74 RID: 7284 RVA: 0x00069108 File Offset: 0x00067308
		[DataSourceProperty]
		public string TotalInfluence
		{
			get
			{
				return this._totalInfluence;
			}
			set
			{
				if (value != this._totalInfluence)
				{
					this._totalInfluence = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalInfluence");
				}
			}
		}

		// Token: 0x170009B0 RID: 2480
		// (get) Token: 0x06001C75 RID: 7285 RVA: 0x0006912B File Offset: 0x0006732B
		// (set) Token: 0x06001C76 RID: 7286 RVA: 0x00069133 File Offset: 0x00067333
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170009B1 RID: 2481
		// (get) Token: 0x06001C77 RID: 7287 RVA: 0x00069151 File Offset: 0x00067351
		// (set) Token: 0x06001C78 RID: 7288 RVA: 0x00069159 File Offset: 0x00067359
		[DataSourceProperty]
		public MBBindingList<ClanSupporterItemVM> Supporters
		{
			get
			{
				return this._supporters;
			}
			set
			{
				if (value != this._supporters)
				{
					this._supporters = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanSupporterItemVM>>(value, "Supporters");
				}
			}
		}

		// Token: 0x04000D40 RID: 3392
		private TextObject _groupNameText;

		// Token: 0x04000D41 RID: 3393
		private float _influenceBonus;

		// Token: 0x04000D42 RID: 3394
		private Action<ClanSupporterGroupVM> _onSelection;

		// Token: 0x04000D43 RID: 3395
		private string _titleText;

		// Token: 0x04000D44 RID: 3396
		private string _influenceBonusDescription;

		// Token: 0x04000D45 RID: 3397
		private string _name;

		// Token: 0x04000D46 RID: 3398
		private string _totalInfluence;

		// Token: 0x04000D47 RID: 3399
		private bool _isSelected;

		// Token: 0x04000D48 RID: 3400
		private MBBindingList<ClanSupporterItemVM> _supporters;

		// Token: 0x04000D49 RID: 3401
		private float _totalInfluenceBonus;
	}
}

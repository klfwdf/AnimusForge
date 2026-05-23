using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000125 RID: 293
	public class ClanFinanceIncomeItemBaseVM : ViewModel
	{
		// Token: 0x170008E8 RID: 2280
		// (get) Token: 0x06001A73 RID: 6771 RVA: 0x0006351B File Offset: 0x0006171B
		// (set) Token: 0x06001A74 RID: 6772 RVA: 0x00063523 File Offset: 0x00061723
		public IncomeTypes IncomeTypeAsEnum
		{
			get
			{
				return this._incomeTypeAsEnum;
			}
			protected set
			{
				if (value != this._incomeTypeAsEnum)
				{
					this._incomeTypeAsEnum = value;
					this.IncomeType = (int)value;
				}
			}
		}

		// Token: 0x06001A75 RID: 6773 RVA: 0x0006353C File Offset: 0x0006173C
		protected ClanFinanceIncomeItemBaseVM(Action<ClanFinanceIncomeItemBaseVM> onSelection, Action onRefresh)
		{
			this._onSelection = onSelection;
			this._onRefresh = onRefresh;
		}

		// Token: 0x06001A76 RID: 6774 RVA: 0x0006355D File Offset: 0x0006175D
		protected virtual void PopulateStatsList()
		{
		}

		// Token: 0x06001A77 RID: 6775 RVA: 0x0006355F File Offset: 0x0006175F
		protected virtual void PopulateActionList()
		{
		}

		// Token: 0x06001A78 RID: 6776 RVA: 0x00063561 File Offset: 0x00061761
		public void OnIncomeSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x06001A79 RID: 6777 RVA: 0x00063570 File Offset: 0x00061770
		protected string DetermineIncomeText(int incomeAmount)
		{
			if (incomeAmount == 0)
			{
				return GameTexts.FindText("str_clan_finance_value_zero", null).ToString();
			}
			GameTexts.SetVariable("IS_POSITIVE", (this.Income > 0) ? 1 : 0);
			GameTexts.SetVariable("NUMBER", MathF.Abs(this.Income));
			return GameTexts.FindText("str_clan_finance_value", null).ToString();
		}

		// Token: 0x170008E9 RID: 2281
		// (get) Token: 0x06001A7A RID: 6778 RVA: 0x000635CD File Offset: 0x000617CD
		// (set) Token: 0x06001A7B RID: 6779 RVA: 0x000635D5 File Offset: 0x000617D5
		[DataSourceProperty]
		public MBBindingList<SelectableItemPropertyVM> ItemProperties
		{
			get
			{
				return this._itemProperties;
			}
			set
			{
				if (value != this._itemProperties)
				{
					this._itemProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<SelectableItemPropertyVM>>(value, "ItemProperties");
				}
			}
		}

		// Token: 0x170008EA RID: 2282
		// (get) Token: 0x06001A7C RID: 6780 RVA: 0x000635F3 File Offset: 0x000617F3
		// (set) Token: 0x06001A7D RID: 6781 RVA: 0x000635FB File Offset: 0x000617FB
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

		// Token: 0x170008EB RID: 2283
		// (get) Token: 0x06001A7E RID: 6782 RVA: 0x0006361E File Offset: 0x0006181E
		// (set) Token: 0x06001A7F RID: 6783 RVA: 0x00063626 File Offset: 0x00061826
		[DataSourceProperty]
		public string Location
		{
			get
			{
				return this._location;
			}
			set
			{
				if (value != this._location)
				{
					this._location = value;
					base.OnPropertyChangedWithValue<string>(value, "Location");
				}
			}
		}

		// Token: 0x170008EC RID: 2284
		// (get) Token: 0x06001A80 RID: 6784 RVA: 0x00063649 File Offset: 0x00061849
		// (set) Token: 0x06001A81 RID: 6785 RVA: 0x00063651 File Offset: 0x00061851
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

		// Token: 0x170008ED RID: 2285
		// (get) Token: 0x06001A82 RID: 6786 RVA: 0x0006366F File Offset: 0x0006186F
		// (set) Token: 0x06001A83 RID: 6787 RVA: 0x00063677 File Offset: 0x00061877
		[DataSourceProperty]
		public string IncomeValueText
		{
			get
			{
				return this._incomeValueText;
			}
			set
			{
				if (value != this._incomeValueText)
				{
					this._incomeValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "IncomeValueText");
				}
			}
		}

		// Token: 0x170008EE RID: 2286
		// (get) Token: 0x06001A84 RID: 6788 RVA: 0x0006369A File Offset: 0x0006189A
		// (set) Token: 0x06001A85 RID: 6789 RVA: 0x000636A2 File Offset: 0x000618A2
		[DataSourceProperty]
		public string ImageName
		{
			get
			{
				return this._imageName;
			}
			set
			{
				if (value != this._imageName)
				{
					this._imageName = value;
					base.OnPropertyChangedWithValue<string>(value, "ImageName");
				}
			}
		}

		// Token: 0x170008EF RID: 2287
		// (get) Token: 0x06001A86 RID: 6790 RVA: 0x000636C5 File Offset: 0x000618C5
		// (set) Token: 0x06001A87 RID: 6791 RVA: 0x000636CD File Offset: 0x000618CD
		[DataSourceProperty]
		public int Income
		{
			get
			{
				return this._income;
			}
			set
			{
				if (value != this._income)
				{
					this._income = value;
					base.OnPropertyChangedWithValue(value, "Income");
				}
			}
		}

		// Token: 0x170008F0 RID: 2288
		// (get) Token: 0x06001A88 RID: 6792 RVA: 0x000636EB File Offset: 0x000618EB
		// (set) Token: 0x06001A89 RID: 6793 RVA: 0x000636F3 File Offset: 0x000618F3
		[DataSourceProperty]
		public ImageIdentifierVM Visual
		{
			get
			{
				return this._visual;
			}
			set
			{
				if (value != this._visual)
				{
					this._visual = value;
					base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x170008F1 RID: 2289
		// (get) Token: 0x06001A8A RID: 6794 RVA: 0x00063711 File Offset: 0x00061911
		// (set) Token: 0x06001A8B RID: 6795 RVA: 0x00063719 File Offset: 0x00061919
		[DataSourceProperty]
		public int IncomeType
		{
			get
			{
				return this._incomeType;
			}
			set
			{
				if (value != this._incomeType)
				{
					this._incomeType = value;
					base.OnPropertyChangedWithValue(value, "IncomeType");
				}
			}
		}

		// Token: 0x04000C53 RID: 3155
		protected Action _onRefresh;

		// Token: 0x04000C54 RID: 3156
		protected Action<ClanFinanceIncomeItemBaseVM> _onSelection;

		// Token: 0x04000C55 RID: 3157
		protected IncomeTypes _incomeTypeAsEnum;

		// Token: 0x04000C56 RID: 3158
		private int _incomeType;

		// Token: 0x04000C57 RID: 3159
		private string _name;

		// Token: 0x04000C58 RID: 3160
		private string _location;

		// Token: 0x04000C59 RID: 3161
		private string _incomeValueText;

		// Token: 0x04000C5A RID: 3162
		private string _imageName;

		// Token: 0x04000C5B RID: 3163
		private int _income;

		// Token: 0x04000C5C RID: 3164
		private bool _isSelected;

		// Token: 0x04000C5D RID: 3165
		private ImageIdentifierVM _visual;

		// Token: 0x04000C5E RID: 3166
		private MBBindingList<SelectableItemPropertyVM> _itemProperties = new MBBindingList<SelectableItemPropertyVM>();
	}
}

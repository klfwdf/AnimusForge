using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection
{
	// Token: 0x020000A0 RID: 160
	public class TroopSelectionItemVM : ViewModel
	{
		// Token: 0x17000502 RID: 1282
		// (get) Token: 0x06000F86 RID: 3974 RVA: 0x0004053A File Offset: 0x0003E73A
		// (set) Token: 0x06000F87 RID: 3975 RVA: 0x00040542 File Offset: 0x0003E742
		public TroopRosterElement Troop { get; private set; }

		// Token: 0x06000F88 RID: 3976 RVA: 0x0004054C File Offset: 0x0003E74C
		public TroopSelectionItemVM(TroopRosterElement troop, Action<TroopSelectionItemVM> onAdd, Action<TroopSelectionItemVM> onRemove)
		{
			this._onAdd = onAdd;
			this._onRemove = onRemove;
			this.Troop = troop;
			this.MaxAmount = this.Troop.Number - this.Troop.WoundedNumber;
			this.Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(troop.Character, false));
			this.Name = troop.Character.Name.ToString();
			this.TierIconData = CampaignUIHelper.GetCharacterTierData(this.Troop.Character, false);
			this.TypeIconData = CampaignUIHelper.GetCharacterTypeData(this.Troop.Character, false);
			this.IsTroopHero = this.Troop.Character.IsHero;
			this.HeroHealthPercent = (this.Troop.Character.IsHero ? MathF.Ceiling((float)this.Troop.Character.HeroObject.HitPoints / (float)this.Troop.Character.MaxHitPoints() * 100f) : 0);
		}

		// Token: 0x06000F89 RID: 3977 RVA: 0x00040656 File Offset: 0x0003E856
		public void ExecuteAdd()
		{
			Action<TroopSelectionItemVM> onAdd = this._onAdd;
			if (onAdd == null)
			{
				return;
			}
			onAdd.DynamicInvokeWithLog(new object[] { this });
		}

		// Token: 0x06000F8A RID: 3978 RVA: 0x00040673 File Offset: 0x0003E873
		public void ExecuteRemove()
		{
			Action<TroopSelectionItemVM> onRemove = this._onRemove;
			if (onRemove == null)
			{
				return;
			}
			onRemove.DynamicInvokeWithLog(new object[] { this });
		}

		// Token: 0x06000F8B RID: 3979 RVA: 0x00040690 File Offset: 0x0003E890
		private void UpdateAmountText()
		{
			GameTexts.SetVariable("LEFT", this.CurrentAmount);
			GameTexts.SetVariable("RIGHT", this.MaxAmount);
			this.AmountText = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
		}

		// Token: 0x06000F8C RID: 3980 RVA: 0x000406C8 File Offset: 0x0003E8C8
		public void ExecuteLink()
		{
			if (this.Troop.Character != null)
			{
				EncyclopediaManager encyclopediaManager = Campaign.Current.EncyclopediaManager;
				Hero heroObject = this.Troop.Character.HeroObject;
				encyclopediaManager.GoToLink(((heroObject != null) ? heroObject.EncyclopediaLink : null) ?? this.Troop.Character.EncyclopediaLink);
			}
		}

		// Token: 0x17000503 RID: 1283
		// (get) Token: 0x06000F8D RID: 3981 RVA: 0x00040721 File Offset: 0x0003E921
		// (set) Token: 0x06000F8E RID: 3982 RVA: 0x00040729 File Offset: 0x0003E929
		[DataSourceProperty]
		public int MaxAmount
		{
			get
			{
				return this._maxAmount;
			}
			set
			{
				if (value != this._maxAmount)
				{
					this._maxAmount = value;
					base.OnPropertyChangedWithValue(value, "MaxAmount");
					this.UpdateAmountText();
				}
			}
		}

		// Token: 0x17000504 RID: 1284
		// (get) Token: 0x06000F8F RID: 3983 RVA: 0x0004074D File Offset: 0x0003E94D
		// (set) Token: 0x06000F90 RID: 3984 RVA: 0x00040755 File Offset: 0x0003E955
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

		// Token: 0x17000505 RID: 1285
		// (get) Token: 0x06000F91 RID: 3985 RVA: 0x00040773 File Offset: 0x0003E973
		// (set) Token: 0x06000F92 RID: 3986 RVA: 0x0004077B File Offset: 0x0003E97B
		[DataSourceProperty]
		public bool IsRosterFull
		{
			get
			{
				return this._isRosterFull;
			}
			set
			{
				if (value != this._isRosterFull)
				{
					this._isRosterFull = value;
					base.OnPropertyChangedWithValue(value, "IsRosterFull");
				}
			}
		}

		// Token: 0x17000506 RID: 1286
		// (get) Token: 0x06000F93 RID: 3987 RVA: 0x00040799 File Offset: 0x0003E999
		// (set) Token: 0x06000F94 RID: 3988 RVA: 0x000407A1 File Offset: 0x0003E9A1
		[DataSourceProperty]
		public bool IsTroopHero
		{
			get
			{
				return this._isTroopHero;
			}
			set
			{
				if (value != this._isTroopHero)
				{
					this._isTroopHero = value;
					base.OnPropertyChangedWithValue(value, "IsTroopHero");
				}
			}
		}

		// Token: 0x17000507 RID: 1287
		// (get) Token: 0x06000F95 RID: 3989 RVA: 0x000407BF File Offset: 0x0003E9BF
		// (set) Token: 0x06000F96 RID: 3990 RVA: 0x000407C7 File Offset: 0x0003E9C7
		[DataSourceProperty]
		public bool IsLocked
		{
			get
			{
				return this._isLocked;
			}
			set
			{
				if (value != this._isLocked)
				{
					this._isLocked = value;
					base.OnPropertyChangedWithValue(value, "IsLocked");
				}
			}
		}

		// Token: 0x17000508 RID: 1288
		// (get) Token: 0x06000F97 RID: 3991 RVA: 0x000407E5 File Offset: 0x0003E9E5
		// (set) Token: 0x06000F98 RID: 3992 RVA: 0x000407ED File Offset: 0x0003E9ED
		[DataSourceProperty]
		public int CurrentAmount
		{
			get
			{
				return this._currentAmount;
			}
			set
			{
				if (value != this._currentAmount)
				{
					this._currentAmount = value;
					base.OnPropertyChangedWithValue(value, "CurrentAmount");
					this.IsSelected = value > 0;
					this.UpdateAmountText();
				}
			}
		}

		// Token: 0x17000509 RID: 1289
		// (get) Token: 0x06000F99 RID: 3993 RVA: 0x0004081B File Offset: 0x0003EA1B
		// (set) Token: 0x06000F9A RID: 3994 RVA: 0x00040823 File Offset: 0x0003EA23
		[DataSourceProperty]
		public int HeroHealthPercent
		{
			get
			{
				return this._heroHealthPercent;
			}
			set
			{
				if (value != this._heroHealthPercent)
				{
					this._heroHealthPercent = value;
					base.OnPropertyChangedWithValue(value, "HeroHealthPercent");
				}
			}
		}

		// Token: 0x1700050A RID: 1290
		// (get) Token: 0x06000F9B RID: 3995 RVA: 0x00040841 File Offset: 0x0003EA41
		// (set) Token: 0x06000F9C RID: 3996 RVA: 0x00040849 File Offset: 0x0003EA49
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

		// Token: 0x1700050B RID: 1291
		// (get) Token: 0x06000F9D RID: 3997 RVA: 0x0004086C File Offset: 0x0003EA6C
		// (set) Token: 0x06000F9E RID: 3998 RVA: 0x00040874 File Offset: 0x0003EA74
		[DataSourceProperty]
		public string AmountText
		{
			get
			{
				return this._amountText;
			}
			set
			{
				if (value != this._amountText)
				{
					this._amountText = value;
					base.OnPropertyChangedWithValue<string>(value, "AmountText");
				}
			}
		}

		// Token: 0x1700050C RID: 1292
		// (get) Token: 0x06000F9F RID: 3999 RVA: 0x00040897 File Offset: 0x0003EA97
		// (set) Token: 0x06000FA0 RID: 4000 RVA: 0x0004089F File Offset: 0x0003EA9F
		[DataSourceProperty]
		public CharacterImageIdentifierVM Visual
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
					base.OnPropertyChangedWithValue<CharacterImageIdentifierVM>(value, "Visual");
				}
			}
		}

		// Token: 0x1700050D RID: 1293
		// (get) Token: 0x06000FA1 RID: 4001 RVA: 0x000408BD File Offset: 0x0003EABD
		// (set) Token: 0x06000FA2 RID: 4002 RVA: 0x000408C5 File Offset: 0x0003EAC5
		[DataSourceProperty]
		public StringItemWithHintVM TierIconData
		{
			get
			{
				return this._tierIconData;
			}
			set
			{
				if (value != this._tierIconData)
				{
					this._tierIconData = value;
					base.OnPropertyChangedWithValue<StringItemWithHintVM>(value, "TierIconData");
				}
			}
		}

		// Token: 0x1700050E RID: 1294
		// (get) Token: 0x06000FA3 RID: 4003 RVA: 0x000408E3 File Offset: 0x0003EAE3
		// (set) Token: 0x06000FA4 RID: 4004 RVA: 0x000408EB File Offset: 0x0003EAEB
		[DataSourceProperty]
		public StringItemWithHintVM TypeIconData
		{
			get
			{
				return this._typeIconData;
			}
			set
			{
				if (value != this._typeIconData)
				{
					this._typeIconData = value;
					base.OnPropertyChangedWithValue<StringItemWithHintVM>(value, "TypeIconData");
				}
			}
		}

		// Token: 0x0400071B RID: 1819
		private readonly Action<TroopSelectionItemVM> _onAdd;

		// Token: 0x0400071C RID: 1820
		private readonly Action<TroopSelectionItemVM> _onRemove;

		// Token: 0x0400071D RID: 1821
		private int _currentAmount;

		// Token: 0x0400071E RID: 1822
		private int _maxAmount;

		// Token: 0x0400071F RID: 1823
		private int _heroHealthPercent;

		// Token: 0x04000720 RID: 1824
		private CharacterImageIdentifierVM _visual;

		// Token: 0x04000721 RID: 1825
		private bool _isSelected;

		// Token: 0x04000722 RID: 1826
		private bool _isRosterFull;

		// Token: 0x04000723 RID: 1827
		private bool _isLocked;

		// Token: 0x04000724 RID: 1828
		private bool _isTroopHero;

		// Token: 0x04000725 RID: 1829
		private string _name;

		// Token: 0x04000726 RID: 1830
		private string _amountText;

		// Token: 0x04000727 RID: 1831
		private StringItemWithHintVM _tierIconData;

		// Token: 0x04000728 RID: 1832
		private StringItemWithHintVM _typeIconData;
	}
}

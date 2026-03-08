using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.HeirSelectionPopup
{
	// Token: 0x02000061 RID: 97
	public class HeirSelectionPopupVM : ViewModel
	{
		// Token: 0x06000737 RID: 1847 RVA: 0x00022BF4 File Offset: 0x00020DF4
		public HeirSelectionPopupVM(Dictionary<Hero, int> heirApparents)
		{
			this.HeirApparents = new MBBindingList<HeirSelectionPopupHeroVM>();
			foreach (KeyValuePair<Hero, int> keyValuePair in from x in heirApparents
				orderby x.Value descending
				select x)
			{
				this.HeirApparents.Add(new HeirSelectionPopupHeroVM(keyValuePair.Key));
			}
			this.CurrentSelectedHero = this.HeirApparents[0];
			this.CurrentSelectedHero.IsSelected = true;
			this.ClanBanner = new BannerImageIdentifierVM(Clan.PlayerClan.Banner, true);
			this.RefreshValues();
		}

		// Token: 0x06000738 RID: 1848 RVA: 0x00022CBC File Offset: 0x00020EBC
		public void Update()
		{
			for (int i = 0; i < this.HeirApparents.Count; i++)
			{
				if (this.HeirApparents[i].IsSelected && this.HeirApparents[i] != this.CurrentSelectedHero)
				{
					this.CurrentSelectedHero.IsSelected = false;
					this.CurrentSelectedHero = this.HeirApparents[i];
				}
			}
			this.AreHotkeysVisible = !InformationManager.IsAnyInquiryActive();
		}

		// Token: 0x06000739 RID: 1849 RVA: 0x00022D34 File Offset: 0x00020F34
		public void ExecuteSelectHeir()
		{
			TextObject textObject = GameTexts.FindText("str_STR1_space_STR2", null);
			TextObject textObject2 = new TextObject("{=GEvP9i5f}You will play on as {HEIR.NAME}.", null);
			textObject2.SetCharacterProperties("HEIR", this.CurrentSelectedHero.Hero.CharacterObject, false);
			textObject.SetTextVariable("STR1", textObject2);
			textObject.SetTextVariable("STR2", new TextObject("{=awjomtnJ}Are you sure?", null));
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision", null).ToString(), textObject.ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), delegate()
			{
				this.ExecuteFinalizeHeirSelection(this.CurrentSelectedHero.Hero);
			}, null, "", 0f, null, null, null), false, false);
		}

		// Token: 0x0600073A RID: 1850 RVA: 0x00022DF7 File Offset: 0x00020FF7
		private void ExecuteFinalizeHeirSelection(Hero selectedHeir)
		{
			CampaignEventDispatcher.Instance.OnHeirSelectionOver(selectedHeir);
		}

		// Token: 0x0600073B RID: 1851 RVA: 0x00022E04 File Offset: 0x00021004
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = new TextObject("{=2maftPJP}Assign As Clan & Faction Leader", null).ToString();
			this.ButtonOkLabel = new TextObject("{=KXQ7Mvec}Select As Main Character", null).ToString();
			this.NameLabel = GameTexts.FindText("str_LEFT_colon_wSpace", null).SetTextVariable("LEFT", GameTexts.FindText("str_name", null)).ToString();
			this.AgeLabel = GameTexts.FindText("str_LEFT_colon_wSpace", null).SetTextVariable("LEFT", GameTexts.FindText("str_age", null)).ToString();
			this.CultureLabel = GameTexts.FindText("str_LEFT_colon_wSpace", null).SetTextVariable("LEFT", GameTexts.FindText("str_culture", null)).ToString();
			this.OccupationLabel = GameTexts.FindText("str_LEFT_colon_wSpace", null).SetTextVariable("LEFT", GameTexts.FindText("str_occupation", null)).ToString();
		}

		// Token: 0x0600073C RID: 1852 RVA: 0x00022EF0 File Offset: 0x000210F0
		public override void OnFinalize()
		{
			base.OnFinalize();
			InputKeyItemVM doneInputKey = this.DoneInputKey;
			if (doneInputKey != null)
			{
				doneInputKey.OnFinalize();
			}
			for (int i = 0; i < this.HeirApparents.Count; i++)
			{
				this.HeirApparents[i].OnFinalize();
			}
		}

		// Token: 0x170001FC RID: 508
		// (get) Token: 0x0600073D RID: 1853 RVA: 0x00022F3B File Offset: 0x0002113B
		// (set) Token: 0x0600073E RID: 1854 RVA: 0x00022F43 File Offset: 0x00021143
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

		// Token: 0x170001FD RID: 509
		// (get) Token: 0x0600073F RID: 1855 RVA: 0x00022F66 File Offset: 0x00021166
		// (set) Token: 0x06000740 RID: 1856 RVA: 0x00022F6E File Offset: 0x0002116E
		[DataSourceProperty]
		public string ButtonOkLabel
		{
			get
			{
				return this._buttonOkLabel;
			}
			set
			{
				if (value != this._buttonOkLabel)
				{
					this._buttonOkLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "ButtonOkLabel");
				}
			}
		}

		// Token: 0x170001FE RID: 510
		// (get) Token: 0x06000741 RID: 1857 RVA: 0x00022F91 File Offset: 0x00021191
		// (set) Token: 0x06000742 RID: 1858 RVA: 0x00022F99 File Offset: 0x00021199
		[DataSourceProperty]
		public string NameLabel
		{
			get
			{
				return this._nameLabel;
			}
			set
			{
				if (value != this._nameLabel)
				{
					this._nameLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "NameLabel");
				}
			}
		}

		// Token: 0x170001FF RID: 511
		// (get) Token: 0x06000743 RID: 1859 RVA: 0x00022FBC File Offset: 0x000211BC
		// (set) Token: 0x06000744 RID: 1860 RVA: 0x00022FC4 File Offset: 0x000211C4
		[DataSourceProperty]
		public string AgeLabel
		{
			get
			{
				return this._ageLabel;
			}
			set
			{
				if (value != this._ageLabel)
				{
					this._ageLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "AgeLabel");
				}
			}
		}

		// Token: 0x17000200 RID: 512
		// (get) Token: 0x06000745 RID: 1861 RVA: 0x00022FE7 File Offset: 0x000211E7
		// (set) Token: 0x06000746 RID: 1862 RVA: 0x00022FEF File Offset: 0x000211EF
		[DataSourceProperty]
		public string CultureLabel
		{
			get
			{
				return this._cultureLabel;
			}
			set
			{
				if (value != this._cultureLabel)
				{
					this._cultureLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "CultureLabel");
				}
			}
		}

		// Token: 0x17000201 RID: 513
		// (get) Token: 0x06000747 RID: 1863 RVA: 0x00023012 File Offset: 0x00021212
		// (set) Token: 0x06000748 RID: 1864 RVA: 0x0002301A File Offset: 0x0002121A
		[DataSourceProperty]
		public string OccupationLabel
		{
			get
			{
				return this._occupationLabel;
			}
			set
			{
				if (value != this._occupationLabel)
				{
					this._occupationLabel = value;
					base.OnPropertyChangedWithValue<string>(value, "OccupationLabel");
				}
			}
		}

		// Token: 0x17000202 RID: 514
		// (get) Token: 0x06000749 RID: 1865 RVA: 0x0002303D File Offset: 0x0002123D
		// (set) Token: 0x0600074A RID: 1866 RVA: 0x00023045 File Offset: 0x00021245
		[DataSourceProperty]
		public BannerImageIdentifierVM ClanBanner
		{
			get
			{
				return this._clanBanner;
			}
			set
			{
				if (value != this._clanBanner)
				{
					this._clanBanner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "ClanBanner");
				}
			}
		}

		// Token: 0x17000203 RID: 515
		// (get) Token: 0x0600074B RID: 1867 RVA: 0x00023063 File Offset: 0x00021263
		// (set) Token: 0x0600074C RID: 1868 RVA: 0x0002306B File Offset: 0x0002126B
		[DataSourceProperty]
		public MBBindingList<HeirSelectionPopupHeroVM> HeirApparents
		{
			get
			{
				return this._heirApparents;
			}
			set
			{
				if (value != this._heirApparents)
				{
					this._heirApparents = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeirSelectionPopupHeroVM>>(value, "HeirApparents");
				}
			}
		}

		// Token: 0x17000204 RID: 516
		// (get) Token: 0x0600074D RID: 1869 RVA: 0x00023089 File Offset: 0x00021289
		// (set) Token: 0x0600074E RID: 1870 RVA: 0x00023091 File Offset: 0x00021291
		[DataSourceProperty]
		public HeirSelectionPopupHeroVM CurrentSelectedHero
		{
			get
			{
				return this._currentSelectedHero;
			}
			set
			{
				if (value != this._currentSelectedHero)
				{
					this._currentSelectedHero = value;
					base.OnPropertyChangedWithValue<HeirSelectionPopupHeroVM>(value, "CurrentSelectedHero");
				}
			}
		}

		// Token: 0x17000205 RID: 517
		// (get) Token: 0x0600074F RID: 1871 RVA: 0x000230AF File Offset: 0x000212AF
		// (set) Token: 0x06000750 RID: 1872 RVA: 0x000230B7 File Offset: 0x000212B7
		[DataSourceProperty]
		public bool AreHotkeysVisible
		{
			get
			{
				return this._areHotkeysVisible;
			}
			set
			{
				if (value != this._areHotkeysVisible)
				{
					this._areHotkeysVisible = value;
					base.OnPropertyChangedWithValue(value, "AreHotkeysVisible");
				}
			}
		}

		// Token: 0x06000751 RID: 1873 RVA: 0x000230D5 File Offset: 0x000212D5
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x17000206 RID: 518
		// (get) Token: 0x06000752 RID: 1874 RVA: 0x000230E4 File Offset: 0x000212E4
		// (set) Token: 0x06000753 RID: 1875 RVA: 0x000230EC File Offset: 0x000212EC
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x04000321 RID: 801
		private string _titleText;

		// Token: 0x04000322 RID: 802
		private string _buttonOkLabel;

		// Token: 0x04000323 RID: 803
		private string _nameLabel;

		// Token: 0x04000324 RID: 804
		private string _ageLabel;

		// Token: 0x04000325 RID: 805
		private string _cultureLabel;

		// Token: 0x04000326 RID: 806
		private string _occupationLabel;

		// Token: 0x04000327 RID: 807
		private BannerImageIdentifierVM _clanBanner;

		// Token: 0x04000328 RID: 808
		private MBBindingList<HeirSelectionPopupHeroVM> _heirApparents;

		// Token: 0x04000329 RID: 809
		private HeirSelectionPopupHeroVM _currentSelectedHero;

		// Token: 0x0400032A RID: 810
		private bool _areHotkeysVisible;

		// Token: 0x0400032B RID: 811
		private InputKeyItemVM _doneInputKey;
	}
}

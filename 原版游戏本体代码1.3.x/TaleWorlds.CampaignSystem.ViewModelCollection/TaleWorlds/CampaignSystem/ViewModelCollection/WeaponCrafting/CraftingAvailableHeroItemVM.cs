using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000F8 RID: 248
	public class CraftingAvailableHeroItemVM : ViewModel
	{
		// Token: 0x17000763 RID: 1891
		// (get) Token: 0x06001645 RID: 5701 RVA: 0x00056ACD File Offset: 0x00054CCD
		public Hero Hero { get; }

		// Token: 0x06001646 RID: 5702 RVA: 0x00056AD8 File Offset: 0x00054CD8
		public CraftingAvailableHeroItemVM(Hero hero, Action<CraftingAvailableHeroItemVM> onSelection)
		{
			this._onSelection = onSelection;
			this._craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
			this.Hero = hero;
			this.HeroData = new HeroVM(this.Hero, false);
			this.Hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetCraftingHeroTooltip(this.Hero, this._craftingOrder));
			this.CraftingPerks = new MBBindingList<CraftingPerkVM>();
		}

		// Token: 0x06001647 RID: 5703 RVA: 0x00056B3D File Offset: 0x00054D3D
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.HeroData.RefreshValues();
		}

		// Token: 0x06001648 RID: 5704 RVA: 0x00056B50 File Offset: 0x00054D50
		public void RefreshStamina()
		{
			this.CurrentStamina = (float)this._craftingBehavior.GetHeroCraftingStamina(this.Hero);
			this.MaxStamina = this._craftingBehavior.GetMaxHeroCraftingStamina(this.Hero);
			int content = (int)(this.CurrentStamina / (float)this.MaxStamina * 100f);
			GameTexts.SetVariable("NUMBER", content);
			this.StaminaPercentage = GameTexts.FindText("str_NUMBER_percent", null).ToString();
		}

		// Token: 0x06001649 RID: 5705 RVA: 0x00056BC3 File Offset: 0x00054DC3
		public void RefreshOrderAvailability(CraftingOrder order)
		{
			this._craftingOrder = order;
			if (order != null)
			{
				this.IsDisabled = !order.IsOrderAvailableForHero(this.Hero);
				return;
			}
			this.IsDisabled = false;
		}

		// Token: 0x0600164A RID: 5706 RVA: 0x00056BEC File Offset: 0x00054DEC
		public void RefreshSkills()
		{
			this.SmithySkillLevel = this.Hero.GetSkillValue(DefaultSkills.Crafting);
		}

		// Token: 0x0600164B RID: 5707 RVA: 0x00056C04 File Offset: 0x00054E04
		public void RefreshPerks()
		{
			this.CraftingPerks.Clear();
			foreach (PerkObject perkObject in PerkObject.All)
			{
				if (perkObject.Skill == DefaultSkills.Crafting && this.Hero.GetPerkValue(perkObject))
				{
					this.CraftingPerks.Add(new CraftingPerkVM(perkObject));
				}
			}
			this.PerksText = ((this.CraftingPerks.Count > 0) ? new TextObject("{=8lCWWK9G}Smithing Perks", null).ToString() : new TextObject("{=WHRq5Dp0}No Smithing Perks", null).ToString());
		}

		// Token: 0x0600164C RID: 5708 RVA: 0x00056CBC File Offset: 0x00054EBC
		public void ExecuteSelection()
		{
			Action<CraftingAvailableHeroItemVM> onSelection = this._onSelection;
			if (onSelection == null)
			{
				return;
			}
			onSelection(this);
		}

		// Token: 0x17000764 RID: 1892
		// (get) Token: 0x0600164D RID: 5709 RVA: 0x00056CCF File Offset: 0x00054ECF
		// (set) Token: 0x0600164E RID: 5710 RVA: 0x00056CD7 File Offset: 0x00054ED7
		[DataSourceProperty]
		public bool IsDisabled
		{
			get
			{
				return this._isDisabled;
			}
			set
			{
				if (value != this._isDisabled)
				{
					this._isDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsDisabled");
				}
			}
		}

		// Token: 0x17000765 RID: 1893
		// (get) Token: 0x0600164F RID: 5711 RVA: 0x00056CF5 File Offset: 0x00054EF5
		// (set) Token: 0x06001650 RID: 5712 RVA: 0x00056CFD File Offset: 0x00054EFD
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

		// Token: 0x17000766 RID: 1894
		// (get) Token: 0x06001651 RID: 5713 RVA: 0x00056D1B File Offset: 0x00054F1B
		// (set) Token: 0x06001652 RID: 5714 RVA: 0x00056D23 File Offset: 0x00054F23
		[DataSourceProperty]
		public HeroVM HeroData
		{
			get
			{
				return this._heroData;
			}
			set
			{
				if (value != this._heroData)
				{
					this._heroData = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "HeroData");
				}
			}
		}

		// Token: 0x17000767 RID: 1895
		// (get) Token: 0x06001653 RID: 5715 RVA: 0x00056D41 File Offset: 0x00054F41
		// (set) Token: 0x06001654 RID: 5716 RVA: 0x00056D49 File Offset: 0x00054F49
		[DataSourceProperty]
		public BasicTooltipViewModel Hint
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
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x17000768 RID: 1896
		// (get) Token: 0x06001655 RID: 5717 RVA: 0x00056D67 File Offset: 0x00054F67
		// (set) Token: 0x06001656 RID: 5718 RVA: 0x00056D6F File Offset: 0x00054F6F
		[DataSourceProperty]
		public float CurrentStamina
		{
			get
			{
				return this._currentStamina;
			}
			set
			{
				if (value != this._currentStamina)
				{
					this._currentStamina = value;
					base.OnPropertyChangedWithValue(value, "CurrentStamina");
				}
			}
		}

		// Token: 0x17000769 RID: 1897
		// (get) Token: 0x06001657 RID: 5719 RVA: 0x00056D8D File Offset: 0x00054F8D
		// (set) Token: 0x06001658 RID: 5720 RVA: 0x00056D95 File Offset: 0x00054F95
		[DataSourceProperty]
		public int MaxStamina
		{
			get
			{
				return this._maxStamina;
			}
			set
			{
				if (value != this._maxStamina)
				{
					this._maxStamina = value;
					base.OnPropertyChangedWithValue(value, "MaxStamina");
				}
			}
		}

		// Token: 0x1700076A RID: 1898
		// (get) Token: 0x06001659 RID: 5721 RVA: 0x00056DB3 File Offset: 0x00054FB3
		// (set) Token: 0x0600165A RID: 5722 RVA: 0x00056DBB File Offset: 0x00054FBB
		[DataSourceProperty]
		public string StaminaPercentage
		{
			get
			{
				return this._staminaPercentage;
			}
			set
			{
				if (value != this._staminaPercentage)
				{
					this._staminaPercentage = value;
					base.OnPropertyChangedWithValue<string>(value, "StaminaPercentage");
				}
			}
		}

		// Token: 0x1700076B RID: 1899
		// (get) Token: 0x0600165B RID: 5723 RVA: 0x00056DDE File Offset: 0x00054FDE
		// (set) Token: 0x0600165C RID: 5724 RVA: 0x00056DE6 File Offset: 0x00054FE6
		[DataSourceProperty]
		public int SmithySkillLevel
		{
			get
			{
				return this._smithySkillLevel;
			}
			set
			{
				if (value != this._smithySkillLevel)
				{
					this._smithySkillLevel = value;
					base.OnPropertyChangedWithValue(value, "SmithySkillLevel");
				}
			}
		}

		// Token: 0x1700076C RID: 1900
		// (get) Token: 0x0600165D RID: 5725 RVA: 0x00056E04 File Offset: 0x00055004
		// (set) Token: 0x0600165E RID: 5726 RVA: 0x00056E0C File Offset: 0x0005500C
		[DataSourceProperty]
		public MBBindingList<CraftingPerkVM> CraftingPerks
		{
			get
			{
				return this._craftingPerks;
			}
			set
			{
				if (value != this._craftingPerks)
				{
					this._craftingPerks = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingPerkVM>>(value, "CraftingPerks");
				}
			}
		}

		// Token: 0x1700076D RID: 1901
		// (get) Token: 0x0600165F RID: 5727 RVA: 0x00056E2A File Offset: 0x0005502A
		// (set) Token: 0x06001660 RID: 5728 RVA: 0x00056E32 File Offset: 0x00055032
		[DataSourceProperty]
		public string PerksText
		{
			get
			{
				return this._perksText;
			}
			set
			{
				if (value != this._perksText)
				{
					this._perksText = value;
					base.OnPropertyChangedWithValue<string>(value, "PerksText");
				}
			}
		}

		// Token: 0x04000A32 RID: 2610
		private readonly Action<CraftingAvailableHeroItemVM> _onSelection;

		// Token: 0x04000A33 RID: 2611
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000A34 RID: 2612
		private CraftingOrder _craftingOrder;

		// Token: 0x04000A35 RID: 2613
		private HeroVM _heroData;

		// Token: 0x04000A36 RID: 2614
		private BasicTooltipViewModel _hint;

		// Token: 0x04000A37 RID: 2615
		private float _currentStamina;

		// Token: 0x04000A38 RID: 2616
		private int _maxStamina;

		// Token: 0x04000A39 RID: 2617
		private string _staminaPercentage;

		// Token: 0x04000A3A RID: 2618
		private bool _isDisabled;

		// Token: 0x04000A3B RID: 2619
		private bool _isSelected;

		// Token: 0x04000A3C RID: 2620
		private int _smithySkillLevel;

		// Token: 0x04000A3D RID: 2621
		private MBBindingList<CraftingPerkVM> _craftingPerks;

		// Token: 0x04000A3E RID: 2622
		private string _perksText;
	}
}

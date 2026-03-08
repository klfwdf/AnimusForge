using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order
{
	// Token: 0x0200010F RID: 271
	public class CraftingOrderItemVM : ViewModel
	{
		// Token: 0x17000841 RID: 2113
		// (get) Token: 0x060018AA RID: 6314 RVA: 0x0005E14A File Offset: 0x0005C34A
		public CraftingOrder CraftingOrder { get; }

		// Token: 0x060018AB RID: 6315 RVA: 0x0005E154 File Offset: 0x0005C354
		public CraftingOrderItemVM(CraftingOrder order, Action<CraftingOrderItemVM> onSelection, Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero, List<CraftingStatData> orderStatDatas, CampaignUIHelper.IssueQuestFlags questFlags = CampaignUIHelper.IssueQuestFlags.None)
		{
			this.CraftingOrder = order;
			this._orderOwner = order.OrderOwner;
			this._getCurrentCraftingHero = getCurrentCraftingHero;
			this._orderStatDatas = orderStatDatas;
			this._onSelection = onSelection;
			this.WeaponAttributes = new MBBindingList<WeaponAttributeVM>();
			this.OrderOwnerData = new HeroVM(this._orderOwner, false);
			this._weaponTemplate = order.PreCraftedWeaponDesignItem.WeaponDesign.Template;
			this.OrderWeaponTypeCode = this._weaponTemplate.StringId;
			this.Quests = this.GetQuestMarkers(questFlags);
			this.IsQuestOrder = this.Quests.Count > 0;
			this.RefreshValues();
			this.RefreshStats();
		}

		// Token: 0x060018AC RID: 6316 RVA: 0x0005E214 File Offset: 0x0005C414
		private MBBindingList<QuestMarkerVM> GetQuestMarkers(CampaignUIHelper.IssueQuestFlags flags)
		{
			MBBindingList<QuestMarkerVM> mbbindingList = new MBBindingList<QuestMarkerVM>();
			if ((flags & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None)
			{
				mbbindingList.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.ActiveIssue, null, null));
			}
			if ((flags & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
			{
				mbbindingList.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest, null, null));
			}
			return mbbindingList;
		}

		// Token: 0x060018AD RID: 6317 RVA: 0x0005E250 File Offset: 0x0005C450
		public void RefreshStats()
		{
			this.WeaponAttributes.Clear();
			ItemObject preCraftedWeaponDesignItem = this.CraftingOrder.PreCraftedWeaponDesignItem;
			if (((preCraftedWeaponDesignItem != null) ? preCraftedWeaponDesignItem.Weapons : null) == null)
			{
				Debug.FailedAssert("Crafting order does not contain any valid weapons", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Crafting\\WeaponDesign\\Order\\CraftingOrderItemVM.cs", "RefreshStats", 71);
				return;
			}
			this.CraftingOrder.GetStatWeapon();
			foreach (CraftingStatData craftingStatData in this._orderStatDatas)
			{
				if (craftingStatData.IsValid)
				{
					this.WeaponAttributes.Add(new WeaponAttributeVM(craftingStatData.Type, craftingStatData.DamageType, craftingStatData.DescriptionText.ToString(), craftingStatData.CurValue));
				}
			}
			IEnumerable<Hero> source = from x in CraftingHelper.GetAvailableHeroesForCrafting()
				where this.CraftingOrder.IsOrderAvailableForHero(x)
				select x;
			this.HasAvailableHeroes = source.Any<Hero>();
			this.OrderPrice = this.CraftingOrder.BaseGoldReward;
			this.RefreshDifficulty();
		}

		// Token: 0x060018AE RID: 6318 RVA: 0x0005E354 File Offset: 0x0005C554
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.OrderNumberText = GameTexts.FindText("str_crafting_order_header", null).ToString();
			this.OrderWeaponType = this._weaponTemplate.TemplateName.ToString();
			this.OrderDifficultyLabelText = this._difficultyText.ToString();
			this.OrderDifficultyValueText = MathF.Round(this.CraftingOrder.OrderDifficulty).ToString();
			this.DisabledReasonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetCraftingOrderDisabledReasonTooltip(this._getCurrentCraftingHero().Hero, this.CraftingOrder));
		}

		// Token: 0x060018AF RID: 6319 RVA: 0x0005E3DC File Offset: 0x0005C5DC
		private void RefreshDifficulty()
		{
			Hero hero = this._getCurrentCraftingHero().Hero;
			int skillValue = hero.GetSkillValue(DefaultSkills.Crafting);
			this.IsEnabled = this.CraftingOrder.IsOrderAvailableForHero(hero);
			this.IsDifficultySuitableForHero = this.CraftingOrder.OrderDifficulty < (float)skillValue;
		}

		// Token: 0x060018B0 RID: 6320 RVA: 0x0005E42D File Offset: 0x0005C62D
		public void ExecuteSelectOrder()
		{
			Action<CraftingOrderItemVM> onSelection = this._onSelection;
			if (onSelection == null)
			{
				return;
			}
			onSelection(this);
		}

		// Token: 0x17000842 RID: 2114
		// (get) Token: 0x060018B1 RID: 6321 RVA: 0x0005E440 File Offset: 0x0005C640
		// (set) Token: 0x060018B2 RID: 6322 RVA: 0x0005E448 File Offset: 0x0005C648
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000843 RID: 2115
		// (get) Token: 0x060018B3 RID: 6323 RVA: 0x0005E466 File Offset: 0x0005C666
		// (set) Token: 0x060018B4 RID: 6324 RVA: 0x0005E46E File Offset: 0x0005C66E
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

		// Token: 0x17000844 RID: 2116
		// (get) Token: 0x060018B5 RID: 6325 RVA: 0x0005E48C File Offset: 0x0005C68C
		// (set) Token: 0x060018B6 RID: 6326 RVA: 0x0005E494 File Offset: 0x0005C694
		[DataSourceProperty]
		public bool HasAvailableHeroes
		{
			get
			{
				return this._hasAvailableHeroes;
			}
			set
			{
				if (value != this._hasAvailableHeroes)
				{
					this._hasAvailableHeroes = value;
					base.OnPropertyChangedWithValue(value, "HasAvailableHeroes");
				}
			}
		}

		// Token: 0x17000845 RID: 2117
		// (get) Token: 0x060018B7 RID: 6327 RVA: 0x0005E4B2 File Offset: 0x0005C6B2
		// (set) Token: 0x060018B8 RID: 6328 RVA: 0x0005E4BA File Offset: 0x0005C6BA
		[DataSourceProperty]
		public bool IsDifficultySuitableForHero
		{
			get
			{
				return this._isDifficultySuitableForHero;
			}
			set
			{
				if (value != this._isDifficultySuitableForHero)
				{
					this._isDifficultySuitableForHero = value;
					base.OnPropertyChangedWithValue(value, "IsDifficultySuitableForHero");
				}
			}
		}

		// Token: 0x17000846 RID: 2118
		// (get) Token: 0x060018B9 RID: 6329 RVA: 0x0005E4D8 File Offset: 0x0005C6D8
		// (set) Token: 0x060018BA RID: 6330 RVA: 0x0005E4E0 File Offset: 0x0005C6E0
		[DataSourceProperty]
		public bool IsQuestOrder
		{
			get
			{
				return this._isQuestOrder;
			}
			set
			{
				if (value != this._isQuestOrder)
				{
					this._isQuestOrder = value;
					base.OnPropertyChangedWithValue(value, "IsQuestOrder");
				}
			}
		}

		// Token: 0x17000847 RID: 2119
		// (get) Token: 0x060018BB RID: 6331 RVA: 0x0005E4FE File Offset: 0x0005C6FE
		// (set) Token: 0x060018BC RID: 6332 RVA: 0x0005E506 File Offset: 0x0005C706
		[DataSourceProperty]
		public int OrderPrice
		{
			get
			{
				return this._orderPrice;
			}
			set
			{
				if (value != this._orderPrice)
				{
					this._orderPrice = value;
					base.OnPropertyChangedWithValue(value, "OrderPrice");
				}
			}
		}

		// Token: 0x17000848 RID: 2120
		// (get) Token: 0x060018BD RID: 6333 RVA: 0x0005E524 File Offset: 0x0005C724
		// (set) Token: 0x060018BE RID: 6334 RVA: 0x0005E52C File Offset: 0x0005C72C
		[DataSourceProperty]
		public string OrderDifficultyLabelText
		{
			get
			{
				return this._orderDifficultyLabelText;
			}
			set
			{
				if (value != this._orderDifficultyLabelText)
				{
					this._orderDifficultyLabelText = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderDifficultyLabelText");
				}
			}
		}

		// Token: 0x17000849 RID: 2121
		// (get) Token: 0x060018BF RID: 6335 RVA: 0x0005E54F File Offset: 0x0005C74F
		// (set) Token: 0x060018C0 RID: 6336 RVA: 0x0005E557 File Offset: 0x0005C757
		[DataSourceProperty]
		public string OrderDifficultyValueText
		{
			get
			{
				return this._orderDifficultyValueText;
			}
			set
			{
				if (value != this._orderDifficultyValueText)
				{
					this._orderDifficultyValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderDifficultyValueText");
				}
			}
		}

		// Token: 0x1700084A RID: 2122
		// (get) Token: 0x060018C1 RID: 6337 RVA: 0x0005E57A File Offset: 0x0005C77A
		// (set) Token: 0x060018C2 RID: 6338 RVA: 0x0005E582 File Offset: 0x0005C782
		[DataSourceProperty]
		public string OrderNumberText
		{
			get
			{
				return this._orderNumberText;
			}
			set
			{
				if (value != this._orderNumberText)
				{
					this._orderNumberText = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderNumberText");
				}
			}
		}

		// Token: 0x1700084B RID: 2123
		// (get) Token: 0x060018C3 RID: 6339 RVA: 0x0005E5A5 File Offset: 0x0005C7A5
		// (set) Token: 0x060018C4 RID: 6340 RVA: 0x0005E5AD File Offset: 0x0005C7AD
		[DataSourceProperty]
		public string OrderWeaponType
		{
			get
			{
				return this._orderWeaponType;
			}
			set
			{
				if (value != this._orderWeaponType)
				{
					this._orderWeaponType = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderWeaponType");
				}
			}
		}

		// Token: 0x1700084C RID: 2124
		// (get) Token: 0x060018C5 RID: 6341 RVA: 0x0005E5D0 File Offset: 0x0005C7D0
		// (set) Token: 0x060018C6 RID: 6342 RVA: 0x0005E5D8 File Offset: 0x0005C7D8
		[DataSourceProperty]
		public string OrderWeaponTypeCode
		{
			get
			{
				return this._orderWeaponTypeCode;
			}
			set
			{
				if (value != this._orderWeaponTypeCode)
				{
					this._orderWeaponTypeCode = value;
					base.OnPropertyChangedWithValue<string>(value, "OrderWeaponTypeCode");
				}
			}
		}

		// Token: 0x1700084D RID: 2125
		// (get) Token: 0x060018C7 RID: 6343 RVA: 0x0005E5FB File Offset: 0x0005C7FB
		// (set) Token: 0x060018C8 RID: 6344 RVA: 0x0005E603 File Offset: 0x0005C803
		[DataSourceProperty]
		public HeroVM OrderOwnerData
		{
			get
			{
				return this._orderOwnerData;
			}
			set
			{
				if (value != this._orderOwnerData)
				{
					this._orderOwnerData = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "OrderOwnerData");
				}
			}
		}

		// Token: 0x1700084E RID: 2126
		// (get) Token: 0x060018C9 RID: 6345 RVA: 0x0005E621 File Offset: 0x0005C821
		// (set) Token: 0x060018CA RID: 6346 RVA: 0x0005E629 File Offset: 0x0005C829
		[DataSourceProperty]
		public BasicTooltipViewModel DisabledReasonHint
		{
			get
			{
				return this._disabledReasonHint;
			}
			set
			{
				if (value != this._disabledReasonHint)
				{
					this._disabledReasonHint = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "DisabledReasonHint");
				}
			}
		}

		// Token: 0x1700084F RID: 2127
		// (get) Token: 0x060018CB RID: 6347 RVA: 0x0005E647 File Offset: 0x0005C847
		// (set) Token: 0x060018CC RID: 6348 RVA: 0x0005E64F File Offset: 0x0005C84F
		[DataSourceProperty]
		public MBBindingList<QuestMarkerVM> Quests
		{
			get
			{
				return this._quests;
			}
			set
			{
				if (value != this._quests)
				{
					this._quests = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestMarkerVM>>(value, "Quests");
				}
			}
		}

		// Token: 0x17000850 RID: 2128
		// (get) Token: 0x060018CD RID: 6349 RVA: 0x0005E66D File Offset: 0x0005C86D
		// (set) Token: 0x060018CE RID: 6350 RVA: 0x0005E675 File Offset: 0x0005C875
		[DataSourceProperty]
		public MBBindingList<WeaponAttributeVM> WeaponAttributes
		{
			get
			{
				return this._weaponAttributes;
			}
			set
			{
				if (value != this._weaponAttributes)
				{
					this._weaponAttributes = value;
					base.OnPropertyChangedWithValue<MBBindingList<WeaponAttributeVM>>(value, "WeaponAttributes");
				}
			}
		}

		// Token: 0x04000B53 RID: 2899
		private Hero _orderOwner;

		// Token: 0x04000B54 RID: 2900
		private Action<CraftingOrderItemVM> _onSelection;

		// Token: 0x04000B55 RID: 2901
		private Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHero;

		// Token: 0x04000B56 RID: 2902
		private CraftingTemplate _weaponTemplate;

		// Token: 0x04000B57 RID: 2903
		private TextObject _difficultyText = new TextObject("{=udPWHmOm}Difficulty:", null);

		// Token: 0x04000B58 RID: 2904
		private List<CraftingStatData> _orderStatDatas;

		// Token: 0x04000B59 RID: 2905
		private bool _isEnabled;

		// Token: 0x04000B5A RID: 2906
		private bool _isSelected;

		// Token: 0x04000B5B RID: 2907
		private bool _hasAvailableHeroes;

		// Token: 0x04000B5C RID: 2908
		private bool _isDifficultySuitableForHero;

		// Token: 0x04000B5D RID: 2909
		private bool _isQuestOrder;

		// Token: 0x04000B5E RID: 2910
		private int _orderPrice;

		// Token: 0x04000B5F RID: 2911
		private string _orderDifficultyLabelText;

		// Token: 0x04000B60 RID: 2912
		private string _orderDifficultyValueText;

		// Token: 0x04000B61 RID: 2913
		private string _orderNumberText;

		// Token: 0x04000B62 RID: 2914
		private string _orderWeaponType;

		// Token: 0x04000B63 RID: 2915
		private string _orderWeaponTypeCode;

		// Token: 0x04000B64 RID: 2916
		private HeroVM _orderOwnerData;

		// Token: 0x04000B65 RID: 2917
		private BasicTooltipViewModel _disabledReasonHint;

		// Token: 0x04000B66 RID: 2918
		private MBBindingList<QuestMarkerVM> _quests;

		// Token: 0x04000B67 RID: 2919
		private MBBindingList<WeaponAttributeVM> _weaponAttributes;
	}
}

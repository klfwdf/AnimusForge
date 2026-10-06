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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign.Order;

public class CraftingOrderItemVM : ViewModel
{
	private Hero _orderOwner;

	private Action<CraftingOrderItemVM> _onSelection;

	private Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHero;

	private CraftingTemplate _weaponTemplate;

	private TextObject _difficultyText = new TextObject("{=udPWHmOm}Difficulty:");

	private List<CraftingStatData> _orderStatDatas;

	private bool _isEnabled;

	private bool _isSelected;

	private bool _hasAvailableHeroes;

	private bool _isDifficultySuitableForHero;

	private bool _isQuestOrder;

	private int _orderPrice;

	private string _orderDifficultyLabelText;

	private string _orderDifficultyValueText;

	private string _orderNumberText;

	private string _orderWeaponType;

	private string _orderWeaponTypeCode;

	private HeroVM _orderOwnerData;

	private BasicTooltipViewModel _disabledReasonHint;

	private MBBindingList<QuestMarkerVM> _quests;

	private MBBindingList<WeaponAttributeVM> _weaponAttributes;

	public CraftingOrder CraftingOrder { get; }

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool HasAvailableHeroes
	{
		get
		{
			return _hasAvailableHeroes;
		}
		set
		{
			if (value != _hasAvailableHeroes)
			{
				_hasAvailableHeroes = value;
				OnPropertyChangedWithValue(value, "HasAvailableHeroes");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDifficultySuitableForHero
	{
		get
		{
			return _isDifficultySuitableForHero;
		}
		set
		{
			if (value != _isDifficultySuitableForHero)
			{
				_isDifficultySuitableForHero = value;
				OnPropertyChangedWithValue(value, "IsDifficultySuitableForHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsQuestOrder
	{
		get
		{
			return _isQuestOrder;
		}
		set
		{
			if (value != _isQuestOrder)
			{
				_isQuestOrder = value;
				OnPropertyChangedWithValue(value, "IsQuestOrder");
			}
		}
	}

	[DataSourceProperty]
	public int OrderPrice
	{
		get
		{
			return _orderPrice;
		}
		set
		{
			if (value != _orderPrice)
			{
				_orderPrice = value;
				OnPropertyChangedWithValue(value, "OrderPrice");
			}
		}
	}

	[DataSourceProperty]
	public string OrderDifficultyLabelText
	{
		get
		{
			return _orderDifficultyLabelText;
		}
		set
		{
			if (value != _orderDifficultyLabelText)
			{
				_orderDifficultyLabelText = value;
				OnPropertyChangedWithValue(value, "OrderDifficultyLabelText");
			}
		}
	}

	[DataSourceProperty]
	public string OrderDifficultyValueText
	{
		get
		{
			return _orderDifficultyValueText;
		}
		set
		{
			if (value != _orderDifficultyValueText)
			{
				_orderDifficultyValueText = value;
				OnPropertyChangedWithValue(value, "OrderDifficultyValueText");
			}
		}
	}

	[DataSourceProperty]
	public string OrderNumberText
	{
		get
		{
			return _orderNumberText;
		}
		set
		{
			if (value != _orderNumberText)
			{
				_orderNumberText = value;
				OnPropertyChangedWithValue(value, "OrderNumberText");
			}
		}
	}

	[DataSourceProperty]
	public string OrderWeaponType
	{
		get
		{
			return _orderWeaponType;
		}
		set
		{
			if (value != _orderWeaponType)
			{
				_orderWeaponType = value;
				OnPropertyChangedWithValue(value, "OrderWeaponType");
			}
		}
	}

	[DataSourceProperty]
	public string OrderWeaponTypeCode
	{
		get
		{
			return _orderWeaponTypeCode;
		}
		set
		{
			if (value != _orderWeaponTypeCode)
			{
				_orderWeaponTypeCode = value;
				OnPropertyChangedWithValue(value, "OrderWeaponTypeCode");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM OrderOwnerData
	{
		get
		{
			return _orderOwnerData;
		}
		set
		{
			if (value != _orderOwnerData)
			{
				_orderOwnerData = value;
				OnPropertyChangedWithValue(value, "OrderOwnerData");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DisabledReasonHint
	{
		get
		{
			return _disabledReasonHint;
		}
		set
		{
			if (value != _disabledReasonHint)
			{
				_disabledReasonHint = value;
				OnPropertyChangedWithValue(value, "DisabledReasonHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<QuestMarkerVM> Quests
	{
		get
		{
			return _quests;
		}
		set
		{
			if (value != _quests)
			{
				_quests = value;
				OnPropertyChangedWithValue(value, "Quests");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<WeaponAttributeVM> WeaponAttributes
	{
		get
		{
			return _weaponAttributes;
		}
		set
		{
			if (value != _weaponAttributes)
			{
				_weaponAttributes = value;
				OnPropertyChangedWithValue(value, "WeaponAttributes");
			}
		}
	}

	public CraftingOrderItemVM(CraftingOrder order, Action<CraftingOrderItemVM> onSelection, Func<CraftingAvailableHeroItemVM> getCurrentCraftingHero, List<CraftingStatData> orderStatDatas, CampaignUIHelper.IssueQuestFlags questFlags = CampaignUIHelper.IssueQuestFlags.None)
	{
		CraftingOrder = order;
		_orderOwner = order.OrderOwner;
		_getCurrentCraftingHero = getCurrentCraftingHero;
		_orderStatDatas = orderStatDatas;
		_onSelection = onSelection;
		WeaponAttributes = new MBBindingList<WeaponAttributeVM>();
		OrderOwnerData = new HeroVM(_orderOwner);
		_weaponTemplate = order.PreCraftedWeaponDesignItem.WeaponDesign.Template;
		OrderWeaponTypeCode = _weaponTemplate.StringId;
		Quests = GetQuestMarkers(questFlags);
		IsQuestOrder = Quests.Count > 0;
		RefreshValues();
		RefreshStats();
	}

	private MBBindingList<QuestMarkerVM> GetQuestMarkers(CampaignUIHelper.IssueQuestFlags flags)
	{
		MBBindingList<QuestMarkerVM> mBBindingList = new MBBindingList<QuestMarkerVM>();
		if ((flags & CampaignUIHelper.IssueQuestFlags.ActiveIssue) != CampaignUIHelper.IssueQuestFlags.None)
		{
			mBBindingList.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.ActiveIssue));
		}
		if ((flags & CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest) != CampaignUIHelper.IssueQuestFlags.None)
		{
			mBBindingList.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.ActiveStoryQuest));
		}
		return mBBindingList;
	}

	public void RefreshStats()
	{
		WeaponAttributes.Clear();
		if (CraftingOrder.PreCraftedWeaponDesignItem?.Weapons == null)
		{
			Debug.FailedAssert("Crafting order does not contain any valid weapons", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Crafting\\WeaponDesign\\Order\\CraftingOrderItemVM.cs", "RefreshStats", 71);
			return;
		}
		CraftingOrder.GetStatWeapon();
		foreach (CraftingStatData orderStatData in _orderStatDatas)
		{
			if (orderStatData.IsValid)
			{
				WeaponAttributes.Add(new WeaponAttributeVM(orderStatData.Type, orderStatData.DamageType, orderStatData.DescriptionText.ToString(), orderStatData.CurValue));
			}
		}
		IEnumerable<Hero> source = from x in CraftingHelper.GetAvailableHeroesForCrafting()
			where CraftingOrder.IsOrderAvailableForHero(x)
			select x;
		HasAvailableHeroes = source.Any();
		OrderPrice = CraftingOrder.BaseGoldReward;
		RefreshDifficulty();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OrderNumberText = GameTexts.FindText("str_crafting_order_header").ToString();
		OrderWeaponType = _weaponTemplate.TemplateName.ToString();
		OrderDifficultyLabelText = _difficultyText.ToString();
		OrderDifficultyValueText = TaleWorlds.Library.MathF.Round(CraftingOrder.OrderDifficulty).ToString();
		DisabledReasonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetCraftingOrderDisabledReasonTooltip(_getCurrentCraftingHero().Hero, CraftingOrder));
	}

	private void RefreshDifficulty()
	{
		Hero hero = _getCurrentCraftingHero().Hero;
		int skillValue = hero.GetSkillValue(DefaultSkills.Crafting);
		IsEnabled = CraftingOrder.IsOrderAvailableForHero(hero);
		IsDifficultySuitableForHero = CraftingOrder.OrderDifficulty < (float)skillValue;
	}

	public void ExecuteSelectOrder()
	{
		_onSelection?.Invoke(this);
	}
}

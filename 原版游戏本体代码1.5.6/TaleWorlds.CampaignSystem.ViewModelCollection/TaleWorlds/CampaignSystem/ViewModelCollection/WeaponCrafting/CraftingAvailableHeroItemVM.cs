using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;

public class CraftingAvailableHeroItemVM : ViewModel
{
	private readonly Action<CraftingAvailableHeroItemVM> _onSelection;

	private readonly ICraftingCampaignBehavior _craftingBehavior;

	private CraftingOrder _craftingOrder;

	private HeroVM _heroData;

	private BasicTooltipViewModel _hint;

	private float _currentStamina;

	private int _maxStamina;

	private string _staminaPercentage;

	private bool _isDisabled;

	private bool _isSelected;

	private int _smithySkillLevel;

	private MBBindingList<CraftingPerkVM> _craftingPerks;

	private string _perksText;

	public Hero Hero { get; }

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
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
	public HeroVM HeroData
	{
		get
		{
			return _heroData;
		}
		set
		{
			if (value != _heroData)
			{
				_heroData = value;
				OnPropertyChangedWithValue(value, "HeroData");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentStamina
	{
		get
		{
			return _currentStamina;
		}
		set
		{
			if (value != _currentStamina)
			{
				_currentStamina = value;
				OnPropertyChangedWithValue(value, "CurrentStamina");
			}
		}
	}

	[DataSourceProperty]
	public int MaxStamina
	{
		get
		{
			return _maxStamina;
		}
		set
		{
			if (value != _maxStamina)
			{
				_maxStamina = value;
				OnPropertyChangedWithValue(value, "MaxStamina");
			}
		}
	}

	[DataSourceProperty]
	public string StaminaPercentage
	{
		get
		{
			return _staminaPercentage;
		}
		set
		{
			if (value != _staminaPercentage)
			{
				_staminaPercentage = value;
				OnPropertyChangedWithValue(value, "StaminaPercentage");
			}
		}
	}

	[DataSourceProperty]
	public int SmithySkillLevel
	{
		get
		{
			return _smithySkillLevel;
		}
		set
		{
			if (value != _smithySkillLevel)
			{
				_smithySkillLevel = value;
				OnPropertyChangedWithValue(value, "SmithySkillLevel");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingPerkVM> CraftingPerks
	{
		get
		{
			return _craftingPerks;
		}
		set
		{
			if (value != _craftingPerks)
			{
				_craftingPerks = value;
				OnPropertyChangedWithValue(value, "CraftingPerks");
			}
		}
	}

	[DataSourceProperty]
	public string PerksText
	{
		get
		{
			return _perksText;
		}
		set
		{
			if (value != _perksText)
			{
				_perksText = value;
				OnPropertyChangedWithValue(value, "PerksText");
			}
		}
	}

	public CraftingAvailableHeroItemVM(Hero hero, Action<CraftingAvailableHeroItemVM> onSelection)
	{
		_onSelection = onSelection;
		_craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
		Hero = hero;
		HeroData = new HeroVM(Hero);
		Hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetCraftingHeroTooltip(Hero, _craftingOrder));
		CraftingPerks = new MBBindingList<CraftingPerkVM>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		HeroData.RefreshValues();
	}

	public void RefreshStamina()
	{
		CurrentStamina = _craftingBehavior.GetHeroCraftingStamina(Hero);
		MaxStamina = _craftingBehavior.GetMaxHeroCraftingStamina(Hero);
		int content = (int)(CurrentStamina / (float)MaxStamina * 100f);
		GameTexts.SetVariable("NUMBER", content);
		StaminaPercentage = GameTexts.FindText("str_NUMBER_percent").ToString();
	}

	public void RefreshOrderAvailability(CraftingOrder order)
	{
		_craftingOrder = order;
		if (order != null)
		{
			IsDisabled = !order.IsOrderAvailableForHero(Hero);
		}
		else
		{
			IsDisabled = false;
		}
	}

	public void RefreshSkills()
	{
		SmithySkillLevel = Hero.GetSkillValue(DefaultSkills.Crafting);
	}

	public void RefreshPerks()
	{
		CraftingPerks.Clear();
		foreach (PerkObject item in PerkObject.All)
		{
			if (item.Skill == DefaultSkills.Crafting && Hero.GetPerkValue(item))
			{
				CraftingPerks.Add(new CraftingPerkVM(item));
			}
		}
		PerksText = ((CraftingPerks.Count > 0) ? new TextObject("{=8lCWWK9G}Smithing Perks").ToString() : new TextObject("{=WHRq5Dp0}No Smithing Perks").ToString());
	}

	public void ExecuteSelection()
	{
		_onSelection?.Invoke(this);
	}
}

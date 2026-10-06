using System;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection;

public class TroopSelectionItemVM : ViewModel
{
	private readonly Action<TroopSelectionItemVM> _onAdd;

	private readonly Action<TroopSelectionItemVM> _onRemove;

	private int _currentAmount;

	private int _maxAmount;

	private int _heroHealthPercent;

	private CharacterImageIdentifierVM _visual;

	private bool _isSelected;

	private bool _isRosterFull;

	private bool _isLocked;

	private bool _isTroopHero;

	private string _name;

	private string _amountText;

	private StringItemWithHintVM _tierIconData;

	private StringItemWithHintVM _typeIconData;

	public TroopRosterElement Troop { get; private set; }

	[DataSourceProperty]
	public int MaxAmount
	{
		get
		{
			return _maxAmount;
		}
		set
		{
			if (value != _maxAmount)
			{
				_maxAmount = value;
				OnPropertyChangedWithValue(value, "MaxAmount");
				UpdateAmountText();
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
	public bool IsRosterFull
	{
		get
		{
			return _isRosterFull;
		}
		set
		{
			if (value != _isRosterFull)
			{
				_isRosterFull = value;
				OnPropertyChangedWithValue(value, "IsRosterFull");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopHero
	{
		get
		{
			return _isTroopHero;
		}
		set
		{
			if (value != _isTroopHero)
			{
				_isTroopHero = value;
				OnPropertyChangedWithValue(value, "IsTroopHero");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (value != _isLocked)
			{
				_isLocked = value;
				OnPropertyChangedWithValue(value, "IsLocked");
			}
		}
	}

	[DataSourceProperty]
	public int CurrentAmount
	{
		get
		{
			return _currentAmount;
		}
		set
		{
			if (value != _currentAmount)
			{
				_currentAmount = value;
				OnPropertyChangedWithValue(value, "CurrentAmount");
				IsSelected = value > 0;
				UpdateAmountText();
			}
		}
	}

	[DataSourceProperty]
	public int HeroHealthPercent
	{
		get
		{
			return _heroHealthPercent;
		}
		set
		{
			if (value != _heroHealthPercent)
			{
				_heroHealthPercent = value;
				OnPropertyChangedWithValue(value, "HeroHealthPercent");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public string AmountText
	{
		get
		{
			return _amountText;
		}
		set
		{
			if (value != _amountText)
			{
				_amountText = value;
				OnPropertyChangedWithValue(value, "AmountText");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TierIconData
	{
		get
		{
			return _tierIconData;
		}
		set
		{
			if (value != _tierIconData)
			{
				_tierIconData = value;
				OnPropertyChangedWithValue(value, "TierIconData");
			}
		}
	}

	[DataSourceProperty]
	public StringItemWithHintVM TypeIconData
	{
		get
		{
			return _typeIconData;
		}
		set
		{
			if (value != _typeIconData)
			{
				_typeIconData = value;
				OnPropertyChangedWithValue(value, "TypeIconData");
			}
		}
	}

	public TroopSelectionItemVM(TroopRosterElement troop, Action<TroopSelectionItemVM> onAdd, Action<TroopSelectionItemVM> onRemove)
	{
		_onAdd = onAdd;
		_onRemove = onRemove;
		Troop = troop;
		MaxAmount = Troop.Number - Troop.WoundedNumber;
		Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(troop.Character));
		Name = troop.Character.Name.ToString();
		TierIconData = CampaignUIHelper.GetCharacterTierData(Troop.Character);
		TypeIconData = CampaignUIHelper.GetCharacterTypeData(Troop.Character);
		IsTroopHero = Troop.Character.IsHero;
		HeroHealthPercent = (Troop.Character.IsHero ? TaleWorlds.Library.MathF.Ceiling((float)Troop.Character.HeroObject.HitPoints / (float)Troop.Character.MaxHitPoints() * 100f) : 0);
	}

	public void ExecuteAdd()
	{
		_onAdd?.DynamicInvokeWithLog(this);
	}

	public void ExecuteRemove()
	{
		_onRemove?.DynamicInvokeWithLog(this);
	}

	private void UpdateAmountText()
	{
		GameTexts.SetVariable("LEFT", CurrentAmount);
		GameTexts.SetVariable("RIGHT", MaxAmount);
		AmountText = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
	}

	public void ExecuteLink()
	{
		if (Troop.Character != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Troop.Character.HeroObject?.EncyclopediaLink ?? Troop.Character.EncyclopediaLink);
		}
	}
}

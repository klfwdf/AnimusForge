using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;

public class RecruitVolunteerTroopVM : ViewModel
{
	public static Action<RecruitVolunteerTroopVM> OnFocused;

	private readonly Action<RecruitVolunteerTroopVM> _onClick;

	private readonly Action<RecruitVolunteerTroopVM> _onRemoveFromCart;

	private CharacterObject _character;

	public CharacterObject Character;

	public int Index;

	private int _maximumIndexCanBeRecruit;

	private int _requiredRelation;

	public RecruitVolunteerVM Owner;

	private CharacterImageIdentifierVM _imageIdentifier;

	private string _nameText;

	private string _level;

	private bool _canBeRecruited;

	private bool _isInCart;

	private int _wage;

	private int _cost;

	private bool _isTroopEmpty;

	private bool _playerHasEnoughRelation;

	private int _currentRelation;

	private bool _isHiglightEnabled;

	private StringItemWithHintVM _tierIconData;

	private StringItemWithHintVM _typeIconData;

	[DataSourceProperty]
	public string Level
	{
		get
		{
			return _level;
		}
		set
		{
			if (value != _level)
			{
				_level = value;
				OnPropertyChangedWithValue(value, "Level");
			}
		}
	}

	[DataSourceProperty]
	public bool CanBeRecruited
	{
		get
		{
			return _canBeRecruited;
		}
		set
		{
			if (value != _canBeRecruited)
			{
				_canBeRecruited = value;
				OnPropertyChangedWithValue(value, "CanBeRecruited");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHiglightEnabled
	{
		get
		{
			return _isHiglightEnabled;
		}
		set
		{
			if (value != _isHiglightEnabled)
			{
				_isHiglightEnabled = value;
				OnPropertyChangedWithValue(value, "IsHiglightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public int Wage
	{
		get
		{
			return _wage;
		}
		set
		{
			if (value != _wage)
			{
				_wage = value;
				OnPropertyChangedWithValue(value, "Wage");
			}
		}
	}

	[DataSourceProperty]
	public int Cost
	{
		get
		{
			return _cost;
		}
		set
		{
			if (value != _cost)
			{
				_cost = value;
				OnPropertyChangedWithValue(value, "Cost");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInCart
	{
		get
		{
			return _isInCart;
		}
		set
		{
			if (value != _isInCart)
			{
				_isInCart = value;
				OnPropertyChangedWithValue(value, "IsInCart");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopEmpty
	{
		get
		{
			return _isTroopEmpty;
		}
		set
		{
			if (value != _isTroopEmpty)
			{
				_isTroopEmpty = value;
				OnPropertyChangedWithValue(value, "IsTroopEmpty");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerHasEnoughRelation
	{
		get
		{
			return _playerHasEnoughRelation;
		}
		set
		{
			if (value != _playerHasEnoughRelation)
			{
				_playerHasEnoughRelation = value;
				OnPropertyChangedWithValue(value, "PlayerHasEnoughRelation");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
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

	public RecruitVolunteerTroopVM(RecruitVolunteerVM owner, CharacterObject character, int index, Action<RecruitVolunteerTroopVM> onClick, Action<RecruitVolunteerTroopVM> onRemoveFromCart)
	{
		if (character != null)
		{
			NameText = character.Name.ToString();
			_character = character;
			GameTexts.SetVariable("LEVEL", character.Level);
			Level = GameTexts.FindText("str_level_with_value").ToString();
			Character = character;
			Wage = Character.TroopWage;
			Cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(Character, Hero.MainHero).RoundedResultNumber;
			IsTroopEmpty = false;
			CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(character);
			ImageIdentifier = new CharacterImageIdentifierVM(characterCode);
			TierIconData = CampaignUIHelper.GetCharacterTierData(character);
			TypeIconData = CampaignUIHelper.GetCharacterTypeData(character);
		}
		else
		{
			IsTroopEmpty = true;
		}
		Owner = owner;
		if (Owner != null)
		{
			_currentRelation = Hero.MainHero.GetRelation(Owner.OwnerHero);
		}
		_maximumIndexCanBeRecruit = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(Hero.MainHero, Owner.OwnerHero);
		for (int i = -100; i < 100; i++)
		{
			if (index < Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(Hero.MainHero, Owner.OwnerHero, i))
			{
				_requiredRelation = i;
				break;
			}
		}
		_onClick = onClick;
		Index = index;
		_onRemoveFromCart = onRemoveFromCart;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_character != null)
		{
			NameText = _character.Name.ToString();
			GameTexts.SetVariable("LEVEL", _character.Level);
			Level = GameTexts.FindText("str_level_with_value").ToString();
		}
	}

	public void ExecuteRecruit()
	{
		if (CanBeRecruited)
		{
			_onClick(this);
		}
		else if (IsInCart)
		{
			_onRemoveFromCart(this);
		}
	}

	public void ExecuteOpenEncyclopedia()
	{
		if (Character != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(Character.EncyclopediaLink);
		}
	}

	public void ExecuteRemoveFromCart()
	{
		if (IsInCart)
		{
			_onRemoveFromCart(this);
		}
	}

	public virtual void ExecuteBeginHint()
	{
		if (_character != null)
		{
			if (PlayerHasEnoughRelation)
			{
				InformationManager.ShowTooltip(typeof(CharacterObject), _character);
				return;
			}
			List<TooltipProperty> list = new List<TooltipProperty>();
			string text = "";
			list.Add(new TooltipProperty(text, _character.Name.ToString(), 1));
			list.Add(new TooltipProperty(text, text, -1));
			GameTexts.SetVariable("LEVEL", _character.Level);
			GameTexts.SetVariable("newline", "\n");
			list.Add(new TooltipProperty(text, GameTexts.FindText("str_level_with_value").ToString(), 0));
			GameTexts.SetVariable("REL1", _currentRelation);
			GameTexts.SetVariable("REL2", _requiredRelation);
			list.Add(new TooltipProperty(text, GameTexts.FindText("str_recruit_volunteers_not_enough_relation").ToString(), 0));
			InformationManager.ShowTooltip(typeof(List<TooltipProperty>), list);
		}
		else if (PlayerHasEnoughRelation)
		{
			MBInformationManager.ShowHint(GameTexts.FindText("str_recruit_volunteers_new_troop").ToString());
		}
		else
		{
			GameTexts.SetVariable("newline", "\n");
			GameTexts.SetVariable("REL1", _currentRelation);
			GameTexts.SetVariable("REL2", _requiredRelation);
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_recruit_volunteers_new_troop"));
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_recruit_volunteers_not_enough_relation"));
			MBInformationManager.ShowHint(GameTexts.FindText("str_string_newline_string").ToString());
		}
	}

	public virtual void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteFocus()
	{
		if (!IsTroopEmpty)
		{
			OnFocused?.Invoke(this);
		}
	}

	public void ExecuteUnfocus()
	{
		OnFocused?.Invoke(null);
	}
}

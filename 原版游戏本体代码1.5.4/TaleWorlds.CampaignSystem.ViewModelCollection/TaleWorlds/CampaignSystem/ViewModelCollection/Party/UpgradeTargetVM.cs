using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class UpgradeTargetVM : ViewModel
{
	private CharacterObject _originalCharacter;

	private CharacterObject _upgradeTarget;

	private Action<int, int> _onUpgraded;

	private Action<UpgradeTargetVM> _onFocused;

	private int _upgradeIndex;

	private string _hintString;

	private InputKeyItemVM _primaryActionInputKey;

	private InputKeyItemVM _secondaryActionInputKey;

	private InputKeyItemVM _tertiaryActionInputKey;

	private UpgradeRequirementsVM _requirements;

	private CharacterImageIdentifierVM _troopImage;

	private BasicTooltipViewModel _hint;

	private int _availableUpgrades;

	private bool _isAvailable;

	private bool _isInsufficient;

	private bool _isHighlighted;

	private bool _isMarinerTroop;

	[DataSourceProperty]
	public InputKeyItemVM PrimaryActionInputKey
	{
		get
		{
			return _primaryActionInputKey;
		}
		set
		{
			if (value != _primaryActionInputKey)
			{
				_primaryActionInputKey = value;
				OnPropertyChangedWithValue(value, "PrimaryActionInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM SecondaryActionInputKey
	{
		get
		{
			return _secondaryActionInputKey;
		}
		set
		{
			if (value != _secondaryActionInputKey)
			{
				_secondaryActionInputKey = value;
				OnPropertyChangedWithValue(value, "SecondaryActionInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM TertiaryActionInputKey
	{
		get
		{
			return _tertiaryActionInputKey;
		}
		set
		{
			if (value != _tertiaryActionInputKey)
			{
				_tertiaryActionInputKey = value;
				OnPropertyChangedWithValue(value, "TertiaryActionInputKey");
			}
		}
	}

	[DataSourceProperty]
	public UpgradeRequirementsVM Requirements
	{
		get
		{
			return _requirements;
		}
		set
		{
			if (value != _requirements)
			{
				_requirements = value;
				OnPropertyChangedWithValue(value, "Requirements");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM TroopImage
	{
		get
		{
			return _troopImage;
		}
		set
		{
			if (value != _troopImage)
			{
				_troopImage = value;
				OnPropertyChangedWithValue(value, "TroopImage");
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
	public int AvailableUpgrades
	{
		get
		{
			return _availableUpgrades;
		}
		set
		{
			if (value != _availableUpgrades)
			{
				_availableUpgrades = value;
				OnPropertyChangedWithValue(value, "AvailableUpgrades");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAvailable
	{
		get
		{
			return _isAvailable;
		}
		set
		{
			if (value != _isAvailable)
			{
				_isAvailable = value;
				OnPropertyChangedWithValue(value, "IsAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInsufficient
	{
		get
		{
			return _isInsufficient;
		}
		set
		{
			if (value != _isInsufficient)
			{
				_isInsufficient = value;
				OnPropertyChangedWithValue(value, "IsInsufficient");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHighlighted
	{
		get
		{
			return _isHighlighted;
		}
		set
		{
			if (value != _isHighlighted)
			{
				_isHighlighted = value;
				OnPropertyChangedWithValue(value, "IsHighlighted");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMarinerTroop
	{
		get
		{
			return _isMarinerTroop;
		}
		set
		{
			if (value != _isMarinerTroop)
			{
				_isMarinerTroop = value;
				OnPropertyChangedWithValue(value, "IsMarinerTroop");
			}
		}
	}

	public UpgradeTargetVM(int upgradeIndex, CharacterObject character, CharacterCode upgradeCharacterCode, Action<int, int> onUpgraded, Action<UpgradeTargetVM> onFocused)
	{
		_upgradeIndex = upgradeIndex;
		_originalCharacter = character;
		_upgradeTarget = _originalCharacter.UpgradeTargets[upgradeIndex];
		_onUpgraded = onUpgraded;
		_onFocused = onFocused;
		Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, _originalCharacter, _upgradeTarget, out var requiredPerk);
		Requirements = new UpgradeRequirementsVM();
		Requirements.SetItemRequirement(_upgradeTarget.UpgradeRequiresItemFromCategory);
		Requirements.SetPerkRequirement(requiredPerk);
		TroopImage = new CharacterImageIdentifierVM(upgradeCharacterCode);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Requirements?.RefreshValues();
	}

	public void Refresh(int upgradableAmount, bool isAvailable, bool isInsufficient, bool itemRequirementsMet, bool perkRequirementsMet, string hintString, bool isMarinerTroop)
	{
		AvailableUpgrades = upgradableAmount;
		IsAvailable = isAvailable;
		IsInsufficient = isInsufficient;
		IsMarinerTroop = isMarinerTroop;
		Requirements?.SetRequirementsMet(itemRequirementsMet, perkRequirementsMet);
		_hintString = hintString;
		Hint = new BasicTooltipViewModel(() => GetHint());
	}

	private string GetHint()
	{
		string stackModifierString = CampaignUIHelper.GetStackModifierString(GameTexts.FindText("str_entire_stack_shortcut_upgrade_units"), GameTexts.FindText("str_five_stack_shortcut_upgrade_units"), AvailableUpgrades >= 5);
		if (string.IsNullOrEmpty(stackModifierString) || AvailableUpgrades < 1)
		{
			return _hintString;
		}
		return GameTexts.FindText("str_string_newline_string").SetTextVariable("STR1", _hintString).SetTextVariable("STR2", stackModifierString)
			.ToString();
	}

	public void ExecuteUpgradeEncyclopediaLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(_upgradeTarget.EncyclopediaLink);
	}

	public void ExecuteUpgrade()
	{
		if (IsAvailable && !IsInsufficient)
		{
			_onUpgraded?.Invoke(_upgradeIndex, AvailableUpgrades);
		}
	}

	public void ExecuteSetFocused()
	{
		if (_upgradeTarget != null)
		{
			_onFocused?.Invoke(this);
		}
	}

	public void ExecuteSetUnfocused()
	{
		_onFocused?.Invoke(null);
	}
}

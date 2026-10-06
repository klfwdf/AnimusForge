using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class ArmyCompositionItemVM : ViewModel
{
	public enum CompositionType
	{
		MeleeInfantry,
		RangedInfantry,
		MeleeCavalry,
		RangedCavalry
	}

	private readonly MBReadOnlyList<SkillObject> _allSkills;

	private readonly List<BasicCharacterObject> _allCharacterObjects;

	private readonly Action<int, int> _onCompositionValueChanged;

	private readonly TroopTypeSelectionPopUpVM _troopTypeSelectionPopUp;

	private BasicCultureObject _culture;

	private readonly CompositionType _type;

	private readonly int[] _compositionValues;

	private MBBindingList<CustomBattleTroopTypeVM> _troopTypes;

	private HintViewModel _invalidHint;

	private HintViewModel _addTroopTypeHint;

	private bool _isLocked;

	private bool _isValid;

	private string _compositionValuePercentageText;

	[DataSourceProperty]
	public MBBindingList<CustomBattleTroopTypeVM> TroopTypes
	{
		get
		{
			return _troopTypes;
		}
		set
		{
			if (value != _troopTypes)
			{
				_troopTypes = value;
				OnPropertyChangedWithValue(value, "TroopTypes");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InvalidHint
	{
		get
		{
			return _invalidHint;
		}
		set
		{
			if (value != _invalidHint)
			{
				_invalidHint = value;
				OnPropertyChangedWithValue(value, "InvalidHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AddTroopTypeHint
	{
		get
		{
			return _addTroopTypeHint;
		}
		set
		{
			if (value != _addTroopTypeHint)
			{
				_addTroopTypeHint = value;
				OnPropertyChangedWithValue(value, "AddTroopTypeHint");
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
	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			if (value != _isValid)
			{
				_isValid = value;
				OnPropertyChangedWithValue(value, "IsValid");
			}
			OnValidityChanged(value);
		}
	}

	[DataSourceProperty]
	public int CompositionValue
	{
		get
		{
			return _compositionValues[(int)_type];
		}
		set
		{
			if (value != _compositionValues[(int)_type])
			{
				_onCompositionValueChanged(value, (int)_type);
			}
		}
	}

	[DataSourceProperty]
	public string CompositionValuePercentageText
	{
		get
		{
			return _compositionValuePercentageText;
		}
		set
		{
			if (value != _compositionValuePercentageText)
			{
				_compositionValuePercentageText = value;
				OnPropertyChangedWithValue(value, "CompositionValuePercentageText");
			}
		}
	}

	public ArmyCompositionItemVM(CompositionType type, List<BasicCharacterObject> allCharacterObjects, MBReadOnlyList<SkillObject> allSkills, Action<int, int> onCompositionValueChanged, TroopTypeSelectionPopUpVM troopTypeSelectionPopUp, int[] compositionValues)
	{
		_allCharacterObjects = allCharacterObjects;
		_allSkills = allSkills;
		_onCompositionValueChanged = onCompositionValueChanged;
		_troopTypeSelectionPopUp = troopTypeSelectionPopUp;
		_type = type;
		_compositionValues = compositionValues;
		TroopTypes = new MBBindingList<CustomBattleTroopTypeVM>();
		InvalidHint = new HintViewModel(new TextObject("{=iSQTtNUD}This faction doesn't have this troop type."));
		AddTroopTypeHint = new HintViewModel(new TextObject("{=eMbuGGus}Select troops to spawn in formation."));
		UpdatePercentageText(_compositionValues[(int)_type]);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
	}

	public void SetCurrentSelectedCulture(BasicCultureObject culture)
	{
		IsLocked = false;
		_culture = culture;
		PopulateTroopTypes();
	}

	public void ExecuteRandomize(int compositionValue)
	{
		IsValid = true;
		IsLocked = false;
		CompositionValue = compositionValue;
		IsValid = TroopTypes.Count > 0;
		TroopTypes.ApplyActionOnAllItems(delegate(CustomBattleTroopTypeVM x)
		{
			x.ExecuteRandomize();
		});
		if (!TroopTypes.Any((CustomBattleTroopTypeVM x) => x.IsSelected) && IsValid)
		{
			TroopTypes[0].IsSelected = true;
		}
	}

	public void ExecuteAddTroopTypes()
	{
		string title = GameTexts.FindText("str_custom_battle_choose_troop", _type.ToString()).ToString();
		_troopTypeSelectionPopUp?.OpenPopUp(title, TroopTypes);
	}

	public void RefreshCompositionValue()
	{
		OnPropertyChanged("CompositionValue");
		UpdatePercentageText(_compositionValues[(int)_type]);
	}

	private void UpdatePercentageText(int percentage)
	{
		int variable = (int)TaleWorlds.Library.MathF.Clamp(percentage, 0f, 100f);
		CompositionValuePercentageText = GameTexts.FindText("str_NUMBER_percent").SetTextVariable("NUMBER", variable).ToString();
	}

	private void OnValidityChanged(bool value)
	{
		IsLocked = false;
		if (!value)
		{
			CompositionValue = 0;
		}
		IsLocked = !value;
	}

	private void PopulateTroopTypes()
	{
		TroopTypes.Clear();
		MBReadOnlyList<BasicCharacterObject> defaultCharacters = GetDefaultCharacters();
		foreach (BasicCharacterObject allCharacterObject in _allCharacterObjects)
		{
			if (IsValidUnitItem(allCharacterObject))
			{
				TroopTypes.Add(new CustomBattleTroopTypeVM(allCharacterObject, _troopTypeSelectionPopUp.OnItemSelectionToggled, GetTroopTypeIconData(allCharacterObject, _type), _allSkills, defaultCharacters.Contains(allCharacterObject)));
			}
		}
		IsValid = TroopTypes.Count > 0;
		if (IsValid && !TroopTypes.Any((CustomBattleTroopTypeVM x) => x.IsDefault))
		{
			TroopTypes[0].IsDefault = true;
		}
		TroopTypes.ApplyActionOnAllItems(delegate(CustomBattleTroopTypeVM x)
		{
			x.IsSelected = x.IsDefault;
		});
	}

	private bool IsValidUnitItem(BasicCharacterObject o)
	{
		if (o != null && _culture == o.Culture)
		{
			switch (_type)
			{
			case CompositionType.MeleeInfantry:
				if (o.DefaultFormationClass != FormationClass.Infantry)
				{
					return o.DefaultFormationClass == FormationClass.HeavyInfantry;
				}
				return true;
			case CompositionType.RangedInfantry:
				return o.DefaultFormationClass == FormationClass.Ranged;
			case CompositionType.MeleeCavalry:
				if (o.DefaultFormationClass != FormationClass.Cavalry && o.DefaultFormationClass != FormationClass.HeavyCavalry)
				{
					return o.DefaultFormationClass == FormationClass.LightCavalry;
				}
				return true;
			case CompositionType.RangedCavalry:
				return o.DefaultFormationClass == FormationClass.HorseArcher;
			default:
				return false;
			}
		}
		return false;
	}

	private MBReadOnlyList<BasicCharacterObject> GetDefaultCharacters()
	{
		MBList<BasicCharacterObject> mBList = new MBList<BasicCharacterObject>();
		FormationClass formation = FormationClass.NumberOfAllFormations;
		switch (_type)
		{
		case CompositionType.MeleeInfantry:
			formation = FormationClass.Infantry;
			break;
		case CompositionType.RangedInfantry:
			formation = FormationClass.Ranged;
			break;
		case CompositionType.MeleeCavalry:
			formation = FormationClass.Cavalry;
			break;
		case CompositionType.RangedCavalry:
			formation = FormationClass.HorseArcher;
			break;
		}
		mBList.Add(CustomBattleHelper.GetDefaultTroopOfFormationForFaction(_culture, formation));
		return mBList;
	}

	public static StringItemWithHintVM GetTroopTypeIconData(BasicCharacterObject basicCharacterObject, CompositionType type, bool isBig = false)
	{
		bool flag = false;
		if (basicCharacterObject != null)
		{
			flag = basicCharacterObject.StringId.Contains("marine") || basicCharacterObject.Culture.StringId.Contains("nord");
		}
		TextObject textObject = new TextObject("{=!}{TYPENAME}{MARINER}{BIG}");
		TextObject textObject2;
		switch (type)
		{
		case CompositionType.RangedCavalry:
			textObject.SetTextVariable("TYPENAME", "horse_archer");
			textObject2 = GameTexts.FindText("str_troop_type_name", "HorseArcher");
			break;
		case CompositionType.RangedInfantry:
		{
			textObject.SetTextVariable("TYPENAME", "bow");
			string variation2 = (flag ? "Ranged_Mariner" : "Ranged");
			textObject2 = GameTexts.FindText("str_troop_type_name", variation2);
			break;
		}
		case CompositionType.MeleeCavalry:
			textObject.SetTextVariable("TYPENAME", "cavalry");
			textObject2 = GameTexts.FindText("str_troop_type_name", "Cavalry");
			break;
		case CompositionType.MeleeInfantry:
		{
			textObject.SetTextVariable("TYPENAME", "infantry");
			string variation = (flag ? "Infantry_Mariner" : "Infantry");
			textObject2 = GameTexts.FindText("str_troop_type_name", variation);
			break;
		}
		default:
			return new StringItemWithHintVM("", null);
		}
		textObject.SetTextVariable("MARINER", flag ? "_mariner" : "");
		textObject.SetTextVariable("BIG", isBig ? "_big" : "");
		return new StringItemWithHintVM("General\\TroopTypeIcons\\icon_troop_type_" + textObject.ToString(), new TextObject("{=!}" + textObject2.ToString()));
	}
}

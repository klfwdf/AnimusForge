using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

[EncyclopediaViewModel(typeof(CharacterObject))]
public class EncyclopediaUnitPageVM : EncyclopediaContentPageVM
{
	private readonly CharacterObject _character;

	private TextObject _equipmentSetTextObj;

	private MBBindingList<EncyclopediaSkillVM> _skills;

	private MBBindingList<StringItemWithHintVM> _propertiesList;

	private SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM> _equipmentSetSelector;

	private EncyclopediaUnitEquipmentSetSelectorItemVM _currentSelectedEquipmentSet;

	private EncyclopediaTroopTreeNodeVM _tree;

	private string _descriptionText;

	private CharacterViewModel _unitCharacter;

	private string _nameText;

	private string _treeDisplayErrorText;

	private string _equipmentSetText;

	private bool _hasErrors;

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSkillVM> Skills
	{
		get
		{
			return _skills;
		}
		set
		{
			if (value != _skills)
			{
				_skills = value;
				OnPropertyChangedWithValue(value, "Skills");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringItemWithHintVM> PropertiesList
	{
		get
		{
			return _propertiesList;
		}
		set
		{
			if (value != _propertiesList)
			{
				_propertiesList = value;
				OnPropertyChangedWithValue(value, "PropertiesList");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM> EquipmentSetSelector
	{
		get
		{
			return _equipmentSetSelector;
		}
		set
		{
			if (value != _equipmentSetSelector)
			{
				_equipmentSetSelector = value;
				OnPropertyChangedWithValue(value, "EquipmentSetSelector");
			}
		}
	}

	[DataSourceProperty]
	public EncyclopediaUnitEquipmentSetSelectorItemVM CurrentSelectedEquipmentSet
	{
		get
		{
			return _currentSelectedEquipmentSet;
		}
		set
		{
			if (value != _currentSelectedEquipmentSet)
			{
				_currentSelectedEquipmentSet = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedEquipmentSet");
			}
		}
	}

	[DataSourceProperty]
	public CharacterViewModel UnitCharacter
	{
		get
		{
			return _unitCharacter;
		}
		set
		{
			if (value != _unitCharacter)
			{
				_unitCharacter = value;
				OnPropertyChangedWithValue(value, "UnitCharacter");
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
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public EncyclopediaTroopTreeNodeVM Tree
	{
		get
		{
			return _tree;
		}
		set
		{
			if (value != _tree)
			{
				_tree = value;
				OnPropertyChangedWithValue(value, "Tree");
			}
		}
	}

	[DataSourceProperty]
	public string TreeDisplayErrorText
	{
		get
		{
			return _treeDisplayErrorText;
		}
		set
		{
			if (value != _treeDisplayErrorText)
			{
				_treeDisplayErrorText = value;
				OnPropertyChangedWithValue(value, "TreeDisplayErrorText");
			}
		}
	}

	[DataSourceProperty]
	public string EquipmentSetText
	{
		get
		{
			return _equipmentSetText;
		}
		set
		{
			if (value != _equipmentSetText)
			{
				_equipmentSetText = value;
				OnPropertyChangedWithValue(value, "EquipmentSetText");
			}
		}
	}

	[DataSourceProperty]
	public bool HasErrors
	{
		get
		{
			return _hasErrors;
		}
		set
		{
			if (value != _hasErrors)
			{
				_hasErrors = value;
				OnPropertyChangedWithValue(value, "HasErrors");
			}
		}
	}

	public EncyclopediaUnitPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
		_character = base.Obj as CharacterObject;
		UnitCharacter = new CharacterViewModel(CharacterViewModel.StanceTypes.OnMount);
		UnitCharacter.FillFrom(_character);
		HasErrors = DoesCharacterHaveCircularUpgradePaths(_character);
		if (!HasErrors)
		{
			CharacterObject rootCharacter = CharacterHelper.FindUpgradeRootOf(_character);
			Tree = new EncyclopediaTroopTreeNodeVM(rootCharacter, _character, isAlternativeUpgrade: false);
		}
		PropertiesList = new MBBindingList<StringItemWithHintVM>();
		EquipmentSetSelector = new SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM>(0, OnEquipmentSetChange);
		base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(_character);
		RefreshValues();
	}

	private bool DoesCharacterHaveCircularUpgradePaths(CharacterObject baseCharacter, CharacterObject character = null)
	{
		bool result = false;
		if (character == null)
		{
			character = baseCharacter;
		}
		for (int i = 0; i < character.UpgradeTargets.Length; i++)
		{
			if (character.UpgradeTargets[i] == baseCharacter)
			{
				Debug.FailedAssert($"Circular dependency on troop upgrade paths: {character.Name} --> {baseCharacter.Name}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Pages\\EncyclopediaUnitPageVM.cs", "DoesCharacterHaveCircularUpgradePaths", 56);
				result = true;
				break;
			}
			result = DoesCharacterHaveCircularUpgradePaths(baseCharacter, character.UpgradeTargets[i]);
		}
		return result;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_equipmentSetTextObj = new TextObject("{=vggt7exj}Set {CURINDEX}/{COUNT}");
		PropertiesList.Clear();
		PropertiesList.Add(CampaignUIHelper.GetCharacterTierData(_character, isBig: true));
		PropertiesList.Add(CampaignUIHelper.GetCharacterTypeData(_character, isBig: true));
		EquipmentSetSelector.ItemList.Clear();
		foreach (Equipment equipment in _character.BattleEquipments)
		{
			if (!EquipmentSetSelector.ItemList.Any((EncyclopediaUnitEquipmentSetSelectorItemVM x) => x.EquipmentSet.IsEquipmentEqualTo(equipment)))
			{
				EquipmentSetSelector.AddItem(new EncyclopediaUnitEquipmentSetSelectorItemVM(equipment));
			}
		}
		if (EquipmentSetSelector.ItemList.Count > 0)
		{
			EquipmentSetSelector.SelectedIndex = 0;
		}
		_equipmentSetTextObj.SetTextVariable("CURINDEX", EquipmentSetSelector.SelectedIndex + 1);
		_equipmentSetTextObj.SetTextVariable("COUNT", EquipmentSetSelector.ItemList.Count);
		EquipmentSetText = _equipmentSetTextObj.ToString();
		TreeDisplayErrorText = new TextObject("{=BkDycbdq}Error while displaying the troop tree").ToString();
		Skills = new MBBindingList<EncyclopediaSkillVM>();
		List<SkillObject> list = TaleWorlds.CampaignSystem.Extensions.Skills.All.ToList();
		list.Sort(CampaignUIHelper.SkillObjectComparerInstance);
		foreach (SkillObject item in list)
		{
			if (_character.GetSkillValue(item) > 0)
			{
				Skills.Add(new EncyclopediaSkillVM(item, _character.GetSkillValue(item)));
			}
		}
		DescriptionText = GameTexts.FindText("str_encyclopedia_unit_description", _character.StringId).ToString();
		NameText = _character.Name.ToString();
		Tree?.RefreshValues();
		UnitCharacter?.RefreshValues();
		UpdateBookmarkHintText();
	}

	private void OnEquipmentSetChange(SelectorVM<EncyclopediaUnitEquipmentSetSelectorItemVM> selector)
	{
		CurrentSelectedEquipmentSet = selector.SelectedItem;
		UnitCharacter.SetEquipment(CurrentSelectedEquipmentSet.EquipmentSet);
		_equipmentSetTextObj.SetTextVariable("CURINDEX", selector.SelectedIndex + 1);
		_equipmentSetTextObj.SetTextVariable("COUNT", selector.ItemList.Count);
		EquipmentSetText = _equipmentSetTextObj.ToString();
	}

	public override string GetName()
	{
		return _character.Name.ToString();
	}

	public override string GetNavigationBarURL()
	{
		return string.Concat(string.Concat(string.Concat(HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ", HyperlinkTexts.GetGenericHyperlinkText("ListPage-Units", GameTexts.FindText("str_encyclopedia_troops").ToString())), " \\ "), GetName());
	}

	public override void ExecuteSwitchBookmarkedState()
	{
		base.ExecuteSwitchBookmarkedState();
		if (base.IsBookmarked)
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(_character);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(_character);
		}
	}
}

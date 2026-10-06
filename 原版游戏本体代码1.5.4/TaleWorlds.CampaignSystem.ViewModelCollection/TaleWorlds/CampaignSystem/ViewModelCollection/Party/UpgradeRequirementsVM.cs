using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party;

public class UpgradeRequirementsVM : ViewModel
{
	private ItemCategory _category;

	private PerkObject _perk;

	private bool _isItemRequirementMet;

	private bool _isPerkRequirementMet;

	private bool _hasItemRequirement;

	private bool _hasPerkRequirement;

	private string _perkRequirement = "";

	private string _itemRequirement = "";

	private HintViewModel _itemRequirementHint;

	private HintViewModel _perkRequirementHint;

	[DataSourceProperty]
	public bool IsItemRequirementMet
	{
		get
		{
			return _isItemRequirementMet;
		}
		set
		{
			if (value != _isItemRequirementMet)
			{
				_isItemRequirementMet = value;
				OnPropertyChangedWithValue(value, "IsItemRequirementMet");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPerkRequirementMet
	{
		get
		{
			return _isPerkRequirementMet;
		}
		set
		{
			if (value != _isPerkRequirementMet)
			{
				_isPerkRequirementMet = value;
				OnPropertyChangedWithValue(value, "IsPerkRequirementMet");
			}
		}
	}

	[DataSourceProperty]
	public bool HasItemRequirement
	{
		get
		{
			return _hasItemRequirement;
		}
		set
		{
			if (value != _hasItemRequirement)
			{
				_hasItemRequirement = value;
				OnPropertyChangedWithValue(value, "HasItemRequirement");
			}
		}
	}

	[DataSourceProperty]
	public bool HasPerkRequirement
	{
		get
		{
			return _hasPerkRequirement;
		}
		set
		{
			if (value != _hasPerkRequirement)
			{
				_hasPerkRequirement = value;
				OnPropertyChangedWithValue(value, "HasPerkRequirement");
			}
		}
	}

	[DataSourceProperty]
	public string PerkRequirement
	{
		get
		{
			return _perkRequirement;
		}
		set
		{
			if (value != _perkRequirement)
			{
				_perkRequirement = value;
				OnPropertyChangedWithValue(value, "PerkRequirement");
			}
		}
	}

	[DataSourceProperty]
	public string ItemRequirement
	{
		get
		{
			return _itemRequirement;
		}
		set
		{
			if (value != _itemRequirement)
			{
				_itemRequirement = value;
				OnPropertyChangedWithValue(value, "ItemRequirement");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ItemRequirementHint
	{
		get
		{
			return _itemRequirementHint;
		}
		set
		{
			if (value != _itemRequirementHint)
			{
				_itemRequirementHint = value;
				OnPropertyChangedWithValue(value, "ItemRequirementHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PerkRequirementHint
	{
		get
		{
			return _perkRequirementHint;
		}
		set
		{
			if (value != _perkRequirementHint)
			{
				_perkRequirementHint = value;
				OnPropertyChangedWithValue(value, "PerkRequirementHint");
			}
		}
	}

	public UpgradeRequirementsVM()
	{
		IsItemRequirementMet = true;
		IsPerkRequirementMet = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		UpdateItemRequirementHint();
		UpdatePerkRequirementHint();
	}

	public void SetItemRequirement(ItemCategory category)
	{
		if (category != null)
		{
			HasItemRequirement = true;
			_category = category;
			ItemRequirement = category.StringId.ToLower();
			UpdateItemRequirementHint();
		}
	}

	public void SetPerkRequirement(PerkObject perk)
	{
		if (perk != null)
		{
			HasPerkRequirement = true;
			_perk = perk;
			PerkRequirement = perk.Skill.StringId.ToLower();
			UpdatePerkRequirementHint();
		}
	}

	public void SetRequirementsMet(bool isItemRequirementMet, bool isPerkRequirementMet)
	{
		IsItemRequirementMet = !HasItemRequirement || isItemRequirementMet;
		IsPerkRequirementMet = !HasPerkRequirement || isPerkRequirementMet;
	}

	private void UpdateItemRequirementHint()
	{
		if (_category != null)
		{
			TextObject textObject = new TextObject("{=Q0j1umAt}Requirement: {REQUIREMENT_NAME}");
			textObject.SetTextVariable("REQUIREMENT_NAME", _category.GetName().ToString());
			ItemRequirementHint = new HintViewModel(textObject);
		}
	}

	private void UpdatePerkRequirementHint()
	{
		if (_perk != null)
		{
			TextObject textObject = new TextObject("{=Q0j1umAt}Requirement: {REQUIREMENT_NAME}");
			textObject.SetTextVariable("REQUIREMENT_NAME", _perk.Name.ToString());
			PerkRequirementHint = new HintViewModel(textObject);
		}
	}
}

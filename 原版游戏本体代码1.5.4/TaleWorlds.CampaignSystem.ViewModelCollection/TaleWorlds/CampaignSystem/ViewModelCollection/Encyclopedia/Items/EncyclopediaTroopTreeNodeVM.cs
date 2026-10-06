using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaTroopTreeNodeVM : ViewModel
{
	private MBBindingList<EncyclopediaTroopTreeNodeVM> _branch;

	private EncyclopediaUnitVM _unit;

	private bool _isActiveUnit;

	private bool _isAlternativeUpgrade;

	private BasicTooltipViewModel _alternativeUpgradeTooltip;

	[DataSourceProperty]
	public bool IsActiveUnit
	{
		get
		{
			return _isActiveUnit;
		}
		set
		{
			if (value != _isActiveUnit)
			{
				_isActiveUnit = value;
				OnPropertyChangedWithValue(value, "IsActiveUnit");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAlternativeUpgrade
	{
		get
		{
			return _isAlternativeUpgrade;
		}
		set
		{
			if (value != _isAlternativeUpgrade)
			{
				_isAlternativeUpgrade = value;
				OnPropertyChangedWithValue(value, "IsAlternativeUpgrade");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaTroopTreeNodeVM> Branch
	{
		get
		{
			return _branch;
		}
		set
		{
			if (value != _branch)
			{
				_branch = value;
				OnPropertyChangedWithValue(value, "Branch");
			}
		}
	}

	[DataSourceProperty]
	public EncyclopediaUnitVM Unit
	{
		get
		{
			return _unit;
		}
		set
		{
			if (value != _unit)
			{
				_unit = value;
				OnPropertyChangedWithValue(value, "Unit");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel AlternativeUpgradeTooltip
	{
		get
		{
			return _alternativeUpgradeTooltip;
		}
		set
		{
			if (value != _alternativeUpgradeTooltip)
			{
				_alternativeUpgradeTooltip = value;
				OnPropertyChangedWithValue(value, "AlternativeUpgradeTooltip");
			}
		}
	}

	public EncyclopediaTroopTreeNodeVM(CharacterObject rootCharacter, CharacterObject activeCharacter, bool isAlternativeUpgrade, PerkObject alternativeUpgradePerk = null)
	{
		Branch = new MBBindingList<EncyclopediaTroopTreeNodeVM>();
		IsActiveUnit = rootCharacter == activeCharacter;
		IsAlternativeUpgrade = isAlternativeUpgrade;
		if (alternativeUpgradePerk != null && IsAlternativeUpgrade)
		{
			AlternativeUpgradeTooltip = new BasicTooltipViewModel(delegate
			{
				TextObject textObject = new TextObject("{=LVJKy6a8}This troop requires {PERK_NAME} ({PERK_SKILL}) perk to upgrade.");
				textObject.SetTextVariable("PERK_NAME", alternativeUpgradePerk.Name);
				textObject.SetTextVariable("PERK_SKILL", alternativeUpgradePerk.Skill.Name);
				return textObject.ToString();
			});
		}
		Unit = new EncyclopediaUnitVM(rootCharacter, IsActiveUnit);
		CharacterObject[] upgradeTargets = rootCharacter.UpgradeTargets;
		foreach (CharacterObject characterObject in upgradeTargets)
		{
			if (characterObject == rootCharacter)
			{
				Debug.FailedAssert("A character cannot be it's own upgrade target!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Items\\EncyclopediaTroopTreeNodeVM.cs", ".ctor", 36);
			}
			else if (Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject)).IsValidEncyclopediaItem(characterObject))
			{
				bool isAlternativeUpgrade2 = rootCharacter.Culture.IsBandit && !characterObject.Culture.IsBandit;
				Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, rootCharacter, characterObject, out var requiredPerk);
				Branch.Add(new EncyclopediaTroopTreeNodeVM(characterObject, activeCharacter, isAlternativeUpgrade2, requiredPerk));
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Branch.ApplyActionOnAllItems(delegate(EncyclopediaTroopTreeNodeVM x)
		{
			x.RefreshValues();
		});
		Unit.RefreshValues();
	}
}

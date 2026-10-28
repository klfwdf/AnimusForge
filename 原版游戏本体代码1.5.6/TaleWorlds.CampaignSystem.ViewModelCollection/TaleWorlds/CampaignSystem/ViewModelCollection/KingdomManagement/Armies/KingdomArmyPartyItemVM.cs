using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;

public class KingdomArmyPartyItemVM : ViewModel
{
	private MobileParty _party;

	private CharacterImageIdentifierVM _visual;

	private string _name;

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

	public KingdomArmyPartyItemVM(MobileParty party)
	{
		_party = party;
		Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(party.LeaderHero?.CharacterObject));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _party.Name.ToString();
	}

	private void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(MobileParty), _party, true, false);
	}

	private void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteLink()
	{
		if (_party != null && _party.LeaderHero != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_party.LeaderHero.EncyclopediaLink);
		}
	}
}

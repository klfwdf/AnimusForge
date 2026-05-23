using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanPartyMemberItemVM : ViewModel
{
	private CharacterImageIdentifierVM _visual;

	private BannerImageIdentifierVM _banner_9;

	private string _name;

	private bool _isLeader;

	private HeroViewModel _heroModel;

	public Hero HeroObject { get; private set; }

	[DataSourceProperty]
	public HeroViewModel HeroModel
	{
		get
		{
			return _heroModel;
		}
		set
		{
			if (value != _heroModel)
			{
				_heroModel = value;
				OnPropertyChangedWithValue(value, "HeroModel");
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
	public BannerImageIdentifierVM Banner_9
	{
		get
		{
			return _banner_9;
		}
		set
		{
			if (value != _banner_9)
			{
				_banner_9 = value;
				OnPropertyChangedWithValue(value, "Banner_9");
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
	public bool IsLeader
	{
		get
		{
			return _isLeader;
		}
		set
		{
			if (value != _isLeader)
			{
				_isLeader = value;
				OnPropertyChangedWithValue(value, "IsLeader");
			}
		}
	}

	public ClanPartyMemberItemVM(Hero hero, MobileParty party)
	{
		HeroObject = hero;
		IsLeader = hero == party.LeaderHero;
		CharacterCode characterCode = CampaignUIHelper.GetCharacterCode(hero.CharacterObject);
		Visual = new CharacterImageIdentifierVM(characterCode);
		HeroModel = new HeroViewModel();
		HeroModel.FillFrom(HeroObject);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = HeroObject.Name.ToString();
		UpdateProperties();
	}

	private void ExecuteLocationLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	public void UpdateProperties()
	{
		HeroModel = new HeroViewModel();
		HeroModel.FillFrom(HeroObject);
		Banner_9 = new BannerImageIdentifierVM(HeroObject.ClanBanner, nineGrid: true);
	}

	public void ExecuteLink()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(HeroObject.EncyclopediaLink);
	}

	public virtual void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(Hero), HeroObject, true);
	}

	public virtual void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		HeroModel.OnFinalize();
	}
}

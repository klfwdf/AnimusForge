using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;

public class SettlementDecisionItemVM : DecisionItemBaseVM
{
	private SettlementClaimantDecision _settlementDecision;

	private SettlementClaimantPreliminaryDecision _settlementPreliminaryDecision;

	private Settlement _settlement;

	private string _settlementName;

	private HeroVM _governor;

	private MBBindingList<EncyclopediaSettlementVM> _boundVillages;

	private MBBindingList<HeroVM> _notableCharacters;

	private BasicTooltipViewModel _militasHint;

	private BasicTooltipViewModel _prosperityHint;

	private BasicTooltipViewModel _loyaltyHint;

	private BasicTooltipViewModel _securityHint;

	private BasicTooltipViewModel _wallsHint;

	private BasicTooltipViewModel _garrisonHint;

	private BasicTooltipViewModel _foodHint;

	private HeroVM _owner;

	private string _ownerText;

	private string _militasText;

	private string _garrisonText;

	private string _prosperityText;

	private string _loyaltyText;

	private string _securityText;

	private string _wallsText;

	private string _foodText;

	private string _descriptorText;

	private string _villagesText;

	private string _notableCharactersText;

	private string _settlementPath;

	private string _informationText;

	private string _settlementImageID;

	private string _boundSettlementText;

	private string _detailsText;

	private double _settlementCropPosition;

	private bool _hasBoundSettlement;

	private bool _hasNotables;

	public Settlement Settlement
	{
		get
		{
			if (_settlementDecision == null && _settlementPreliminaryDecision == null)
			{
				if (_decision is SettlementClaimantDecision settlementDecision)
				{
					_settlementDecision = settlementDecision;
				}
				else if (_decision is SettlementClaimantPreliminaryDecision settlementPreliminaryDecision)
				{
					_settlementPreliminaryDecision = settlementPreliminaryDecision;
				}
			}
			if (_settlementDecision == null)
			{
				return _settlementPreliminaryDecision.Settlement;
			}
			return _settlementDecision.Settlement;
		}
	}

	[DataSourceProperty]
	public bool HasBoundSettlement
	{
		get
		{
			return _hasBoundSettlement;
		}
		set
		{
			if (value != _hasBoundSettlement)
			{
				_hasBoundSettlement = value;
				OnPropertyChangedWithValue(value, "HasBoundSettlement");
			}
		}
	}

	[DataSourceProperty]
	public double SettlementCropPosition
	{
		get
		{
			return _settlementCropPosition;
		}
		set
		{
			if (value != _settlementCropPosition)
			{
				_settlementCropPosition = value;
				OnPropertyChangedWithValue(value, "SettlementCropPosition");
			}
		}
	}

	[DataSourceProperty]
	public string BoundSettlementText
	{
		get
		{
			return _boundSettlementText;
		}
		set
		{
			if (value != _boundSettlementText)
			{
				_boundSettlementText = value;
				OnPropertyChangedWithValue(value, "BoundSettlementText");
			}
		}
	}

	[DataSourceProperty]
	public string DetailsText
	{
		get
		{
			return _detailsText;
		}
		set
		{
			if (value != _detailsText)
			{
				_detailsText = value;
				OnPropertyChangedWithValue(value, "DetailsText");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementPath
	{
		get
		{
			return _settlementPath;
		}
		set
		{
			if (value != _settlementPath)
			{
				_settlementPath = value;
				OnPropertyChangedWithValue(value, "SettlementPath");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementName
	{
		get
		{
			return _settlementName;
		}
		set
		{
			if (value != _settlementName)
			{
				_settlementName = value;
				OnPropertyChangedWithValue(value, "SettlementName");
			}
		}
	}

	[DataSourceProperty]
	public string InformationText
	{
		get
		{
			return _informationText;
		}
		set
		{
			if (value != _informationText)
			{
				_informationText = value;
				OnPropertyChangedWithValue(value, "InformationText");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Owner
	{
		get
		{
			return _owner;
		}
		set
		{
			if (value != _owner)
			{
				_owner = value;
				OnPropertyChangedWithValue(value, "Owner");
			}
		}
	}

	[DataSourceProperty]
	public string VillagesText
	{
		get
		{
			return _villagesText;
		}
		set
		{
			if (value != _villagesText)
			{
				_villagesText = value;
				OnPropertyChangedWithValue(value, "VillagesText");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementImageID
	{
		get
		{
			return _settlementImageID;
		}
		set
		{
			if (value != _settlementImageID)
			{
				_settlementImageID = value;
				OnPropertyChangedWithValue(value, "SettlementImageID");
			}
		}
	}

	[DataSourceProperty]
	public string NotableCharactersText
	{
		get
		{
			return _notableCharactersText;
		}
		set
		{
			if (value != _notableCharactersText)
			{
				_notableCharactersText = value;
				OnPropertyChangedWithValue(value, "NotableCharactersText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSettlementVM> BoundVillages
	{
		get
		{
			return _boundVillages;
		}
		set
		{
			if (value != _boundVillages)
			{
				_boundVillages = value;
				OnPropertyChangedWithValue(value, "BoundVillages");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> NotableCharacters
	{
		get
		{
			return _notableCharacters;
		}
		set
		{
			if (value != _notableCharacters)
			{
				_notableCharacters = value;
				OnPropertyChangedWithValue(value, "NotableCharacters");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel MilitasHint
	{
		get
		{
			return _militasHint;
		}
		set
		{
			if (value != _militasHint)
			{
				_militasHint = value;
				OnPropertyChangedWithValue(value, "MilitasHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel FoodHint
	{
		get
		{
			return _foodHint;
		}
		set
		{
			if (value != _foodHint)
			{
				_foodHint = value;
				OnPropertyChangedWithValue(value, "FoodHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel GarrisonHint
	{
		get
		{
			return _garrisonHint;
		}
		set
		{
			if (value != _garrisonHint)
			{
				_garrisonHint = value;
				OnPropertyChangedWithValue(value, "GarrisonHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ProsperityHint
	{
		get
		{
			return _prosperityHint;
		}
		set
		{
			if (value != _prosperityHint)
			{
				_prosperityHint = value;
				OnPropertyChangedWithValue(value, "ProsperityHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel LoyaltyHint
	{
		get
		{
			return _loyaltyHint;
		}
		set
		{
			if (value != _loyaltyHint)
			{
				_loyaltyHint = value;
				OnPropertyChangedWithValue(value, "LoyaltyHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SecurityHint
	{
		get
		{
			return _securityHint;
		}
		set
		{
			if (value != _securityHint)
			{
				_securityHint = value;
				OnPropertyChangedWithValue(value, "SecurityHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel WallsHint
	{
		get
		{
			return _wallsHint;
		}
		set
		{
			if (value != _wallsHint)
			{
				_wallsHint = value;
				OnPropertyChangedWithValue(value, "WallsHint");
			}
		}
	}

	[DataSourceProperty]
	public string MilitasText
	{
		get
		{
			return _militasText;
		}
		set
		{
			if (value != _militasText)
			{
				_militasText = value;
				OnPropertyChangedWithValue(value, "MilitasText");
			}
		}
	}

	[DataSourceProperty]
	public string ProsperityText
	{
		get
		{
			return _prosperityText;
		}
		set
		{
			if (value != _prosperityText)
			{
				_prosperityText = value;
				OnPropertyChangedWithValue(value, "ProsperityText");
			}
		}
	}

	[DataSourceProperty]
	public string LoyaltyText
	{
		get
		{
			return _loyaltyText;
		}
		set
		{
			if (value != _loyaltyText)
			{
				_loyaltyText = value;
				OnPropertyChangedWithValue(value, "LoyaltyText");
			}
		}
	}

	[DataSourceProperty]
	public string SecurityText
	{
		get
		{
			return _securityText;
		}
		set
		{
			if (value != _securityText)
			{
				_securityText = value;
				OnPropertyChangedWithValue(value, "SecurityText");
			}
		}
	}

	[DataSourceProperty]
	public string WallsText
	{
		get
		{
			return _wallsText;
		}
		set
		{
			if (value != _wallsText)
			{
				_wallsText = value;
				OnPropertyChangedWithValue(value, "WallsText");
			}
		}
	}

	[DataSourceProperty]
	public string FoodText
	{
		get
		{
			return _foodText;
		}
		set
		{
			if (value != _foodText)
			{
				_foodText = value;
				OnPropertyChangedWithValue(value, "FoodText");
			}
		}
	}

	[DataSourceProperty]
	public string GarrisonText
	{
		get
		{
			return _garrisonText;
		}
		set
		{
			if (value != _garrisonText)
			{
				_garrisonText = value;
				OnPropertyChangedWithValue(value, "GarrisonText");
			}
		}
	}

	[DataSourceProperty]
	public string DescriptorText
	{
		get
		{
			return _descriptorText;
		}
		set
		{
			if (value != _descriptorText)
			{
				_descriptorText = value;
				OnPropertyChangedWithValue(value, "DescriptorText");
			}
		}
	}

	[DataSourceProperty]
	public string OwnerText
	{
		get
		{
			return _ownerText;
		}
		set
		{
			if (value != _ownerText)
			{
				_ownerText = value;
				OnPropertyChangedWithValue(value, "OwnerText");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Governor
	{
		get
		{
			return _governor;
		}
		set
		{
			if (value != _governor)
			{
				_governor = value;
				OnPropertyChangedWithValue(value, "Governor");
			}
		}
	}

	[DataSourceProperty]
	public bool HasNotables
	{
		get
		{
			return _hasNotables;
		}
		set
		{
			if (value != _hasNotables)
			{
				_hasNotables = value;
				OnPropertyChangedWithValue(value, "HasNotables");
			}
		}
	}

	public SettlementDecisionItemVM(Settlement settlement, KingdomDecision decision, Action onDecisionOver)
		: base(decision, onDecisionOver)
	{
		_settlement = settlement;
		base.DecisionType = 1;
	}

	protected override void InitValues()
	{
		base.InitValues();
		base.DecisionType = 1;
		SettlementImageID = ((Settlement.SettlementComponent != null) ? Settlement.SettlementComponent.WaitMeshName : "");
		BoundVillages = new MBBindingList<EncyclopediaSettlementVM>();
		NotableCharacters = new MBBindingList<HeroVM>();
		SettlementName = Settlement.Name.ToString();
		Governor = new HeroVM(Settlement.Town?.Governor);
		foreach (Village boundVillage in Settlement.BoundVillages)
		{
			BoundVillages.Add(new EncyclopediaSettlementVM(boundVillage.Settlement));
		}
		Town town = Settlement.Town;
		WallsText = town.GetWallLevel().ToString();
		WallsHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(Settlement.Town));
		HasNotables = Settlement.Notables.Count > 0;
		if (!Settlement.IsCastle)
		{
			Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
			foreach (Hero notable in Settlement.Notables)
			{
				NotableCharacters.Add(new HeroVM(notable));
			}
		}
		DescriptorText = Settlement.Culture.Name.ToString();
		DetailsText = GameTexts.FindText("str_people_encyclopedia_details").ToString();
		OwnerText = GameTexts.FindText("str_owner").ToString();
		Owner = new HeroVM(Settlement.OwnerClan.Leader);
		SettlementComponent settlementComponent = Settlement.SettlementComponent;
		SettlementPath = settlementComponent.BackgroundMeshName;
		SettlementCropPosition = settlementComponent.BackgroundCropPosition;
		NotableCharactersText = GameTexts.FindText("str_notable_characters").ToString();
		BoundSettlementText = GameTexts.FindText("str_villages").ToString();
		if (HasBoundSettlement)
		{
			GameTexts.SetVariable("SETTLEMENT_LINK", Settlement.Village.Bound.EncyclopediaLinkWithName);
			BoundSettlementText = GameTexts.FindText("str_bound_settlement_encyclopedia").ToString();
		}
		MilitasText = ((int)Settlement.Militia).ToString();
		MilitasHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownMilitiaTooltip(Settlement.Town));
		ProsperityText = ((Settlement.Town != null) ? ((int)Settlement.Town.Prosperity).ToString() : (Settlement.IsVillage ? ((int)Settlement.Village.Hearth).ToString() : string.Empty));
		if (Settlement.IsTown)
		{
			ProsperityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(Settlement.Town));
		}
		else
		{
			ProsperityHint = new BasicTooltipViewModel(() => GameTexts.FindText("str_prosperity").ToString());
		}
		if (Settlement.Town != null)
		{
			LoyaltyHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(Settlement.Town));
			LoyaltyText = $"{Settlement.Town.Loyalty:0.#}";
			SecurityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(Settlement.Town));
			SecurityText = $"{Settlement.Town.Security:0.#}";
		}
		else
		{
			LoyaltyText = "-";
			SecurityText = "-";
		}
		FoodText = Settlement.Town?.FoodStocks.ToString("0.0");
		if (Settlement.IsFortification)
		{
			FoodHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(Settlement.Town));
			GarrisonText = Settlement.Town.GarrisonParty?.Party.NumberOfAllMembers.ToString() ?? "0";
			GarrisonHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(Settlement.Town));
		}
		else
		{
			FoodHint = new BasicTooltipViewModel();
			GarrisonText = "-";
		}
	}
}

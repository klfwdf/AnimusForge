using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

[EncyclopediaViewModel(typeof(Kingdom))]
public class EncyclopediaFactionPageVM : EncyclopediaContentPageVM
{
	private Kingdom _faction;

	private MBBindingList<EncyclopediaFactionVM> _clans;

	private MBBindingList<EncyclopediaFactionVM> _enemies;

	private MBBindingList<EncyclopediaFactionVM> _tradeAgreements;

	private MBBindingList<EncyclopediaFactionVM> _alliances;

	private MBBindingList<EncyclopediaSettlementVM> _settlements;

	private MBBindingList<EncyclopediaHistoryEventVM> _history;

	private HeroVM _leader;

	private BannerImageIdentifierVM _banner;

	private string _membersText;

	private string _enemiesText;

	private string _tradeAgreementsText;

	private string _alliancesText;

	private string _clansText;

	private string _settlementsText;

	private string _villagesText;

	private string _leaderText;

	private string _descriptorText;

	private string _prosperityText;

	private string _strengthText;

	private string _informationText;

	private HintViewModel _prosperityHint;

	private HintViewModel _strengthHint;

	private string _nameText;

	[DataSourceProperty]
	public MBBindingList<EncyclopediaFactionVM> Clans
	{
		get
		{
			return _clans;
		}
		set
		{
			if (value != _clans)
			{
				_clans = value;
				OnPropertyChangedWithValue(value, "Clans");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaFactionVM> Enemies
	{
		get
		{
			return _enemies;
		}
		set
		{
			if (value != _enemies)
			{
				_enemies = value;
				OnPropertyChangedWithValue(value, "Enemies");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaFactionVM> TradeAgreements
	{
		get
		{
			return _tradeAgreements;
		}
		set
		{
			if (value != _tradeAgreements)
			{
				_tradeAgreements = value;
				OnPropertyChangedWithValue(value, "TradeAgreements");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaFactionVM> Alliances
	{
		get
		{
			return _alliances;
		}
		set
		{
			if (value != _alliances)
			{
				_alliances = value;
				OnPropertyChangedWithValue(value, "Alliances");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaSettlementVM> Settlements
	{
		get
		{
			return _settlements;
		}
		set
		{
			if (value != _settlements)
			{
				_settlements = value;
				OnPropertyChangedWithValue(value, "Settlements");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaHistoryEventVM> History
	{
		get
		{
			return _history;
		}
		set
		{
			if (value != _history)
			{
				_history = value;
				OnPropertyChangedWithValue(value, "History");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Leader
	{
		get
		{
			return _leader;
		}
		set
		{
			if (value != _leader)
			{
				_leader = value;
				OnPropertyChangedWithValue(value, "Leader");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM Banner
	{
		get
		{
			return _banner;
		}
		set
		{
			if (value != _banner)
			{
				_banner = value;
				OnPropertyChangedWithValue(value, "Banner");
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
	public string MembersText
	{
		get
		{
			return _membersText;
		}
		set
		{
			if (value != _membersText)
			{
				_membersText = value;
				OnPropertyChangedWithValue(value, "MembersText");
			}
		}
	}

	[DataSourceProperty]
	public string EnemiesText
	{
		get
		{
			return _enemiesText;
		}
		set
		{
			if (value != _enemiesText)
			{
				_enemiesText = value;
				OnPropertyChangedWithValue(value, "EnemiesText");
			}
		}
	}

	[DataSourceProperty]
	public string TradeAgreementsText
	{
		get
		{
			return _tradeAgreementsText;
		}
		set
		{
			if (value != _tradeAgreementsText)
			{
				_tradeAgreementsText = value;
				OnPropertyChangedWithValue(value, "TradeAgreementsText");
			}
		}
	}

	[DataSourceProperty]
	public string AlliancesText
	{
		get
		{
			return _alliancesText;
		}
		set
		{
			if (value != _alliancesText)
			{
				_alliancesText = value;
				OnPropertyChangedWithValue(value, "AlliancesText");
			}
		}
	}

	[DataSourceProperty]
	public string ClansText
	{
		get
		{
			return _clansText;
		}
		set
		{
			if (value != _clansText)
			{
				_clansText = value;
				OnPropertyChangedWithValue(value, "ClansText");
			}
		}
	}

	[DataSourceProperty]
	public string SettlementsText
	{
		get
		{
			return _settlementsText;
		}
		set
		{
			if (value != _settlementsText)
			{
				_settlementsText = value;
				OnPropertyChangedWithValue(value, "SettlementsText");
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
	public string LeaderText
	{
		get
		{
			return _leaderText;
		}
		set
		{
			if (value != _leaderText)
			{
				_leaderText = value;
				OnPropertyChangedWithValue(value, "LeaderText");
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
	public string StrengthText
	{
		get
		{
			return _strengthText;
		}
		set
		{
			if (value != _strengthText)
			{
				_strengthText = value;
				OnPropertyChangedWithValue(value, "StrengthText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ProsperityHint
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
	public HintViewModel StrengthHint
	{
		get
		{
			return _strengthHint;
		}
		set
		{
			if (value != _strengthHint)
			{
				_strengthHint = value;
				OnPropertyChangedWithValue(value, "StrengthHint");
			}
		}
	}

	public EncyclopediaFactionPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
		_faction = base.Obj as Kingdom;
		Clans = new MBBindingList<EncyclopediaFactionVM>();
		Enemies = new MBBindingList<EncyclopediaFactionVM>();
		TradeAgreements = new MBBindingList<EncyclopediaFactionVM>();
		Alliances = new MBBindingList<EncyclopediaFactionVM>();
		Settlements = new MBBindingList<EncyclopediaSettlementVM>();
		History = new MBBindingList<EncyclopediaHistoryEventVM>();
		base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(_faction);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		StrengthHint = new HintViewModel(GameTexts.FindText("str_strength"));
		ProsperityHint = new HintViewModel(GameTexts.FindText("str_prosperity"));
		MembersText = GameTexts.FindText("str_members").ToString();
		ClansText = new TextObject("{=bfQLwMUp}Clans").ToString();
		EnemiesText = new TextObject("{=zZlWRZjO}Wars").ToString();
		TradeAgreementsText = new TextObject("{=pWyRg13f}Trade Agreements").ToString();
		AlliancesText = new TextObject("{=f90A6PGd}Alliances").ToString();
		SettlementsText = new TextObject("{=LBNzsqyb}Fiefs").ToString();
		VillagesText = GameTexts.FindText("str_villages").ToString();
		InformationText = _faction.EncyclopediaText?.ToString() ?? string.Empty;
		UpdateBookmarkHintText();
		Refresh();
	}

	public override void Refresh()
	{
		base.IsLoadingOver = false;
		Clans.Clear();
		Enemies.Clear();
		TradeAgreements.Clear();
		Alliances.Clear();
		Settlements.Clear();
		History.Clear();
		Leader = new HeroVM(_faction.Leader);
		LeaderText = GameTexts.FindText("str_leader").ToString();
		NameText = _faction.Name.ToString();
		DescriptorText = GameTexts.FindText("str_kingdom_faction").ToString();
		int num = 0;
		float num2 = 0f;
		EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
		foreach (Hero aliveLord in _faction.AliveLords)
		{
			if (pageOf.IsValidEncyclopediaItem(aliveLord))
			{
				num += aliveLord.Gold;
			}
		}
		Banner = new BannerImageIdentifierVM(_faction.Banner, nineGrid: true);
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			if (allLordParty.MapFaction == _faction && !allLordParty.IsDisbanding)
			{
				num2 += allLordParty.Party.CalculateCurrentStrength();
			}
		}
		ProsperityText = num.ToString();
		StrengthText = num2.ToString();
		for (int num3 = Campaign.Current.LogEntryHistory.GameActionLogs.Count - 1; num3 >= 0; num3--)
		{
			if (Campaign.Current.LogEntryHistory.GameActionLogs[num3] is IEncyclopediaLog encyclopediaLog && encyclopediaLog.IsVisibleInEncyclopediaPageOf(_faction))
			{
				History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
			}
		}
		EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Clan));
		IOrderedEnumerable<IFaction> orderedEnumerable = Campaign.Current.Factions.OrderBy((IFaction x) => !x.IsKingdomFaction).ThenBy((IFaction f) => f.Name.ToString());
		foreach (IFaction factionObject in orderedEnumerable)
		{
			if (pageOf2.IsValidEncyclopediaItem(factionObject) && factionObject != _faction && !factionObject.IsBanditFaction && FactionManager.IsAtWarAgainstFaction(_faction, factionObject.MapFaction) && !Enemies.Any((EncyclopediaFactionVM x) => x.Faction == factionObject.MapFaction))
			{
				Enemies.Add(new EncyclopediaFactionVM(factionObject.MapFaction));
			}
		}
		foreach (IFaction item in orderedEnumerable.Where((IFaction x) => x.IsKingdomFaction))
		{
			Kingdom kingdom;
			if (pageOf2.IsValidEncyclopediaItem(item) && item != _faction && (kingdom = item as Kingdom) != null)
			{
				if (HasTradeAgreementWithFaction(_faction, kingdom.MapFaction) && !TradeAgreements.Any((EncyclopediaFactionVM x) => x.Faction == kingdom.MapFaction))
				{
					TradeAgreements.Add(new EncyclopediaFactionVM(kingdom.MapFaction));
				}
				if (DiplomacyHelper.HasAllianceWithFaction(_faction, kingdom.MapFaction) && !Alliances.Any((EncyclopediaFactionVM x) => x.Faction == kingdom.MapFaction))
				{
					Alliances.Add(new EncyclopediaFactionVM(kingdom.MapFaction));
				}
			}
		}
		foreach (Clan item2 in Campaign.Current.Clans.Where((Clan c) => c.Kingdom == _faction))
		{
			Clans.Add(new EncyclopediaFactionVM(item2));
		}
		EncyclopediaPage pageOf3 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
		foreach (Settlement item3 in from s in Settlement.All
			where s.IsTown || s.IsCastle
			orderby s.IsCastle, s.IsTown
			select s)
		{
			if ((item3.MapFaction == _faction || (item3.OwnerClan == _faction.RulingClan && item3.OwnerClan.Leader != null)) && pageOf3.IsValidEncyclopediaItem(item3))
			{
				Settlements.Add(new EncyclopediaSettlementVM(item3));
			}
		}
		base.IsLoadingOver = true;
	}

	private bool HasTradeAgreementWithFaction(IFaction faction1, IFaction faction2)
	{
		if (faction1 == null || faction2 == null || faction1 == faction2 || faction1.IsEliminated || faction2.IsEliminated || !faction1.IsKingdomFaction || !faction2.IsKingdomFaction)
		{
			return false;
		}
		TradeAgreementsCampaignBehavior.TradeAgreement tradeAgreement;
		return Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>()?.HasTradeAgreement(faction1 as Kingdom, faction2 as Kingdom, out tradeAgreement) ?? false;
	}

	public override string GetName()
	{
		return _faction.Name.ToString();
	}

	public override string GetNavigationBarURL()
	{
		return string.Concat(string.Concat(string.Concat(HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ", HyperlinkTexts.GetGenericHyperlinkText("ListPage-Kingdoms", GameTexts.FindText("str_encyclopedia_kingdoms").ToString())), " \\ "), GetName());
	}

	public override void ExecuteSwitchBookmarkedState()
	{
		base.ExecuteSwitchBookmarkedState();
		if (base.IsBookmarked)
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(_faction);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(_faction);
		}
	}
}

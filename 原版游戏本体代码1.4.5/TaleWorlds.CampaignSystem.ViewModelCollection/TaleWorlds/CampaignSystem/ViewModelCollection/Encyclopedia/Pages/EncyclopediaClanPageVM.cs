using System.Linq;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

[EncyclopediaViewModel(typeof(Clan))]
public class EncyclopediaClanPageVM : EncyclopediaContentPageVM
{
	private readonly IFaction _faction;

	private readonly Clan _clan;

	private MBBindingList<StringPairItemVM> _clanInfo;

	private MBBindingList<HeroVM> _members;

	private MBBindingList<EncyclopediaFactionVM> _enemies;

	private MBBindingList<EncyclopediaSettlementVM> _settlements;

	private MBBindingList<EncyclopediaHistoryEventVM> _history;

	private HeroVM _leader;

	private BannerImageIdentifierVM _banner;

	private string _membersText;

	private string _enemiesText;

	private string _alliesText;

	private string _settlementsText;

	private string _villagesText;

	private string _leaderText;

	private string _descriptorText;

	private string _informationText;

	private string _prosperityText;

	private string _strengthText;

	private string _destroyedText;

	private string _partOfText;

	private string _tierText;

	private string _infoText;

	private HintViewModel _prosperityHint;

	private HintViewModel _strengthHint;

	private EncyclopediaFactionVM _parentKingdom;

	private string _nameText;

	private bool _hasParentKingdom;

	private bool _isClanDestroyed;

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> ClanInfo
	{
		get
		{
			return _clanInfo;
		}
		set
		{
			if (value != _clanInfo)
			{
				_clanInfo = value;
				OnPropertyChangedWithValue(value, "ClanInfo");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<HeroVM> Members
	{
		get
		{
			return _members;
		}
		set
		{
			if (value != _members)
			{
				_members = value;
				OnPropertyChangedWithValue(value, "Members");
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
	public EncyclopediaFactionVM ParentKingdom
	{
		get
		{
			return _parentKingdom;
		}
		set
		{
			if (value != _parentKingdom)
			{
				_parentKingdom = value;
				OnPropertyChangedWithValue(value, "ParentKingdom");
			}
		}
	}

	[DataSourceProperty]
	public bool HasParentKingdom
	{
		get
		{
			return _hasParentKingdom;
		}
		set
		{
			if (value != _hasParentKingdom)
			{
				_hasParentKingdom = value;
				OnPropertyChangedWithValue(value, "HasParentKingdom");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClanDestroyed
	{
		get
		{
			return _isClanDestroyed;
		}
		set
		{
			if (value != _isClanDestroyed)
			{
				_isClanDestroyed = value;
				OnPropertyChangedWithValue(value, "IsClanDestroyed");
			}
		}
	}

	[DataSourceProperty]
	public string DestroyedText
	{
		get
		{
			return _destroyedText;
		}
		set
		{
			if (value != _destroyedText)
			{
				_destroyedText = value;
				OnPropertyChangedWithValue(value, "DestroyedText");
			}
		}
	}

	[DataSourceProperty]
	public string PartOfText
	{
		get
		{
			return _partOfText;
		}
		set
		{
			if (value != _partOfText)
			{
				_partOfText = value;
				OnPropertyChangedWithValue(value, "PartOfText");
			}
		}
	}

	[DataSourceProperty]
	public string TierText
	{
		get
		{
			return _tierText;
		}
		set
		{
			if (value != _tierText)
			{
				_tierText = value;
				OnPropertyChangedWithValue(value, "TierText");
			}
		}
	}

	[DataSourceProperty]
	public string InfoText
	{
		get
		{
			return _infoText;
		}
		set
		{
			if (value != _infoText)
			{
				_infoText = value;
				OnPropertyChangedWithValue(value, "InfoText");
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
	public string AlliesText
	{
		get
		{
			return _alliesText;
		}
		set
		{
			if (value != _alliesText)
			{
				_alliesText = value;
				OnPropertyChangedWithValue(value, "AlliesText");
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

	public EncyclopediaClanPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
		_faction = base.Obj as IFaction;
		_clan = _faction as Clan;
		Members = new MBBindingList<HeroVM>();
		Enemies = new MBBindingList<EncyclopediaFactionVM>();
		Settlements = new MBBindingList<EncyclopediaSettlementVM>();
		History = new MBBindingList<EncyclopediaHistoryEventVM>();
		ClanInfo = new MBBindingList<StringPairItemVM>();
		base.IsBookmarked = Campaign.Current.EncyclopediaManager.ViewDataTracker.IsEncyclopediaBookmarked(_clan);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		StrengthHint = new HintViewModel(GameTexts.FindText("str_strength"));
		ProsperityHint = new HintViewModel(GameTexts.FindText("str_prosperity"));
		MembersText = GameTexts.FindText("str_members").ToString();
		AlliesText = new TextObject("{=bfQLwMUp}Clans").ToString();
		EnemiesText = new TextObject("{=zZlWRZjO}Wars").ToString();
		SettlementsText = GameTexts.FindText("str_settlements").ToString();
		VillagesText = GameTexts.FindText("str_villages").ToString();
		DestroyedText = new TextObject("{=w8Yzf0F0}Destroyed").ToString();
		PartOfText = GameTexts.FindText("str_encyclopedia_clan_part_of_kingdom").ToString();
		LeaderText = GameTexts.FindText("str_leader").ToString();
		InfoText = GameTexts.FindText("str_info").ToString();
		UpdateBookmarkHintText();
		Refresh();
	}

	public override void Refresh()
	{
		Members.Clear();
		Enemies.Clear();
		Settlements.Clear();
		History.Clear();
		ClanInfo.Clear();
		InformationText = _faction.EncyclopediaText?.ToString();
		Leader = new HeroVM(_faction.Leader, useCivilian: true);
		NameText = _clan.Name.ToString();
		HasParentKingdom = _clan.Kingdom != null;
		ParentKingdom = (HasParentKingdom ? new EncyclopediaFactionVM(((Clan)_faction).Kingdom) : null);
		if (_faction.IsKingdomFaction)
		{
			DescriptorText = GameTexts.FindText("str_kingdom_faction").ToString();
		}
		else if (_faction.IsBanditFaction)
		{
			DescriptorText = GameTexts.FindText("str_bandit_faction").ToString();
		}
		else if (_faction.IsMinorFaction)
		{
			DescriptorText = GameTexts.FindText("str_minor_faction").ToString();
		}
		int num = 0;
		float num2 = 0f;
		EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
		foreach (Hero item in _faction.Heroes.Union(_clan?.Companions))
		{
			if (pageOf.IsValidEncyclopediaItem(item))
			{
				if (item != Leader.Hero)
				{
					Members.Add(new HeroVM(item, useCivilian: true));
				}
				num += item.Gold;
			}
		}
		Members.Sort(new HeroAgeComparer(isAscending: false));
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
			if (Campaign.Current.LogEntryHistory.GameActionLogs[num3] is IEncyclopediaLog encyclopediaLog && ((_faction.IsKingdomFaction && encyclopediaLog.IsVisibleInEncyclopediaPageOf((Kingdom)_faction)) || (_faction.IsClan && encyclopediaLog.IsVisibleInEncyclopediaPageOf((Clan)_faction))))
			{
				History.Add(new EncyclopediaHistoryEventVM(encyclopediaLog));
			}
		}
		EncyclopediaPage pageOf2 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Clan));
		foreach (IFaction item2 in Campaign.Current.Factions.OrderBy((IFaction x) => !x.IsKingdomFaction).ThenBy((IFaction f) => f.Name.ToString()))
		{
			IFaction mapFaction = item2.MapFaction;
			if (pageOf2.IsValidEncyclopediaItem(mapFaction) && mapFaction != _faction.MapFaction && mapFaction != _faction && !mapFaction.IsBanditFaction && FactionManager.IsAtWarAgainstFaction(_faction.MapFaction, mapFaction) && !Enemies.Any((EncyclopediaFactionVM x) => x.Faction == mapFaction))
			{
				Enemies.Add(new EncyclopediaFactionVM(mapFaction));
			}
		}
		EncyclopediaPage pageOf3 = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Settlement));
		foreach (Settlement item3 in from s in Settlement.All
			orderby s.IsVillage, s.IsCastle, s.IsTown
			select s)
		{
			if ((item3.MapFaction == _faction || (item3.OwnerClan == _faction && item3.OwnerClan.Leader != null)) && pageOf3.IsValidEncyclopediaItem(item3) && (item3.IsTown || item3.IsCastle))
			{
				Settlements.Add(new EncyclopediaSettlementVM(item3));
			}
		}
		GameTexts.SetVariable("LEFT", new TextObject("{=tTLvo8sM}Clan Tier").ToString());
		ClanInfo.Add(new StringPairItemVM(GameTexts.FindText("str_LEFT_colon").ToString(), _clan.Tier.ToString()));
		GameTexts.SetVariable("LEFT", new TextObject("{=ODEnkg0o}Clan Strength").ToString());
		ClanInfo.Add(new StringPairItemVM(GameTexts.FindText("str_LEFT_colon").ToString(), _clan.CurrentTotalStrength.ToString("F0")));
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_wealth").ToString());
		ClanInfo.Add(new StringPairItemVM(GameTexts.FindText("str_LEFT_colon").ToString(), CampaignUIHelper.GetClanWealthStatusText(_clan)));
		IsClanDestroyed = _clan.IsEliminated;
	}

	public override string GetName()
	{
		return _clan.Name.ToString();
	}

	public override string GetNavigationBarURL()
	{
		return string.Concat(string.Concat(string.Concat(HyperlinkTexts.GetGenericHyperlinkText("Home", GameTexts.FindText("str_encyclopedia_home").ToString()) + " \\ ", HyperlinkTexts.GetGenericHyperlinkText("ListPage-Clans", GameTexts.FindText("str_encyclopedia_clans").ToString())), " \\ "), GetName());
	}

	public override void ExecuteSwitchBookmarkedState()
	{
		base.ExecuteSwitchBookmarkedState();
		if (base.IsBookmarked)
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.AddEncyclopediaBookmarkToItem(_clan);
		}
		else
		{
			Campaign.Current.EncyclopediaManager.ViewDataTracker.RemoveEncyclopediaBookmarkFromItem(_clan);
		}
	}
}

using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;

public class ExpelClanDecisionItemVM : DecisionItemBaseVM
{
	private ExpelClanFromKingdomDecision _expelDecision;

	private MBBindingList<HeroVM> _members;

	private MBBindingList<EncyclopediaSettlementVM> _fiefs;

	private HeroVM _leader;

	private string _nameText;

	private string _membersText;

	private string _settlementsText;

	private string _leaderText;

	private string _informationText;

	private string _prosperityText;

	private string _strengthText;

	private BasicTooltipViewModel _prosperityHint;

	private BasicTooltipViewModel _strengthHint;

	public ExpelClanFromKingdomDecision ExpelDecision => _expelDecision ?? (_expelDecision = _decision as ExpelClanFromKingdomDecision);

	public Clan Clan => ExpelDecision.ClanToExpel;

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
	public MBBindingList<EncyclopediaSettlementVM> Fiefs
	{
		get
		{
			return _fiefs;
		}
		set
		{
			if (value != _fiefs)
			{
				_fiefs = value;
				OnPropertyChangedWithValue(value, "Fiefs");
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
	public BasicTooltipViewModel StrengthHint
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

	public ExpelClanDecisionItemVM(ExpelClanFromKingdomDecision decision, Action onDecisionOver)
		: base(decision, onDecisionOver)
	{
		base.DecisionType = 2;
	}

	protected override void InitValues()
	{
		base.InitValues();
		base.DecisionType = 2;
		Members = new MBBindingList<HeroVM>();
		Fiefs = new MBBindingList<EncyclopediaSettlementVM>();
		GameTexts.SetVariable("RENOWN", Clan.Renown);
		GameTexts.SetVariable("STR1", Clan.EncyclopediaText?.ToString());
		GameTexts.SetVariable("STR2", GameTexts.FindText("str_encyclopedia_renown").ToString());
		InformationText = GameTexts.FindText("str_STR1_space_STR2").ToString();
		Leader = new HeroVM(Clan.Leader);
		LeaderText = GameTexts.FindText("str_leader").ToString();
		MembersText = GameTexts.FindText("str_members").ToString();
		SettlementsText = GameTexts.FindText("str_fiefs").ToString();
		NameText = Clan.Name.ToString();
		int num = 0;
		float num2 = 0f;
		EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(Hero));
		foreach (Hero hero in Clan.Heroes)
		{
			if (hero.IsAlive && hero.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(hero))
			{
				if (hero != Leader.Hero)
				{
					Members.Add(new HeroVM(hero));
				}
				num += hero.Gold;
			}
		}
		foreach (Hero companion in Clan.Companions)
		{
			if (companion.IsAlive && companion.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && pageOf.IsValidEncyclopediaItem(companion))
			{
				if (companion != Leader.Hero)
				{
					Members.Add(new HeroVM(companion));
				}
				num += companion.Gold;
			}
		}
		foreach (MobileParty allLordParty in MobileParty.AllLordParties)
		{
			if (allLordParty.ActualClan == Clan && !allLordParty.IsDisbanding)
			{
				num2 += allLordParty.Party.CalculateCurrentStrength();
			}
		}
		ProsperityText = num.ToString();
		ProsperityHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetClanProsperityTooltip(Clan));
		StrengthText = num2.ToString();
		StrengthHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetClanStrengthTooltip(Clan));
		foreach (Town item in from s in Clan.Fiefs
			orderby s.IsCastle, s.IsTown
			select s)
		{
			if (item.Settlement.OwnerClan == Clan)
			{
				Fiefs.Add(new EncyclopediaSettlementVM(item.Settlement));
			}
		}
	}
}

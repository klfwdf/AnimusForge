using System;
using Helpers;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;

public class MakePeaceDecisionItemVM : DecisionItemBaseVM
{
	private readonly MakePeaceKingdomDecision _makePeaceDecision;

	private string _nameText;

	private string _peaceDescriptionText;

	private BannerImageIdentifierVM _sourceFactionBanner;

	private BannerImageIdentifierVM _targetFactionBanner;

	private string _leaderText;

	private HeroVM _sourceFactionLeader;

	private HeroVM _targetFactionLeader;

	private MBBindingList<KingdomWarComparableStatVM> _comparedStats;

	private bool _isTargetFactionOtherWarsVisible;

	private MBBindingList<KingdomDiplomacyFactionItemVM> _targetFactionOtherWars;

	private Kingdom _sourceFaction => Hero.MainHero.Clan.Kingdom;

	public IFaction TargetFaction => (_decision as MakePeaceKingdomDecision).FactionToMakePeaceWith;

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
	public string PeaceDescriptionText
	{
		get
		{
			return _peaceDescriptionText;
		}
		set
		{
			if (value != _peaceDescriptionText)
			{
				_peaceDescriptionText = value;
				OnPropertyChangedWithValue(value, "PeaceDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM SourceFactionBanner
	{
		get
		{
			return _sourceFactionBanner;
		}
		set
		{
			if (value != _sourceFactionBanner)
			{
				_sourceFactionBanner = value;
				OnPropertyChangedWithValue(value, "SourceFactionBanner");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM TargetFactionBanner
	{
		get
		{
			return _targetFactionBanner;
		}
		set
		{
			if (value != _targetFactionBanner)
			{
				_targetFactionBanner = value;
				OnPropertyChangedWithValue(value, "TargetFactionBanner");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomWarComparableStatVM> ComparedStats
	{
		get
		{
			return _comparedStats;
		}
		set
		{
			if (value != _comparedStats)
			{
				_comparedStats = value;
				OnPropertyChangedWithValue(value, "ComparedStats");
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
	public HeroVM SourceFactionLeader
	{
		get
		{
			return _sourceFactionLeader;
		}
		set
		{
			if (value != _sourceFactionLeader)
			{
				_sourceFactionLeader = value;
				OnPropertyChangedWithValue(value, "SourceFactionLeader");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM TargetFactionLeader
	{
		get
		{
			return _targetFactionLeader;
		}
		set
		{
			if (value != _targetFactionLeader)
			{
				_targetFactionLeader = value;
				OnPropertyChangedWithValue(value, "TargetFactionLeader");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTargetFactionOtherWarsVisible
	{
		get
		{
			return _isTargetFactionOtherWarsVisible;
		}
		set
		{
			if (value != _isTargetFactionOtherWarsVisible)
			{
				_isTargetFactionOtherWarsVisible = value;
				OnPropertyChangedWithValue(value, "IsTargetFactionOtherWarsVisible");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomDiplomacyFactionItemVM> TargetFactionOtherWars
	{
		get
		{
			return _targetFactionOtherWars;
		}
		set
		{
			if (value != _targetFactionOtherWars)
			{
				_targetFactionOtherWars = value;
				OnPropertyChangedWithValue(value, "TargetFactionOtherWars");
			}
		}
	}

	public MakePeaceDecisionItemVM(MakePeaceKingdomDecision decision, Action onDecisionOver)
		: base(decision, onDecisionOver)
	{
		_makePeaceDecision = decision;
		base.DecisionType = 5;
	}

	protected override void InitValues()
	{
		base.InitValues();
		TextObject textObject = GameTexts.FindText("str_kingdom_decision_make_peace");
		NameText = textObject.ToString();
		TextObject textObject2 = GameTexts.FindText("str_kingdom_decision_make_peace_desc");
		textObject2.SetTextVariable("FACTION", TargetFaction.Name);
		PeaceDescriptionText = textObject2.ToString();
		SourceFactionBanner = new BannerImageIdentifierVM(_sourceFaction.Banner, nineGrid: true);
		TargetFactionBanner = new BannerImageIdentifierVM(TargetFaction.Banner, nineGrid: true);
		LeaderText = GameTexts.FindText("str_leader").ToString();
		SourceFactionLeader = new HeroVM(_sourceFaction.Leader);
		TargetFactionLeader = new HeroVM(TargetFaction.Leader);
		ComparedStats = new MBBindingList<KingdomWarComparableStatVM>();
		Kingdom targetFaction = TargetFaction as Kingdom;
		string faction1Color = Color.FromUint(_sourceFaction.Color).ToString();
		string faction2Color = Color.FromUint(targetFaction.Color).ToString();
		StanceLink stanceWith = _sourceFaction.GetStanceWith(TargetFaction);
		KingdomWarComparableStatVM item = new KingdomWarComparableStatVM((int)_sourceFaction.CurrentTotalStrength, (int)targetFaction.CurrentTotalStrength, GameTexts.FindText("str_strength"), faction1Color, faction2Color, 10000);
		ComparedStats.Add(item);
		KingdomWarComparableStatVM item2 = new KingdomWarComparableStatVM(stanceWith.GetCasualties(targetFaction), stanceWith.GetCasualties(_sourceFaction), GameTexts.FindText("str_war_casualties_inflicted"), faction1Color, faction2Color, 10000);
		ComparedStats.Add(item2);
		KingdomWarComparableStatVM item3 = new KingdomWarComparableStatVM(stanceWith.GetSuccessfulSieges(_sourceFaction), stanceWith.GetSuccessfulSieges(targetFaction), GameTexts.FindText("str_war_successful_sieges"), faction1Color, faction2Color, 5);
		ComparedStats.Add(item3);
		KingdomWarComparableStatVM item4 = new KingdomWarComparableStatVM(stanceWith.GetSuccessfulRaids(_sourceFaction), stanceWith.GetSuccessfulRaids(targetFaction), GameTexts.FindText("str_war_successful_raids"), faction1Color, faction2Color, 10);
		ComparedStats.Add(item4);
		ExplainedNumber warProgressOfFaction1 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(_sourceFaction, targetFaction, includeDescriptions: true);
		ExplainedNumber warProgressOfFaction2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(targetFaction, _sourceFaction, includeDescriptions: true);
		int num = (int)(warProgressOfFaction1.ResultNumber * 100f / warProgressOfFaction1.LimitMaxValue);
		int num2 = (int)(warProgressOfFaction2.ResultNumber * 100f / warProgressOfFaction2.LimitMaxValue);
		int faction1Stat = TaleWorlds.Library.MathF.Max(0, num - num2);
		int faction2Stat = TaleWorlds.Library.MathF.Max(0, num2 - num);
		KingdomWarComparableStatVM item5 = new KingdomWarComparableStatVM(faction1Stat, faction2Stat, new TextObject("{=8qbkS5D2}War Progress"), faction1Color, faction2Color, 100, new BasicTooltipViewModel(() => CampaignUIHelper.GetNormalizedWarProgressTooltip(warProgressOfFaction1, warProgressOfFaction2, warProgressOfFaction1.LimitMaxValue, _sourceFaction.Name, targetFaction.Name)), new BasicTooltipViewModel(() => CampaignUIHelper.GetNormalizedWarProgressTooltip(warProgressOfFaction2, warProgressOfFaction1, warProgressOfFaction2.LimitMaxValue, targetFaction.Name, _sourceFaction.Name)));
		ComparedStats.Add(item5);
		TargetFactionOtherWars = new MBBindingList<KingdomDiplomacyFactionItemVM>();
		foreach (StanceLink stance in FactionHelper.GetStances(TargetFaction))
		{
			if (stance.IsAtWar && stance.Faction1 != _sourceFaction && stance.Faction2 != _sourceFaction && (stance.Faction1.IsKingdomFaction || stance.Faction1.Leader == Hero.MainHero) && (stance.Faction2.IsKingdomFaction || stance.Faction2.Leader == Hero.MainHero) && !stance.Faction1.IsRebelClan && !stance.Faction2.IsRebelClan && !stance.Faction1.IsBanditFaction && !stance.Faction2.IsBanditFaction)
			{
				TargetFactionOtherWars.Add(new KingdomDiplomacyFactionItemVM((stance.Faction1 == TargetFaction) ? stance.Faction2 : stance.Faction1));
			}
		}
		IsTargetFactionOtherWarsVisible = TargetFactionOtherWars.Count > 0;
	}
}

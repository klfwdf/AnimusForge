using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;

public class KingSelectionDecisionItemVM : DecisionItemBaseVM
{
	private readonly KingSelectionKingdomDecision _kingSelectionDecision;

	private string _nameText;

	private string _factionName;

	private BannerImageIdentifierVM _factionBanner;

	private string _settlementsText;

	private string _settlementsListText;

	private string _castlesText;

	private string _castlesListText;

	private int _totalStrength;

	private string _totalStrengthText;

	private string _activePoliciesText;

	private string _activePoliciesListText;

	public IFaction TargetFaction => (_decision as KingSelectionKingdomDecision).Kingdom;

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
	public string FactionName
	{
		get
		{
			return _factionName;
		}
		set
		{
			if (value != _factionName)
			{
				_factionName = value;
				OnPropertyChangedWithValue(value, "FactionName");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM FactionBanner
	{
		get
		{
			return _factionBanner;
		}
		set
		{
			if (value != _factionBanner)
			{
				_factionBanner = value;
				OnPropertyChangedWithValue(value, "FactionBanner");
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
	public string SettlementsListText
	{
		get
		{
			return _settlementsListText;
		}
		set
		{
			if (value != _settlementsListText)
			{
				_settlementsListText = value;
				OnPropertyChangedWithValue(value, "SettlementsListText");
			}
		}
	}

	[DataSourceProperty]
	public string CastlesText
	{
		get
		{
			return _castlesText;
		}
		set
		{
			if (value != _castlesText)
			{
				_castlesText = value;
				OnPropertyChangedWithValue(value, "CastlesText");
			}
		}
	}

	[DataSourceProperty]
	public string CastlesListText
	{
		get
		{
			return _castlesListText;
		}
		set
		{
			if (value != _castlesListText)
			{
				_castlesListText = value;
				OnPropertyChangedWithValue(value, "CastlesListText");
			}
		}
	}

	[DataSourceProperty]
	public string TotalStrengthText
	{
		get
		{
			return _totalStrengthText;
		}
		set
		{
			if (value != _totalStrengthText)
			{
				_totalStrengthText = value;
				OnPropertyChangedWithValue(value, "TotalStrengthText");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStrength
	{
		get
		{
			return _totalStrength;
		}
		set
		{
			if (value != _totalStrength)
			{
				_totalStrength = value;
				OnPropertyChangedWithValue(value, "TotalStrength");
			}
		}
	}

	[DataSourceProperty]
	public string ActivePoliciesText
	{
		get
		{
			return _activePoliciesText;
		}
		set
		{
			if (value != _activePoliciesText)
			{
				_activePoliciesText = value;
				OnPropertyChangedWithValue(value, "ActivePoliciesText");
			}
		}
	}

	[DataSourceProperty]
	public string ActivePoliciesListText
	{
		get
		{
			return _activePoliciesListText;
		}
		set
		{
			if (value != _activePoliciesListText)
			{
				_activePoliciesListText = value;
				OnPropertyChangedWithValue(value, "ActivePoliciesListText");
			}
		}
	}

	public KingSelectionDecisionItemVM(KingSelectionKingdomDecision decision, Action onDecisionOver)
		: base(decision, onDecisionOver)
	{
		_kingSelectionDecision = decision;
		base.DecisionType = 6;
	}

	protected override void InitValues()
	{
		base.InitValues();
		TextObject textObject = GameTexts.FindText("str_kingdom_decision_king_selection");
		textObject.SetTextVariable("FACTION", TargetFaction.Name);
		NameText = textObject.ToString();
		FactionBanner = new BannerImageIdentifierVM(TargetFaction.Banner, nineGrid: true);
		FactionName = TargetFaction.Culture.Name.ToString();
		bool flag = true;
		bool flag2 = true;
		int num = 0;
		int num2 = 0;
		foreach (Settlement settlement in TargetFaction.Settlements)
		{
			if (settlement.IsTown)
			{
				if (flag)
				{
					SettlementsListText = settlement.EncyclopediaLinkWithName.ToString();
					flag = false;
				}
				else
				{
					GameTexts.SetVariable("LEFT", SettlementsListText);
					GameTexts.SetVariable("RIGHT", settlement.EncyclopediaLinkWithName);
					SettlementsListText = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
				}
				num++;
			}
			else if (settlement.IsCastle)
			{
				if (flag2)
				{
					CastlesListText = settlement.EncyclopediaLinkWithName.ToString();
					flag2 = false;
				}
				else
				{
					GameTexts.SetVariable("LEFT", CastlesListText);
					GameTexts.SetVariable("RIGHT", settlement.EncyclopediaLinkWithName);
					CastlesListText = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
				}
				num2++;
			}
		}
		TextObject variable = GameTexts.FindText("str_settlements");
		TextObject textObject2 = GameTexts.FindText("str_STR_in_parentheses");
		textObject2.SetTextVariable("STR", num);
		TextObject textObject3 = GameTexts.FindText("str_LEFT_RIGHT");
		textObject3.SetTextVariable("LEFT", variable);
		textObject3.SetTextVariable("RIGHT", textObject2);
		SettlementsText = textObject3.ToString();
		TextObject variable2 = GameTexts.FindText("str_castles");
		TextObject textObject4 = GameTexts.FindText("str_STR_in_parentheses");
		textObject4.SetTextVariable("STR", num2);
		TextObject textObject5 = GameTexts.FindText("str_LEFT_RIGHT");
		textObject5.SetTextVariable("LEFT", variable2);
		textObject5.SetTextVariable("RIGHT", textObject4);
		CastlesText = textObject5.ToString();
		TotalStrengthText = GameTexts.FindText("str_total_strength").ToString();
		TotalStrength = (int)TargetFaction.CurrentTotalStrength;
		ActivePoliciesText = GameTexts.FindText("str_active_policies").ToString();
		Kingdom kingdom = TargetFaction as Kingdom;
		foreach (PolicyObject activePolicy in kingdom.ActivePolicies)
		{
			if (activePolicy == kingdom.ActivePolicies[0])
			{
				ActivePoliciesListText = activePolicy.Name.ToString();
				continue;
			}
			GameTexts.SetVariable("LEFT", ActivePoliciesListText);
			GameTexts.SetVariable("RIGHT", activePolicy.Name.ToString());
			ActivePoliciesListText = GameTexts.FindText("str_LEFT_comma_RIGHT").ToString();
		}
	}

	private void ExecuteLocationLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}
}

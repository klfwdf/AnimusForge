using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Issues;

public class DefaultIssueEffects
{
	private IssueEffect _issueEffectSettlementGarrison;

	private IssueEffect _issueEffectSettlementLoyalty;

	private IssueEffect _issueEffectSettlementSecurity;

	private IssueEffect _issueEffectSettlementMilitia;

	private IssueEffect _issueEffectSettlementProsperity;

	private IssueEffect _issueEffectVillageHearth;

	private IssueEffect _issueEffectSettlementFood;

	private IssueEffect _issueEffectSettlementTax;

	private IssueEffect _issueEffectHalfVillageProduction;

	private IssueEffect _issueEffectIssueOwnerPower;

	private IssueEffect _clanInfluence;

	private static DefaultIssueEffects Instance => Campaign.Current.DefaultIssueEffects;

	public static IssueEffect SettlementLoyalty => Instance._issueEffectSettlementLoyalty;

	public static IssueEffect SettlementSecurity => Instance._issueEffectSettlementSecurity;

	public static IssueEffect SettlementMilitia => Instance._issueEffectSettlementMilitia;

	public static IssueEffect SettlementProsperity => Instance._issueEffectSettlementProsperity;

	public static IssueEffect VillageHearth => Instance._issueEffectVillageHearth;

	public static IssueEffect SettlementFood => Instance._issueEffectSettlementFood;

	public static IssueEffect SettlementTax => Instance._issueEffectSettlementTax;

	public static IssueEffect SettlementGarrison => Instance._issueEffectSettlementGarrison;

	public static IssueEffect HalfVillageProduction => Instance._issueEffectHalfVillageProduction;

	public static IssueEffect IssueOwnerPower => Instance._issueEffectIssueOwnerPower;

	public static IssueEffect ClanInfluence => Instance._clanInfluence;

	public DefaultIssueEffects()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_issueEffectSettlementLoyalty = Create("issue_effect_settlement_loyalty");
		_issueEffectSettlementSecurity = Create("issue_effect_settlement_security");
		_issueEffectSettlementMilitia = Create("issue_effect_settlement_militia");
		_issueEffectSettlementProsperity = Create("issue_effect_settlement_prosperity");
		_issueEffectVillageHearth = Create("issue_effect_village_hearth");
		_issueEffectSettlementFood = Create("issue_effect_settlement_food");
		_issueEffectSettlementTax = Create("issue_effect_settlement_tax");
		_issueEffectSettlementGarrison = Create("issue_effect_settlement_garrison");
		_issueEffectHalfVillageProduction = Create("issue_effect_half_village_production");
		_issueEffectIssueOwnerPower = Create("issue_effect_issue_owner_power");
		_clanInfluence = Create("issue_effect_clan_influence");
		InitializeAll();
	}

	private IssueEffect Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new IssueEffect(stringId));
	}

	private void InitializeAll()
	{
		_issueEffectSettlementLoyalty.Initialize(new TextObject("{=YO0x7ZAo}Loyalty"), new TextObject("{=xAWvm25T}Effects settlement's loyalty."));
		_issueEffectSettlementSecurity.Initialize(new TextObject("{=MqCH7R4A}Security"), new TextObject("{=h117Qj3E}Effects settlement's security."));
		_issueEffectSettlementMilitia.Initialize(new TextObject("{=gsVtO9A7}Militia"), new TextObject("{=dTmPV82D}Effects settlement's militia."));
		_issueEffectSettlementProsperity.Initialize(new TextObject("{=IagYTD5O}Prosperity"), new TextObject("{=ETye0JMY}Effects settlement's prosperity."));
		_issueEffectVillageHearth.Initialize(new TextObject("{=f5X5uU0m}Village Hearth"), new TextObject("{=7TbVhbT9}Effects village's hearth."));
		_issueEffectSettlementFood.Initialize(new TextObject("{=qSi4DlT4}Food"), new TextObject("{=onDsUkUl}Effects settlement's food."));
		_issueEffectSettlementTax.Initialize(new TextObject("{=2awf1tei}Tax"), new TextObject("{=q2Ovtr1s}Effects settlement's tax."));
		_issueEffectSettlementGarrison.Initialize(new TextObject("{=jlgjLDo7}Garrison"), new TextObject("{=WJ7SnBgN}Effects settlement's garrison."));
		_issueEffectHalfVillageProduction.Initialize(new TextObject("{=bGyrPe8c}Production"), new TextObject("{=arbaXvQf}Effects village's production."));
		_issueEffectIssueOwnerPower.Initialize(new TextObject("{=gGXelWQX}Issue owner power"), new TextObject("{=tjudHtDB}Effects the power of issue owner in the settlement."));
		_clanInfluence.Initialize(new TextObject("{=KN6khbSl}Clan Influence"), new TextObject("{=y2aLOwOs}Effects the influence of clan."));
	}
}

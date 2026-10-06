using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public class DefaultCulturalFeats
{
	private FeatObject _aseraiTraderFeat;

	private FeatObject _aseraiDesertSpeedFeat;

	private FeatObject _aseraiWageFeat;

	private FeatObject _battaniaForestSpeedFeat;

	private FeatObject _battaniaMilitiaFeat;

	private FeatObject _battaniaConstructionFeat;

	private FeatObject _empireGarrisonWageFeat;

	private FeatObject _empireArmyInfluenceFeat;

	private FeatObject _empireVillageHearthFeat;

	private FeatObject _khuzaitCheaperRecruitsFeat;

	private FeatObject _khuzaitAnimalProductionFeat;

	private FeatObject _khuzaitDecreasedTaxFeat;

	private FeatObject _sturgianGrainProductionFeat;

	private FeatObject _sturgianArmyInfluenceCostFeat;

	private FeatObject _sturgianDecisionPenaltyFeat;

	private FeatObject _vlandianRenownIncomeFeat;

	private FeatObject _vlandianVillageProductionFeat;

	private FeatObject _vlandianArmyInfluenceCostFeat;

	private static DefaultCulturalFeats Instance => Campaign.Current.DefaultFeats;

	public static FeatObject AseraiTraderFeat => Instance._aseraiTraderFeat;

	public static FeatObject AseraiDesertFeat => Instance._aseraiDesertSpeedFeat;

	public static FeatObject AseraiIncreasedWageFeat => Instance._aseraiWageFeat;

	public static FeatObject BattanianForestSpeedFeat => Instance._battaniaForestSpeedFeat;

	public static FeatObject BattanianMilitiaFeat => Instance._battaniaMilitiaFeat;

	public static FeatObject BattanianConstructionFeat => Instance._battaniaConstructionFeat;

	public static FeatObject EmpireGarrisonWageFeat => Instance._empireGarrisonWageFeat;

	public static FeatObject EmpireArmyInfluenceFeat => Instance._empireArmyInfluenceFeat;

	public static FeatObject EmpireVillageHearthFeat => Instance._empireVillageHearthFeat;

	public static FeatObject KhuzaitRecruitUpgradeFeat => Instance._khuzaitCheaperRecruitsFeat;

	public static FeatObject KhuzaitAnimalProductionFeat => Instance._khuzaitAnimalProductionFeat;

	public static FeatObject KhuzaitDecreasedTaxFeat => Instance._khuzaitDecreasedTaxFeat;

	public static FeatObject SturgianGrainProductionFeat => Instance._sturgianGrainProductionFeat;

	public static FeatObject SturgianArmyInfluenceCostFeat => Instance._sturgianArmyInfluenceCostFeat;

	public static FeatObject SturgianDecisionPenaltyFeat => Instance._sturgianDecisionPenaltyFeat;

	public static FeatObject VlandianRenownMercenaryFeat => Instance._vlandianRenownIncomeFeat;

	public static FeatObject VlandianCastleVillageProductionFeat => Instance._vlandianVillageProductionFeat;

	public static FeatObject VlandianArmyInfluenceFeat => Instance._vlandianArmyInfluenceCostFeat;

	public DefaultCulturalFeats()
	{
		RegisterAll();
	}

	private void RegisterAll()
	{
		_aseraiTraderFeat = Create("aserai_cheaper_caravans");
		_aseraiDesertSpeedFeat = Create("aserai_desert_speed");
		_aseraiWageFeat = Create("aserai_increased_wages");
		_battaniaForestSpeedFeat = Create("battanian_forest_speed");
		_battaniaMilitiaFeat = Create("battanian_militia_production");
		_battaniaConstructionFeat = Create("battanian_slower_construction");
		_empireGarrisonWageFeat = Create("empire_decreased_garrison_wage");
		_empireArmyInfluenceFeat = Create("empire_army_influence");
		_empireVillageHearthFeat = Create("empire_slower_hearth_production");
		_khuzaitCheaperRecruitsFeat = Create("khuzait_cheaper_recruits_mounted");
		_khuzaitAnimalProductionFeat = Create("khuzait_increased_animal_production");
		_khuzaitDecreasedTaxFeat = Create("khuzait_decreased_town_tax");
		_sturgianGrainProductionFeat = Create("sturgian_increased_grain_production");
		_sturgianArmyInfluenceCostFeat = Create("sturgian_decreased_army_influence_cost");
		_sturgianDecisionPenaltyFeat = Create("sturgian_increased_decision_penalty");
		_vlandianRenownIncomeFeat = Create("vlandian_renown_mercenary_income");
		_vlandianVillageProductionFeat = Create("vlandian_villages_production_bonus");
		_vlandianArmyInfluenceCostFeat = Create("vlandian_increased_army_influence_cost");
		InitializeAll();
	}

	private FeatObject Create(string stringId)
	{
		return Game.Current.ObjectManager.RegisterPresumedObject(new FeatObject(stringId));
	}

	private void InitializeAll()
	{
		_aseraiTraderFeat.Initialize("{=!}aserai_cheaper_caravans", "{=7kGGgkro}Caravans are 30% cheaper to build. 10% less trade penalty.", 0.7f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_aseraiDesertSpeedFeat.Initialize("{=!}aserai_desert_speed", "{=6aFTN1Nb}No speed penalty on desert.", 1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_aseraiWageFeat.Initialize("{=!}aserai_increased_wages", "{=GacrZ1Jl}Daily wages of troops in the party are increased by 5%.", 0.05f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
		_battaniaForestSpeedFeat.Initialize("{=!}battanian_forest_speed", "{=38W2WloI}50% less speed penalty and 15% sight range bonus in forests.", 0.5f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_battaniaMilitiaFeat.Initialize("{=!}battanian_militia_production", "{=HLI5zAMV}Towns owned by Battanian rulers will have +20% chance of militias to spawn as veteran militias.", 0.2f, isPositiveEffect: true, FeatObject.AdditionType.Add);
		_battaniaConstructionFeat.Initialize("{=!}battanian_slower_construction", "{=ruP9jbSq}10% slower build rate for town projects in settlements.", -0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
		_empireGarrisonWageFeat.Initialize("{=!}empire_decreased_garrison_wage", "{=a2eM0QUb}20% less garrison troop wage.", -0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_empireArmyInfluenceFeat.Initialize("{=!}empire_army_influence", "{=xgPNGOa8}Being in army brings 25% more influence.", 0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_empireVillageHearthFeat.Initialize("{=!}empire_slower_hearth_production", "{=UWiqIFUb}Village hearths increase 20% less.", -0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
		_khuzaitCheaperRecruitsFeat.Initialize("{=!}khuzait_cheaper_recruits_mounted", "{=JUpZuals}Recruiting and upgrading mounted troops are 10% cheaper.", -0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_khuzaitAnimalProductionFeat.Initialize("{=!}khuzait_increased_animal_production", "{=Xaw2CoCG}25% production bonus to horse, mule, cow and sheep in villages owned by Khuzait rulers.", 0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_khuzaitDecreasedTaxFeat.Initialize("{=!}khuzait_decreased_town_tax", "{=8PsaGhI8}20% less tax income from towns.", -0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
		_sturgianGrainProductionFeat.Initialize("{=!}sturgian_increased_grain_production", "{=5BabRyaa}Villages grain production is increased by 10%.", 0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_sturgianArmyInfluenceCostFeat.Initialize("{=!}sturgian_decreased_army_influence_cost", "{=Lmjm5Q9D}Armies are gathered with 50% less influence.", -0.5f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_sturgianDecisionPenaltyFeat.Initialize("{=!}sturgian_increased_decision_penalty", "{=fB7kS9Cx}20% more relationship penalty from kingdom decisions.", 0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
		_vlandianRenownIncomeFeat.Initialize("{=!}vlandian_renown_mercenary_income", "{=ppdrgOL8}5% more renown from the battles, 15% more income while serving as a mercenary.", 0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_vlandianVillageProductionFeat.Initialize("{=!}vlandian_villages_production_bonus", "{=3GsZXXOi}10% production bonus to villages that are bound to castles.", 0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
		_vlandianArmyInfluenceCostFeat.Initialize("{=!}vlandian_increased_army_influence_cost", "{=O1XCNeZr}Recruiting lords to armies costs 20% more influence.", 0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
	}
}

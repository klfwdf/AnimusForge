using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
	// Token: 0x020003A5 RID: 933
	public class DefaultCulturalFeats
	{
		// Token: 0x17000CA8 RID: 3240
		// (get) Token: 0x0600357A RID: 13690 RVA: 0x000D79F0 File Offset: 0x000D5BF0
		private static DefaultCulturalFeats Instance
		{
			get
			{
				return Campaign.Current.DefaultFeats;
			}
		}

		// Token: 0x0600357B RID: 13691 RVA: 0x000D79FC File Offset: 0x000D5BFC
		public DefaultCulturalFeats()
		{
			this.RegisterAll();
		}

		// Token: 0x0600357C RID: 13692 RVA: 0x000D7A0C File Offset: 0x000D5C0C
		private void RegisterAll()
		{
			this._aseraiTraderFeat = this.Create("aserai_cheaper_caravans");
			this._aseraiDesertSpeedFeat = this.Create("aserai_desert_speed");
			this._aseraiWageFeat = this.Create("aserai_increased_wages");
			this._battaniaForestSpeedFeat = this.Create("battanian_forest_speed");
			this._battaniaMilitiaFeat = this.Create("battanian_militia_production");
			this._battaniaConstructionFeat = this.Create("battanian_slower_construction");
			this._empireGarrisonWageFeat = this.Create("empire_decreased_garrison_wage");
			this._empireArmyInfluenceFeat = this.Create("empire_army_influence");
			this._empireVillageHearthFeat = this.Create("empire_slower_hearth_production");
			this._khuzaitCheaperRecruitsFeat = this.Create("khuzait_cheaper_recruits_mounted");
			this._khuzaitAnimalProductionFeat = this.Create("khuzait_increased_animal_production");
			this._khuzaitDecreasedTaxFeat = this.Create("khuzait_decreased_town_tax");
			this._sturgianGrainProductionFeat = this.Create("sturgian_increased_grain_production");
			this._sturgianArmyInfluenceCostFeat = this.Create("sturgian_decreased_army_influence_cost");
			this._sturgianDecisionPenaltyFeat = this.Create("sturgian_increased_decision_penalty");
			this._vlandianRenownIncomeFeat = this.Create("vlandian_renown_mercenary_income");
			this._vlandianVillageProductionFeat = this.Create("vlandian_villages_production_bonus");
			this._vlandianArmyInfluenceCostFeat = this.Create("vlandian_increased_army_influence_cost");
			this.InitializeAll();
		}

		// Token: 0x0600357D RID: 13693 RVA: 0x000D7B51 File Offset: 0x000D5D51
		private FeatObject Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<FeatObject>(new FeatObject(stringId));
		}

		// Token: 0x0600357E RID: 13694 RVA: 0x000D7B68 File Offset: 0x000D5D68
		private void InitializeAll()
		{
			this._aseraiTraderFeat.Initialize("{=!}aserai_cheaper_caravans", "{=7kGGgkro}Caravans are 30% cheaper to build. 10% less trade penalty.", 0.7f, true, FeatObject.AdditionType.AddFactor);
			this._aseraiDesertSpeedFeat.Initialize("{=!}aserai_desert_speed", "{=6aFTN1Nb}No speed penalty on desert.", 1f, true, FeatObject.AdditionType.AddFactor);
			this._aseraiWageFeat.Initialize("{=!}aserai_increased_wages", "{=GacrZ1Jl}Daily wages of troops in the party are increased by 5%.", 0.05f, false, FeatObject.AdditionType.AddFactor);
			this._battaniaForestSpeedFeat.Initialize("{=!}battanian_forest_speed", "{=38W2WloI}50% less speed penalty and 15% sight range bonus in forests.", 0.5f, true, FeatObject.AdditionType.AddFactor);
			this._battaniaMilitiaFeat.Initialize("{=!}battanian_militia_production", "{=HLI5zAMV}Towns owned by Battanian rulers will have +20% chance of militias to spawn as veteran militias.", 0.2f, true, FeatObject.AdditionType.Add);
			this._battaniaConstructionFeat.Initialize("{=!}battanian_slower_construction", "{=ruP9jbSq}10% slower build rate for town projects in settlements.", -0.1f, false, FeatObject.AdditionType.AddFactor);
			this._empireGarrisonWageFeat.Initialize("{=!}empire_decreased_garrison_wage", "{=a2eM0QUb}20% less garrison troop wage.", -0.2f, true, FeatObject.AdditionType.AddFactor);
			this._empireArmyInfluenceFeat.Initialize("{=!}empire_army_influence", "{=xgPNGOa8}Being in army brings 25% more influence.", 0.25f, true, FeatObject.AdditionType.AddFactor);
			this._empireVillageHearthFeat.Initialize("{=!}empire_slower_hearth_production", "{=UWiqIFUb}Village hearths increase 20% less.", -0.2f, false, FeatObject.AdditionType.AddFactor);
			this._khuzaitCheaperRecruitsFeat.Initialize("{=!}khuzait_cheaper_recruits_mounted", "{=JUpZuals}Recruiting and upgrading mounted troops are 10% cheaper.", -0.1f, true, FeatObject.AdditionType.AddFactor);
			this._khuzaitAnimalProductionFeat.Initialize("{=!}khuzait_increased_animal_production", "{=Xaw2CoCG}25% production bonus to horse, mule, cow and sheep in villages owned by Khuzait rulers.", 0.25f, true, FeatObject.AdditionType.AddFactor);
			this._khuzaitDecreasedTaxFeat.Initialize("{=!}khuzait_decreased_town_tax", "{=8PsaGhI8}20% less tax income from towns.", -0.2f, false, FeatObject.AdditionType.AddFactor);
			this._sturgianGrainProductionFeat.Initialize("{=!}sturgian_increased_grain_production", "{=5BabRyaa}Villages grain production is increased by 10%.", 0.1f, true, FeatObject.AdditionType.AddFactor);
			this._sturgianArmyInfluenceCostFeat.Initialize("{=!}sturgian_decreased_army_influence_cost", "{=Lmjm5Q9D}Armies are gathered with 50% less influence.", -0.5f, true, FeatObject.AdditionType.AddFactor);
			this._sturgianDecisionPenaltyFeat.Initialize("{=!}sturgian_increased_decision_penalty", "{=fB7kS9Cx}20% more relationship penalty from kingdom decisions.", 0.2f, false, FeatObject.AdditionType.AddFactor);
			this._vlandianRenownIncomeFeat.Initialize("{=!}vlandian_renown_mercenary_income", "{=ppdrgOL8}5% more renown from the battles, 15% more income while serving as a mercenary.", 0.05f, true, FeatObject.AdditionType.AddFactor);
			this._vlandianVillageProductionFeat.Initialize("{=!}vlandian_villages_production_bonus", "{=3GsZXXOi}10% production bonus to villages that are bound to castles.", 0.1f, true, FeatObject.AdditionType.AddFactor);
			this._vlandianArmyInfluenceCostFeat.Initialize("{=!}vlandian_increased_army_influence_cost", "{=O1XCNeZr}Recruiting lords to armies costs 20% more influence.", 0.2f, false, FeatObject.AdditionType.AddFactor);
		}

		// Token: 0x17000CA9 RID: 3241
		// (get) Token: 0x0600357F RID: 13695 RVA: 0x000D7D6D File Offset: 0x000D5F6D
		public static FeatObject AseraiTraderFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._aseraiTraderFeat;
			}
		}

		// Token: 0x17000CAA RID: 3242
		// (get) Token: 0x06003580 RID: 13696 RVA: 0x000D7D79 File Offset: 0x000D5F79
		public static FeatObject AseraiDesertFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._aseraiDesertSpeedFeat;
			}
		}

		// Token: 0x17000CAB RID: 3243
		// (get) Token: 0x06003581 RID: 13697 RVA: 0x000D7D85 File Offset: 0x000D5F85
		public static FeatObject AseraiIncreasedWageFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._aseraiWageFeat;
			}
		}

		// Token: 0x17000CAC RID: 3244
		// (get) Token: 0x06003582 RID: 13698 RVA: 0x000D7D91 File Offset: 0x000D5F91
		public static FeatObject BattanianForestSpeedFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._battaniaForestSpeedFeat;
			}
		}

		// Token: 0x17000CAD RID: 3245
		// (get) Token: 0x06003583 RID: 13699 RVA: 0x000D7D9D File Offset: 0x000D5F9D
		public static FeatObject BattanianMilitiaFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._battaniaMilitiaFeat;
			}
		}

		// Token: 0x17000CAE RID: 3246
		// (get) Token: 0x06003584 RID: 13700 RVA: 0x000D7DA9 File Offset: 0x000D5FA9
		public static FeatObject BattanianConstructionFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._battaniaConstructionFeat;
			}
		}

		// Token: 0x17000CAF RID: 3247
		// (get) Token: 0x06003585 RID: 13701 RVA: 0x000D7DB5 File Offset: 0x000D5FB5
		public static FeatObject EmpireGarrisonWageFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._empireGarrisonWageFeat;
			}
		}

		// Token: 0x17000CB0 RID: 3248
		// (get) Token: 0x06003586 RID: 13702 RVA: 0x000D7DC1 File Offset: 0x000D5FC1
		public static FeatObject EmpireArmyInfluenceFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._empireArmyInfluenceFeat;
			}
		}

		// Token: 0x17000CB1 RID: 3249
		// (get) Token: 0x06003587 RID: 13703 RVA: 0x000D7DCD File Offset: 0x000D5FCD
		public static FeatObject EmpireVillageHearthFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._empireVillageHearthFeat;
			}
		}

		// Token: 0x17000CB2 RID: 3250
		// (get) Token: 0x06003588 RID: 13704 RVA: 0x000D7DD9 File Offset: 0x000D5FD9
		public static FeatObject KhuzaitRecruitUpgradeFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._khuzaitCheaperRecruitsFeat;
			}
		}

		// Token: 0x17000CB3 RID: 3251
		// (get) Token: 0x06003589 RID: 13705 RVA: 0x000D7DE5 File Offset: 0x000D5FE5
		public static FeatObject KhuzaitAnimalProductionFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._khuzaitAnimalProductionFeat;
			}
		}

		// Token: 0x17000CB4 RID: 3252
		// (get) Token: 0x0600358A RID: 13706 RVA: 0x000D7DF1 File Offset: 0x000D5FF1
		public static FeatObject KhuzaitDecreasedTaxFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._khuzaitDecreasedTaxFeat;
			}
		}

		// Token: 0x17000CB5 RID: 3253
		// (get) Token: 0x0600358B RID: 13707 RVA: 0x000D7DFD File Offset: 0x000D5FFD
		public static FeatObject SturgianGrainProductionFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._sturgianGrainProductionFeat;
			}
		}

		// Token: 0x17000CB6 RID: 3254
		// (get) Token: 0x0600358C RID: 13708 RVA: 0x000D7E09 File Offset: 0x000D6009
		public static FeatObject SturgianArmyInfluenceCostFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._sturgianArmyInfluenceCostFeat;
			}
		}

		// Token: 0x17000CB7 RID: 3255
		// (get) Token: 0x0600358D RID: 13709 RVA: 0x000D7E15 File Offset: 0x000D6015
		public static FeatObject SturgianDecisionPenaltyFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._sturgianDecisionPenaltyFeat;
			}
		}

		// Token: 0x17000CB8 RID: 3256
		// (get) Token: 0x0600358E RID: 13710 RVA: 0x000D7E21 File Offset: 0x000D6021
		public static FeatObject VlandianRenownMercenaryFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._vlandianRenownIncomeFeat;
			}
		}

		// Token: 0x17000CB9 RID: 3257
		// (get) Token: 0x0600358F RID: 13711 RVA: 0x000D7E2D File Offset: 0x000D602D
		public static FeatObject VlandianCastleVillageProductionFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._vlandianVillageProductionFeat;
			}
		}

		// Token: 0x17000CBA RID: 3258
		// (get) Token: 0x06003590 RID: 13712 RVA: 0x000D7E39 File Offset: 0x000D6039
		public static FeatObject VlandianArmyInfluenceFeat
		{
			get
			{
				return DefaultCulturalFeats.Instance._vlandianArmyInfluenceCostFeat;
			}
		}

		// Token: 0x04000F32 RID: 3890
		private FeatObject _aseraiTraderFeat;

		// Token: 0x04000F33 RID: 3891
		private FeatObject _aseraiDesertSpeedFeat;

		// Token: 0x04000F34 RID: 3892
		private FeatObject _aseraiWageFeat;

		// Token: 0x04000F35 RID: 3893
		private FeatObject _battaniaForestSpeedFeat;

		// Token: 0x04000F36 RID: 3894
		private FeatObject _battaniaMilitiaFeat;

		// Token: 0x04000F37 RID: 3895
		private FeatObject _battaniaConstructionFeat;

		// Token: 0x04000F38 RID: 3896
		private FeatObject _empireGarrisonWageFeat;

		// Token: 0x04000F39 RID: 3897
		private FeatObject _empireArmyInfluenceFeat;

		// Token: 0x04000F3A RID: 3898
		private FeatObject _empireVillageHearthFeat;

		// Token: 0x04000F3B RID: 3899
		private FeatObject _khuzaitCheaperRecruitsFeat;

		// Token: 0x04000F3C RID: 3900
		private FeatObject _khuzaitAnimalProductionFeat;

		// Token: 0x04000F3D RID: 3901
		private FeatObject _khuzaitDecreasedTaxFeat;

		// Token: 0x04000F3E RID: 3902
		private FeatObject _sturgianGrainProductionFeat;

		// Token: 0x04000F3F RID: 3903
		private FeatObject _sturgianArmyInfluenceCostFeat;

		// Token: 0x04000F40 RID: 3904
		private FeatObject _sturgianDecisionPenaltyFeat;

		// Token: 0x04000F41 RID: 3905
		private FeatObject _vlandianRenownIncomeFeat;

		// Token: 0x04000F42 RID: 3906
		private FeatObject _vlandianVillageProductionFeat;

		// Token: 0x04000F43 RID: 3907
		private FeatObject _vlandianArmyInfluenceCostFeat;
	}
}

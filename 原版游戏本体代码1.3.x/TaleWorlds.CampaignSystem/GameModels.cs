using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000087 RID: 135
	public sealed class GameModels : GameModelsManager
	{
		// Token: 0x17000455 RID: 1109
		// (get) Token: 0x0600111D RID: 4381 RVA: 0x00051BC6 File Offset: 0x0004FDC6
		// (set) Token: 0x0600111E RID: 4382 RVA: 0x00051BCE File Offset: 0x0004FDCE
		public MapVisibilityModel MapVisibilityModel { get; private set; }

		// Token: 0x17000456 RID: 1110
		// (get) Token: 0x0600111F RID: 4383 RVA: 0x00051BD7 File Offset: 0x0004FDD7
		// (set) Token: 0x06001120 RID: 4384 RVA: 0x00051BDF File Offset: 0x0004FDDF
		public InformationRestrictionModel InformationRestrictionModel { get; private set; }

		// Token: 0x17000457 RID: 1111
		// (get) Token: 0x06001121 RID: 4385 RVA: 0x00051BE8 File Offset: 0x0004FDE8
		// (set) Token: 0x06001122 RID: 4386 RVA: 0x00051BF0 File Offset: 0x0004FDF0
		public PartySpeedModel PartySpeedCalculatingModel { get; private set; }

		// Token: 0x17000458 RID: 1112
		// (get) Token: 0x06001123 RID: 4387 RVA: 0x00051BF9 File Offset: 0x0004FDF9
		// (set) Token: 0x06001124 RID: 4388 RVA: 0x00051C01 File Offset: 0x0004FE01
		public PartyHealingModel PartyHealingModel { get; private set; }

		// Token: 0x17000459 RID: 1113
		// (get) Token: 0x06001125 RID: 4389 RVA: 0x00051C0A File Offset: 0x0004FE0A
		// (set) Token: 0x06001126 RID: 4390 RVA: 0x00051C12 File Offset: 0x0004FE12
		public CaravanModel CaravanModel { get; private set; }

		// Token: 0x1700045A RID: 1114
		// (get) Token: 0x06001127 RID: 4391 RVA: 0x00051C1B File Offset: 0x0004FE1B
		// (set) Token: 0x06001128 RID: 4392 RVA: 0x00051C23 File Offset: 0x0004FE23
		public PartyTrainingModel PartyTrainingModel { get; private set; }

		// Token: 0x1700045B RID: 1115
		// (get) Token: 0x06001129 RID: 4393 RVA: 0x00051C2C File Offset: 0x0004FE2C
		// (set) Token: 0x0600112A RID: 4394 RVA: 0x00051C34 File Offset: 0x0004FE34
		public BarterModel BarterModel { get; private set; }

		// Token: 0x1700045C RID: 1116
		// (get) Token: 0x0600112B RID: 4395 RVA: 0x00051C3D File Offset: 0x0004FE3D
		// (set) Token: 0x0600112C RID: 4396 RVA: 0x00051C45 File Offset: 0x0004FE45
		public PersuasionModel PersuasionModel { get; private set; }

		// Token: 0x1700045D RID: 1117
		// (get) Token: 0x0600112D RID: 4397 RVA: 0x00051C4E File Offset: 0x0004FE4E
		// (set) Token: 0x0600112E RID: 4398 RVA: 0x00051C56 File Offset: 0x0004FE56
		public DefectionModel DefectionModel { get; private set; }

		// Token: 0x1700045E RID: 1118
		// (get) Token: 0x0600112F RID: 4399 RVA: 0x00051C5F File Offset: 0x0004FE5F
		// (set) Token: 0x06001130 RID: 4400 RVA: 0x00051C67 File Offset: 0x0004FE67
		public CombatSimulationModel CombatSimulationModel { get; private set; }

		// Token: 0x1700045F RID: 1119
		// (get) Token: 0x06001131 RID: 4401 RVA: 0x00051C70 File Offset: 0x0004FE70
		// (set) Token: 0x06001132 RID: 4402 RVA: 0x00051C78 File Offset: 0x0004FE78
		public CombatXpModel CombatXpModel { get; private set; }

		// Token: 0x17000460 RID: 1120
		// (get) Token: 0x06001133 RID: 4403 RVA: 0x00051C81 File Offset: 0x0004FE81
		// (set) Token: 0x06001134 RID: 4404 RVA: 0x00051C89 File Offset: 0x0004FE89
		public GenericXpModel GenericXpModel { get; private set; }

		// Token: 0x17000461 RID: 1121
		// (get) Token: 0x06001135 RID: 4405 RVA: 0x00051C92 File Offset: 0x0004FE92
		// (set) Token: 0x06001136 RID: 4406 RVA: 0x00051C9A File Offset: 0x0004FE9A
		public TradeAgreementModel TradeAgreementModel { get; private set; }

		// Token: 0x17000462 RID: 1122
		// (get) Token: 0x06001137 RID: 4407 RVA: 0x00051CA3 File Offset: 0x0004FEA3
		// (set) Token: 0x06001138 RID: 4408 RVA: 0x00051CAB File Offset: 0x0004FEAB
		public SmithingModel SmithingModel { get; private set; }

		// Token: 0x17000463 RID: 1123
		// (get) Token: 0x06001139 RID: 4409 RVA: 0x00051CB4 File Offset: 0x0004FEB4
		// (set) Token: 0x0600113A RID: 4410 RVA: 0x00051CBC File Offset: 0x0004FEBC
		public PartyTradeModel PartyTradeModel { get; private set; }

		// Token: 0x17000464 RID: 1124
		// (get) Token: 0x0600113B RID: 4411 RVA: 0x00051CC5 File Offset: 0x0004FEC5
		// (set) Token: 0x0600113C RID: 4412 RVA: 0x00051CCD File Offset: 0x0004FECD
		public RansomValueCalculationModel RansomValueCalculationModel { get; private set; }

		// Token: 0x17000465 RID: 1125
		// (get) Token: 0x0600113D RID: 4413 RVA: 0x00051CD6 File Offset: 0x0004FED6
		// (set) Token: 0x0600113E RID: 4414 RVA: 0x00051CDE File Offset: 0x0004FEDE
		public RaidModel RaidModel { get; private set; }

		// Token: 0x17000466 RID: 1126
		// (get) Token: 0x0600113F RID: 4415 RVA: 0x00051CE7 File Offset: 0x0004FEE7
		// (set) Token: 0x06001140 RID: 4416 RVA: 0x00051CEF File Offset: 0x0004FEEF
		public MobilePartyFoodConsumptionModel MobilePartyFoodConsumptionModel { get; private set; }

		// Token: 0x17000467 RID: 1127
		// (get) Token: 0x06001141 RID: 4417 RVA: 0x00051CF8 File Offset: 0x0004FEF8
		// (set) Token: 0x06001142 RID: 4418 RVA: 0x00051D00 File Offset: 0x0004FF00
		public PartyFoodBuyingModel PartyFoodBuyingModel { get; private set; }

		// Token: 0x17000468 RID: 1128
		// (get) Token: 0x06001143 RID: 4419 RVA: 0x00051D09 File Offset: 0x0004FF09
		// (set) Token: 0x06001144 RID: 4420 RVA: 0x00051D11 File Offset: 0x0004FF11
		public PartyImpairmentModel PartyImpairmentModel { get; private set; }

		// Token: 0x17000469 RID: 1129
		// (get) Token: 0x06001145 RID: 4421 RVA: 0x00051D1A File Offset: 0x0004FF1A
		// (set) Token: 0x06001146 RID: 4422 RVA: 0x00051D22 File Offset: 0x0004FF22
		public PartyMoraleModel PartyMoraleModel { get; private set; }

		// Token: 0x1700046A RID: 1130
		// (get) Token: 0x06001147 RID: 4423 RVA: 0x00051D2B File Offset: 0x0004FF2B
		// (set) Token: 0x06001148 RID: 4424 RVA: 0x00051D33 File Offset: 0x0004FF33
		public PartyDesertionModel PartyDesertionModel { get; private set; }

		// Token: 0x1700046B RID: 1131
		// (get) Token: 0x06001149 RID: 4425 RVA: 0x00051D3C File Offset: 0x0004FF3C
		// (set) Token: 0x0600114A RID: 4426 RVA: 0x00051D44 File Offset: 0x0004FF44
		public PartyTransitionModel PartyTransitionModel { get; private set; }

		// Token: 0x1700046C RID: 1132
		// (get) Token: 0x0600114B RID: 4427 RVA: 0x00051D4D File Offset: 0x0004FF4D
		// (set) Token: 0x0600114C RID: 4428 RVA: 0x00051D55 File Offset: 0x0004FF55
		public DiplomacyModel DiplomacyModel { get; private set; }

		// Token: 0x1700046D RID: 1133
		// (get) Token: 0x0600114D RID: 4429 RVA: 0x00051D5E File Offset: 0x0004FF5E
		// (set) Token: 0x0600114E RID: 4430 RVA: 0x00051D66 File Offset: 0x0004FF66
		public AllianceModel AllianceModel { get; private set; }

		// Token: 0x1700046E RID: 1134
		// (get) Token: 0x0600114F RID: 4431 RVA: 0x00051D6F File Offset: 0x0004FF6F
		// (set) Token: 0x06001150 RID: 4432 RVA: 0x00051D77 File Offset: 0x0004FF77
		public MinorFactionsModel MinorFactionsModel { get; private set; }

		// Token: 0x1700046F RID: 1135
		// (get) Token: 0x06001151 RID: 4433 RVA: 0x00051D80 File Offset: 0x0004FF80
		// (set) Token: 0x06001152 RID: 4434 RVA: 0x00051D88 File Offset: 0x0004FF88
		public HideoutModel HideoutModel { get; private set; }

		// Token: 0x17000470 RID: 1136
		// (get) Token: 0x06001153 RID: 4435 RVA: 0x00051D91 File Offset: 0x0004FF91
		// (set) Token: 0x06001154 RID: 4436 RVA: 0x00051D99 File Offset: 0x0004FF99
		public KingdomCreationModel KingdomCreationModel { get; private set; }

		// Token: 0x17000471 RID: 1137
		// (get) Token: 0x06001155 RID: 4437 RVA: 0x00051DA2 File Offset: 0x0004FFA2
		// (set) Token: 0x06001156 RID: 4438 RVA: 0x00051DAA File Offset: 0x0004FFAA
		public KingdomDecisionPermissionModel KingdomDecisionPermissionModel { get; private set; }

		// Token: 0x17000472 RID: 1138
		// (get) Token: 0x06001157 RID: 4439 RVA: 0x00051DB3 File Offset: 0x0004FFB3
		// (set) Token: 0x06001158 RID: 4440 RVA: 0x00051DBB File Offset: 0x0004FFBB
		public EmissaryModel EmissaryModel { get; private set; }

		// Token: 0x17000473 RID: 1139
		// (get) Token: 0x06001159 RID: 4441 RVA: 0x00051DC4 File Offset: 0x0004FFC4
		// (set) Token: 0x0600115A RID: 4442 RVA: 0x00051DCC File Offset: 0x0004FFCC
		public CharacterDevelopmentModel CharacterDevelopmentModel { get; private set; }

		// Token: 0x17000474 RID: 1140
		// (get) Token: 0x0600115B RID: 4443 RVA: 0x00051DD5 File Offset: 0x0004FFD5
		// (set) Token: 0x0600115C RID: 4444 RVA: 0x00051DDD File Offset: 0x0004FFDD
		public CharacterStatsModel CharacterStatsModel { get; private set; }

		// Token: 0x17000475 RID: 1141
		// (get) Token: 0x0600115D RID: 4445 RVA: 0x00051DE6 File Offset: 0x0004FFE6
		// (set) Token: 0x0600115E RID: 4446 RVA: 0x00051DEE File Offset: 0x0004FFEE
		public EncounterModel EncounterModel { get; private set; }

		// Token: 0x17000476 RID: 1142
		// (get) Token: 0x0600115F RID: 4447 RVA: 0x00051DF7 File Offset: 0x0004FFF7
		// (set) Token: 0x06001160 RID: 4448 RVA: 0x00051DFF File Offset: 0x0004FFFF
		public SettlementPatrolModel SettlementPatrolModel { get; private set; }

		// Token: 0x17000477 RID: 1143
		// (get) Token: 0x06001161 RID: 4449 RVA: 0x00051E08 File Offset: 0x00050008
		// (set) Token: 0x06001162 RID: 4450 RVA: 0x00051E10 File Offset: 0x00050010
		public ItemDiscardModel ItemDiscardModel { get; private set; }

		// Token: 0x17000478 RID: 1144
		// (get) Token: 0x06001163 RID: 4451 RVA: 0x00051E19 File Offset: 0x00050019
		// (set) Token: 0x06001164 RID: 4452 RVA: 0x00051E21 File Offset: 0x00050021
		public ValuationModel ValuationModel { get; private set; }

		// Token: 0x17000479 RID: 1145
		// (get) Token: 0x06001165 RID: 4453 RVA: 0x00051E2A File Offset: 0x0005002A
		// (set) Token: 0x06001166 RID: 4454 RVA: 0x00051E32 File Offset: 0x00050032
		public PartySizeLimitModel PartySizeLimitModel { get; private set; }

		// Token: 0x1700047A RID: 1146
		// (get) Token: 0x06001167 RID: 4455 RVA: 0x00051E3B File Offset: 0x0005003B
		// (set) Token: 0x06001168 RID: 4456 RVA: 0x00051E43 File Offset: 0x00050043
		public PartyShipLimitModel PartyShipLimitModel { get; private set; }

		// Token: 0x1700047B RID: 1147
		// (get) Token: 0x06001169 RID: 4457 RVA: 0x00051E4C File Offset: 0x0005004C
		// (set) Token: 0x0600116A RID: 4458 RVA: 0x00051E54 File Offset: 0x00050054
		public InventoryCapacityModel InventoryCapacityModel { get; private set; }

		// Token: 0x1700047C RID: 1148
		// (get) Token: 0x0600116B RID: 4459 RVA: 0x00051E5D File Offset: 0x0005005D
		// (set) Token: 0x0600116C RID: 4460 RVA: 0x00051E65 File Offset: 0x00050065
		public PartyWageModel PartyWageModel { get; private set; }

		// Token: 0x1700047D RID: 1149
		// (get) Token: 0x0600116D RID: 4461 RVA: 0x00051E6E File Offset: 0x0005006E
		// (set) Token: 0x0600116E RID: 4462 RVA: 0x00051E76 File Offset: 0x00050076
		public VillageProductionCalculatorModel VillageProductionCalculatorModel { get; private set; }

		// Token: 0x1700047E RID: 1150
		// (get) Token: 0x0600116F RID: 4463 RVA: 0x00051E7F File Offset: 0x0005007F
		// (set) Token: 0x06001170 RID: 4464 RVA: 0x00051E87 File Offset: 0x00050087
		public VolunteerModel VolunteerModel { get; private set; }

		// Token: 0x1700047F RID: 1151
		// (get) Token: 0x06001171 RID: 4465 RVA: 0x00051E90 File Offset: 0x00050090
		// (set) Token: 0x06001172 RID: 4466 RVA: 0x00051E98 File Offset: 0x00050098
		public RomanceModel RomanceModel { get; private set; }

		// Token: 0x17000480 RID: 1152
		// (get) Token: 0x06001173 RID: 4467 RVA: 0x00051EA1 File Offset: 0x000500A1
		// (set) Token: 0x06001174 RID: 4468 RVA: 0x00051EA9 File Offset: 0x000500A9
		public MobilePartyAIModel MobilePartyAIModel { get; private set; }

		// Token: 0x17000481 RID: 1153
		// (get) Token: 0x06001175 RID: 4469 RVA: 0x00051EB2 File Offset: 0x000500B2
		// (set) Token: 0x06001176 RID: 4470 RVA: 0x00051EBA File Offset: 0x000500BA
		public ArmyManagementCalculationModel ArmyManagementCalculationModel { get; private set; }

		// Token: 0x17000482 RID: 1154
		// (get) Token: 0x06001177 RID: 4471 RVA: 0x00051EC3 File Offset: 0x000500C3
		// (set) Token: 0x06001178 RID: 4472 RVA: 0x00051ECB File Offset: 0x000500CB
		public BanditDensityModel BanditDensityModel { get; private set; }

		// Token: 0x17000483 RID: 1155
		// (get) Token: 0x06001179 RID: 4473 RVA: 0x00051ED4 File Offset: 0x000500D4
		// (set) Token: 0x0600117A RID: 4474 RVA: 0x00051EDC File Offset: 0x000500DC
		public EncounterGameMenuModel EncounterGameMenuModel { get; private set; }

		// Token: 0x17000484 RID: 1156
		// (get) Token: 0x0600117B RID: 4475 RVA: 0x00051EE5 File Offset: 0x000500E5
		// (set) Token: 0x0600117C RID: 4476 RVA: 0x00051EED File Offset: 0x000500ED
		public BattleRewardModel BattleRewardModel { get; private set; }

		// Token: 0x17000485 RID: 1157
		// (get) Token: 0x0600117D RID: 4477 RVA: 0x00051EF6 File Offset: 0x000500F6
		// (set) Token: 0x0600117E RID: 4478 RVA: 0x00051EFE File Offset: 0x000500FE
		public MapTrackModel MapTrackModel { get; private set; }

		// Token: 0x17000486 RID: 1158
		// (get) Token: 0x0600117F RID: 4479 RVA: 0x00051F07 File Offset: 0x00050107
		// (set) Token: 0x06001180 RID: 4480 RVA: 0x00051F0F File Offset: 0x0005010F
		public MapDistanceModel MapDistanceModel { get; private set; }

		// Token: 0x17000487 RID: 1159
		// (get) Token: 0x06001181 RID: 4481 RVA: 0x00051F18 File Offset: 0x00050118
		// (set) Token: 0x06001182 RID: 4482 RVA: 0x00051F20 File Offset: 0x00050120
		public PartyNavigationModel PartyNavigationModel { get; private set; }

		// Token: 0x17000488 RID: 1160
		// (get) Token: 0x06001183 RID: 4483 RVA: 0x00051F29 File Offset: 0x00050129
		// (set) Token: 0x06001184 RID: 4484 RVA: 0x00051F31 File Offset: 0x00050131
		public MapWeatherModel MapWeatherModel { get; private set; }

		// Token: 0x17000489 RID: 1161
		// (get) Token: 0x06001185 RID: 4485 RVA: 0x00051F3A File Offset: 0x0005013A
		// (set) Token: 0x06001186 RID: 4486 RVA: 0x00051F42 File Offset: 0x00050142
		public TargetScoreCalculatingModel TargetScoreCalculatingModel { get; private set; }

		// Token: 0x1700048A RID: 1162
		// (get) Token: 0x06001187 RID: 4487 RVA: 0x00051F4B File Offset: 0x0005014B
		// (set) Token: 0x06001188 RID: 4488 RVA: 0x00051F53 File Offset: 0x00050153
		public TradeItemPriceFactorModel TradeItemPriceFactorModel { get; private set; }

		// Token: 0x1700048B RID: 1163
		// (get) Token: 0x06001189 RID: 4489 RVA: 0x00051F5C File Offset: 0x0005015C
		// (set) Token: 0x0600118A RID: 4490 RVA: 0x00051F64 File Offset: 0x00050164
		public SettlementEconomyModel SettlementEconomyModel { get; private set; }

		// Token: 0x1700048C RID: 1164
		// (get) Token: 0x0600118B RID: 4491 RVA: 0x00051F6D File Offset: 0x0005016D
		// (set) Token: 0x0600118C RID: 4492 RVA: 0x00051F75 File Offset: 0x00050175
		public SettlementFoodModel SettlementFoodModel { get; private set; }

		// Token: 0x1700048D RID: 1165
		// (get) Token: 0x0600118D RID: 4493 RVA: 0x00051F7E File Offset: 0x0005017E
		// (set) Token: 0x0600118E RID: 4494 RVA: 0x00051F86 File Offset: 0x00050186
		public SettlementValueModel SettlementValueModel { get; private set; }

		// Token: 0x1700048E RID: 1166
		// (get) Token: 0x0600118F RID: 4495 RVA: 0x00051F8F File Offset: 0x0005018F
		// (set) Token: 0x06001190 RID: 4496 RVA: 0x00051F97 File Offset: 0x00050197
		public SettlementMilitiaModel SettlementMilitiaModel { get; private set; }

		// Token: 0x1700048F RID: 1167
		// (get) Token: 0x06001191 RID: 4497 RVA: 0x00051FA0 File Offset: 0x000501A0
		// (set) Token: 0x06001192 RID: 4498 RVA: 0x00051FA8 File Offset: 0x000501A8
		public SettlementLoyaltyModel SettlementLoyaltyModel { get; private set; }

		// Token: 0x17000490 RID: 1168
		// (get) Token: 0x06001193 RID: 4499 RVA: 0x00051FB1 File Offset: 0x000501B1
		// (set) Token: 0x06001194 RID: 4500 RVA: 0x00051FB9 File Offset: 0x000501B9
		public SettlementSecurityModel SettlementSecurityModel { get; private set; }

		// Token: 0x17000491 RID: 1169
		// (get) Token: 0x06001195 RID: 4501 RVA: 0x00051FC2 File Offset: 0x000501C2
		// (set) Token: 0x06001196 RID: 4502 RVA: 0x00051FCA File Offset: 0x000501CA
		public SettlementProsperityModel SettlementProsperityModel { get; private set; }

		// Token: 0x17000492 RID: 1170
		// (get) Token: 0x06001197 RID: 4503 RVA: 0x00051FD3 File Offset: 0x000501D3
		// (set) Token: 0x06001198 RID: 4504 RVA: 0x00051FDB File Offset: 0x000501DB
		public SettlementGarrisonModel SettlementGarrisonModel { get; private set; }

		// Token: 0x17000493 RID: 1171
		// (get) Token: 0x06001199 RID: 4505 RVA: 0x00051FE4 File Offset: 0x000501E4
		// (set) Token: 0x0600119A RID: 4506 RVA: 0x00051FEC File Offset: 0x000501EC
		public ClanTierModel ClanTierModel { get; private set; }

		// Token: 0x17000494 RID: 1172
		// (get) Token: 0x0600119B RID: 4507 RVA: 0x00051FF5 File Offset: 0x000501F5
		// (set) Token: 0x0600119C RID: 4508 RVA: 0x00051FFD File Offset: 0x000501FD
		public VassalRewardsModel VassalRewardsModel { get; private set; }

		// Token: 0x17000495 RID: 1173
		// (get) Token: 0x0600119D RID: 4509 RVA: 0x00052006 File Offset: 0x00050206
		// (set) Token: 0x0600119E RID: 4510 RVA: 0x0005200E File Offset: 0x0005020E
		public ClanPoliticsModel ClanPoliticsModel { get; private set; }

		// Token: 0x17000496 RID: 1174
		// (get) Token: 0x0600119F RID: 4511 RVA: 0x00052017 File Offset: 0x00050217
		// (set) Token: 0x060011A0 RID: 4512 RVA: 0x0005201F File Offset: 0x0005021F
		public ClanFinanceModel ClanFinanceModel { get; private set; }

		// Token: 0x17000497 RID: 1175
		// (get) Token: 0x060011A1 RID: 4513 RVA: 0x00052028 File Offset: 0x00050228
		// (set) Token: 0x060011A2 RID: 4514 RVA: 0x00052030 File Offset: 0x00050230
		public SettlementTaxModel SettlementTaxModel { get; private set; }

		// Token: 0x17000498 RID: 1176
		// (get) Token: 0x060011A3 RID: 4515 RVA: 0x00052039 File Offset: 0x00050239
		// (set) Token: 0x060011A4 RID: 4516 RVA: 0x00052041 File Offset: 0x00050241
		public HeroAgentLocationModel HeroAgentLocationModel { get; private set; }

		// Token: 0x17000499 RID: 1177
		// (get) Token: 0x060011A5 RID: 4517 RVA: 0x0005204A File Offset: 0x0005024A
		// (set) Token: 0x060011A6 RID: 4518 RVA: 0x00052052 File Offset: 0x00050252
		public HeirSelectionCalculationModel HeirSelectionCalculationModel { get; private set; }

		// Token: 0x1700049A RID: 1178
		// (get) Token: 0x060011A7 RID: 4519 RVA: 0x0005205B File Offset: 0x0005025B
		// (set) Token: 0x060011A8 RID: 4520 RVA: 0x00052063 File Offset: 0x00050263
		public HeroDeathProbabilityCalculationModel HeroDeathProbabilityCalculationModel { get; private set; }

		// Token: 0x1700049B RID: 1179
		// (get) Token: 0x060011A9 RID: 4521 RVA: 0x0005206C File Offset: 0x0005026C
		// (set) Token: 0x060011AA RID: 4522 RVA: 0x00052074 File Offset: 0x00050274
		public BuildingConstructionModel BuildingConstructionModel { get; private set; }

		// Token: 0x1700049C RID: 1180
		// (get) Token: 0x060011AB RID: 4523 RVA: 0x0005207D File Offset: 0x0005027D
		// (set) Token: 0x060011AC RID: 4524 RVA: 0x00052085 File Offset: 0x00050285
		public BuildingEffectModel BuildingEffectModel { get; private set; }

		// Token: 0x1700049D RID: 1181
		// (get) Token: 0x060011AD RID: 4525 RVA: 0x0005208E File Offset: 0x0005028E
		// (set) Token: 0x060011AE RID: 4526 RVA: 0x00052096 File Offset: 0x00050296
		public WallHitPointCalculationModel WallHitPointCalculationModel { get; private set; }

		// Token: 0x1700049E RID: 1182
		// (get) Token: 0x060011AF RID: 4527 RVA: 0x0005209F File Offset: 0x0005029F
		// (set) Token: 0x060011B0 RID: 4528 RVA: 0x000520A7 File Offset: 0x000502A7
		public MarriageModel MarriageModel { get; private set; }

		// Token: 0x1700049F RID: 1183
		// (get) Token: 0x060011B1 RID: 4529 RVA: 0x000520B0 File Offset: 0x000502B0
		// (set) Token: 0x060011B2 RID: 4530 RVA: 0x000520B8 File Offset: 0x000502B8
		public AgeModel AgeModel { get; private set; }

		// Token: 0x170004A0 RID: 1184
		// (get) Token: 0x060011B3 RID: 4531 RVA: 0x000520C1 File Offset: 0x000502C1
		// (set) Token: 0x060011B4 RID: 4532 RVA: 0x000520C9 File Offset: 0x000502C9
		public PlayerProgressionModel PlayerProgressionModel { get; private set; }

		// Token: 0x170004A1 RID: 1185
		// (get) Token: 0x060011B5 RID: 4533 RVA: 0x000520D2 File Offset: 0x000502D2
		// (set) Token: 0x060011B6 RID: 4534 RVA: 0x000520DA File Offset: 0x000502DA
		public DailyTroopXpBonusModel DailyTroopXpBonusModel { get; private set; }

		// Token: 0x170004A2 RID: 1186
		// (get) Token: 0x060011B7 RID: 4535 RVA: 0x000520E3 File Offset: 0x000502E3
		// (set) Token: 0x060011B8 RID: 4536 RVA: 0x000520EB File Offset: 0x000502EB
		public PregnancyModel PregnancyModel { get; private set; }

		// Token: 0x170004A3 RID: 1187
		// (get) Token: 0x060011B9 RID: 4537 RVA: 0x000520F4 File Offset: 0x000502F4
		// (set) Token: 0x060011BA RID: 4538 RVA: 0x000520FC File Offset: 0x000502FC
		public NotablePowerModel NotablePowerModel { get; private set; }

		// Token: 0x170004A4 RID: 1188
		// (get) Token: 0x060011BB RID: 4539 RVA: 0x00052105 File Offset: 0x00050305
		// (set) Token: 0x060011BC RID: 4540 RVA: 0x0005210D File Offset: 0x0005030D
		public MilitaryPowerModel MilitaryPowerModel { get; private set; }

		// Token: 0x170004A5 RID: 1189
		// (get) Token: 0x060011BD RID: 4541 RVA: 0x00052116 File Offset: 0x00050316
		// (set) Token: 0x060011BE RID: 4542 RVA: 0x0005211E File Offset: 0x0005031E
		public PrisonerDonationModel PrisonerDonationModel { get; private set; }

		// Token: 0x170004A6 RID: 1190
		// (get) Token: 0x060011BF RID: 4543 RVA: 0x00052127 File Offset: 0x00050327
		// (set) Token: 0x060011C0 RID: 4544 RVA: 0x0005212F File Offset: 0x0005032F
		public NotableSpawnModel NotableSpawnModel { get; private set; }

		// Token: 0x170004A7 RID: 1191
		// (get) Token: 0x060011C1 RID: 4545 RVA: 0x00052138 File Offset: 0x00050338
		// (set) Token: 0x060011C2 RID: 4546 RVA: 0x00052140 File Offset: 0x00050340
		public TournamentModel TournamentModel { get; private set; }

		// Token: 0x170004A8 RID: 1192
		// (get) Token: 0x060011C3 RID: 4547 RVA: 0x00052149 File Offset: 0x00050349
		// (set) Token: 0x060011C4 RID: 4548 RVA: 0x00052151 File Offset: 0x00050351
		public CrimeModel CrimeModel { get; private set; }

		// Token: 0x170004A9 RID: 1193
		// (get) Token: 0x060011C5 RID: 4549 RVA: 0x0005215A File Offset: 0x0005035A
		// (set) Token: 0x060011C6 RID: 4550 RVA: 0x00052162 File Offset: 0x00050362
		public DisguiseDetectionModel DisguiseDetectionModel { get; private set; }

		// Token: 0x170004AA RID: 1194
		// (get) Token: 0x060011C7 RID: 4551 RVA: 0x0005216B File Offset: 0x0005036B
		// (set) Token: 0x060011C8 RID: 4552 RVA: 0x00052173 File Offset: 0x00050373
		public BribeCalculationModel BribeCalculationModel { get; private set; }

		// Token: 0x170004AB RID: 1195
		// (get) Token: 0x060011C9 RID: 4553 RVA: 0x0005217C File Offset: 0x0005037C
		// (set) Token: 0x060011CA RID: 4554 RVA: 0x00052184 File Offset: 0x00050384
		public TroopSacrificeModel TroopSacrificeModel { get; private set; }

		// Token: 0x170004AC RID: 1196
		// (get) Token: 0x060011CB RID: 4555 RVA: 0x0005218D File Offset: 0x0005038D
		// (set) Token: 0x060011CC RID: 4556 RVA: 0x00052195 File Offset: 0x00050395
		public SiegeStrategyActionModel SiegeStrategyActionModel { get; private set; }

		// Token: 0x170004AD RID: 1197
		// (get) Token: 0x060011CD RID: 4557 RVA: 0x0005219E File Offset: 0x0005039E
		// (set) Token: 0x060011CE RID: 4558 RVA: 0x000521A6 File Offset: 0x000503A6
		public SiegeEventModel SiegeEventModel { get; private set; }

		// Token: 0x170004AE RID: 1198
		// (get) Token: 0x060011CF RID: 4559 RVA: 0x000521AF File Offset: 0x000503AF
		// (set) Token: 0x060011D0 RID: 4560 RVA: 0x000521B7 File Offset: 0x000503B7
		public SiegeAftermathModel SiegeAftermathModel { get; private set; }

		// Token: 0x170004AF RID: 1199
		// (get) Token: 0x060011D1 RID: 4561 RVA: 0x000521C0 File Offset: 0x000503C0
		// (set) Token: 0x060011D2 RID: 4562 RVA: 0x000521C8 File Offset: 0x000503C8
		public SiegeLordsHallFightModel SiegeLordsHallFightModel { get; private set; }

		// Token: 0x170004B0 RID: 1200
		// (get) Token: 0x060011D3 RID: 4563 RVA: 0x000521D1 File Offset: 0x000503D1
		// (set) Token: 0x060011D4 RID: 4564 RVA: 0x000521D9 File Offset: 0x000503D9
		public CompanionHiringPriceCalculationModel CompanionHiringPriceCalculationModel { get; private set; }

		// Token: 0x170004B1 RID: 1201
		// (get) Token: 0x060011D5 RID: 4565 RVA: 0x000521E2 File Offset: 0x000503E2
		// (set) Token: 0x060011D6 RID: 4566 RVA: 0x000521EA File Offset: 0x000503EA
		public BuildingScoreCalculationModel BuildingScoreCalculationModel { get; private set; }

		// Token: 0x170004B2 RID: 1202
		// (get) Token: 0x060011D7 RID: 4567 RVA: 0x000521F3 File Offset: 0x000503F3
		// (set) Token: 0x060011D8 RID: 4568 RVA: 0x000521FB File Offset: 0x000503FB
		public SettlementAccessModel SettlementAccessModel { get; private set; }

		// Token: 0x170004B3 RID: 1203
		// (get) Token: 0x060011D9 RID: 4569 RVA: 0x00052204 File Offset: 0x00050404
		// (set) Token: 0x060011DA RID: 4570 RVA: 0x0005220C File Offset: 0x0005040C
		public IssueModel IssueModel { get; private set; }

		// Token: 0x170004B4 RID: 1204
		// (get) Token: 0x060011DB RID: 4571 RVA: 0x00052215 File Offset: 0x00050415
		// (set) Token: 0x060011DC RID: 4572 RVA: 0x0005221D File Offset: 0x0005041D
		public PrisonerRecruitmentCalculationModel PrisonerRecruitmentCalculationModel { get; private set; }

		// Token: 0x170004B5 RID: 1205
		// (get) Token: 0x060011DD RID: 4573 RVA: 0x00052226 File Offset: 0x00050426
		// (set) Token: 0x060011DE RID: 4574 RVA: 0x0005222E File Offset: 0x0005042E
		public PartyTroopUpgradeModel PartyTroopUpgradeModel { get; private set; }

		// Token: 0x170004B6 RID: 1206
		// (get) Token: 0x060011DF RID: 4575 RVA: 0x00052237 File Offset: 0x00050437
		// (set) Token: 0x060011E0 RID: 4576 RVA: 0x0005223F File Offset: 0x0005043F
		public TavernMercenaryTroopsModel TavernMercenaryTroopsModel { get; private set; }

		// Token: 0x170004B7 RID: 1207
		// (get) Token: 0x060011E1 RID: 4577 RVA: 0x00052248 File Offset: 0x00050448
		// (set) Token: 0x060011E2 RID: 4578 RVA: 0x00052250 File Offset: 0x00050450
		public WorkshopModel WorkshopModel { get; private set; }

		// Token: 0x170004B8 RID: 1208
		// (get) Token: 0x060011E3 RID: 4579 RVA: 0x00052259 File Offset: 0x00050459
		// (set) Token: 0x060011E4 RID: 4580 RVA: 0x00052261 File Offset: 0x00050461
		public DifficultyModel DifficultyModel { get; private set; }

		// Token: 0x170004B9 RID: 1209
		// (get) Token: 0x060011E5 RID: 4581 RVA: 0x0005226A File Offset: 0x0005046A
		// (set) Token: 0x060011E6 RID: 4582 RVA: 0x00052272 File Offset: 0x00050472
		public LocationModel LocationModel { get; private set; }

		// Token: 0x170004BA RID: 1210
		// (get) Token: 0x060011E7 RID: 4583 RVA: 0x0005227B File Offset: 0x0005047B
		// (set) Token: 0x060011E8 RID: 4584 RVA: 0x00052283 File Offset: 0x00050483
		public PrisonBreakModel PrisonBreakModel { get; private set; }

		// Token: 0x170004BB RID: 1211
		// (get) Token: 0x060011E9 RID: 4585 RVA: 0x0005228C File Offset: 0x0005048C
		// (set) Token: 0x060011EA RID: 4586 RVA: 0x00052294 File Offset: 0x00050494
		public BattleCaptainModel BattleCaptainModel { get; private set; }

		// Token: 0x170004BC RID: 1212
		// (get) Token: 0x060011EB RID: 4587 RVA: 0x0005229D File Offset: 0x0005049D
		// (set) Token: 0x060011EC RID: 4588 RVA: 0x000522A5 File Offset: 0x000504A5
		public ExecutionRelationModel ExecutionRelationModel { get; private set; }

		// Token: 0x170004BD RID: 1213
		// (get) Token: 0x060011ED RID: 4589 RVA: 0x000522AE File Offset: 0x000504AE
		// (set) Token: 0x060011EE RID: 4590 RVA: 0x000522B6 File Offset: 0x000504B6
		public BannerItemModel BannerItemModel { get; private set; }

		// Token: 0x170004BE RID: 1214
		// (get) Token: 0x060011EF RID: 4591 RVA: 0x000522BF File Offset: 0x000504BF
		// (set) Token: 0x060011F0 RID: 4592 RVA: 0x000522C7 File Offset: 0x000504C7
		public DelayedTeleportationModel DelayedTeleportationModel { get; private set; }

		// Token: 0x170004BF RID: 1215
		// (get) Token: 0x060011F1 RID: 4593 RVA: 0x000522D0 File Offset: 0x000504D0
		// (set) Token: 0x060011F2 RID: 4594 RVA: 0x000522D8 File Offset: 0x000504D8
		public TroopSupplierProbabilityModel TroopSupplierProbabilityModel { get; private set; }

		// Token: 0x170004C0 RID: 1216
		// (get) Token: 0x060011F3 RID: 4595 RVA: 0x000522E1 File Offset: 0x000504E1
		// (set) Token: 0x060011F4 RID: 4596 RVA: 0x000522E9 File Offset: 0x000504E9
		public CutsceneSelectionModel CutsceneSelectionModel { get; private set; }

		// Token: 0x170004C1 RID: 1217
		// (get) Token: 0x060011F5 RID: 4597 RVA: 0x000522F2 File Offset: 0x000504F2
		// (set) Token: 0x060011F6 RID: 4598 RVA: 0x000522FA File Offset: 0x000504FA
		public EquipmentSelectionModel EquipmentSelectionModel { get; private set; }

		// Token: 0x170004C2 RID: 1218
		// (get) Token: 0x060011F7 RID: 4599 RVA: 0x00052303 File Offset: 0x00050503
		// (set) Token: 0x060011F8 RID: 4600 RVA: 0x0005230B File Offset: 0x0005050B
		public AlleyModel AlleyModel { get; private set; }

		// Token: 0x170004C3 RID: 1219
		// (get) Token: 0x060011F9 RID: 4601 RVA: 0x00052314 File Offset: 0x00050514
		// (set) Token: 0x060011FA RID: 4602 RVA: 0x0005231C File Offset: 0x0005051C
		public VoiceOverModel VoiceOverModel { get; private set; }

		// Token: 0x170004C4 RID: 1220
		// (get) Token: 0x060011FB RID: 4603 RVA: 0x00052325 File Offset: 0x00050525
		// (set) Token: 0x060011FC RID: 4604 RVA: 0x0005232D File Offset: 0x0005052D
		public CampaignTimeModel CampaignTimeModel { get; private set; }

		// Token: 0x170004C5 RID: 1221
		// (get) Token: 0x060011FD RID: 4605 RVA: 0x00052336 File Offset: 0x00050536
		// (set) Token: 0x060011FE RID: 4606 RVA: 0x0005233E File Offset: 0x0005053E
		public VillageTradeModel VillageTradeModel { get; private set; }

		// Token: 0x170004C6 RID: 1222
		// (get) Token: 0x060011FF RID: 4607 RVA: 0x00052347 File Offset: 0x00050547
		// (set) Token: 0x06001200 RID: 4608 RVA: 0x0005234F File Offset: 0x0005054F
		public HeroCreationModel HeroCreationModel { get; private set; }

		// Token: 0x170004C7 RID: 1223
		// (get) Token: 0x06001201 RID: 4609 RVA: 0x00052358 File Offset: 0x00050558
		// (set) Token: 0x06001202 RID: 4610 RVA: 0x00052360 File Offset: 0x00050560
		public CampaignShipDamageModel CampaignShipDamageModel { get; private set; }

		// Token: 0x170004C8 RID: 1224
		// (get) Token: 0x06001203 RID: 4611 RVA: 0x00052369 File Offset: 0x00050569
		// (set) Token: 0x06001204 RID: 4612 RVA: 0x00052371 File Offset: 0x00050571
		public CampaignShipParametersModel CampaignShipParametersModel { get; private set; }

		// Token: 0x170004C9 RID: 1225
		// (get) Token: 0x06001205 RID: 4613 RVA: 0x0005237A File Offset: 0x0005057A
		// (set) Token: 0x06001206 RID: 4614 RVA: 0x00052382 File Offset: 0x00050582
		public BuildingModel BuildingModel { get; private set; }

		// Token: 0x170004CA RID: 1226
		// (get) Token: 0x06001207 RID: 4615 RVA: 0x0005238B File Offset: 0x0005058B
		// (set) Token: 0x06001208 RID: 4616 RVA: 0x00052393 File Offset: 0x00050593
		public ShipCostModel ShipCostModel { get; private set; }

		// Token: 0x170004CB RID: 1227
		// (get) Token: 0x06001209 RID: 4617 RVA: 0x0005239C File Offset: 0x0005059C
		// (set) Token: 0x0600120A RID: 4618 RVA: 0x000523A4 File Offset: 0x000505A4
		public ShipStatModel ShipStatModel { get; private set; }

		// Token: 0x170004CC RID: 1228
		// (get) Token: 0x0600120B RID: 4619 RVA: 0x000523AD File Offset: 0x000505AD
		// (set) Token: 0x0600120C RID: 4620 RVA: 0x000523B5 File Offset: 0x000505B5
		public SceneModel SceneModel { get; private set; }

		// Token: 0x170004CD RID: 1229
		// (get) Token: 0x0600120D RID: 4621 RVA: 0x000523BE File Offset: 0x000505BE
		// (set) Token: 0x0600120E RID: 4622 RVA: 0x000523C6 File Offset: 0x000505C6
		public BodyPropertiesModel BodyPropertiesModel { get; private set; }

		// Token: 0x170004CE RID: 1230
		// (get) Token: 0x0600120F RID: 4623 RVA: 0x000523CF File Offset: 0x000505CF
		// (set) Token: 0x06001210 RID: 4624 RVA: 0x000523D7 File Offset: 0x000505D7
		public IncidentModel IncidentModel { get; private set; }

		// Token: 0x170004CF RID: 1231
		// (get) Token: 0x06001211 RID: 4625 RVA: 0x000523E0 File Offset: 0x000505E0
		// (set) Token: 0x06001212 RID: 4626 RVA: 0x000523E8 File Offset: 0x000505E8
		public FleetManagementModel FleetManagementModel { get; private set; }

		// Token: 0x06001213 RID: 4627 RVA: 0x000523F4 File Offset: 0x000505F4
		private void GetSpecificGameBehaviors()
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign || Campaign.Current.GameMode == CampaignGameMode.Tutorial)
			{
				this.CharacterDevelopmentModel = base.GetGameModel<CharacterDevelopmentModel>();
				this.CharacterStatsModel = base.GetGameModel<CharacterStatsModel>();
				this.EncounterModel = base.GetGameModel<EncounterModel>();
				this.SettlementPatrolModel = base.GetGameModel<SettlementPatrolModel>();
				this.ItemDiscardModel = base.GetGameModel<ItemDiscardModel>();
				this.ValuationModel = base.GetGameModel<ValuationModel>();
				this.MapVisibilityModel = base.GetGameModel<MapVisibilityModel>();
				this.InformationRestrictionModel = base.GetGameModel<InformationRestrictionModel>();
				this.PartySpeedCalculatingModel = base.GetGameModel<PartySpeedModel>();
				this.PartyHealingModel = base.GetGameModel<PartyHealingModel>();
				this.CaravanModel = base.GetGameModel<CaravanModel>();
				this.PartyTrainingModel = base.GetGameModel<PartyTrainingModel>();
				this.PartyTradeModel = base.GetGameModel<PartyTradeModel>();
				this.RansomValueCalculationModel = base.GetGameModel<RansomValueCalculationModel>();
				this.RaidModel = base.GetGameModel<RaidModel>();
				this.CombatSimulationModel = base.GetGameModel<CombatSimulationModel>();
				this.CombatXpModel = base.GetGameModel<CombatXpModel>();
				this.GenericXpModel = base.GetGameModel<GenericXpModel>();
				this.TradeAgreementModel = base.GetGameModel<TradeAgreementModel>();
				this.SmithingModel = base.GetGameModel<SmithingModel>();
				this.MobilePartyFoodConsumptionModel = base.GetGameModel<MobilePartyFoodConsumptionModel>();
				this.PartyImpairmentModel = base.GetGameModel<PartyImpairmentModel>();
				this.PartyFoodBuyingModel = base.GetGameModel<PartyFoodBuyingModel>();
				this.PartyMoraleModel = base.GetGameModel<PartyMoraleModel>();
				this.PartyDesertionModel = base.GetGameModel<PartyDesertionModel>();
				this.HideoutModel = base.GetGameModel<HideoutModel>();
				this.DiplomacyModel = base.GetGameModel<DiplomacyModel>();
				this.AllianceModel = base.GetGameModel<AllianceModel>();
				this.PartyTransitionModel = base.GetGameModel<PartyTransitionModel>();
				this.MinorFactionsModel = base.GetGameModel<MinorFactionsModel>();
				this.KingdomCreationModel = base.GetGameModel<KingdomCreationModel>();
				this.EmissaryModel = base.GetGameModel<EmissaryModel>();
				this.KingdomDecisionPermissionModel = base.GetGameModel<KingdomDecisionPermissionModel>();
				this.VillageProductionCalculatorModel = base.GetGameModel<VillageProductionCalculatorModel>();
				this.RomanceModel = base.GetGameModel<RomanceModel>();
				this.VolunteerModel = base.GetGameModel<VolunteerModel>();
				this.ArmyManagementCalculationModel = base.GetGameModel<ArmyManagementCalculationModel>();
				this.BanditDensityModel = base.GetGameModel<BanditDensityModel>();
				this.EncounterGameMenuModel = base.GetGameModel<EncounterGameMenuModel>();
				this.BattleRewardModel = base.GetGameModel<BattleRewardModel>();
				this.MapTrackModel = base.GetGameModel<MapTrackModel>();
				this.MapDistanceModel = base.GetGameModel<MapDistanceModel>();
				this.PartyNavigationModel = base.GetGameModel<PartyNavigationModel>();
				this.MapWeatherModel = base.GetGameModel<MapWeatherModel>();
				this.TargetScoreCalculatingModel = base.GetGameModel<TargetScoreCalculatingModel>();
				this.PartySizeLimitModel = base.GetGameModel<PartySizeLimitModel>();
				this.PartyShipLimitModel = base.GetGameModel<PartyShipLimitModel>();
				this.PartyWageModel = base.GetGameModel<PartyWageModel>();
				this.PlayerProgressionModel = base.GetGameModel<PlayerProgressionModel>();
				this.InventoryCapacityModel = base.GetGameModel<InventoryCapacityModel>();
				this.TradeItemPriceFactorModel = base.GetGameModel<TradeItemPriceFactorModel>();
				this.SettlementValueModel = base.GetGameModel<SettlementValueModel>();
				this.SettlementEconomyModel = base.GetGameModel<SettlementEconomyModel>();
				this.SettlementMilitiaModel = base.GetGameModel<SettlementMilitiaModel>();
				this.SettlementFoodModel = base.GetGameModel<SettlementFoodModel>();
				this.SettlementLoyaltyModel = base.GetGameModel<SettlementLoyaltyModel>();
				this.SettlementSecurityModel = base.GetGameModel<SettlementSecurityModel>();
				this.SettlementProsperityModel = base.GetGameModel<SettlementProsperityModel>();
				this.SettlementGarrisonModel = base.GetGameModel<SettlementGarrisonModel>();
				this.SettlementTaxModel = base.GetGameModel<SettlementTaxModel>();
				this.HeroAgentLocationModel = base.GetGameModel<HeroAgentLocationModel>();
				this.BarterModel = base.GetGameModel<BarterModel>();
				this.PersuasionModel = base.GetGameModel<PersuasionModel>();
				this.DefectionModel = base.GetGameModel<DefectionModel>();
				this.ClanTierModel = base.GetGameModel<ClanTierModel>();
				this.VassalRewardsModel = base.GetGameModel<VassalRewardsModel>();
				this.ClanPoliticsModel = base.GetGameModel<ClanPoliticsModel>();
				this.ClanFinanceModel = base.GetGameModel<ClanFinanceModel>();
				this.HeirSelectionCalculationModel = base.GetGameModel<HeirSelectionCalculationModel>();
				this.HeroDeathProbabilityCalculationModel = base.GetGameModel<HeroDeathProbabilityCalculationModel>();
				this.BuildingConstructionModel = base.GetGameModel<BuildingConstructionModel>();
				this.BuildingEffectModel = base.GetGameModel<BuildingEffectModel>();
				this.WallHitPointCalculationModel = base.GetGameModel<WallHitPointCalculationModel>();
				this.MarriageModel = base.GetGameModel<MarriageModel>();
				this.AgeModel = base.GetGameModel<AgeModel>();
				this.DailyTroopXpBonusModel = base.GetGameModel<DailyTroopXpBonusModel>();
				this.PregnancyModel = base.GetGameModel<PregnancyModel>();
				this.NotablePowerModel = base.GetGameModel<NotablePowerModel>();
				this.NotableSpawnModel = base.GetGameModel<NotableSpawnModel>();
				this.TournamentModel = base.GetGameModel<TournamentModel>();
				this.SiegeStrategyActionModel = base.GetGameModel<SiegeStrategyActionModel>();
				this.SiegeEventModel = base.GetGameModel<SiegeEventModel>();
				this.SiegeAftermathModel = base.GetGameModel<SiegeAftermathModel>();
				this.SiegeLordsHallFightModel = base.GetGameModel<SiegeLordsHallFightModel>();
				this.CrimeModel = base.GetGameModel<CrimeModel>();
				this.DisguiseDetectionModel = base.GetGameModel<DisguiseDetectionModel>();
				this.BribeCalculationModel = base.GetGameModel<BribeCalculationModel>();
				this.CompanionHiringPriceCalculationModel = base.GetGameModel<CompanionHiringPriceCalculationModel>();
				this.TroopSacrificeModel = base.GetGameModel<TroopSacrificeModel>();
				this.BuildingScoreCalculationModel = base.GetGameModel<BuildingScoreCalculationModel>();
				this.SettlementAccessModel = base.GetGameModel<SettlementAccessModel>();
				this.IssueModel = base.GetGameModel<IssueModel>();
				this.PrisonerRecruitmentCalculationModel = base.GetGameModel<PrisonerRecruitmentCalculationModel>();
				this.PartyTroopUpgradeModel = base.GetGameModel<PartyTroopUpgradeModel>();
				this.TavernMercenaryTroopsModel = base.GetGameModel<TavernMercenaryTroopsModel>();
				this.WorkshopModel = base.GetGameModel<WorkshopModel>();
				this.DifficultyModel = base.GetGameModel<DifficultyModel>();
				this.LocationModel = base.GetGameModel<LocationModel>();
				this.MilitaryPowerModel = base.GetGameModel<MilitaryPowerModel>();
				this.PrisonerDonationModel = base.GetGameModel<PrisonerDonationModel>();
				this.PrisonBreakModel = base.GetGameModel<PrisonBreakModel>();
				this.BattleCaptainModel = base.GetGameModel<BattleCaptainModel>();
				this.ExecutionRelationModel = base.GetGameModel<ExecutionRelationModel>();
				this.BannerItemModel = base.GetGameModel<BannerItemModel>();
				this.DelayedTeleportationModel = base.GetGameModel<DelayedTeleportationModel>();
				this.TroopSupplierProbabilityModel = base.GetGameModel<TroopSupplierProbabilityModel>();
				this.CutsceneSelectionModel = base.GetGameModel<CutsceneSelectionModel>();
				this.EquipmentSelectionModel = base.GetGameModel<EquipmentSelectionModel>();
				this.AlleyModel = base.GetGameModel<AlleyModel>();
				this.VoiceOverModel = base.GetGameModel<VoiceOverModel>();
				this.CampaignTimeModel = base.GetGameModel<CampaignTimeModel>();
				this.VillageTradeModel = base.GetGameModel<VillageTradeModel>();
				this.PartyNavigationModel = base.GetGameModel<PartyNavigationModel>();
				this.MobilePartyAIModel = base.GetGameModel<MobilePartyAIModel>();
				this.HeroCreationModel = base.GetGameModel<HeroCreationModel>();
				this.CampaignShipDamageModel = base.GetGameModel<CampaignShipDamageModel>();
				this.CampaignShipParametersModel = base.GetGameModel<CampaignShipParametersModel>();
				this.BuildingModel = base.GetGameModel<BuildingModel>();
				this.ShipCostModel = base.GetGameModel<ShipCostModel>();
				this.SceneModel = base.GetGameModel<SceneModel>();
				this.IncidentModel = base.GetGameModel<IncidentModel>();
				this.BodyPropertiesModel = base.GetGameModel<BodyPropertiesModel>();
				this.FleetManagementModel = base.GetGameModel<FleetManagementModel>();
				this.ShipStatModel = base.GetGameModel<ShipStatModel>();
			}
		}

		// Token: 0x06001214 RID: 4628 RVA: 0x000529EE File Offset: 0x00050BEE
		public GameModels(IEnumerable<GameModel> inputComponents)
			: base(inputComponents)
		{
			this.GetSpecificGameBehaviors();
		}
	}
}

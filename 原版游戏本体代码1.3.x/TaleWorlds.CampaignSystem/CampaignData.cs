using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000036 RID: 54
	public static class CampaignData
	{
		// Token: 0x170000AF RID: 175
		// (get) Token: 0x060003CC RID: 972 RVA: 0x0001E0C6 File Offset: 0x0001C2C6
		public static TextObject NeutralFactionName
		{
			get
			{
				return new TextObject("{=JQNKrAI3}Neutral Faction", null);
			}
		}

		// Token: 0x040000DC RID: 220
		public const string MainStorylineSpecialQuestType = "MainStoryline";

		// Token: 0x040000DD RID: 221
		public const string MainHeroTag = "main_hero";

		// Token: 0x040000DE RID: 222
		public const string PlayerTag = "spawnpoint_player";

		// Token: 0x040000DF RID: 223
		public const string PlayerConversationTag = "sp_player_conversation";

		// Token: 0x040000E0 RID: 224
		public const string PlayerOutsideTag = "spawnpoint_player_outside";

		// Token: 0x040000E1 RID: 225
		public const string PlayerNearTownMainGate = "sp_outside_near_town_main_gate";

		// Token: 0x040000E2 RID: 226
		public const string PlayerPrisonBreakTag = "sp_prison_break";

		// Token: 0x040000E3 RID: 227
		public const string TournamentMasterTag = "spawnpoint_tournamentmaster";

		// Token: 0x040000E4 RID: 228
		public const string PlayerNearArenaMasterTag = "sp_player_near_arena_master";

		// Token: 0x040000E5 RID: 229
		public const string ArcherySetTag = "tournament_archery";

		// Token: 0x040000E6 RID: 230
		public const string FightSetTag = "tournament_fight";

		// Token: 0x040000E7 RID: 231
		public const string JoustingSetTag = "tournament_jousting";

		// Token: 0x040000E8 RID: 232
		public const string TavernWenchTag = "sp_tavern_wench";

		// Token: 0x040000E9 RID: 233
		public const string TavernKeeperTag = "spawnpoint_tavernkeeper";

		// Token: 0x040000EA RID: 234
		public const string MercenaryTag = "spawnpoint_mercenary";

		// Token: 0x040000EB RID: 235
		public const string MusicianTag = "musician";

		// Token: 0x040000EC RID: 236
		public const string DesertWarHorseSpawnpointTag = "desert_war_horse";

		// Token: 0x040000ED RID: 237
		public const string SteppeChargerSpawnpointTag = "steppe_charger";

		// Token: 0x040000EE RID: 238
		public const string WarHorseSpawnpointTag = "war_horse";

		// Token: 0x040000EF RID: 239
		public const string HorseChargerSpawnpointTag = "charger";

		// Token: 0x040000F0 RID: 240
		public const string DesertHorseSpawnpointTag = "desert_horse";

		// Token: 0x040000F1 RID: 241
		public const string HunterHorseSpawnpointTag = "hunter";

		// Token: 0x040000F2 RID: 242
		public const string SheepSpawnpointTag = "sp_sheep";

		// Token: 0x040000F3 RID: 243
		public const string CowSpawnpointTag = "sp_cow";

		// Token: 0x040000F4 RID: 244
		public const string HogSpawnpointTag = "sp_hog";

		// Token: 0x040000F5 RID: 245
		public const string GooseSpawnpointTag = "sp_goose";

		// Token: 0x040000F6 RID: 246
		public const string ChickenSpawnpointTag = "sp_chicken";

		// Token: 0x040000F7 RID: 247
		public const string HorseSpawnpointTag = "sp_horse";

		// Token: 0x040000F8 RID: 248
		public const string SpawnpointNPCTag = "sp_npc";

		// Token: 0x040000F9 RID: 249
		public const string GuardTag = "sp_guard";

		// Token: 0x040000FA RID: 250
		public const string GuardWithSpearTag = "sp_guard_with_spear";

		// Token: 0x040000FB RID: 251
		public const string GuardPatrolTag = "sp_guard_patrol";

		// Token: 0x040000FC RID: 252
		public const string PrisonGuard = "sp_prison_guard";

		// Token: 0x040000FD RID: 253
		public const string GuardLordsHallGateTag = "sp_guard_castle";

		// Token: 0x040000FE RID: 254
		public const string UnarmedGuardTag = "sp_guard_unarmed";

		// Token: 0x040000FF RID: 255
		public const string TraderTag = "sp_merchant";

		// Token: 0x04000100 RID: 256
		public const string HorseTraderTag = "sp_horse_merchant";

		// Token: 0x04000101 RID: 257
		public const string ArmorerTag = "sp_armorer";

		// Token: 0x04000102 RID: 258
		public const string BarberTag = "sp_barber";

		// Token: 0x04000103 RID: 259
		public const string WeaponSmithTag = "sp_weaponsmith";

		// Token: 0x04000104 RID: 260
		public const string BlacksmithTag = "sp_blacksmith";

		// Token: 0x04000105 RID: 261
		public const string DancerTag = "npc_dancer";

		// Token: 0x04000106 RID: 262
		public const string BeggarTag = "npc_beggar";

		// Token: 0x04000107 RID: 263
		public const string GovernorTag = "sp_governor";

		// Token: 0x04000108 RID: 264
		public const string CleanerTag = "spawnpoint_cleaner";

		// Token: 0x04000109 RID: 265
		public const string WorkshopSellerTag = "sp_shop_seller";

		// Token: 0x0400010A RID: 266
		public const string NpcCommonTag = "npc_common";

		// Token: 0x0400010B RID: 267
		public const string NpcCommonLimitedTag = "npc_common_limited";

		// Token: 0x0400010C RID: 268
		public const string ElderTag = "spawnpoint_elder";

		// Token: 0x0400010D RID: 269
		public const string IdleTag = "npc_idle";

		// Token: 0x0400010E RID: 270
		public const string GamblerTag = "gambler_npc";

		// Token: 0x0400010F RID: 271
		public const string ReservedTag = "reserved";

		// Token: 0x04000110 RID: 272
		public const string HiddenSpawnPointTag = "sp_common_hidden";

		// Token: 0x04000111 RID: 273
		public const string PassageTag = "npc_passage";

		// Token: 0x04000112 RID: 274
		public const string DisableAtNightTag = "disable_at_night";

		// Token: 0x04000113 RID: 275
		public const string EnableAtNightTag = "enable_at_night";

		// Token: 0x04000114 RID: 276
		public const string KingTag = "sp_king";

		// Token: 0x04000115 RID: 277
		public const string ThroneTag = "sp_throne";

		// Token: 0x04000116 RID: 278
		public const string SpawnPointCleanerTag = "spawnpoint_cleaner";

		// Token: 0x04000117 RID: 279
		public const string NavigationMeshDeactivatorTag = "navigation_mesh_deactivator";

		// Token: 0x04000118 RID: 280
		public const string BattleSetTag = "battle_set";

		// Token: 0x04000119 RID: 281
		public const string AlleyMarker = "alley_marker";

		// Token: 0x0400011A RID: 282
		public const string Alley1Tag = "alley_1";

		// Token: 0x0400011B RID: 283
		public const string Alley2Tag = "alley_2";

		// Token: 0x0400011C RID: 284
		public const string Alley3Tag = "alley_3";

		// Token: 0x0400011D RID: 285
		public const string WorkshopAreaMarkerTag = "workshop_area_marker";

		// Token: 0x0400011E RID: 286
		public const string WorkshopSignTag = "shop_sign";

		// Token: 0x0400011F RID: 287
		public const string PrisonerTag = "sp_prisoner";

		// Token: 0x04000120 RID: 288
		public const string PrisonerGuardTag = "sp_prison_guard";

		// Token: 0x04000121 RID: 289
		public const string NotableTag = "sp_notable";

		// Token: 0x04000122 RID: 290
		public const string NotableGangLeaderTag = "sp_notable_gangleader";

		// Token: 0x04000123 RID: 291
		public const string NotableRuralNotableTag = "sp_notable_rural_notable";

		// Token: 0x04000124 RID: 292
		public const string NotablePreacherTag = "sp_notable_preacher";

		// Token: 0x04000125 RID: 293
		public const string NotableArtisanTag = "sp_notable_artisan";

		// Token: 0x04000126 RID: 294
		public const string NotableMerchantTag = "sp_notable_merchant";

		// Token: 0x04000127 RID: 295
		public const string GangLeaderBodyGuard = "sp_gangleader_bodyguard";

		// Token: 0x04000128 RID: 296
		public const string MerchantNotary = "sp_merchant_notary";

		// Token: 0x04000129 RID: 297
		public const string ArtisanNotary = "sp_artisan_notary";

		// Token: 0x0400012A RID: 298
		public const string PreacherNotary = "sp_preacher_notary";

		// Token: 0x0400012B RID: 299
		public const string RuralNotableNotary = "sp_rural_notable_notary";

		// Token: 0x0400012C RID: 300
		public const string PrisonBreakPrisonerTag = "sp_prison_break_prisoner";

		// Token: 0x0400012D RID: 301
		public const string HermitTag = "sp_hermit";

		// Token: 0x0400012E RID: 302
		public const string SpShipWright = "sp_shipwright";

		// Token: 0x0400012F RID: 303
		public const string CarpenterTag = "merchant_carpenter";

		// Token: 0x04000130 RID: 304
		public const string ShipyardWorkerTag = "shipyard_worker";

		// Token: 0x04000131 RID: 305
		public const string MarketWorkerTag = "market_worker";

		// Token: 0x04000132 RID: 306
		public const string StaticNpcTag = "static_npc";

		// Token: 0x04000133 RID: 307
		public const string StealthCharacterStringId = "stealth_character";

		// Token: 0x04000134 RID: 308
		public const string DisguiseDefaultCharacterStringId = "disguise_default_character";

		// Token: 0x04000135 RID: 309
		public const string DisguiseDefaultContractorCharacterStringId = "disguise_contractor_character";

		// Token: 0x04000136 RID: 310
		public const string DisguiseOfficerCharacterStringId = "disguise_officer_character";

		// Token: 0x04000137 RID: 311
		public const string DisguiseShadowTargetCharacterStringId = "disguise_shadow_target";

		// Token: 0x04000138 RID: 312
		public const string StealthCharacterSpawnTag = "stealth_agent";

		// Token: 0x04000139 RID: 313
		public const string ForcedStealthCharacterSpawnTag = "stealth_agent_forced";

		// Token: 0x0400013A RID: 314
		public const string DisguiseDefaultCharacterSpawnTag = "disguise_default_agent";

		// Token: 0x0400013B RID: 315
		public const string DisguiseOfficerCharacterSpawnTag = "disguise_officer_agent";

		// Token: 0x0400013C RID: 316
		public const string DisguiseShadowTargetCharacterSpawnTag = "disguise_shadow_agent";

		// Token: 0x0400013D RID: 317
		public const string PrisonBreakStealthReinforcementSpawnTag = "prison_break_reinforcement_point";

		// Token: 0x0400013E RID: 318
		public const string Level1Tag = "level_1";

		// Token: 0x0400013F RID: 319
		public const string Level2Tag = "level_2";

		// Token: 0x04000140 RID: 320
		public const string Level3Tag = "level_3";

		// Token: 0x04000141 RID: 321
		public const string CivilianTag = "civilian";

		// Token: 0x04000142 RID: 322
		public const string PrisonBreakLevelTag = "prison_break";

		// Token: 0x04000143 RID: 323
		public const string SiegeTag = "siege";

		// Token: 0x04000144 RID: 324
		public const string RaidTag = "raid";

		// Token: 0x04000145 RID: 325
		public const string BurnedTag = "burned";

		// Token: 0x04000146 RID: 326
		public const string PlayerStealthTag = "sp_player_stealth";

		// Token: 0x04000147 RID: 327
		public const string Shop1Tag = "shop_1";

		// Token: 0x04000148 RID: 328
		public const string Shop2Tag = "shop_2";

		// Token: 0x04000149 RID: 329
		public const string Shop3Tag = "shop_3";

		// Token: 0x0400014A RID: 330
		public const string Shop4Tag = "shop_4";

		// Token: 0x0400014B RID: 331
		public const string CultureEmpire = "empire";

		// Token: 0x0400014C RID: 332
		public const string CultureSturgia = "sturgia";

		// Token: 0x0400014D RID: 333
		public const string CultureAserai = "aserai";

		// Token: 0x0400014E RID: 334
		public const string CultureVlandia = "vlandia";

		// Token: 0x0400014F RID: 335
		public const string CultureBattania = "battania";

		// Token: 0x04000150 RID: 336
		public const string CultureKhuzait = "khuzait";

		// Token: 0x04000151 RID: 337
		public const string CultureNord = "nord";

		// Token: 0x04000152 RID: 338
		public const string CultureDarshi = "darshi";

		// Token: 0x04000153 RID: 339
		public const string CultureVakken = "vakken";

		// Token: 0x04000154 RID: 340
		public const string CultureNeutral = "neutral_culture";

		// Token: 0x04000155 RID: 341
		public const string CultureForestHideout = "forest_bandits";

		// Token: 0x04000156 RID: 342
		public const string CultureSeaHideout = "sea_raiders";

		// Token: 0x04000157 RID: 343
		public const string CultureMountainHideout = "mountain_bandits";

		// Token: 0x04000158 RID: 344
		public const string CultureDesertHideout = "desert_bandits";

		// Token: 0x04000159 RID: 345
		public const string CultureSteppeHideout = "steppe_bandits";

		// Token: 0x0400015A RID: 346
		public const string Deserters = "deserters";

		// Token: 0x0400015B RID: 347
		public const string Looters = "looters";

		// Token: 0x0400015C RID: 348
		public const string LocationCenter = "center";

		// Token: 0x0400015D RID: 349
		public const string LocationArena = "arena";

		// Token: 0x0400015E RID: 350
		public const string LocationPrison = "prison";

		// Token: 0x0400015F RID: 351
		public const string LocationLordsHall = "lordshall";

		// Token: 0x04000160 RID: 352
		public const string LocationTavern = "tavern";

		// Token: 0x04000161 RID: 353
		public const string LocationVillageCenter = "village_center";

		// Token: 0x04000162 RID: 354
		public const string LocationHouse1 = "house_1";

		// Token: 0x04000163 RID: 355
		public const string LocationHouse2 = "house_2";

		// Token: 0x04000164 RID: 356
		public const string LocationHouse3 = "house_3";

		// Token: 0x04000165 RID: 357
		public const string LocationAlley = "alley";

		// Token: 0x04000166 RID: 358
		public const string RetreatSettlement = "retirement_retreat";

		// Token: 0x04000167 RID: 359
		public const string LocationHideout = "hideout_center";

		// Token: 0x04000168 RID: 360
		public const string LocationPort = "port";

		// Token: 0x04000169 RID: 361
		public const string LameHorseModifier = "lame_horse";

		// Token: 0x0400016A RID: 362
		public const int MinFactionNameLength = 1;

		// Token: 0x0400016B RID: 363
		public const int MaxFactionNameLength = 50;

		// Token: 0x0400016C RID: 364
		public const uint NeutralColor1 = 4291609515U;

		// Token: 0x0400016D RID: 365
		public const uint NeutralColor2 = 4291609515U;

		// Token: 0x0400016E RID: 366
		public const uint NeutralAlternativeColor1 = 4291609515U;

		// Token: 0x0400016F RID: 367
		public const uint NeutralAlternativeColor2 = 4291609515U;

		// Token: 0x04000170 RID: 368
		public const uint StealthColor1 = 4281281067U;

		// Token: 0x04000171 RID: 369
		public const uint StealthColor2 = 4281281067U;

		// Token: 0x04000172 RID: 370
		public static readonly uint[] EmpireHeroClothColors = new uint[]
		{
			4291528279U, 4290276438U, 4290269521U, 4284852366U, 4286228910U, 4283542194U, 4286151096U, 4284963729U, 4288833640U, 4288572253U,
			4287586439U, 4285745029U, 4286071139U
		};

		// Token: 0x04000173 RID: 371
		public static readonly uint[] SturgiaHeroClothColors = new uint[]
		{
			4290289001U, 4288652894U, 4287868002U, 4285442652U, 4287080046U, 4284842410U, 4285047948U, 4283010986U, 4282619302U, 4285512133U,
			4290148171U, 4290086464U, 4291270999U
		};

		// Token: 0x04000174 RID: 372
		public static readonly uint[] AseraiHeroClothColors = new uint[]
		{
			4290276438U, 4290148171U, 4291528279U, 4282880383U, 4289758794U, 4281434977U, 4290879574U, 4292654718U, 4291735684U, 4290285174U,
			4290475610U, 4288438863U, 4290472789U, 4283397725U, 4283144585U
		};

		// Token: 0x04000175 RID: 373
		public static readonly uint[] VlandiaHeroClothColors = new uint[]
		{
			4289761132U, 4289958756U, 4290610530U, 4290085254U, 4288438646U, 4288115639U, 4284835242U, 4285571727U, 4288569965U, 4286230451U,
			4290080353U, 4288911473U, 4287930717U, 4284918381U
		};

		// Token: 0x04000176 RID: 374
		public static readonly uint[] BattaniaHeroClothColors = new uint[]
		{
			4285839483U, 4287282028U, 4288129365U, 4290221889U, 4289887020U, 4288977744U, 4289499237U, 4290220376U, 4291855694U, 4288832825U,
			4281894195U, 4286027611U, 4285364809U
		};

		// Token: 0x04000177 RID: 375
		public static readonly uint[] KhuzaitHeroClothColors = new uint[]
		{
			4286362809U, 4285633960U, 4285642139U, 4289629300U, 4290874464U, 4289626441U, 4289100112U, 4290356599U, 4291141740U, 4291539052U,
			4290227326U, 4291016062U
		};

		// Token: 0x04000178 RID: 376
		public const char EventParameterSplitCharacter = ' ';

		// Token: 0x04000179 RID: 377
		public const string BattleEquipmentUpdateTag = "battle";

		// Token: 0x0400017A RID: 378
		public const string CivilianEquipmentUpdateTag = "civilian";

		// Token: 0x0400017B RID: 379
		public const string StealthEquipmentUpdateTag = "stealth";

		// Token: 0x0400017C RID: 380
		public const string NoEquipmentUpdateTag = "none";

		// Token: 0x0400017D RID: 381
		public const string NavalDLCStringId = "NavalDLC";

		// Token: 0x0400017E RID: 382
		public const string DisguiseShadowTargetCharacter = "disguise_shadow_target";

		// Token: 0x0400017F RID: 383
		public const string StealthCharacter = "stealth_character";
	}
}

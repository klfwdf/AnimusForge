using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Handlers;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200002F RID: 47
	public class Campaign : GameType
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060001F1 RID: 497 RVA: 0x00013931 File Offset: 0x00011B31
		// (set) Token: 0x060001F2 RID: 498 RVA: 0x00013938 File Offset: 0x00011B38
		public static float MapDiagonal { get; private set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060001F3 RID: 499 RVA: 0x00013940 File Offset: 0x00011B40
		// (set) Token: 0x060001F4 RID: 500 RVA: 0x00013947 File Offset: 0x00011B47
		public static float MapDiagonalSquared { get; private set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060001F5 RID: 501 RVA: 0x0001394F File Offset: 0x00011B4F
		// (set) Token: 0x060001F6 RID: 502 RVA: 0x00013956 File Offset: 0x00011B56
		public static Vec2 MapMinimumPosition { get; private set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060001F7 RID: 503 RVA: 0x0001395E File Offset: 0x00011B5E
		// (set) Token: 0x060001F8 RID: 504 RVA: 0x00013965 File Offset: 0x00011B65
		public static Vec2 MapMaximumPosition { get; private set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060001F9 RID: 505 RVA: 0x0001396D File Offset: 0x00011B6D
		// (set) Token: 0x060001FA RID: 506 RVA: 0x00013974 File Offset: 0x00011B74
		public static float MapMaximumHeight { get; private set; }

		// Token: 0x060001FB RID: 507 RVA: 0x0001397C File Offset: 0x00011B7C
		public float GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType navigationType)
		{
			return this._averageDistanceBetweenClosestTwoTowns[navigationType];
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060001FC RID: 508 RVA: 0x0001398A File Offset: 0x00011B8A
		// (set) Token: 0x060001FD RID: 509 RVA: 0x00013992 File Offset: 0x00011B92
		[CachedData]
		public float AverageWage { get; private set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060001FE RID: 510 RVA: 0x0001399B File Offset: 0x00011B9B
		public string NewGameVersion
		{
			get
			{
				return this._newGameVersion;
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060001FF RID: 511 RVA: 0x000139A3 File Offset: 0x00011BA3
		public MBReadOnlyList<string> PreviouslyUsedModules
		{
			get
			{
				return this._previouslyUsedModules;
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000200 RID: 512 RVA: 0x000139AB File Offset: 0x00011BAB
		public MBReadOnlyList<string> UsedGameVersions
		{
			get
			{
				return this._usedGameVersions;
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000201 RID: 513 RVA: 0x000139B3 File Offset: 0x00011BB3
		// (set) Token: 0x06000202 RID: 514 RVA: 0x000139BB File Offset: 0x00011BBB
		[SaveableProperty(83)]
		public bool EnabledCheatsBefore { get; set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000203 RID: 515 RVA: 0x000139C4 File Offset: 0x00011BC4
		// (set) Token: 0x06000204 RID: 516 RVA: 0x000139CC File Offset: 0x00011BCC
		[SaveableProperty(82)]
		public string PlatformID { get; private set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000205 RID: 517 RVA: 0x000139D5 File Offset: 0x00011BD5
		// (set) Token: 0x06000206 RID: 518 RVA: 0x000139DD File Offset: 0x00011BDD
		internal CampaignEventDispatcher CampaignEventDispatcher { get; private set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000207 RID: 519 RVA: 0x000139E6 File Offset: 0x00011BE6
		// (set) Token: 0x06000208 RID: 520 RVA: 0x000139EE File Offset: 0x00011BEE
		[SaveableProperty(80)]
		public string UniqueGameId { get; private set; }

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000209 RID: 521 RVA: 0x000139F7 File Offset: 0x00011BF7
		// (set) Token: 0x0600020A RID: 522 RVA: 0x000139FF File Offset: 0x00011BFF
		public SaveHandler SaveHandler { get; private set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600020B RID: 523 RVA: 0x00013A08 File Offset: 0x00011C08
		public override bool SupportsSaving
		{
			get
			{
				return this.GameMode == CampaignGameMode.Campaign;
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x0600020C RID: 524 RVA: 0x00013A13 File Offset: 0x00011C13
		// (set) Token: 0x0600020D RID: 525 RVA: 0x00013A1B File Offset: 0x00011C1B
		[SaveableProperty(211)]
		public CampaignObjectManager CampaignObjectManager { get; private set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600020E RID: 526 RVA: 0x00013A24 File Offset: 0x00011C24
		public override bool IsDevelopment
		{
			get
			{
				return this.GameMode == CampaignGameMode.Tutorial;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600020F RID: 527 RVA: 0x00013A2F File Offset: 0x00011C2F
		// (set) Token: 0x06000210 RID: 528 RVA: 0x00013A37 File Offset: 0x00011C37
		[SaveableProperty(3)]
		public bool IsCraftingEnabled { get; set; } = true;

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000211 RID: 529 RVA: 0x00013A40 File Offset: 0x00011C40
		// (set) Token: 0x06000212 RID: 530 RVA: 0x00013A48 File Offset: 0x00011C48
		[SaveableProperty(4)]
		public bool IsBannerEditorEnabled { get; set; } = true;

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000213 RID: 531 RVA: 0x00013A51 File Offset: 0x00011C51
		// (set) Token: 0x06000214 RID: 532 RVA: 0x00013A59 File Offset: 0x00011C59
		[SaveableProperty(5)]
		public bool IsFaceGenEnabled { get; set; } = true;

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000215 RID: 533 RVA: 0x00013A62 File Offset: 0x00011C62
		public ICampaignBehaviorManager CampaignBehaviorManager
		{
			get
			{
				return this._campaignBehaviorManager;
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000216 RID: 534 RVA: 0x00013A6A File Offset: 0x00011C6A
		// (set) Token: 0x06000217 RID: 535 RVA: 0x00013A72 File Offset: 0x00011C72
		[SaveableProperty(8)]
		public QuestManager QuestManager { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000218 RID: 536 RVA: 0x00013A7B File Offset: 0x00011C7B
		// (set) Token: 0x06000219 RID: 537 RVA: 0x00013A83 File Offset: 0x00011C83
		[SaveableProperty(9)]
		public IssueManager IssueManager { get; private set; }

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x0600021A RID: 538 RVA: 0x00013A8C File Offset: 0x00011C8C
		// (set) Token: 0x0600021B RID: 539 RVA: 0x00013A94 File Offset: 0x00011C94
		[SaveableProperty(11)]
		public FactionManager FactionManager { get; private set; }

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x0600021C RID: 540 RVA: 0x00013A9D File Offset: 0x00011C9D
		// (set) Token: 0x0600021D RID: 541 RVA: 0x00013AA5 File Offset: 0x00011CA5
		[SaveableProperty(12)]
		public CharacterRelationManager CharacterRelationManager { get; private set; }

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x0600021E RID: 542 RVA: 0x00013AAE File Offset: 0x00011CAE
		// (set) Token: 0x0600021F RID: 543 RVA: 0x00013AB6 File Offset: 0x00011CB6
		[SaveableProperty(14)]
		public Romance Romance { get; private set; }

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000220 RID: 544 RVA: 0x00013ABF File Offset: 0x00011CBF
		// (set) Token: 0x06000221 RID: 545 RVA: 0x00013AC7 File Offset: 0x00011CC7
		[SaveableProperty(16)]
		public PlayerCaptivity PlayerCaptivity { get; private set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000222 RID: 546 RVA: 0x00013AD0 File Offset: 0x00011CD0
		// (set) Token: 0x06000223 RID: 547 RVA: 0x00013AD8 File Offset: 0x00011CD8
		[SaveableProperty(17)]
		internal Clan PlayerDefaultFaction { get; set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000224 RID: 548 RVA: 0x00013AE1 File Offset: 0x00011CE1
		// (set) Token: 0x06000225 RID: 549 RVA: 0x00013AE9 File Offset: 0x00011CE9
		public CampaignMission.ICampaignMissionManager CampaignMissionManager { get; set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000226 RID: 550 RVA: 0x00013AF2 File Offset: 0x00011CF2
		// (set) Token: 0x06000227 RID: 551 RVA: 0x00013AFA File Offset: 0x00011CFA
		public ISkillLevelingManager SkillLevelingManager { get; set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x06000228 RID: 552 RVA: 0x00013B03 File Offset: 0x00011D03
		// (set) Token: 0x06000229 RID: 553 RVA: 0x00013B0B File Offset: 0x00011D0B
		public IMapSceneCreator MapSceneCreator { get; set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x0600022A RID: 554 RVA: 0x00013B14 File Offset: 0x00011D14
		public override bool IsInventoryAccessibleAtMission
		{
			get
			{
				return this.GameMode == CampaignGameMode.Tutorial;
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600022B RID: 555 RVA: 0x00013B1F File Offset: 0x00011D1F
		// (set) Token: 0x0600022C RID: 556 RVA: 0x00013B27 File Offset: 0x00011D27
		public GameMenuCallbackManager GameMenuCallbackManager { get; private set; }

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600022D RID: 557 RVA: 0x00013B30 File Offset: 0x00011D30
		// (set) Token: 0x0600022E RID: 558 RVA: 0x00013B38 File Offset: 0x00011D38
		public VisualCreator VisualCreator { get; set; }

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x0600022F RID: 559 RVA: 0x00013B41 File Offset: 0x00011D41
		// (set) Token: 0x06000230 RID: 560 RVA: 0x00013B49 File Offset: 0x00011D49
		[SaveableProperty(28)]
		public MapStateData MapStateData { get; private set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000231 RID: 561 RVA: 0x00013B52 File Offset: 0x00011D52
		// (set) Token: 0x06000232 RID: 562 RVA: 0x00013B5A File Offset: 0x00011D5A
		public DefaultPerks DefaultPerks { get; private set; }

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000233 RID: 563 RVA: 0x00013B63 File Offset: 0x00011D63
		// (set) Token: 0x06000234 RID: 564 RVA: 0x00013B6B File Offset: 0x00011D6B
		public DefaultTraits DefaultTraits { get; private set; }

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000235 RID: 565 RVA: 0x00013B74 File Offset: 0x00011D74
		// (set) Token: 0x06000236 RID: 566 RVA: 0x00013B7C File Offset: 0x00011D7C
		public DefaultPolicies DefaultPolicies { get; private set; }

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000237 RID: 567 RVA: 0x00013B85 File Offset: 0x00011D85
		// (set) Token: 0x06000238 RID: 568 RVA: 0x00013B8D File Offset: 0x00011D8D
		public DefaultBuildingTypes DefaultBuildingTypes { get; private set; }

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000239 RID: 569 RVA: 0x00013B96 File Offset: 0x00011D96
		// (set) Token: 0x0600023A RID: 570 RVA: 0x00013B9E File Offset: 0x00011D9E
		public DefaultIssueEffects DefaultIssueEffects { get; private set; }

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600023B RID: 571 RVA: 0x00013BA7 File Offset: 0x00011DA7
		// (set) Token: 0x0600023C RID: 572 RVA: 0x00013BAF File Offset: 0x00011DAF
		public DefaultItems DefaultItems { get; private set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600023D RID: 573 RVA: 0x00013BB8 File Offset: 0x00011DB8
		// (set) Token: 0x0600023E RID: 574 RVA: 0x00013BC0 File Offset: 0x00011DC0
		public DefaultFigureheads DefaultFigureheads { get; private set; }

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600023F RID: 575 RVA: 0x00013BC9 File Offset: 0x00011DC9
		// (set) Token: 0x06000240 RID: 576 RVA: 0x00013BD1 File Offset: 0x00011DD1
		public DefaultSiegeStrategies DefaultSiegeStrategies { get; private set; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000241 RID: 577 RVA: 0x00013BDA File Offset: 0x00011DDA
		// (set) Token: 0x06000242 RID: 578 RVA: 0x00013BE2 File Offset: 0x00011DE2
		internal MBReadOnlyList<PerkObject> AllPerks { get; private set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000243 RID: 579 RVA: 0x00013BEB File Offset: 0x00011DEB
		// (set) Token: 0x06000244 RID: 580 RVA: 0x00013BF3 File Offset: 0x00011DF3
		public DefaultSkillEffects DefaultSkillEffects { get; private set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000245 RID: 581 RVA: 0x00013BFC File Offset: 0x00011DFC
		// (set) Token: 0x06000246 RID: 582 RVA: 0x00013C04 File Offset: 0x00011E04
		public DefaultVillageTypes DefaultVillageTypes { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000247 RID: 583 RVA: 0x00013C0D File Offset: 0x00011E0D
		// (set) Token: 0x06000248 RID: 584 RVA: 0x00013C15 File Offset: 0x00011E15
		internal MBReadOnlyList<TraitObject> AllTraits { get; private set; }

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000249 RID: 585 RVA: 0x00013C1E File Offset: 0x00011E1E
		// (set) Token: 0x0600024A RID: 586 RVA: 0x00013C26 File Offset: 0x00011E26
		internal MBReadOnlyList<MBEquipmentRoster> AllEquipmentRosters { get; private set; }

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600024B RID: 587 RVA: 0x00013C2F File Offset: 0x00011E2F
		// (set) Token: 0x0600024C RID: 588 RVA: 0x00013C37 File Offset: 0x00011E37
		public DefaultCulturalFeats DefaultFeats { get; private set; }

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600024D RID: 589 RVA: 0x00013C40 File Offset: 0x00011E40
		// (set) Token: 0x0600024E RID: 590 RVA: 0x00013C48 File Offset: 0x00011E48
		internal MBReadOnlyList<PolicyObject> AllPolicies { get; private set; }

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x0600024F RID: 591 RVA: 0x00013C51 File Offset: 0x00011E51
		// (set) Token: 0x06000250 RID: 592 RVA: 0x00013C59 File Offset: 0x00011E59
		internal MBReadOnlyList<BuildingType> AllBuildingTypes { get; private set; }

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000251 RID: 593 RVA: 0x00013C62 File Offset: 0x00011E62
		// (set) Token: 0x06000252 RID: 594 RVA: 0x00013C6A File Offset: 0x00011E6A
		internal MBReadOnlyList<IssueEffect> AllIssueEffects { get; private set; }

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000253 RID: 595 RVA: 0x00013C73 File Offset: 0x00011E73
		// (set) Token: 0x06000254 RID: 596 RVA: 0x00013C7B File Offset: 0x00011E7B
		internal MBReadOnlyList<SiegeStrategy> AllSiegeStrategies { get; private set; }

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000255 RID: 597 RVA: 0x00013C84 File Offset: 0x00011E84
		// (set) Token: 0x06000256 RID: 598 RVA: 0x00013C8C File Offset: 0x00011E8C
		internal MBReadOnlyList<VillageType> AllVillageTypes { get; private set; }

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x06000257 RID: 599 RVA: 0x00013C95 File Offset: 0x00011E95
		// (set) Token: 0x06000258 RID: 600 RVA: 0x00013C9D File Offset: 0x00011E9D
		internal MBReadOnlyList<SkillEffect> AllSkillEffects { get; private set; }

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x06000259 RID: 601 RVA: 0x00013CA6 File Offset: 0x00011EA6
		// (set) Token: 0x0600025A RID: 602 RVA: 0x00013CAE File Offset: 0x00011EAE
		internal MBReadOnlyList<FeatObject> AllFeats { get; private set; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600025B RID: 603 RVA: 0x00013CB7 File Offset: 0x00011EB7
		// (set) Token: 0x0600025C RID: 604 RVA: 0x00013CBF File Offset: 0x00011EBF
		internal MBReadOnlyList<SkillObject> AllSkills { get; private set; }

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x0600025D RID: 605 RVA: 0x00013CC8 File Offset: 0x00011EC8
		// (set) Token: 0x0600025E RID: 606 RVA: 0x00013CD0 File Offset: 0x00011ED0
		internal MBReadOnlyList<SiegeEngineType> AllSiegeEngineTypes { get; private set; }

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x0600025F RID: 607 RVA: 0x00013CD9 File Offset: 0x00011ED9
		// (set) Token: 0x06000260 RID: 608 RVA: 0x00013CE1 File Offset: 0x00011EE1
		internal MBReadOnlyList<ItemCategory> AllItemCategories { get; private set; }

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000261 RID: 609 RVA: 0x00013CEA File Offset: 0x00011EEA
		// (set) Token: 0x06000262 RID: 610 RVA: 0x00013CF2 File Offset: 0x00011EF2
		internal MBReadOnlyList<CharacterAttribute> AllCharacterAttributes { get; private set; }

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000263 RID: 611 RVA: 0x00013CFB File Offset: 0x00011EFB
		// (set) Token: 0x06000264 RID: 612 RVA: 0x00013D03 File Offset: 0x00011F03
		internal MBReadOnlyList<ItemObject> AllItems { get; private set; }

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000265 RID: 613 RVA: 0x00013D0C File Offset: 0x00011F0C
		// (set) Token: 0x06000266 RID: 614 RVA: 0x00013D14 File Offset: 0x00011F14
		public float EstimatedMaximumLordPartySpeedExceptPlayer { get; set; }

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000267 RID: 615 RVA: 0x00013D1D File Offset: 0x00011F1D
		// (set) Token: 0x06000268 RID: 616 RVA: 0x00013D25 File Offset: 0x00011F25
		public float EstimatedAverageLordPartySpeed { get; set; }

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000269 RID: 617 RVA: 0x00013D2E File Offset: 0x00011F2E
		// (set) Token: 0x0600026A RID: 618 RVA: 0x00013D36 File Offset: 0x00011F36
		public float EstimatedAverageCaravanPartySpeed { get; set; }

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x0600026B RID: 619 RVA: 0x00013D3F File Offset: 0x00011F3F
		// (set) Token: 0x0600026C RID: 620 RVA: 0x00013D47 File Offset: 0x00011F47
		public float EstimatedAverageVillagerPartySpeed { get; set; }

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x0600026D RID: 621 RVA: 0x00013D50 File Offset: 0x00011F50
		// (set) Token: 0x0600026E RID: 622 RVA: 0x00013D58 File Offset: 0x00011F58
		public float EstimatedAverageBanditPartySpeed { get; set; }

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x0600026F RID: 623 RVA: 0x00013D61 File Offset: 0x00011F61
		// (set) Token: 0x06000270 RID: 624 RVA: 0x00013D69 File Offset: 0x00011F69
		public float EstimatedAverageLordPartyNavalSpeed { get; set; }

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x06000271 RID: 625 RVA: 0x00013D72 File Offset: 0x00011F72
		// (set) Token: 0x06000272 RID: 626 RVA: 0x00013D7A File Offset: 0x00011F7A
		public float EstimatedAverageCaravanPartyNavalSpeed { get; set; }

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x06000273 RID: 627 RVA: 0x00013D83 File Offset: 0x00011F83
		// (set) Token: 0x06000274 RID: 628 RVA: 0x00013D8B File Offset: 0x00011F8B
		public float EstimatedAverageVillagerPartyNavalSpeed { get; set; }

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000275 RID: 629 RVA: 0x00013D94 File Offset: 0x00011F94
		// (set) Token: 0x06000276 RID: 630 RVA: 0x00013D9C File Offset: 0x00011F9C
		public float EstimatedAverageBanditPartyNavalSpeed { get; set; }

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000277 RID: 631 RVA: 0x00013DA5 File Offset: 0x00011FA5
		// (set) Token: 0x06000278 RID: 632 RVA: 0x00013DAD File Offset: 0x00011FAD
		[SaveableProperty(100)]
		internal MapTimeTracker MapTimeTracker { get; private set; }

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000279 RID: 633 RVA: 0x00013DB6 File Offset: 0x00011FB6
		// (set) Token: 0x0600027A RID: 634 RVA: 0x00013DBE File Offset: 0x00011FBE
		public bool TimeControlModeLock { get; private set; }

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600027B RID: 635 RVA: 0x00013DC7 File Offset: 0x00011FC7
		// (set) Token: 0x0600027C RID: 636 RVA: 0x00013DCF File Offset: 0x00011FCF
		public CampaignTimeControlMode TimeControlMode
		{
			get
			{
				return this._timeControlMode;
			}
			set
			{
				if (!this.TimeControlModeLock && value != this._timeControlMode)
				{
					this._timeControlMode = value;
				}
			}
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x0600027D RID: 637 RVA: 0x00013DE9 File Offset: 0x00011FE9
		// (set) Token: 0x0600027E RID: 638 RVA: 0x00013DF1 File Offset: 0x00011FF1
		public bool IsMapTooltipLongForm { get; set; }

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x0600027F RID: 639 RVA: 0x00013DFA File Offset: 0x00011FFA
		// (set) Token: 0x06000280 RID: 640 RVA: 0x00013E02 File Offset: 0x00012002
		public float SpeedUpMultiplier { get; set; } = 4f;

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000281 RID: 641 RVA: 0x00013E0B File Offset: 0x0001200B
		public float CampaignDt
		{
			get
			{
				return this._dt;
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000282 RID: 642 RVA: 0x00013E13 File Offset: 0x00012013
		// (set) Token: 0x06000283 RID: 643 RVA: 0x00013E1B File Offset: 0x0001201B
		public bool TrueSight { get; set; }

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000284 RID: 644 RVA: 0x00013E24 File Offset: 0x00012024
		// (set) Token: 0x06000285 RID: 645 RVA: 0x00013E2B File Offset: 0x0001202B
		public static Campaign Current { get; private set; }

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000286 RID: 646 RVA: 0x00013E33 File Offset: 0x00012033
		// (set) Token: 0x06000287 RID: 647 RVA: 0x00013E3B File Offset: 0x0001203B
		[SaveableProperty(37)]
		public CampaignGameMode GameMode { get; private set; }

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x06000288 RID: 648 RVA: 0x00013E44 File Offset: 0x00012044
		// (set) Token: 0x06000289 RID: 649 RVA: 0x00013E4C File Offset: 0x0001204C
		[SaveableProperty(38)]
		public float PlayerProgress { get; private set; }

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x0600028A RID: 650 RVA: 0x00013E55 File Offset: 0x00012055
		// (set) Token: 0x0600028B RID: 651 RVA: 0x00013E5D File Offset: 0x0001205D
		public GameMenuManager GameMenuManager { get; private set; }

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x0600028C RID: 652 RVA: 0x00013E66 File Offset: 0x00012066
		public GameModels Models
		{
			get
			{
				return this._gameModels;
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x0600028D RID: 653 RVA: 0x00013E6E File Offset: 0x0001206E
		// (set) Token: 0x0600028E RID: 654 RVA: 0x00013E76 File Offset: 0x00012076
		public SandBoxManager SandBoxManager { get; private set; }

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600028F RID: 655 RVA: 0x00013E7F File Offset: 0x0001207F
		public Campaign.GameLoadingType CampaignGameLoadingType
		{
			get
			{
				return this._gameLoadingType;
			}
		}

		// Token: 0x06000290 RID: 656 RVA: 0x00013E88 File Offset: 0x00012088
		public Campaign(CampaignGameMode gameMode)
		{
			this.GameMode = gameMode;
			this.Options = new CampaignOptions();
			this.CampaignObjectManager = new CampaignObjectManager();
			this.CurrentConversationContext = ConversationContext.Default;
			this.QuestManager = new QuestManager();
			this.IssueManager = new IssueManager();
			this.FactionManager = new FactionManager();
			this.CharacterRelationManager = new CharacterRelationManager();
			this.Romance = new Romance();
			this.PlayerCaptivity = new PlayerCaptivity();
			this.BarterManager = new BarterManager();
			this.GameMenuCallbackManager = new GameMenuCallbackManager();
			this._campaignPeriodicEventManager = new CampaignPeriodicEventManager();
			this._tickData = new CampaignTickCacheDataStore();
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000291 RID: 657 RVA: 0x00013F7C File Offset: 0x0001217C
		// (set) Token: 0x06000292 RID: 658 RVA: 0x00013F84 File Offset: 0x00012184
		[SaveableProperty(40)]
		public SiegeEventManager SiegeEventManager { get; internal set; }

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000293 RID: 659 RVA: 0x00013F8D File Offset: 0x0001218D
		// (set) Token: 0x06000294 RID: 660 RVA: 0x00013F95 File Offset: 0x00012195
		[SaveableProperty(41)]
		public MapEventManager MapEventManager { get; internal set; }

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000295 RID: 661 RVA: 0x00013F9E File Offset: 0x0001219E
		// (set) Token: 0x06000296 RID: 662 RVA: 0x00013FA6 File Offset: 0x000121A6
		[SaveableProperty(43)]
		public MapMarkerManager MapMarkerManager { get; internal set; }

		// Token: 0x06000297 RID: 663 RVA: 0x00013FAF File Offset: 0x000121AF
		public void AddCustomManager<T>() where T : ICustomSystemManager, new()
		{
			this._customManagers.Add(Activator.CreateInstance<T>());
		}

		// Token: 0x06000298 RID: 664 RVA: 0x00013FC8 File Offset: 0x000121C8
		public T GetCustomManager<T>() where T : ICustomSystemManager
		{
			foreach (ICustomSystemManager customSystemManager in this._customManagers)
			{
				if (customSystemManager.GetType() == typeof(T))
				{
					return (T)((object)customSystemManager);
				}
			}
			return default(T);
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000299 RID: 665 RVA: 0x00014040 File Offset: 0x00012240
		// (set) Token: 0x0600029A RID: 666 RVA: 0x00014048 File Offset: 0x00012248
		internal CampaignEvents CampaignEvents { get; private set; }

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x0600029B RID: 667 RVA: 0x00014054 File Offset: 0x00012254
		public MenuContext CurrentMenuContext
		{
			get
			{
				GameStateManager gameStateManager = base.CurrentGame.GameStateManager;
				TutorialState tutorialState = gameStateManager.ActiveState as TutorialState;
				if (tutorialState != null)
				{
					return tutorialState.MenuContext;
				}
				MapState mapState = gameStateManager.ActiveState as MapState;
				if (mapState != null)
				{
					return mapState.MenuContext;
				}
				GameState activeState = gameStateManager.ActiveState;
				MapState mapState2;
				if (((activeState != null) ? activeState.Predecessor : null) != null && (mapState2 = gameStateManager.ActiveState.Predecessor as MapState) != null)
				{
					return mapState2.MenuContext;
				}
				return null;
			}
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x0600029C RID: 668 RVA: 0x000140C9 File Offset: 0x000122C9
		// (set) Token: 0x0600029D RID: 669 RVA: 0x000140D1 File Offset: 0x000122D1
		internal List<MBCampaignEvent> CustomPeriodicCampaignEvents { get; private set; }

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600029E RID: 670 RVA: 0x000140DA File Offset: 0x000122DA
		// (set) Token: 0x0600029F RID: 671 RVA: 0x000140E2 File Offset: 0x000122E2
		public bool IsMainPartyWaiting
		{
			get
			{
				return this._isMainPartyWaiting;
			}
			private set
			{
				this._isMainPartyWaiting = value;
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x060002A0 RID: 672 RVA: 0x000140EB File Offset: 0x000122EB
		// (set) Token: 0x060002A1 RID: 673 RVA: 0x000140F3 File Offset: 0x000122F3
		[SaveableProperty(45)]
		private int _curMapFrame { get; set; }

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x060002A2 RID: 674 RVA: 0x000140FC File Offset: 0x000122FC
		internal LocatorGrid<Settlement> SettlementLocator
		{
			get
			{
				LocatorGrid<Settlement> result;
				if ((result = this._settlementLocator) == null)
				{
					result = (this._settlementLocator = new LocatorGrid<Settlement>(5f, 32, 32));
				}
				return result;
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x060002A3 RID: 675 RVA: 0x0001412C File Offset: 0x0001232C
		internal LocatorGrid<MobileParty> MobilePartyLocator
		{
			get
			{
				LocatorGrid<MobileParty> result;
				if ((result = this._mobilePartyLocator) == null)
				{
					result = (this._mobilePartyLocator = new LocatorGrid<MobileParty>(5f, 32, 32));
				}
				return result;
			}
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x060002A4 RID: 676 RVA: 0x0001415A File Offset: 0x0001235A
		public IMapScene MapSceneWrapper
		{
			get
			{
				return this._mapSceneWrapper;
			}
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x060002A5 RID: 677 RVA: 0x00014162 File Offset: 0x00012362
		// (set) Token: 0x060002A6 RID: 678 RVA: 0x0001416A File Offset: 0x0001236A
		[SaveableProperty(54)]
		public PlayerEncounter PlayerEncounter { get; internal set; }

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x060002A7 RID: 679 RVA: 0x00014173 File Offset: 0x00012373
		// (set) Token: 0x060002A8 RID: 680 RVA: 0x0001417B File Offset: 0x0001237B
		[CachedData]
		internal LocationEncounter LocationEncounter { get; set; }

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x060002A9 RID: 681 RVA: 0x00014184 File Offset: 0x00012384
		// (set) Token: 0x060002AA RID: 682 RVA: 0x0001418C File Offset: 0x0001238C
		internal NameGenerator NameGenerator { get; private set; }

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060002AB RID: 683 RVA: 0x00014195 File Offset: 0x00012395
		// (set) Token: 0x060002AC RID: 684 RVA: 0x0001419D File Offset: 0x0001239D
		[SaveableProperty(58)]
		public BarterManager BarterManager { get; private set; }

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x060002AD RID: 685 RVA: 0x000141A6 File Offset: 0x000123A6
		// (set) Token: 0x060002AE RID: 686 RVA: 0x000141AE File Offset: 0x000123AE
		[SaveableProperty(69)]
		public bool IsMainHeroDisguised { get; set; }

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x060002AF RID: 687 RVA: 0x000141B7 File Offset: 0x000123B7
		// (set) Token: 0x060002B0 RID: 688 RVA: 0x000141BF File Offset: 0x000123BF
		public Equipment DeadBattleEquipment { get; set; }

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060002B1 RID: 689 RVA: 0x000141C8 File Offset: 0x000123C8
		// (set) Token: 0x060002B2 RID: 690 RVA: 0x000141D0 File Offset: 0x000123D0
		public Equipment DeadCivilianEquipment { get; set; }

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060002B3 RID: 691 RVA: 0x000141D9 File Offset: 0x000123D9
		// (set) Token: 0x060002B4 RID: 692 RVA: 0x000141E1 File Offset: 0x000123E1
		public Equipment DefaultStealthEquipment { get; private set; }

		// Token: 0x060002B5 RID: 693 RVA: 0x000141EC File Offset: 0x000123EC
		public void InitializeMainParty()
		{
			this.InitializeSinglePlayerReferences();
			CampaignVec2 position = NavigationHelper.FindReachablePointAroundPosition(this.Settlements.Find((Settlement x) => x.IsTown).GatePosition, MobileParty.MainParty.NavigationCapability, 20f, 0f, false);
			this.MainParty.InitializeMobilePartyAtPosition(base.CurrentGame.ObjectManager.GetObject<PartyTemplateObject>("main_hero_party_template"), position);
			LordPartyComponent.ConvertPartyToLordParty(this.MainParty, Hero.MainHero, Hero.MainHero);
			this.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, 1);
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x00014298 File Offset: 0x00012498
		[LoadInitializationCallback]
		private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
		{
			this._campaignEntitySystem = new EntitySystem<CampaignEntityComponent>();
			this.PlayerFormationPreferences = this._playerFormationPreferences.GetReadOnlyDictionary<CharacterObject, FormationClass>();
			this.SpeedUpMultiplier = 4f;
			if (this.UniqueGameId == null && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.2.2", 0))
			{
				this.UniqueGameId = "oldSave";
			}
			if (MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.0", 0))
			{
				if (this._previouslyUsedModules == null)
				{
					this._previouslyUsedModules = new MBList<string>();
				}
				MBList<string> source = new MBList<string>(this._previouslyUsedModules);
				this._previouslyUsedModules.Clear();
				if (source.Any<string>())
				{
					this._previouslyUsedModules.Add(string.Join(MBSaveLoad.ModuleCodeSeperator.ToString(), from x in source
						select x + MBSaveLoad.ModuleVersionSeperator.ToString() + ApplicationVersion.Empty.ToString()));
				}
			}
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.0", 0))
			{
				this.UnlockedFigureheadsByMainHero = new List<Figurehead>();
				this._customManagers = new List<ICustomSystemManager>();
				this.MapMarkerManager = new MapMarkerManager();
			}
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x000143C8 File Offset: 0x000125C8
		private void InitializeForSavedGame()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				settlement.Party.OnFinishLoadState();
			}
			foreach (MobileParty mobileParty in this.MobileParties.ToList<MobileParty>())
			{
				mobileParty.Party.OnFinishLoadState();
			}
			foreach (Settlement settlement2 in Settlement.All)
			{
				settlement2.OnFinishLoadState();
			}
			this.GameMenuCallbackManager = new GameMenuCallbackManager();
			this.GameMenuCallbackManager.OnGameLoad();
			this.IssueManager.InitializeForSavedGame();
			this.MinSettlementX = float.MaxValue;
			this.MinSettlementY = float.MaxValue;
			this.MaxSettlementX = float.MinValue;
			this.MaxSettlementY = float.MinValue;
			foreach (Settlement settlement3 in Settlement.All)
			{
				if (settlement3.Position.X < this.MinSettlementX)
				{
					this.MinSettlementX = settlement3.Position.X;
				}
				if (settlement3.Position.Y < this.MinSettlementY)
				{
					this.MinSettlementY = settlement3.Position.Y;
				}
				if (settlement3.Position.X > this.MaxSettlementX)
				{
					this.MaxSettlementX = settlement3.Position.X;
				}
				if (settlement3.Position.Y > this.MaxSettlementY)
				{
					this.MaxSettlementY = settlement3.Position.Y;
				}
			}
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x000145D8 File Offset: 0x000127D8
		private void OnGameLoaded(CampaignGameStarter starter)
		{
			TroopRoster.CalculateCachedStatsOnLoad();
			this._tickData = new CampaignTickCacheDataStore();
			base.ObjectManager.PreAfterLoad();
			this.CampaignObjectManager.PreAfterLoad();
			this.IssueManager.PreAfterLoad();
			this.QuestManager.PreAfterLoad();
			base.ObjectManager.AfterLoad();
			this.CampaignObjectManager.AfterLoad();
			this.CharacterRelationManager.AfterLoad();
			CampaignEventDispatcher.Instance.OnGameEarlyLoaded(starter);
			CampaignEventDispatcher.Instance.OnGameLoaded(starter);
			this.InitializeForSavedGame();
			this._tickData.InitializeDataCache();
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0001466C File Offset: 0x0001286C
		private void OnDataLoadFinished(CampaignGameStarter starter)
		{
			this._towns = new MBList<Town>();
			this._castles = new MBList<Town>();
			this._villages = new MBList<Village>();
			this._hideouts = new MBList<Hideout>();
			for (int i = 0; i < Settlement.All.Count; i++)
			{
				Settlement settlement = Settlement.All[i];
				if (settlement.IsTown)
				{
					this._towns.Add(settlement.Town);
				}
				else if (settlement.IsCastle)
				{
					this._castles.Add(settlement.Town);
				}
				else if (settlement.IsVillage)
				{
					this._villages.Add(settlement.Village);
				}
				else if (settlement.IsHideout)
				{
					this._hideouts.Add(settlement.Hideout);
				}
			}
			this._campaignPeriodicEventManager.InitializeTickers();
			this.CreateCampaignEvents();
		}

		// Token: 0x060002BA RID: 698 RVA: 0x00014744 File Offset: 0x00012944
		private void OnSessionStart(CampaignGameStarter starter)
		{
			CampaignEventDispatcher.Instance.OnSessionStart(starter);
			CampaignEventDispatcher.Instance.OnAfterSessionStart(starter);
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			this.ConversationManager.Build();
			foreach (Settlement settlement in this.Settlements)
			{
				settlement.OnSessionStart();
			}
			this.IsCraftingEnabled = true;
			this.IsBannerEditorEnabled = true;
			this.IsFaceGenEnabled = true;
			this.MapEventManager.OnAfterLoad();
			this.SiegeEventManager.OnAfterLoad();
			this.KingdomManager.RegisterEvents();
			this.KingdomManager.OnSessionStart();
			this.CampaignInformationManager.RegisterEvents();
		}

		// Token: 0x060002BB RID: 699 RVA: 0x00014818 File Offset: 0x00012A18
		private void DailyTickSettlement(Settlement settlement)
		{
			if (settlement.IsVillage)
			{
				settlement.Village.DailyTick();
				return;
			}
			if (settlement.Town != null)
			{
				settlement.Town.DailyTick();
			}
		}

		// Token: 0x060002BC RID: 700 RVA: 0x00014844 File Offset: 0x00012A44
		private void GameInitTick()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				settlement.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
			}
			foreach (MobileParty mobileParty in this.MobileParties)
			{
				mobileParty.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
			}
		}

		// Token: 0x060002BD RID: 701 RVA: 0x000148FC File Offset: 0x00012AFC
		internal void HourlyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
		{
			CampaignEventDispatcher.Instance.HourlyTick();
			MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
			if (mapState == null)
			{
				return;
			}
			mapState.OnHourlyTick();
		}

		// Token: 0x060002BE RID: 702 RVA: 0x00014926 File Offset: 0x00012B26
		internal void QuarterHourlyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
		{
			CampaignEventDispatcher.Instance.QuarterHourlyTick();
		}

		// Token: 0x060002BF RID: 703 RVA: 0x00014934 File Offset: 0x00012B34
		internal void DailyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
		{
			this.PlayerProgress = (this.PlayerProgress + this.Models.PlayerProgressionModel.GetPlayerProgress()) / 2f;
			Debug.Print("Before Daily Tick: " + CampaignTime.Now.ToString(), 0, Debug.DebugColor.White, 17592186044416UL);
			CampaignEventDispatcher.Instance.DailyTick();
			if ((int)this.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow % CampaignTime.DaysInWeek == 0)
			{
				CampaignEventDispatcher.Instance.WeeklyTick();
				this.OnWeeklyTick();
			}
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x000149CD File Offset: 0x00012BCD
		public void WaitAsyncTasks()
		{
			if (this.CampaignLateAITickTask != null)
			{
				this.CampaignLateAITickTask.Wait();
			}
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x000149E2 File Offset: 0x00012BE2
		private void OnWeeklyTick()
		{
			this.LogEntryHistory.DeleteOutdatedLogs();
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x000149F0 File Offset: 0x00012BF0
		public CampaignTimeControlMode GetSimplifiedTimeControlMode()
		{
			switch (this.TimeControlMode)
			{
			case CampaignTimeControlMode.Stop:
				return CampaignTimeControlMode.Stop;
			case CampaignTimeControlMode.UnstoppablePlay:
				return CampaignTimeControlMode.UnstoppablePlay;
			case CampaignTimeControlMode.UnstoppableFastForward:
			case CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime:
				return CampaignTimeControlMode.UnstoppableFastForward;
			case CampaignTimeControlMode.StoppablePlay:
				if (!this.IsMainPartyWaiting)
				{
					return CampaignTimeControlMode.StoppablePlay;
				}
				return CampaignTimeControlMode.Stop;
			case CampaignTimeControlMode.StoppableFastForward:
				if (!this.IsMainPartyWaiting)
				{
					return CampaignTimeControlMode.StoppableFastForward;
				}
				return CampaignTimeControlMode.Stop;
			default:
				return CampaignTimeControlMode.Stop;
			}
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x00014A43 File Offset: 0x00012C43
		private void CheckMainPartyNeedsUpdate()
		{
			MobileParty.MainParty.Ai.CheckPartyNeedsUpdate();
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x00014A54 File Offset: 0x00012C54
		private void TickMapTime(float realDt)
		{
			float num = 0f;
			float speedUpMultiplier = this.SpeedUpMultiplier;
			float num2 = 0.25f * realDt;
			this.IsMainPartyWaiting = MobileParty.MainParty.ComputeIsWaiting();
			switch (this.TimeControlMode)
			{
			case CampaignTimeControlMode.Stop:
			case CampaignTimeControlMode.FastForwardStop:
				break;
			case CampaignTimeControlMode.UnstoppablePlay:
				num = num2;
				break;
			case CampaignTimeControlMode.UnstoppableFastForward:
			case CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime:
				num = num2 * speedUpMultiplier;
				break;
			case CampaignTimeControlMode.StoppablePlay:
				if (!this.IsMainPartyWaiting)
				{
					num = num2;
				}
				break;
			case CampaignTimeControlMode.StoppableFastForward:
				if (!this.IsMainPartyWaiting)
				{
					num = num2 * speedUpMultiplier;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			this._dt = num;
			this.MapTimeTracker.Tick(4320f * num);
		}

		// Token: 0x060002C5 RID: 709 RVA: 0x00014AF4 File Offset: 0x00012CF4
		public void OnGameOver()
		{
			if (CampaignOptions.IsIronmanMode)
			{
				this.SaveHandler.QuickSaveCurrentGame();
			}
		}

		// Token: 0x060002C6 RID: 710 RVA: 0x00014B08 File Offset: 0x00012D08
		internal void RealTick(float realDt)
		{
			this.WaitAsyncTasks();
			this.CheckMainPartyNeedsUpdate();
			this.TickMapTime(realDt);
			foreach (CampaignEntityComponent campaignEntityComponent in this._campaignEntitySystem.GetComponents())
			{
				campaignEntityComponent.OnTick(realDt, this._dt);
			}
			if (!this.GameStarted)
			{
				this.GameStarted = true;
				this._tickData.InitializeDataCache();
				this.SiegeEventManager.Tick(this._dt);
			}
			this._tickData.RealTick(this._dt, realDt);
			this.SiegeEventManager.Tick(this._dt);
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060002C7 RID: 711 RVA: 0x00014BC8 File Offset: 0x00012DC8
		public static float CurrentTime
		{
			get
			{
				return (float)CampaignTime.Now.ToHours;
			}
		}

		// Token: 0x060002C8 RID: 712 RVA: 0x00014BE4 File Offset: 0x00012DE4
		public void SetTimeSpeed(int speed)
		{
			switch (speed)
			{
			case 0:
				if (this.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward || this.TimeControlMode == CampaignTimeControlMode.StoppableFastForward)
				{
					this.TimeControlMode = CampaignTimeControlMode.FastForwardStop;
					return;
				}
				if (this.TimeControlMode != CampaignTimeControlMode.FastForwardStop && this.TimeControlMode != CampaignTimeControlMode.Stop)
				{
					this.TimeControlMode = CampaignTimeControlMode.Stop;
					return;
				}
				break;
			case 1:
				if (((this.TimeControlMode == CampaignTimeControlMode.Stop || this.TimeControlMode == CampaignTimeControlMode.FastForwardStop) && this.MainParty.DefaultBehavior == AiBehavior.Hold) || this.IsMainPartyWaiting || (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty))
				{
					this.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
					return;
				}
				this.TimeControlMode = CampaignTimeControlMode.StoppablePlay;
				return;
			case 2:
				if (((this.TimeControlMode == CampaignTimeControlMode.Stop || this.TimeControlMode == CampaignTimeControlMode.FastForwardStop) && this.MainParty.DefaultBehavior == AiBehavior.Hold) || this.IsMainPartyWaiting || (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty))
				{
					this.TimeControlMode = CampaignTimeControlMode.UnstoppableFastForward;
					return;
				}
				this.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
				break;
			default:
				return;
			}
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x00014CEC File Offset: 0x00012EEC
		public static void LateAITick()
		{
			Campaign.Current.LateAITickAux();
		}

		// Token: 0x060002CA RID: 714 RVA: 0x00014CF8 File Offset: 0x00012EF8
		internal void LateAITickAux()
		{
			if (this._dt > 0f || this.CurrentTickCount < 3)
			{
				this.PartiesThink(this._dt);
			}
		}

		// Token: 0x060002CB RID: 715 RVA: 0x00014D1C File Offset: 0x00012F1C
		internal void Tick()
		{
			int curMapFrame = this._curMapFrame;
			this._curMapFrame = curMapFrame + 1;
			this.CurrentTickCount++;
			if (this._dt > 0f || this.CurrentTickCount < 3)
			{
				CampaignEventDispatcher.Instance.Tick(this._dt);
				this._campaignPeriodicEventManager.OnTick(this._dt);
				this.MapEventManager.Tick();
				this._lastNonZeroDtFrame = this._curMapFrame;
				this._campaignPeriodicEventManager.MobilePartyHourlyTick();
			}
			if (this._dt > 0f)
			{
				this._campaignPeriodicEventManager.TickPeriodicEvents();
			}
			this._tickData.Tick();
			Campaign.Current.PlayerCaptivity.Update(this._dt);
			if (this._dt > 0f || (MobileParty.MainParty.MapEvent == null && this._curMapFrame == this._lastNonZeroDtFrame + 1))
			{
				EncounterManager.Tick(this._dt);
				MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
				if (mapState != null && mapState.AtMenu && !mapState.MenuContext.GameMenu.IsWaitActive)
				{
					this._dt = 0f;
				}
			}
			if (this._dt > 0f || this.CurrentTickCount < 3)
			{
				this._campaignPeriodicEventManager.TickPartialHourlyAi();
			}
			MapState mapState2;
			if ((mapState2 = Game.Current.GameStateManager.ActiveState as MapState) != null && mapState2.NextIncident != null)
			{
				if (mapState2.NextIncident.CanIncidentBeInvoked())
				{
					mapState2.StartIncident(mapState2.NextIncident);
				}
				mapState2.NextIncident = null;
			}
			MapState mapState3;
			if ((mapState3 = Game.Current.GameStateManager.ActiveState as MapState) != null && !mapState3.AtMenu)
			{
				string genericStateMenu = this.Models.EncounterGameMenuModel.GetGenericStateMenu();
				if (!string.IsNullOrEmpty(genericStateMenu))
				{
					GameMenu.ActivateGameMenu(genericStateMenu);
				}
			}
		}

		// Token: 0x060002CC RID: 716 RVA: 0x00014EEC File Offset: 0x000130EC
		private void CreateCampaignEvents()
		{
			long numTicks = (CampaignTime.Now - Campaign.Current.Models.CampaignTimeModel.CampaignStartTime).NumTicks;
			CampaignTime initialWait = CampaignTime.Days(1f);
			if (numTicks % CampaignTime.TimeTicksPerDay != 0L)
			{
				initialWait = CampaignTime.Days((float)(numTicks % CampaignTime.TimeTicksPerDay) / (float)CampaignTime.TimeTicksPerDay);
			}
			this._dailyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Days(1f), initialWait);
			this._dailyTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.DailyTick));
			CampaignTime initialWait2 = CampaignTime.Hours(0.5f);
			if (numTicks % CampaignTime.TimeTicksPerHour != 0L)
			{
				initialWait2 = CampaignTime.Hours((float)(numTicks % CampaignTime.TimeTicksPerHour) / (float)CampaignTime.TimeTicksPerHour);
			}
			this._hourlyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(1f), initialWait2);
			this._hourlyTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.HourlyTick));
			initialWait2 = CampaignTime.Hours(0.125f);
			if (numTicks % (CampaignTime.TimeTicksPerHour / 4L) != 0L)
			{
				initialWait2 = CampaignTime.Hours((float)(numTicks % (CampaignTime.TimeTicksPerHour / 4L)) / (float)(CampaignTime.TimeTicksPerHour / 4L));
			}
			this._QuarterHourlyTickEvent = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(0.25f), initialWait2);
			this._QuarterHourlyTickEvent.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.QuarterHourlyTick));
		}

		// Token: 0x060002CD RID: 717 RVA: 0x0001502C File Offset: 0x0001322C
		private void PartiesThink(float dt)
		{
			for (int i = 0; i < this.MobileParties.Count; i++)
			{
				this.MobileParties[i].Ai.Tick(dt);
			}
		}

		// Token: 0x060002CE RID: 718 RVA: 0x00015068 File Offset: 0x00013268
		public TComponent GetEntityComponent<TComponent>() where TComponent : CampaignEntityComponent
		{
			EntitySystem<CampaignEntityComponent> campaignEntitySystem = this._campaignEntitySystem;
			if (campaignEntitySystem == null)
			{
				return default(TComponent);
			}
			return campaignEntitySystem.GetComponent<TComponent>();
		}

		// Token: 0x060002CF RID: 719 RVA: 0x0001508E File Offset: 0x0001328E
		public TComponent AddEntityComponent<TComponent>() where TComponent : CampaignEntityComponent, new()
		{
			return this._campaignEntitySystem.AddComponent<TComponent>();
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x0001509B File Offset: 0x0001329B
		public void RemoveEntityComponent<TComponent>() where TComponent : CampaignEntityComponent
		{
			this._campaignEntitySystem.RemoveComponent<TComponent>();
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x000150A8 File Offset: 0x000132A8
		public void RemoveEntityComponent<TComponent>(TComponent component) where TComponent : CampaignEntityComponent
		{
			this._campaignEntitySystem.RemoveComponent(component);
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x000150BB File Offset: 0x000132BB
		public List<TComponent> GetComponents<TComponent>() where TComponent : CampaignEntityComponent
		{
			return this._campaignEntitySystem.GetComponents<TComponent>();
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060002D3 RID: 723 RVA: 0x000150C8 File Offset: 0x000132C8
		public MBReadOnlyList<CampaignEntityComponent> CampaignEntityComponents
		{
			get
			{
				return this._campaignEntitySystem.Components;
			}
		}

		// Token: 0x060002D4 RID: 724 RVA: 0x000150D5 File Offset: 0x000132D5
		public T GetCampaignBehavior<T>()
		{
			return this._campaignBehaviorManager.GetBehavior<T>();
		}

		// Token: 0x060002D5 RID: 725 RVA: 0x000150E2 File Offset: 0x000132E2
		public IEnumerable<T> GetCampaignBehaviors<T>()
		{
			return this._campaignBehaviorManager.GetBehaviors<T>();
		}

		// Token: 0x060002D6 RID: 726 RVA: 0x000150EF File Offset: 0x000132EF
		public void AddCampaignBehaviorManager(ICampaignBehaviorManager manager)
		{
			this._campaignBehaviorManager = manager;
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060002D7 RID: 727 RVA: 0x000150F8 File Offset: 0x000132F8
		public MBReadOnlyList<Hero> AliveHeroes
		{
			get
			{
				return this.CampaignObjectManager.AliveHeroes;
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060002D8 RID: 728 RVA: 0x00015105 File Offset: 0x00013305
		public MBReadOnlyList<Hero> DeadOrDisabledHeroes
		{
			get
			{
				return this.CampaignObjectManager.DeadOrDisabledHeroes;
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060002D9 RID: 729 RVA: 0x00015112 File Offset: 0x00013312
		public MBReadOnlyList<MobileParty> MobileParties
		{
			get
			{
				return this.CampaignObjectManager.MobileParties;
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060002DA RID: 730 RVA: 0x0001511F File Offset: 0x0001331F
		public MBReadOnlyList<MobileParty> CaravanParties
		{
			get
			{
				return this.CampaignObjectManager.CaravanParties;
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060002DB RID: 731 RVA: 0x0001512C File Offset: 0x0001332C
		public MBReadOnlyList<MobileParty> PatrolParties
		{
			get
			{
				return this.CampaignObjectManager.PatrolParties;
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060002DC RID: 732 RVA: 0x00015139 File Offset: 0x00013339
		public MBReadOnlyList<MobileParty> VillagerParties
		{
			get
			{
				return this.CampaignObjectManager.VillagerParties;
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x060002DD RID: 733 RVA: 0x00015146 File Offset: 0x00013346
		public MBReadOnlyList<MobileParty> MilitiaParties
		{
			get
			{
				return this.CampaignObjectManager.MilitiaParties;
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x060002DE RID: 734 RVA: 0x00015153 File Offset: 0x00013353
		public MBReadOnlyList<MobileParty> GarrisonParties
		{
			get
			{
				return this.CampaignObjectManager.GarrisonParties;
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x060002DF RID: 735 RVA: 0x00015160 File Offset: 0x00013360
		public MBReadOnlyList<MobileParty> CustomParties
		{
			get
			{
				return this.CampaignObjectManager.CustomParties;
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x060002E0 RID: 736 RVA: 0x0001516D File Offset: 0x0001336D
		public MBReadOnlyList<MobileParty> LordParties
		{
			get
			{
				return this.CampaignObjectManager.LordParties;
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x060002E1 RID: 737 RVA: 0x0001517A File Offset: 0x0001337A
		public MBReadOnlyList<MobileParty> BanditParties
		{
			get
			{
				return this.CampaignObjectManager.BanditParties;
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x060002E2 RID: 738 RVA: 0x00015187 File Offset: 0x00013387
		public MBReadOnlyList<MobileParty> PartiesWithoutPartyComponent
		{
			get
			{
				return this.CampaignObjectManager.PartiesWithoutPartyComponent;
			}
		}

		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060002E3 RID: 739 RVA: 0x00015194 File Offset: 0x00013394
		public MBReadOnlyList<Settlement> Settlements
		{
			get
			{
				return this.CampaignObjectManager.Settlements;
			}
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060002E4 RID: 740 RVA: 0x000151A1 File Offset: 0x000133A1
		public IEnumerable<IFaction> Factions
		{
			get
			{
				return this.CampaignObjectManager.Factions;
			}
		}

		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060002E5 RID: 741 RVA: 0x000151AE File Offset: 0x000133AE
		public MBReadOnlyList<Kingdom> Kingdoms
		{
			get
			{
				return this.CampaignObjectManager.Kingdoms;
			}
		}

		// Token: 0x17000097 RID: 151
		// (get) Token: 0x060002E6 RID: 742 RVA: 0x000151BB File Offset: 0x000133BB
		public MBReadOnlyList<Clan> Clans
		{
			get
			{
				return this.CampaignObjectManager.Clans;
			}
		}

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x060002E7 RID: 743 RVA: 0x000151C8 File Offset: 0x000133C8
		public MBReadOnlyList<CharacterObject> Characters
		{
			get
			{
				return this._characters;
			}
		}

		// Token: 0x17000099 RID: 153
		// (get) Token: 0x060002E8 RID: 744 RVA: 0x000151D0 File Offset: 0x000133D0
		public MBReadOnlyList<WorkshopType> Workshops
		{
			get
			{
				return this._workshops;
			}
		}

		// Token: 0x1700009A RID: 154
		// (get) Token: 0x060002E9 RID: 745 RVA: 0x000151D8 File Offset: 0x000133D8
		public MBReadOnlyList<ItemModifier> ItemModifiers
		{
			get
			{
				return this._itemModifiers;
			}
		}

		// Token: 0x1700009B RID: 155
		// (get) Token: 0x060002EA RID: 746 RVA: 0x000151E0 File Offset: 0x000133E0
		public MBReadOnlyList<ItemModifierGroup> ItemModifierGroups
		{
			get
			{
				return this._itemModifierGroups;
			}
		}

		// Token: 0x1700009C RID: 156
		// (get) Token: 0x060002EB RID: 747 RVA: 0x000151E8 File Offset: 0x000133E8
		public MBReadOnlyList<Concept> Concepts
		{
			get
			{
				return this._concepts;
			}
		}

		// Token: 0x1700009D RID: 157
		// (get) Token: 0x060002EC RID: 748 RVA: 0x000151F0 File Offset: 0x000133F0
		// (set) Token: 0x060002ED RID: 749 RVA: 0x000151F8 File Offset: 0x000133F8
		[SaveableProperty(60)]
		public MobileParty MainParty { get; private set; }

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x060002EE RID: 750 RVA: 0x00015201 File Offset: 0x00013401
		// (set) Token: 0x060002EF RID: 751 RVA: 0x00015209 File Offset: 0x00013409
		public PartyBase CameraFollowParty
		{
			get
			{
				return this._cameraFollowParty;
			}
			set
			{
				this._cameraFollowParty = value;
			}
		}

		// Token: 0x1700009F RID: 159
		// (get) Token: 0x060002F0 RID: 752 RVA: 0x00015212 File Offset: 0x00013412
		// (set) Token: 0x060002F1 RID: 753 RVA: 0x0001521A File Offset: 0x0001341A
		[SaveableProperty(62)]
		public CampaignInformationManager CampaignInformationManager { get; set; }

		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x060002F2 RID: 754 RVA: 0x00015223 File Offset: 0x00013423
		// (set) Token: 0x060002F3 RID: 755 RVA: 0x0001522B File Offset: 0x0001342B
		[SaveableProperty(63)]
		public VisualTrackerManager VisualTrackerManager { get; set; }

		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x060002F4 RID: 756 RVA: 0x00015234 File Offset: 0x00013434
		public LogEntryHistory LogEntryHistory
		{
			get
			{
				return this._logEntryHistory;
			}
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x060002F5 RID: 757 RVA: 0x0001523C File Offset: 0x0001343C
		// (set) Token: 0x060002F6 RID: 758 RVA: 0x00015244 File Offset: 0x00013444
		public EncyclopediaManager EncyclopediaManager { get; private set; }

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x060002F7 RID: 759 RVA: 0x0001524D File Offset: 0x0001344D
		// (set) Token: 0x060002F8 RID: 760 RVA: 0x00015255 File Offset: 0x00013455
		public ConversationManager ConversationManager { get; private set; }

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x060002F9 RID: 761 RVA: 0x0001525E File Offset: 0x0001345E
		public bool IsDay
		{
			get
			{
				return !this.IsNight;
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x060002FA RID: 762 RVA: 0x0001526C File Offset: 0x0001346C
		public bool IsNight
		{
			get
			{
				return CampaignTime.Now.IsNightTime;
			}
		}

		// Token: 0x060002FB RID: 763 RVA: 0x00015286 File Offset: 0x00013486
		internal int GeneratePartyId(PartyBase party)
		{
			int lastPartyIndex = this._lastPartyIndex;
			this._lastPartyIndex++;
			return lastPartyIndex;
		}

		// Token: 0x060002FC RID: 764 RVA: 0x0001529C File Offset: 0x0001349C
		private void LoadMapScene()
		{
			this._mapSceneWrapper = this.MapSceneCreator.CreateMapScene();
			this._mapSceneWrapper.SetSceneLevels(new List<string> { "level_1", "level_2", "level_3", "siege", "raid", "burned" });
			this._mapSceneWrapper.Load();
			Vec2 mapMinimumPosition;
			Vec2 mapMaximumPosition;
			float mapMaximumHeight;
			this._mapSceneWrapper.GetMapBorders(out mapMinimumPosition, out mapMaximumPosition, out mapMaximumHeight);
			Campaign.MapMinimumPosition = mapMinimumPosition;
			Campaign.MapMaximumPosition = mapMaximumPosition;
			Campaign.MapMaximumHeight = mapMaximumHeight;
			Campaign.MapDiagonal = Campaign.MapMinimumPosition.Distance(Campaign.MapMaximumPosition);
			Campaign.MapDiagonalSquared = Campaign.MapDiagonal * Campaign.MapDiagonal;
			Campaign.PlayerRegionSwitchCostFromLandToSea = (int)(Campaign.MapDiagonal * (float)this.Models.MapDistanceModel.RegionSwitchCostFromLandToSea * 0.2f);
			Campaign.PathFindingMaxCostLimit = Math.Max(Campaign.PlayerRegionSwitchCostFromLandToSea * 100, (int)(Campaign.MapDiagonal * 500f));
			this._mapSceneWrapper.AfterLoad();
		}

		// Token: 0x060002FD RID: 765 RVA: 0x000153B0 File Offset: 0x000135B0
		private void InitializeCachedLists()
		{
			MBObjectManager objectManager = Game.Current.ObjectManager;
			this._characters = objectManager.GetObjectTypeList<CharacterObject>();
			this._workshops = objectManager.GetObjectTypeList<WorkshopType>();
			this._itemModifiers = objectManager.GetObjectTypeList<ItemModifier>();
			this._itemModifierGroups = objectManager.GetObjectTypeList<ItemModifierGroup>();
			this._concepts = objectManager.GetObjectTypeList<Concept>();
		}

		// Token: 0x060002FE RID: 766 RVA: 0x00015404 File Offset: 0x00013604
		private void InitializeDefaultEquipments()
		{
			this.DeadBattleEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("default_battle_equipment_roster_neutral").DefaultEquipment;
			this.DeadCivilianEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("default_civilian_equipment_roster_neutral").DefaultEquipment;
			this.DefaultStealthEquipment = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>("default_stealth_equipment_roster").DefaultEquipment;
		}

		// Token: 0x060002FF RID: 767 RVA: 0x00015470 File Offset: 0x00013670
		public override void OnDestroy()
		{
			this.WaitAsyncTasks();
			GameTexts.ClearInstance();
			IMapScene mapSceneWrapper = this._mapSceneWrapper;
			if (mapSceneWrapper != null)
			{
				mapSceneWrapper.Destroy();
			}
			ConversationManager.Clear();
			MBTextManager.ClearAll();
			GameSceneDataManager.Destroy();
			this.CampaignInformationManager.DeRegisterEvents();
			ICampaignBehaviorManager campaignBehaviorManager = this._campaignBehaviorManager;
			if (campaignBehaviorManager != null)
			{
				campaignBehaviorManager.ClearBehaviors();
			}
			MBSaveLoad.OnGameDestroy();
			Campaign.Current = null;
		}

		// Token: 0x06000300 RID: 768 RVA: 0x000154CF File Offset: 0x000136CF
		public void InitializeSinglePlayerReferences()
		{
			this.IsSinglePlayerReferencesInitialized = true;
			this.InitializeGamePlayReferences();
		}

		// Token: 0x06000301 RID: 769 RVA: 0x000154E0 File Offset: 0x000136E0
		private void CreateLists()
		{
			this.AllPerks = MBObjectManager.Instance.GetObjectTypeList<PerkObject>();
			this.AllTraits = MBObjectManager.Instance.GetObjectTypeList<TraitObject>();
			this.AllEquipmentRosters = MBObjectManager.Instance.GetObjectTypeList<MBEquipmentRoster>();
			this.AllPolicies = MBObjectManager.Instance.GetObjectTypeList<PolicyObject>();
			this.AllBuildingTypes = MBObjectManager.Instance.GetObjectTypeList<BuildingType>();
			this.AllIssueEffects = MBObjectManager.Instance.GetObjectTypeList<IssueEffect>();
			this.AllSiegeStrategies = MBObjectManager.Instance.GetObjectTypeList<SiegeStrategy>();
			this.AllVillageTypes = MBObjectManager.Instance.GetObjectTypeList<VillageType>();
			this.AllSkillEffects = MBObjectManager.Instance.GetObjectTypeList<SkillEffect>();
			this.AllFeats = MBObjectManager.Instance.GetObjectTypeList<FeatObject>();
			this.AllSkills = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
			this.AllSiegeEngineTypes = MBObjectManager.Instance.GetObjectTypeList<SiegeEngineType>();
			this.AllItemCategories = MBObjectManager.Instance.GetObjectTypeList<ItemCategory>();
			this.AllCharacterAttributes = MBObjectManager.Instance.GetObjectTypeList<CharacterAttribute>();
			this.AllItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();
		}

		// Token: 0x06000302 RID: 770 RVA: 0x000155E0 File Offset: 0x000137E0
		private void CheckMapUpdate()
		{
			uint sceneXmlCrc = this.MapSceneWrapper.GetSceneXmlCrc();
			uint sceneNavigationMeshCrc = this.MapSceneWrapper.GetSceneNavigationMeshCrc();
			if (sceneXmlCrc != this._campaignMapSceneXmlCrc || sceneNavigationMeshCrc != this._campaignMapSceneNavigationMeshCrc)
			{
				this.CalculateCachedValues();
				foreach (Settlement settlement in this.Settlements)
				{
					settlement.CheckPositionsForMapChangeAndUpdateIfNeeded();
				}
				foreach (MapEvent mapEvent in this.MapEventManager.MapEvents)
				{
					mapEvent.CheckPositionsForMapChangeAndUpdateIfNeeded();
				}
				foreach (Kingdom kingdom in this.Kingdoms)
				{
					foreach (Army army in kingdom.Armies)
					{
						army.CheckPositionsForMapChangeAndUpdateIfNeeded();
					}
				}
				foreach (MobileParty mobileParty in this.MobileParties)
				{
					mobileParty.CheckPositionsForMapChangeAndUpdateIfNeeded();
					mobileParty.CheckAiForMapChangeAndUpdateIfNeeded();
				}
				this._campaignMapSceneXmlCrc = sceneXmlCrc;
				this._campaignMapSceneNavigationMeshCrc = sceneNavigationMeshCrc;
			}
		}

		// Token: 0x06000303 RID: 771 RVA: 0x00015778 File Offset: 0x00013978
		private void CalculateCachedValues()
		{
			this.EstimatedMaximumLordPartySpeedExceptPlayer = 10f;
			this.EstimatedAverageLordPartySpeed = 3.36f;
			this.EstimatedAverageCaravanPartySpeed = 4.2f;
			this.EstimatedAverageVillagerPartySpeed = 3.43f;
			this.EstimatedAverageBanditPartySpeed = 3.41f;
			this.EstimatedAverageLordPartyNavalSpeed = this.EstimatedAverageLordPartySpeed * 1.2f;
			this.EstimatedAverageCaravanPartyNavalSpeed = 3.53f;
			this.EstimatedAverageVillagerPartyNavalSpeed = 4.01f;
			this.EstimatedAverageBanditPartyNavalSpeed = 3.57f;
			this.CalculateAverageDistanceBetweenTowns();
			this.CalculateAverageWage();
		}

		// Token: 0x06000304 RID: 772 RVA: 0x000157FC File Offset: 0x000139FC
		private void CalculateAverageWage()
		{
			float num = 0f;
			float num2 = 0f;
			foreach (CultureObject cultureObject in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
			{
				if (cultureObject.IsMainCulture)
				{
					foreach (PartyTemplateStack partyTemplateStack in cultureObject.DefaultPartyTemplate.Stacks)
					{
						int troopWage = partyTemplateStack.Character.TroopWage;
						float num3 = (float)(partyTemplateStack.MaxValue + partyTemplateStack.MinValue) * 0.5f;
						num += (float)troopWage * num3;
						num2 += num3;
					}
				}
			}
			if (num2 > 0f)
			{
				this.AverageWage = num / num2;
			}
		}

		// Token: 0x06000305 RID: 773 RVA: 0x000158EC File Offset: 0x00013AEC
		private void CalculateAverageDistanceBetweenTowns()
		{
			this._averageDistanceBetweenClosestTwoTowns = new Dictionary<MobileParty.NavigationType, float>();
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			int num4 = 0;
			foreach (Town town in this.AllTowns)
			{
				float num5 = float.MaxValue;
				float num6 = float.MaxValue;
				float num7 = float.MaxValue;
				foreach (Town town2 in this.AllTowns)
				{
					if (town != town2)
					{
						float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement, false, false, MobileParty.NavigationType.Default);
						if (distance < Campaign.MapDiagonal && distance < num6)
						{
							num6 = distance;
						}
						if (town.Settlement.HasPort && town2.Settlement.HasPort)
						{
							float distance2 = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement, true, true, MobileParty.NavigationType.Naval);
							if (distance2 < Campaign.MapDiagonal && distance2 < num7)
							{
								num7 = distance2;
							}
						}
						float num8 = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement, false, false, MobileParty.NavigationType.All);
						if (town.Settlement.HasPort)
						{
							float distance3 = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement, true, false, MobileParty.NavigationType.All);
							if (distance3 < Campaign.MapDiagonal && distance3 < num8)
							{
								num8 = distance3;
							}
						}
						if (town2.Settlement.HasPort)
						{
							float distance4 = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement, false, true, MobileParty.NavigationType.All);
							if (distance4 < Campaign.MapDiagonal && distance4 < num8)
							{
								num8 = distance4;
							}
						}
						if (town.Settlement.HasPort && town2.Settlement.HasPort)
						{
							float distance5 = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement, true, true, MobileParty.NavigationType.All);
							if (distance5 < Campaign.MapDiagonal && distance5 < num8)
							{
								num8 = distance5;
							}
						}
						if (num8 < num5)
						{
							num5 = num8;
						}
					}
				}
				if (num5 < Campaign.MapDiagonal)
				{
					num += num5;
				}
				if (num7 < Campaign.MapDiagonal)
				{
					num2 += num7;
				}
				if (num6 < Campaign.MapDiagonal)
				{
					num3 += num6;
				}
				num4++;
			}
			this._averageDistanceBetweenClosestTwoTowns.Add(MobileParty.NavigationType.Default, num3 / (float)num4);
			this._averageDistanceBetweenClosestTwoTowns.Add(MobileParty.NavigationType.Naval, num2 / (float)num4);
			this._averageDistanceBetweenClosestTwoTowns.Add(MobileParty.NavigationType.All, num / (float)num4);
		}

		// Token: 0x06000306 RID: 774 RVA: 0x00015BE0 File Offset: 0x00013DE0
		public void InitializeGamePlayReferences()
		{
			base.CurrentGame.PlayerTroop = base.CurrentGame.ObjectManager.GetObject<CharacterObject>("main_hero");
			if (Hero.MainHero.Mother != null)
			{
				Hero.MainHero.Mother.SetHasMet();
			}
			if (Hero.MainHero.Father != null)
			{
				Hero.MainHero.Father.SetHasMet();
			}
			this.PlayerDefaultFaction = this.CampaignObjectManager.Find<Clan>("player_faction");
			GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 1000, true);
			Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
		}

		// Token: 0x06000307 RID: 775 RVA: 0x00015C78 File Offset: 0x00013E78
		private void InitializeScenes()
		{
			foreach (ModuleInfo moduleInfo in ModuleHelper.GetActiveModules())
			{
				string str = ModuleHelper.GetModuleFullPath(moduleInfo.Id) + "ModuleData/";
				string path = str + "sp_battle_scenes.xml";
				string path2 = str + "conversation_scenes.xml";
				string path3 = str + "meeting_scenes.xml";
				if (File.Exists(path))
				{
					GameSceneDataManager.Instance.LoadSPBattleScenes(path);
				}
				if (File.Exists(path2))
				{
					GameSceneDataManager.Instance.LoadConversationScenes(path2);
				}
				if (File.Exists(path3))
				{
					GameSceneDataManager.Instance.LoadMeetingScenes(path3);
				}
			}
		}

		// Token: 0x06000308 RID: 776 RVA: 0x00015D34 File Offset: 0x00013F34
		public void SetLoadingParameters(Campaign.GameLoadingType gameLoadingType)
		{
			Campaign.Current = this;
			this._gameLoadingType = gameLoadingType;
			if (gameLoadingType == Campaign.GameLoadingType.SavedCampaign)
			{
				Campaign.Current.GameStarted = true;
			}
		}

		// Token: 0x06000309 RID: 777 RVA: 0x00015D52 File Offset: 0x00013F52
		public void AddCampaignEventReceiver(CampaignEventReceiver receiver)
		{
			this.CampaignEventDispatcher.AddCampaignEventReceiver(receiver);
		}

		// Token: 0x0600030A RID: 778 RVA: 0x00015D60 File Offset: 0x00013F60
		protected override void OnInitialize()
		{
			this.CampaignEvents = new CampaignEvents();
			this.CustomPeriodicCampaignEvents = new List<MBCampaignEvent>();
			this.CampaignEventDispatcher = new CampaignEventDispatcher(new CampaignEventReceiver[] { this.CampaignEvents, this.IssueManager, this.QuestManager });
			this.SandBoxManager = Game.Current.AddGameHandler<SandBoxManager>();
			this.SaveHandler = new SaveHandler();
			this.VisualCreator = new VisualCreator();
			this.GameMenuManager = new GameMenuManager();
			this._towns = new MBList<Town>();
			this._castles = new MBList<Town>();
			this._villages = new MBList<Village>();
			this._hideouts = new MBList<Hideout>();
			if (this._gameLoadingType != Campaign.GameLoadingType.Editor)
			{
				this.CreateManagers();
			}
			CampaignGameStarter campaignGameStarter = new CampaignGameStarter(this.GameMenuManager, this.ConversationManager);
			this.SandBoxManager.Initialize(campaignGameStarter);
			base.GameManager.InitializeGameStarter(base.CurrentGame, campaignGameStarter);
			GameSceneDataManager.Initialize();
			if (this._gameLoadingType == Campaign.GameLoadingType.NewCampaign || this._gameLoadingType == Campaign.GameLoadingType.SavedCampaign)
			{
				this.InitializeScenes();
			}
			base.GameManager.OnGameStart(base.CurrentGame, campaignGameStarter);
			base.CurrentGame.SetBasicModels(campaignGameStarter.Models);
			this._gameModels = base.CurrentGame.AddGameModelsManager<GameModels>(campaignGameStarter.Models);
			CampaignTime.Initialize();
			base.CurrentGame.CreateGameManager();
			if (this._gameLoadingType == Campaign.GameLoadingType.SavedCampaign)
			{
				this.InitializeDefaultCampaignObjects();
			}
			else
			{
				this.MapTimeTracker = new MapTimeTracker(this.Models.CampaignTimeModel.CampaignStartTime);
			}
			base.GameManager.BeginGameStart(base.CurrentGame);
			if (this._gameLoadingType != Campaign.GameLoadingType.SavedCampaign)
			{
				this.OnNewCampaignStart();
			}
			this.CreateLists();
			this.InitializeBasicObjectXmls();
			if (this._gameLoadingType != Campaign.GameLoadingType.SavedCampaign)
			{
				base.GameManager.OnNewCampaignStart(base.CurrentGame, campaignGameStarter);
			}
			this.SandBoxManager.OnCampaignStart(campaignGameStarter, base.GameManager, this._gameLoadingType == Campaign.GameLoadingType.SavedCampaign);
			if (this._gameLoadingType == Campaign.GameLoadingType.NewCampaign || this._gameLoadingType == Campaign.GameLoadingType.SavedCampaign)
			{
				this.DetermineSavedStats(this._gameLoadingType);
			}
			if (this._gameLoadingType != Campaign.GameLoadingType.SavedCampaign)
			{
				this.AddCampaignBehaviorManager(new CampaignBehaviorManager(campaignGameStarter.CampaignBehaviors));
				base.GameManager.OnAfterCampaignStart(base.CurrentGame);
			}
			else
			{
				base.GameManager.OnGameLoaded(base.CurrentGame, campaignGameStarter);
				this._campaignBehaviorManager.InitializeCampaignBehaviors(campaignGameStarter.CampaignBehaviors);
				this._campaignBehaviorManager.LoadBehaviorData();
				this._campaignBehaviorManager.RegisterEvents();
			}
			foreach (INonReadyObjectHandler nonReadyObjectHandler in this.GetCampaignBehaviors<INonReadyObjectHandler>())
			{
				nonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted();
			}
			if (this._gameLoadingType != Campaign.GameLoadingType.Tutorial)
			{
				campaignGameStarter.UnregisterNonReadyObjects();
			}
			if (this._gameLoadingType == Campaign.GameLoadingType.SavedCampaign)
			{
				this.InitializeCampaignObjectsOnAfterLoad();
			}
			else if (this._gameLoadingType == Campaign.GameLoadingType.NewCampaign || this._gameLoadingType == Campaign.GameLoadingType.Tutorial)
			{
				this.CampaignObjectManager.InitializeOnNewGame();
			}
			this.InitializeCachedLists();
			this.InitializeDefaultEquipments();
			this.NameGenerator.Initialize();
			base.CurrentGame.OnGameStart();
			base.GameManager.OnGameInitializationFinished(base.CurrentGame);
		}

		// Token: 0x0600030B RID: 779 RVA: 0x00016070 File Offset: 0x00014270
		private void CalculateCachedStatsOnLoad()
		{
			ItemRoster.CalculateCachedStatsOnLoad();
		}

		// Token: 0x0600030C RID: 780 RVA: 0x00016077 File Offset: 0x00014277
		private void InitializeBasicObjectXmls()
		{
			base.ObjectManager.LoadXML("SPCultures", false);
			base.ObjectManager.LoadXML("Concepts", false);
		}

		// Token: 0x0600030D RID: 781 RVA: 0x0001609C File Offset: 0x0001429C
		private void InitializeDefaultCampaignObjects()
		{
			base.CurrentGame.InitializeDefaultGameObjects();
			this.DefaultItems = new DefaultItems();
			base.CurrentGame.LoadBasicFiles();
			base.ObjectManager.LoadXML("Items", false);
			base.ObjectManager.LoadXML("EquipmentRosters", false);
			base.ObjectManager.LoadXML("partyTemplates", false);
			WeaponDescription @object = MBObjectManager.Instance.GetObject<WeaponDescription>("OneHandedBastardSwordAlternative");
			if (@object != null)
			{
				@object.IsHiddenFromUI = true;
			}
			WeaponDescription object2 = MBObjectManager.Instance.GetObject<WeaponDescription>("OneHandedBastardAxeAlternative");
			if (object2 != null)
			{
				object2.IsHiddenFromUI = true;
			}
			this.DefaultIssueEffects = new DefaultIssueEffects();
			this.DefaultTraits = new DefaultTraits();
			this.DefaultPolicies = new DefaultPolicies();
			this.DefaultPerks = new DefaultPerks();
			this.DefaultBuildingTypes = new DefaultBuildingTypes();
			this.DefaultVillageTypes = new DefaultVillageTypes();
			this.DefaultSiegeStrategies = new DefaultSiegeStrategies();
			this.DefaultSkillEffects = new DefaultSkillEffects();
			this.DefaultFeats = new DefaultCulturalFeats();
			this.DefaultFigureheads = new DefaultFigureheads();
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0001619F File Offset: 0x0001439F
		private void InitializeManagers()
		{
			this.KingdomManager = new KingdomManager();
			this.CampaignInformationManager = new CampaignInformationManager();
			this.VisualTrackerManager = new VisualTrackerManager();
			this.TournamentManager = new TournamentManager();
		}

		// Token: 0x0600030F RID: 783 RVA: 0x000161D0 File Offset: 0x000143D0
		private void InitializeCampaignObjectsOnAfterLoad()
		{
			this.CampaignObjectManager.InitializeOnLoad();
			this.FactionManager.AfterLoad();
			List<PerkObject> collection = (from x in this.AllPerks
				where !x.IsTrash
				select x).ToList<PerkObject>();
			this.AllPerks = new MBReadOnlyList<PerkObject>(collection);
			this.LogEntryHistory.OnAfterLoad();
			foreach (Kingdom kingdom in this.Kingdoms)
			{
				foreach (Army army in kingdom.Armies)
				{
					army.OnAfterLoad();
				}
			}
		}

		// Token: 0x06000310 RID: 784 RVA: 0x000162B8 File Offset: 0x000144B8
		private void OnNewCampaignStart()
		{
			Game.Current.PlayerTroop = null;
			this.MapStateData = new MapStateData();
			this.InitializeDefaultCampaignObjects();
			this.MainParty = MBObjectManager.Instance.CreateObject<MobileParty>("player_party");
			this.InitializeManagers();
		}

		// Token: 0x06000311 RID: 785 RVA: 0x000162F1 File Offset: 0x000144F1
		protected override void BeforeRegisterTypes(MBObjectManager objectManager)
		{
			objectManager.RegisterType<FeatObject>("feat", "Feats", 0U, true, false);
		}

		// Token: 0x06000312 RID: 786 RVA: 0x00016308 File Offset: 0x00014508
		protected override void OnRegisterTypes(MBObjectManager objectManager)
		{
			objectManager.RegisterType<MobileParty>("MobileParty", "MobileParties", 14U, true, true);
			objectManager.RegisterType<CharacterObject>("NPCCharacter", "NPCCharacters", 16U, true, false);
			if (this.GameMode == CampaignGameMode.Tutorial)
			{
				objectManager.RegisterType<BasicCharacterObject>("NPCCharacter", "MPCharacters", 43U, true, false);
			}
			objectManager.RegisterType<CultureObject>("Culture", "SPCultures", 17U, true, false);
			objectManager.RegisterType<Clan>("Faction", "Factions", 18U, true, true);
			objectManager.RegisterType<PerkObject>("Perk", "Perks", 19U, true, false);
			objectManager.RegisterType<Kingdom>("Kingdom", "Kingdoms", 20U, true, true);
			objectManager.RegisterType<TraitObject>("Trait", "Traits", 21U, true, false);
			objectManager.RegisterType<VillageType>("VillageType", "VillageTypes", 22U, true, false);
			objectManager.RegisterType<BuildingType>("BuildingType", "BuildingTypes", 23U, true, false);
			objectManager.RegisterType<PartyTemplateObject>("PartyTemplate", "partyTemplates", 24U, true, false);
			objectManager.RegisterType<Settlement>("Settlement", "Settlements", 25U, true, false);
			objectManager.RegisterType<WorkshopType>("WorkshopType", "WorkshopTypes", 26U, true, false);
			objectManager.RegisterType<Village>("Village", "Components", 27U, true, false);
			objectManager.RegisterType<Hideout>("Hideout", "Components", 30U, true, false);
			objectManager.RegisterType<Town>("Town", "Components", 31U, true, false);
			objectManager.RegisterType<Hero>("Hero", "Heroes", 32U, true, true);
			objectManager.RegisterType<MenuContext>("MenuContext", "MenuContexts", 35U, true, false);
			objectManager.RegisterType<PolicyObject>("Policy", "Policies", 36U, true, false);
			objectManager.RegisterType<Concept>("Concept", "Concepts", 37U, true, false);
			objectManager.RegisterType<IssueEffect>("IssueEffect", "IssueEffects", 39U, true, false);
			objectManager.RegisterType<SiegeStrategy>("SiegeStrategy", "SiegeStrategies", 40U, true, false);
			objectManager.RegisterType<SkillEffect>("SkillEffect", "SkillEffects", 53U, true, false);
			objectManager.RegisterType<LocationComplexTemplate>("LocationComplexTemplate", "LocationComplexTemplates", 42U, true, false);
			objectManager.RegisterType<RetirementSettlementComponent>("RetirementSettlementComponent", "Components", 56U, true, false);
			objectManager.RegisterType<MissionShipObject>("MissionShip", "MissionShips", 57U, true, false);
			objectManager.RegisterType<ShipHull>("ShipHull", "ShipHulls", 58U, true, false);
			objectManager.RegisterType<ShipSlot>("ShipSlot", "ShipSlots", 59U, true, false);
			objectManager.RegisterType<ShipUpgradePiece>("ShipUpgradePiece", "ShipUpgradePieces", 60U, true, false);
			objectManager.RegisterType<Incident>("Incident", "Incidents", 62U, true, false);
			objectManager.RegisterType<Figurehead>("Figurehead", "Figureheads", 63U, true, false);
			objectManager.RegisterType<ShipPhysicsReference>("ShipPhysicsReference", "ShipPhysicsReferences", 64U, true, false);
		}

		// Token: 0x06000313 RID: 787 RVA: 0x0001659E File Offset: 0x0001479E
		private void CreateManagers()
		{
			this.EncyclopediaManager = new EncyclopediaManager();
			this.ConversationManager = new ConversationManager();
			this.NameGenerator = new NameGenerator();
			this.SkillLevelingManager = new DefaultSkillLevelingManager();
		}

		// Token: 0x06000314 RID: 788 RVA: 0x000165CC File Offset: 0x000147CC
		private void OnNewGameCreated(CampaignGameStarter gameStarter)
		{
			this.OnNewGameCreatedInternal();
			GameManagerBase gameManager = base.GameManager;
			if (gameManager != null)
			{
				gameManager.OnNewGameCreated(base.CurrentGame, gameStarter);
			}
			CampaignEventDispatcher.Instance.OnNewGameCreated(gameStarter);
			this.OnAfterNewGameCreatedInternal();
		}

		// Token: 0x06000315 RID: 789 RVA: 0x00016600 File Offset: 0x00014800
		private void OnNewGameCreatedInternal()
		{
			this.UniqueGameId = MiscHelper.GenerateCampaignId(12);
			this._newGameVersion = MBSaveLoad.CurrentVersion.ToString();
			this.PlatformID = ApplicationPlatform.CurrentPlatform.ToString();
			this.PlayerTraitDeveloper = new PropertyOwner<PropertyObject>();
			TraitLevelingHelper.UpdateTraitXPAccordingToTraitLevels();
			this.TimeControlMode = CampaignTimeControlMode.Stop;
			this._campaignEntitySystem = new EntitySystem<CampaignEntityComponent>();
			this.SiegeEventManager = new SiegeEventManager();
			this.MapEventManager = new MapEventManager();
			this.MapMarkerManager = new MapMarkerManager();
			this.MinSettlementX = float.MaxValue;
			this.MinSettlementY = float.MaxValue;
			this.MaxSettlementX = float.MinValue;
			this.MaxSettlementY = float.MinValue;
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.Position.X < this.MinSettlementX)
				{
					this.MinSettlementX = settlement.Position.X;
				}
				if (settlement.Position.Y < this.MinSettlementY)
				{
					this.MinSettlementY = settlement.Position.Y;
				}
				if (settlement.Position.X > this.MaxSettlementX)
				{
					this.MaxSettlementX = settlement.Position.X;
				}
				if (settlement.Position.Y > this.MaxSettlementY)
				{
					this.MaxSettlementY = settlement.Position.Y;
				}
			}
			this.CampaignBehaviorManager.RegisterEvents();
			this.CameraFollowParty = this.MainParty.Party;
		}

		// Token: 0x06000316 RID: 790 RVA: 0x000167CC File Offset: 0x000149CC
		private void OnAfterNewGameCreatedInternal()
		{
			Hero.MainHero.Gold = 1000;
			if (Clan.PlayerClan.Influence != 0f)
			{
				ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -Clan.PlayerClan.Influence);
			}
			Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
			this.GameInitTick();
			this._playerFormationPreferences = new Dictionary<CharacterObject, FormationClass>();
			this.PlayerFormationPreferences = this._playerFormationPreferences.GetReadOnlyDictionary<CharacterObject, FormationClass>();
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0001683C File Offset: 0x00014A3C
		protected override void DoLoadingForGameType(GameTypeLoadingStates gameTypeLoadingState, out GameTypeLoadingStates nextState)
		{
			nextState = GameTypeLoadingStates.None;
			switch (gameTypeLoadingState)
			{
			case GameTypeLoadingStates.InitializeFirstStep:
				base.CurrentGame.Initialize();
				nextState = GameTypeLoadingStates.WaitSecondStep;
				return;
			case GameTypeLoadingStates.WaitSecondStep:
				nextState = GameTypeLoadingStates.LoadVisualsThirdState;
				return;
			case GameTypeLoadingStates.LoadVisualsThirdState:
				if (this.GameMode == CampaignGameMode.Campaign)
				{
					this.LoadMapScene();
				}
				nextState = GameTypeLoadingStates.PostInitializeFourthState;
				return;
			case GameTypeLoadingStates.PostInitializeFourthState:
			{
				CampaignGameStarter gameStarter = this.SandBoxManager.GameStarter;
				if (this._gameLoadingType == Campaign.GameLoadingType.SavedCampaign)
				{
					this.CheckMapUpdate();
					this.OnDataLoadFinished(gameStarter);
					this.CalculateCachedValues();
					this.CalculateCachedStatsOnLoad();
					base.GameManager.OnAfterGameLoaded(base.CurrentGame);
					this.OnGameLoaded(gameStarter);
					this.OnSessionStart(gameStarter);
					foreach (Hero hero in Hero.AllAliveHeroes)
					{
						hero.CheckInvalidEquipmentsAndReplaceIfNeeded();
					}
					using (List<Hero>.Enumerator enumerator = Hero.DeadOrDisabledHeroes.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							Hero hero2 = enumerator.Current;
							hero2.CheckInvalidEquipmentsAndReplaceIfNeeded();
						}
						goto IL_19D;
					}
				}
				if (this._gameLoadingType == Campaign.GameLoadingType.NewCampaign)
				{
					this._campaignMapSceneXmlCrc = this.MapSceneWrapper.GetSceneXmlCrc();
					this._campaignMapSceneNavigationMeshCrc = this.MapSceneWrapper.GetSceneNavigationMeshCrc();
					this.OnDataLoadFinished(gameStarter);
					this.CalculateCachedValues();
					MBSaveLoad.OnNewGame();
					this.InitializeMainParty();
					foreach (Settlement settlement in Settlement.All)
					{
						settlement.OnGameCreated();
					}
					MBObjectManager.Instance.RemoveTemporaryTypes();
					this.OnNewGameCreated(gameStarter);
					this.OnSessionStart(gameStarter);
					Debug.Print("Finished starting a new game.", 0, Debug.DebugColor.White, 17592186044416UL);
				}
				IL_19D:
				base.GameManager.OnAfterGameInitializationFinished(base.CurrentGame, gameStarter);
				return;
			}
			default:
				return;
			}
		}

		// Token: 0x06000318 RID: 792 RVA: 0x00016A20 File Offset: 0x00014C20
		private void DetermineSavedStats(Campaign.GameLoadingType gameLoadingType)
		{
			if (this._previouslyUsedModules == null)
			{
				this._previouslyUsedModules = new MBList<string>();
			}
			if (this._usedGameVersions == null)
			{
				this._usedGameVersions = new MBList<string>();
			}
			string text = MBSaveLoad.CurrentVersion.ToString();
			string text2 = string.Join(MBSaveLoad.ModuleCodeSeperator.ToString(), from x in ModuleHelper.GetActiveModules()
				select x.Id + MBSaveLoad.ModuleVersionSeperator.ToString() + x.Version);
			if (this._usedGameVersions.Count <= 0 || this._usedGameVersions.Last<string>() != text)
			{
				this._usedGameVersions.Add(text);
			}
			if (this._previouslyUsedModules.LastOrDefault<string>() != text2)
			{
				this._previouslyUsedModules.Add(text2);
			}
		}

		// Token: 0x06000319 RID: 793 RVA: 0x00016AEF File Offset: 0x00014CEF
		public override void OnMissionIsStarting(string missionName, MissionInitializerRecord rec)
		{
			if (rec.PlayingInCampaignMode)
			{
				CampaignEventDispatcher.Instance.BeforeMissionOpened();
			}
		}

		// Token: 0x0600031A RID: 794 RVA: 0x00016B03 File Offset: 0x00014D03
		public override void InitializeParameters()
		{
			ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("Native", "managed_campaign_parameters"));
		}

		// Token: 0x0600031B RID: 795 RVA: 0x00016B1E File Offset: 0x00014D1E
		public void SetTimeControlModeLock(bool isLocked)
		{
			this.TimeControlModeLock = isLocked;
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x0600031C RID: 796 RVA: 0x00016B27 File Offset: 0x00014D27
		public override bool IsPartyWindowAccessibleAtMission
		{
			get
			{
				return this.GameMode == CampaignGameMode.Tutorial;
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x0600031D RID: 797 RVA: 0x00016B32 File Offset: 0x00014D32
		internal MBReadOnlyList<Town> AllTowns
		{
			get
			{
				return this._towns;
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x0600031E RID: 798 RVA: 0x00016B3A File Offset: 0x00014D3A
		internal MBReadOnlyList<Town> AllCastles
		{
			get
			{
				return this._castles;
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x0600031F RID: 799 RVA: 0x00016B42 File Offset: 0x00014D42
		internal MBReadOnlyList<Village> AllVillages
		{
			get
			{
				return this._villages;
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x06000320 RID: 800 RVA: 0x00016B4A File Offset: 0x00014D4A
		internal MBReadOnlyList<Hideout> AllHideouts
		{
			get
			{
				return this._hideouts;
			}
		}

		// Token: 0x06000321 RID: 801 RVA: 0x00016B54 File Offset: 0x00014D54
		public void OnPlayerCharacterChanged(out bool isMainPartyChanged)
		{
			isMainPartyChanged = false;
			if (MobileParty.MainParty != Hero.MainHero.PartyBelongedTo)
			{
				isMainPartyChanged = true;
			}
			this.MainParty = Hero.MainHero.PartyBelongedTo;
			if (Hero.MainHero.CurrentSettlement != null && !Hero.MainHero.IsPrisoner)
			{
				if (this.MainParty == null)
				{
					LeaveSettlementAction.ApplyForCharacterOnly(Hero.MainHero);
				}
				else
				{
					LeaveSettlementAction.ApplyForParty(this.MainParty);
				}
			}
			if (Hero.MainHero.IsFugitive)
			{
				Hero.MainHero.ChangeState(Hero.CharacterStates.Active);
			}
			this.PlayerTraitDeveloper = new PropertyOwner<PropertyObject>();
			TraitLevelingHelper.UpdateTraitXPAccordingToTraitLevels();
			if (this.MainParty == null)
			{
				this.MainParty = MobileParty.CreateParty("player_party_" + Hero.MainHero.StringId, null);
				LordPartyComponent.ConvertPartyToLordParty(this.MainParty, Hero.MainHero, Hero.MainHero);
				isMainPartyChanged = true;
				CampaignVec2 position;
				if (Hero.MainHero.IsPrisoner)
				{
					this.MainParty.RemovePartyLeader();
					PartyBase partyBelongedToAsPrisoner = Hero.MainHero.PartyBelongedToAsPrisoner;
					if (partyBelongedToAsPrisoner.IsMobile)
					{
						position = partyBelongedToAsPrisoner.MobileParty.Position;
					}
					else
					{
						position = partyBelongedToAsPrisoner.Settlement.GatePosition;
					}
					this.MainParty.IsActive = false;
				}
				else
				{
					CampaignVec2 campaignPosition = Hero.MainHero.GetCampaignPosition();
					position = ((campaignPosition.IsValid() && campaignPosition != CampaignVec2.Zero) ? campaignPosition : HeroHelper.FindASuitableSettlementToTeleportForHero(Hero.MainHero, 0f).GatePosition);
					this.MainParty.IsActive = true;
					this.MainParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1, true, 0, 0, true, -1);
				}
				this.MainParty.InitializeMobilePartyAtPosition(position);
			}
			PartyBase.MainParty.ItemRoster.UpdateVersion();
			PartyBase.MainParty.MemberRoster.UpdateVersion();
			PartyBase.MainParty.PrisonRoster.UpdateVersion();
			if (MobileParty.MainParty.IsActive)
			{
				PartyBase.MainParty.SetAsCameraFollowParty();
			}
			PartyBase.MainParty.UpdateVisibilityAndInspected(MobileParty.MainParty.Position, 0f);
			if (Hero.MainHero.Mother != null)
			{
				Hero.MainHero.Mother.SetHasMet();
			}
			if (Hero.MainHero.Father != null)
			{
				Hero.MainHero.Father.SetHasMet();
			}
			this.MainParty.SetWagePaymentLimit(Campaign.Current.Models.PartyWageModel.MaxWagePaymentLimit);
		}

		// Token: 0x06000322 RID: 802 RVA: 0x00016D9E File Offset: 0x00014F9E
		public void SetPlayerFormationPreference(CharacterObject character, FormationClass formation)
		{
			if (!this._playerFormationPreferences.ContainsKey(character))
			{
				this._playerFormationPreferences.Add(character, formation);
				return;
			}
			this._playerFormationPreferences[character] = formation;
		}

		// Token: 0x06000323 RID: 803 RVA: 0x00016DC9 File Offset: 0x00014FC9
		public override void OnStateChanged(GameState oldState)
		{
		}

		// Token: 0x06000324 RID: 804 RVA: 0x00016DCB File Offset: 0x00014FCB
		public void UnlockFigurehead(Figurehead figurehead)
		{
			this.UnlockedFigureheadsByMainHero.Add(figurehead);
			CampaignEventDispatcher.Instance.OnFigureheadUnlocked(figurehead);
		}

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x06000325 RID: 805 RVA: 0x00016DE4 File Offset: 0x00014FE4
		// (set) Token: 0x06000326 RID: 806 RVA: 0x00016DEC File Offset: 0x00014FEC
		[SaveableProperty(68)]
		public PropertyOwner<PropertyObject> PlayerTraitDeveloper { get; private set; }

		// Token: 0x06000327 RID: 807 RVA: 0x00016DF5 File Offset: 0x00014FF5
		internal static void AutoGeneratedStaticCollectObjectsCampaign(object o, List<object> collectedObjects)
		{
			((Campaign)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x06000328 RID: 808 RVA: 0x00016E04 File Offset: 0x00015004
		protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			base.AutoGeneratedInstanceCollectObjects(collectedObjects);
			collectedObjects.Add(this.Options);
			collectedObjects.Add(this.TournamentManager);
			collectedObjects.Add(this.UnlockedFigureheadsByMainHero);
			collectedObjects.Add(this.KingdomManager);
			collectedObjects.Add(this._campaignPeriodicEventManager);
			collectedObjects.Add(this._previouslyUsedModules);
			collectedObjects.Add(this._usedGameVersions);
			collectedObjects.Add(this._campaignBehaviorManager);
			collectedObjects.Add(this._customManagers);
			collectedObjects.Add(this._cameraFollowParty);
			collectedObjects.Add(this._logEntryHistory);
			collectedObjects.Add(this._playerFormationPreferences);
			collectedObjects.Add(this.CampaignObjectManager);
			collectedObjects.Add(this.QuestManager);
			collectedObjects.Add(this.IssueManager);
			collectedObjects.Add(this.FactionManager);
			collectedObjects.Add(this.CharacterRelationManager);
			collectedObjects.Add(this.Romance);
			collectedObjects.Add(this.PlayerCaptivity);
			collectedObjects.Add(this.PlayerDefaultFaction);
			collectedObjects.Add(this.MapStateData);
			collectedObjects.Add(this.MapTimeTracker);
			collectedObjects.Add(this.SiegeEventManager);
			collectedObjects.Add(this.MapEventManager);
			collectedObjects.Add(this.MapMarkerManager);
			collectedObjects.Add(this.PlayerEncounter);
			collectedObjects.Add(this.BarterManager);
			collectedObjects.Add(this.MainParty);
			collectedObjects.Add(this.CampaignInformationManager);
			collectedObjects.Add(this.VisualTrackerManager);
			collectedObjects.Add(this.PlayerTraitDeveloper);
		}

		// Token: 0x06000329 RID: 809 RVA: 0x00016F8C File Offset: 0x0001518C
		internal static object AutoGeneratedGetMemberValueEnabledCheatsBefore(object o)
		{
			return ((Campaign)o).EnabledCheatsBefore;
		}

		// Token: 0x0600032A RID: 810 RVA: 0x00016F9E File Offset: 0x0001519E
		internal static object AutoGeneratedGetMemberValuePlatformID(object o)
		{
			return ((Campaign)o).PlatformID;
		}

		// Token: 0x0600032B RID: 811 RVA: 0x00016FAB File Offset: 0x000151AB
		internal static object AutoGeneratedGetMemberValueUniqueGameId(object o)
		{
			return ((Campaign)o).UniqueGameId;
		}

		// Token: 0x0600032C RID: 812 RVA: 0x00016FB8 File Offset: 0x000151B8
		internal static object AutoGeneratedGetMemberValueCampaignObjectManager(object o)
		{
			return ((Campaign)o).CampaignObjectManager;
		}

		// Token: 0x0600032D RID: 813 RVA: 0x00016FC5 File Offset: 0x000151C5
		internal static object AutoGeneratedGetMemberValueIsCraftingEnabled(object o)
		{
			return ((Campaign)o).IsCraftingEnabled;
		}

		// Token: 0x0600032E RID: 814 RVA: 0x00016FD7 File Offset: 0x000151D7
		internal static object AutoGeneratedGetMemberValueIsBannerEditorEnabled(object o)
		{
			return ((Campaign)o).IsBannerEditorEnabled;
		}

		// Token: 0x0600032F RID: 815 RVA: 0x00016FE9 File Offset: 0x000151E9
		internal static object AutoGeneratedGetMemberValueIsFaceGenEnabled(object o)
		{
			return ((Campaign)o).IsFaceGenEnabled;
		}

		// Token: 0x06000330 RID: 816 RVA: 0x00016FFB File Offset: 0x000151FB
		internal static object AutoGeneratedGetMemberValueQuestManager(object o)
		{
			return ((Campaign)o).QuestManager;
		}

		// Token: 0x06000331 RID: 817 RVA: 0x00017008 File Offset: 0x00015208
		internal static object AutoGeneratedGetMemberValueIssueManager(object o)
		{
			return ((Campaign)o).IssueManager;
		}

		// Token: 0x06000332 RID: 818 RVA: 0x00017015 File Offset: 0x00015215
		internal static object AutoGeneratedGetMemberValueFactionManager(object o)
		{
			return ((Campaign)o).FactionManager;
		}

		// Token: 0x06000333 RID: 819 RVA: 0x00017022 File Offset: 0x00015222
		internal static object AutoGeneratedGetMemberValueCharacterRelationManager(object o)
		{
			return ((Campaign)o).CharacterRelationManager;
		}

		// Token: 0x06000334 RID: 820 RVA: 0x0001702F File Offset: 0x0001522F
		internal static object AutoGeneratedGetMemberValueRomance(object o)
		{
			return ((Campaign)o).Romance;
		}

		// Token: 0x06000335 RID: 821 RVA: 0x0001703C File Offset: 0x0001523C
		internal static object AutoGeneratedGetMemberValuePlayerCaptivity(object o)
		{
			return ((Campaign)o).PlayerCaptivity;
		}

		// Token: 0x06000336 RID: 822 RVA: 0x00017049 File Offset: 0x00015249
		internal static object AutoGeneratedGetMemberValuePlayerDefaultFaction(object o)
		{
			return ((Campaign)o).PlayerDefaultFaction;
		}

		// Token: 0x06000337 RID: 823 RVA: 0x00017056 File Offset: 0x00015256
		internal static object AutoGeneratedGetMemberValueMapStateData(object o)
		{
			return ((Campaign)o).MapStateData;
		}

		// Token: 0x06000338 RID: 824 RVA: 0x00017063 File Offset: 0x00015263
		internal static object AutoGeneratedGetMemberValueMapTimeTracker(object o)
		{
			return ((Campaign)o).MapTimeTracker;
		}

		// Token: 0x06000339 RID: 825 RVA: 0x00017070 File Offset: 0x00015270
		internal static object AutoGeneratedGetMemberValueGameMode(object o)
		{
			return ((Campaign)o).GameMode;
		}

		// Token: 0x0600033A RID: 826 RVA: 0x00017082 File Offset: 0x00015282
		internal static object AutoGeneratedGetMemberValuePlayerProgress(object o)
		{
			return ((Campaign)o).PlayerProgress;
		}

		// Token: 0x0600033B RID: 827 RVA: 0x00017094 File Offset: 0x00015294
		internal static object AutoGeneratedGetMemberValueSiegeEventManager(object o)
		{
			return ((Campaign)o).SiegeEventManager;
		}

		// Token: 0x0600033C RID: 828 RVA: 0x000170A1 File Offset: 0x000152A1
		internal static object AutoGeneratedGetMemberValueMapEventManager(object o)
		{
			return ((Campaign)o).MapEventManager;
		}

		// Token: 0x0600033D RID: 829 RVA: 0x000170AE File Offset: 0x000152AE
		internal static object AutoGeneratedGetMemberValueMapMarkerManager(object o)
		{
			return ((Campaign)o).MapMarkerManager;
		}

		// Token: 0x0600033E RID: 830 RVA: 0x000170BB File Offset: 0x000152BB
		internal static object AutoGeneratedGetMemberValue_curMapFrame(object o)
		{
			return ((Campaign)o)._curMapFrame;
		}

		// Token: 0x0600033F RID: 831 RVA: 0x000170CD File Offset: 0x000152CD
		internal static object AutoGeneratedGetMemberValuePlayerEncounter(object o)
		{
			return ((Campaign)o).PlayerEncounter;
		}

		// Token: 0x06000340 RID: 832 RVA: 0x000170DA File Offset: 0x000152DA
		internal static object AutoGeneratedGetMemberValueBarterManager(object o)
		{
			return ((Campaign)o).BarterManager;
		}

		// Token: 0x06000341 RID: 833 RVA: 0x000170E7 File Offset: 0x000152E7
		internal static object AutoGeneratedGetMemberValueIsMainHeroDisguised(object o)
		{
			return ((Campaign)o).IsMainHeroDisguised;
		}

		// Token: 0x06000342 RID: 834 RVA: 0x000170F9 File Offset: 0x000152F9
		internal static object AutoGeneratedGetMemberValueMainParty(object o)
		{
			return ((Campaign)o).MainParty;
		}

		// Token: 0x06000343 RID: 835 RVA: 0x00017106 File Offset: 0x00015306
		internal static object AutoGeneratedGetMemberValueCampaignInformationManager(object o)
		{
			return ((Campaign)o).CampaignInformationManager;
		}

		// Token: 0x06000344 RID: 836 RVA: 0x00017113 File Offset: 0x00015313
		internal static object AutoGeneratedGetMemberValueVisualTrackerManager(object o)
		{
			return ((Campaign)o).VisualTrackerManager;
		}

		// Token: 0x06000345 RID: 837 RVA: 0x00017120 File Offset: 0x00015320
		internal static object AutoGeneratedGetMemberValuePlayerTraitDeveloper(object o)
		{
			return ((Campaign)o).PlayerTraitDeveloper;
		}

		// Token: 0x06000346 RID: 838 RVA: 0x0001712D File Offset: 0x0001532D
		internal static object AutoGeneratedGetMemberValueOptions(object o)
		{
			return ((Campaign)o).Options;
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0001713A File Offset: 0x0001533A
		internal static object AutoGeneratedGetMemberValueTournamentManager(object o)
		{
			return ((Campaign)o).TournamentManager;
		}

		// Token: 0x06000348 RID: 840 RVA: 0x00017147 File Offset: 0x00015347
		internal static object AutoGeneratedGetMemberValueIsSinglePlayerReferencesInitialized(object o)
		{
			return ((Campaign)o).IsSinglePlayerReferencesInitialized;
		}

		// Token: 0x06000349 RID: 841 RVA: 0x00017159 File Offset: 0x00015359
		internal static object AutoGeneratedGetMemberValueLastTimeControlMode(object o)
		{
			return ((Campaign)o).LastTimeControlMode;
		}

		// Token: 0x0600034A RID: 842 RVA: 0x0001716B File Offset: 0x0001536B
		internal static object AutoGeneratedGetMemberValueMainHeroIllDays(object o)
		{
			return ((Campaign)o).MainHeroIllDays;
		}

		// Token: 0x0600034B RID: 843 RVA: 0x0001717D File Offset: 0x0001537D
		internal static object AutoGeneratedGetMemberValueUnlockedFigureheadsByMainHero(object o)
		{
			return ((Campaign)o).UnlockedFigureheadsByMainHero;
		}

		// Token: 0x0600034C RID: 844 RVA: 0x0001718A File Offset: 0x0001538A
		internal static object AutoGeneratedGetMemberValueKingdomManager(object o)
		{
			return ((Campaign)o).KingdomManager;
		}

		// Token: 0x0600034D RID: 845 RVA: 0x00017197 File Offset: 0x00015397
		internal static object AutoGeneratedGetMemberValue_campaignPeriodicEventManager(object o)
		{
			return ((Campaign)o)._campaignPeriodicEventManager;
		}

		// Token: 0x0600034E RID: 846 RVA: 0x000171A4 File Offset: 0x000153A4
		internal static object AutoGeneratedGetMemberValue_isMainPartyWaiting(object o)
		{
			return ((Campaign)o)._isMainPartyWaiting;
		}

		// Token: 0x0600034F RID: 847 RVA: 0x000171B6 File Offset: 0x000153B6
		internal static object AutoGeneratedGetMemberValue_newGameVersion(object o)
		{
			return ((Campaign)o)._newGameVersion;
		}

		// Token: 0x06000350 RID: 848 RVA: 0x000171C3 File Offset: 0x000153C3
		internal static object AutoGeneratedGetMemberValue_previouslyUsedModules(object o)
		{
			return ((Campaign)o)._previouslyUsedModules;
		}

		// Token: 0x06000351 RID: 849 RVA: 0x000171D0 File Offset: 0x000153D0
		internal static object AutoGeneratedGetMemberValue_campaignMapSceneXmlCrc(object o)
		{
			return ((Campaign)o)._campaignMapSceneXmlCrc;
		}

		// Token: 0x06000352 RID: 850 RVA: 0x000171E2 File Offset: 0x000153E2
		internal static object AutoGeneratedGetMemberValue_campaignMapSceneNavigationMeshCrc(object o)
		{
			return ((Campaign)o)._campaignMapSceneNavigationMeshCrc;
		}

		// Token: 0x06000353 RID: 851 RVA: 0x000171F4 File Offset: 0x000153F4
		internal static object AutoGeneratedGetMemberValue_usedGameVersions(object o)
		{
			return ((Campaign)o)._usedGameVersions;
		}

		// Token: 0x06000354 RID: 852 RVA: 0x00017201 File Offset: 0x00015401
		internal static object AutoGeneratedGetMemberValue_campaignBehaviorManager(object o)
		{
			return ((Campaign)o)._campaignBehaviorManager;
		}

		// Token: 0x06000355 RID: 853 RVA: 0x0001720E File Offset: 0x0001540E
		internal static object AutoGeneratedGetMemberValue_customManagers(object o)
		{
			return ((Campaign)o)._customManagers;
		}

		// Token: 0x06000356 RID: 854 RVA: 0x0001721B File Offset: 0x0001541B
		internal static object AutoGeneratedGetMemberValue_lastPartyIndex(object o)
		{
			return ((Campaign)o)._lastPartyIndex;
		}

		// Token: 0x06000357 RID: 855 RVA: 0x0001722D File Offset: 0x0001542D
		internal static object AutoGeneratedGetMemberValue_cameraFollowParty(object o)
		{
			return ((Campaign)o)._cameraFollowParty;
		}

		// Token: 0x06000358 RID: 856 RVA: 0x0001723A File Offset: 0x0001543A
		internal static object AutoGeneratedGetMemberValue_logEntryHistory(object o)
		{
			return ((Campaign)o)._logEntryHistory;
		}

		// Token: 0x06000359 RID: 857 RVA: 0x00017247 File Offset: 0x00015447
		internal static object AutoGeneratedGetMemberValue_playerFormationPreferences(object o)
		{
			return ((Campaign)o)._playerFormationPreferences;
		}

		// Token: 0x04000037 RID: 55
		public const float ConfigTimeMultiplier = 0.25f;

		// Token: 0x04000038 RID: 56
		private EntitySystem<CampaignEntityComponent> _campaignEntitySystem;

		// Token: 0x0400003E RID: 62
		public static int PlayerRegionSwitchCostFromLandToSea;

		// Token: 0x0400003F RID: 63
		public static int PathFindingMaxCostLimit;

		// Token: 0x04000040 RID: 64
		public ITask CampaignLateAITickTask;

		// Token: 0x04000041 RID: 65
		[SaveableField(210)]
		private CampaignPeriodicEventManager _campaignPeriodicEventManager;

		// Token: 0x04000042 RID: 66
		private Dictionary<MobileParty.NavigationType, float> _averageDistanceBetweenClosestTwoTowns;

		// Token: 0x04000044 RID: 68
		[SaveableField(53)]
		private bool _isMainPartyWaiting;

		// Token: 0x04000045 RID: 69
		[SaveableField(344)]
		private string _newGameVersion;

		// Token: 0x04000046 RID: 70
		[SaveableField(78)]
		private MBList<string> _previouslyUsedModules;

		// Token: 0x04000047 RID: 71
		[SaveableField(85)]
		private uint _campaignMapSceneXmlCrc;

		// Token: 0x04000048 RID: 72
		[SaveableField(86)]
		private uint _campaignMapSceneNavigationMeshCrc;

		// Token: 0x04000049 RID: 73
		[SaveableField(81)]
		private MBList<string> _usedGameVersions;

		// Token: 0x0400004F RID: 79
		[SaveableField(7)]
		private ICampaignBehaviorManager _campaignBehaviorManager;

		// Token: 0x04000051 RID: 81
		private CampaignTickCacheDataStore _tickData;

		// Token: 0x04000052 RID: 82
		[SaveableField(2)]
		public readonly CampaignOptions Options;

		// Token: 0x04000053 RID: 83
		public MBReadOnlyDictionary<CharacterObject, FormationClass> PlayerFormationPreferences;

		// Token: 0x04000054 RID: 84
		[SaveableField(13)]
		public ITournamentManager TournamentManager;

		// Token: 0x04000055 RID: 85
		public float MinSettlementX;

		// Token: 0x04000056 RID: 86
		public float MaxSettlementX;

		// Token: 0x04000057 RID: 87
		public float MinSettlementY;

		// Token: 0x04000058 RID: 88
		public float MaxSettlementY;

		// Token: 0x04000059 RID: 89
		[SaveableField(27)]
		public bool IsSinglePlayerReferencesInitialized;

		// Token: 0x0400005A RID: 90
		private LocatorGrid<MobileParty> _mobilePartyLocator;

		// Token: 0x0400005B RID: 91
		private LocatorGrid<Settlement> _settlementLocator;

		// Token: 0x0400005C RID: 92
		private GameModels _gameModels;

		// Token: 0x0400005F RID: 95
		[SaveableField(31)]
		public CampaignTimeControlMode LastTimeControlMode = CampaignTimeControlMode.UnstoppablePlay;

		// Token: 0x04000060 RID: 96
		private IMapScene _mapSceneWrapper;

		// Token: 0x04000061 RID: 97
		public bool GameStarted;

		// Token: 0x04000063 RID: 99
		private Campaign.GameLoadingType _gameLoadingType;

		// Token: 0x04000064 RID: 100
		public ConversationContext CurrentConversationContext;

		// Token: 0x04000065 RID: 101
		[CachedData]
		private float _dt;

		// Token: 0x04000068 RID: 104
		private CampaignTimeControlMode _timeControlMode;

		// Token: 0x04000069 RID: 105
		public int CurrentTickCount;

		// Token: 0x04000098 RID: 152
		[SaveableField(30)]
		public int MainHeroIllDays = -1;

		// Token: 0x040000A5 RID: 165
		[SaveableField(42)]
		private List<ICustomSystemManager> _customManagers = new List<ICustomSystemManager>();

		// Token: 0x040000A9 RID: 169
		private MBCampaignEvent _dailyTickEvent;

		// Token: 0x040000AA RID: 170
		private MBCampaignEvent _hourlyTickEvent;

		// Token: 0x040000AB RID: 171
		private MBCampaignEvent _QuarterHourlyTickEvent;

		// Token: 0x040000AD RID: 173
		[CachedData]
		private int _lastNonZeroDtFrame;

		// Token: 0x040000AE RID: 174
		public int DefaultWeatherNodeDimension;

		// Token: 0x040000B7 RID: 183
		[SaveableField(333)]
		public List<Figurehead> UnlockedFigureheadsByMainHero = new List<Figurehead>();

		// Token: 0x040000B8 RID: 184
		private MBList<Town> _towns;

		// Token: 0x040000B9 RID: 185
		private MBList<Town> _castles;

		// Token: 0x040000BA RID: 186
		private MBList<Village> _villages;

		// Token: 0x040000BB RID: 187
		private MBList<Hideout> _hideouts;

		// Token: 0x040000BC RID: 188
		private MBReadOnlyList<CharacterObject> _characters;

		// Token: 0x040000BD RID: 189
		private MBReadOnlyList<WorkshopType> _workshops;

		// Token: 0x040000BE RID: 190
		private MBReadOnlyList<ItemModifier> _itemModifiers;

		// Token: 0x040000BF RID: 191
		private MBReadOnlyList<Concept> _concepts;

		// Token: 0x040000C0 RID: 192
		private MBReadOnlyList<ItemModifierGroup> _itemModifierGroups;

		// Token: 0x040000C1 RID: 193
		[SaveableField(79)]
		private int _lastPartyIndex;

		// Token: 0x040000C3 RID: 195
		[SaveableField(61)]
		private PartyBase _cameraFollowParty;

		// Token: 0x040000C6 RID: 198
		[SaveableField(64)]
		private readonly LogEntryHistory _logEntryHistory = new LogEntryHistory();

		// Token: 0x040000C9 RID: 201
		[SaveableField(65)]
		public KingdomManager KingdomManager;

		// Token: 0x040000CB RID: 203
		[SaveableField(77)]
		private Dictionary<CharacterObject, FormationClass> _playerFormationPreferences;

		// Token: 0x020004F5 RID: 1269
		[Flags]
		public enum PartyRestFlags : uint
		{
			// Token: 0x04001521 RID: 5409
			None = 0U,
			// Token: 0x04001522 RID: 5410
			SafeMode = 1U
		}

		// Token: 0x020004F6 RID: 1270
		public enum GameLoadingType
		{
			// Token: 0x04001524 RID: 5412
			Tutorial,
			// Token: 0x04001525 RID: 5413
			NewCampaign,
			// Token: 0x04001526 RID: 5414
			SavedCampaign,
			// Token: 0x04001527 RID: 5415
			Editor
		}
	}
}

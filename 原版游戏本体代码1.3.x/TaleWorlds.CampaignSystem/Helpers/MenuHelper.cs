using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x02000004 RID: 4
	public static class MenuHelper
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		public static bool SetOptionProperties(MenuCallbackArgs args, bool canPlayerDo, bool shouldBeDisabled, TextObject disabledText)
		{
			if (canPlayerDo)
			{
				return true;
			}
			if (!shouldBeDisabled)
			{
				return false;
			}
			args.IsEnabled = false;
			args.Tooltip = disabledText;
			return true;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002074 File Offset: 0x00000274
		public static void SetIssueAndQuestDataForHero(MenuCallbackArgs args, Hero hero)
		{
			if (hero.Issue != null && hero.Issue.IssueQuest == null)
			{
				args.OptionQuestData |= GameMenuOption.IssueQuestFlags.AvailableIssue;
			}
			List<QuestBase> list;
			Campaign.Current.QuestManager.TrackedObjects.TryGetValue(hero, out list);
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].IsTrackEnabled)
					{
						if (list[i].IsSpecialQuest)
						{
							if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedStoryQuest) == GameMenuOption.IssueQuestFlags.None && list[i].QuestGiver != hero)
							{
								args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedStoryQuest;
							}
							else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveStoryQuest) == GameMenuOption.IssueQuestFlags.None && list[i].QuestGiver == hero)
							{
								args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveStoryQuest;
							}
						}
						else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedIssue) == GameMenuOption.IssueQuestFlags.None && list[i].QuestGiver != hero)
						{
							args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedIssue;
						}
						else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == GameMenuOption.IssueQuestFlags.None && list[i].QuestGiver == hero)
						{
							args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
						}
					}
				}
			}
			if (hero.PartyBelongedTo != null && ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveStoryQuest) == GameMenuOption.IssueQuestFlags.None || (args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == GameMenuOption.IssueQuestFlags.None || (args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedIssue) == GameMenuOption.IssueQuestFlags.None || (args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedStoryQuest) == GameMenuOption.IssueQuestFlags.None))
			{
				List<QuestBase> list2;
				Campaign.Current.QuestManager.TrackedObjects.TryGetValue(hero.PartyBelongedTo, out list2);
				if (list2 != null)
				{
					for (int j = 0; j < list2.Count; j++)
					{
						if (list2[j].IsTrackEnabled)
						{
							if (list2[j].IsSpecialQuest)
							{
								if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedStoryQuest) == GameMenuOption.IssueQuestFlags.None && list2[j].QuestGiver != hero)
								{
									args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedStoryQuest;
								}
								else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveStoryQuest) == GameMenuOption.IssueQuestFlags.None && list2[j].QuestGiver == hero)
								{
									args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveStoryQuest;
								}
							}
							else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.TrackedIssue) == GameMenuOption.IssueQuestFlags.None && list2[j].QuestGiver != hero)
							{
								args.OptionQuestData |= GameMenuOption.IssueQuestFlags.TrackedIssue;
							}
							else if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == GameMenuOption.IssueQuestFlags.None && list2[j].QuestGiver == hero)
							{
								args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
							}
						}
					}
				}
			}
			if ((args.OptionQuestData & GameMenuOption.IssueQuestFlags.ActiveIssue) == GameMenuOption.IssueQuestFlags.None)
			{
				IssueBase issue = hero.Issue;
				if (((issue != null) ? issue.IssueQuest : null) != null && hero.Issue.IssueQuest.IsTrackEnabled)
				{
					args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
				}
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002310 File Offset: 0x00000510
		public static void SetIssueAndQuestDataForLocations(MenuCallbackArgs args, List<Location> locations)
		{
			GameMenuOption.IssueQuestFlags issueQuestFlags = Campaign.Current.IssueManager.CheckIssueForMenuLocations(locations, true);
			args.OptionQuestData |= issueQuestFlags;
			args.OptionQuestData |= Campaign.Current.QuestManager.CheckQuestForMenuLocations(locations);
		}

		// Token: 0x06000006 RID: 6 RVA: 0x0000235C File Offset: 0x0000055C
		public static bool CheckAndOpenNextLocation(MenuCallbackArgs args)
		{
			if (Campaign.Current.GameMenuManager.NextLocation != null && GameStateManager.Current.ActiveState is MapState)
			{
				PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation, Campaign.Current.GameMenuManager.PreviousLocation, null, null);
				string stringId = Campaign.Current.GameMenuManager.NextLocation.StringId;
				if (!(stringId == "center"))
				{
					if (!(stringId == "tavern"))
					{
						if (!(stringId == "arena"))
						{
							if (!(stringId == "lordshall") && !(stringId == "prison"))
							{
								if (stringId == "port")
								{
									Campaign.Current.GameMenuManager.SetNextMenu("port_menu");
								}
							}
							else
							{
								Campaign.Current.GameMenuManager.SetNextMenu("town_keep");
							}
						}
						else
						{
							Campaign.Current.GameMenuManager.SetNextMenu("town_arena");
						}
					}
					else
					{
						Campaign.Current.GameMenuManager.SetNextMenu("town_backstreet");
					}
				}
				else if (Settlement.CurrentSettlement.IsCastle)
				{
					Campaign.Current.GameMenuManager.SetNextMenu("castle");
				}
				else if (Settlement.CurrentSettlement.IsTown)
				{
					Campaign.Current.GameMenuManager.SetNextMenu("town");
				}
				else if (Settlement.CurrentSettlement.IsVillage)
				{
					Campaign.Current.GameMenuManager.SetNextMenu("village");
				}
				else
				{
					Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "CheckAndOpenNextLocation", 192);
				}
				Campaign.Current.GameMenuManager.NextLocation = null;
				Campaign.Current.GameMenuManager.PreviousLocation = null;
				return true;
			}
			return false;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002534 File Offset: 0x00000734
		public static void DecideMenuState()
		{
			string genericStateMenu = Campaign.Current.Models.EncounterGameMenuModel.GetGenericStateMenu();
			if (!string.IsNullOrEmpty(genericStateMenu))
			{
				GameMenu.SwitchToMenu(genericStateMenu);
				return;
			}
			GameMenu.ExitToLast();
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000256C File Offset: 0x0000076C
		public static bool EncounterAttackCondition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
			if (MapEvent.PlayerMapEvent == null)
			{
				return false;
			}
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			Settlement mapEventSettlement = playerMapEvent.MapEventSettlement;
			if (mapEventSettlement != null && mapEventSettlement.IsFortification && playerMapEvent.IsSiegeAssault && PlayerSiege.PlayerSiegeEvent != null && !PlayerSiege.PlayerSiegeEvent.BesiegerCamp.IsPreparationComplete)
			{
				return false;
			}
			bool flag = MapEvent.PlayerMapEvent.PartiesOnSide(PartyBase.MainParty.OpponentSide).Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0);
			if (Hero.MainHero.IsWounded)
			{
				args.Tooltip = new TextObject("{=UL8za0AO}You are wounded.", null);
				args.IsEnabled = false;
			}
			bool flag2 = (playerMapEvent.HasTroopsOnBothSides() || playerMapEvent.IsSiegeAssault) && MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide) != null;
			if (!MobileParty.MainParty.IsInRaftState)
			{
				MobileParty mobileParty = playerMapEvent.PartiesOnSide(PlayerEncounter.Current.OpponentSide)[0].Party.MobileParty;
				if (mobileParty == null || !mobileParty.IsInRaftState)
				{
					goto IL_125;
				}
			}
			args.Tooltip = new TextObject("{=x9ePfpw5}You are on a raft, in desperate circumstances, and cannot fight", null);
			args.IsEnabled = false;
			IL_125:
			if (flag && !flag2 && !Hero.MainHero.IsWounded)
			{
				Debug.FailedAssert("This encounter case should be investigated", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "EncounterAttackCondition", 275);
				return false;
			}
			if (flag && Game.Current.IsDevelopmentMode && (mapEventSettlement == null || playerMapEvent.IsBlockadeSallyOut || playerMapEvent.IsSallyOut || playerMapEvent.IsSiegeOutside || playerMapEvent.IsBlockade))
			{
				bool isNavalEncounter = PlayerEncounter.IsNavalEncounter();
				IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
				CampaignVec2 position = MobileParty.MainParty.Position;
				MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(position);
				string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, isNavalEncounter);
				args.Tooltip = new TextObject("{=!}[DEV] Scene: (" + battleSceneForMapPatch + ")", null);
			}
			return flag;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002768 File Offset: 0x00000968
		public static bool EncounterCaptureEnemyCondition(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
			MapEvent battle = PlayerEncounter.Battle;
			if (battle != null)
			{
				return battle.PartiesOnSide(battle.GetOtherSide(battle.PlayerSide)).All((MapEventParty party) => !party.Party.IsSettlement && (party.Party.NumberOfHealthyMembers == 0 || party.Party.MobileParty.IsInRaftState));
			}
			return false;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000027C0 File Offset: 0x000009C0
		public static void EncounterAttackConsequence(MenuCallbackArgs args)
		{
			MapEvent battle = PlayerEncounter.Battle;
			PartyBase leaderParty = battle.GetLeaderParty(PartyBase.MainParty.OpponentSide);
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, leaderParty);
			if (PlayerEncounter.Current != null)
			{
				Settlement mapEventSettlement = MobileParty.MainParty.MapEvent.MapEventSettlement;
				if (mapEventSettlement != null && !battle.IsBlockadeSallyOut && !battle.IsSallyOut && !battle.IsSiegeOutside && !battle.IsBlockade)
				{
					if (mapEventSettlement.IsFortification)
					{
						if (battle.IsRaid)
						{
							PlayerEncounter.StartVillageBattleMission();
						}
						else if (battle.IsSiegeAmbush)
						{
							PlayerEncounter.StartSiegeAmbushMission();
						}
						else if (battle.IsSiegeAssault)
						{
							if (PlayerSiege.PlayerSiegeEvent == null && PartyBase.MainParty.Side == BattleSideEnum.Attacker)
							{
								PlayerSiege.StartPlayerSiege(MobileParty.MainParty.Party.Side, false, mapEventSettlement);
							}
							else
							{
								if (PlayerSiege.PlayerSiegeEvent != null)
								{
									if (!PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide.GetOppositeSide()).GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Any((PartyBase party) => party.NumberOfHealthyMembers > 0))
									{
										PlayerEncounter.Update();
										return;
									}
								}
								if (PlayerSiege.BesiegedSettlement != null && PlayerSiege.BesiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
								{
									FlattenedTroopRoster priorityListForLordsHallFightMission = Campaign.Current.Models.SiegeLordsHallFightModel.GetPriorityListForLordsHallFightMission(MapEvent.PlayerMapEvent, BattleSideEnum.Defender, Campaign.Current.Models.SiegeLordsHallFightModel.MaxDefenderSideTroopCount);
									int num = MathF.Max(1, MathF.Min(Campaign.Current.Models.SiegeLordsHallFightModel.MaxAttackerSideTroopCount, MathF.Round((float)priorityListForLordsHallFightMission.Troops.Count<CharacterObject>() * Campaign.Current.Models.SiegeLordsHallFightModel.AttackerDefenderTroopCountRatio)));
									TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
									MobileParty mobileParty = ((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty);
									troopRoster.Add(mobileParty.MemberRoster);
									foreach (MobileParty mobileParty2 in mobileParty.AttachedParties)
									{
										troopRoster.Add(mobileParty2.MemberRoster);
									}
									TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
									FlattenedTroopRoster flattenedTroopRoster = troopRoster.ToFlattenedRoster();
									flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
									troopRoster2.Add(MobilePartyHelper.GetStrongestAndPriorTroops(flattenedTroopRoster, num, true));
									int minSelectableTroopCount = 1;
									args.MenuContext.OpenTroopSelection(troopRoster, troopRoster2, (CharacterObject character) => !character.IsPlayerCharacter, new Action<TroopRoster>(MenuHelper.LordsHallTroopRosterManageDone), num, minSelectableTroopCount);
								}
								else
								{
									PlayerSiege.StartSiegeMission(mapEventSettlement);
								}
							}
						}
					}
					else if (mapEventSettlement.IsVillage)
					{
						PlayerEncounter.StartVillageBattleMission();
					}
					else if (mapEventSettlement.IsHideout)
					{
						CampaignMission.OpenHideoutBattleMission("sea_bandit_a", null, false);
					}
				}
				else
				{
					bool flag = PlayerEncounter.IsNavalEncounter();
					IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
					CampaignVec2 position = MobileParty.MainParty.Position;
					MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(position);
					string battleSceneForMapPatch = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, flag);
					MissionInitializerRecord rec = new MissionInitializerRecord(battleSceneForMapPatch);
					TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
					rec.TerrainType = (int)faceTerrainType;
					rec.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
					rec.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
					rec.NeedsRandomTerrain = false;
					rec.PlayingInCampaignMode = true;
					rec.RandomTerrainSeed = MBRandom.RandomInt(10000);
					rec.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
					rec.SceneHasMapPatch = true;
					rec.DecalAtlasGroup = 2;
					rec.PatchCoordinates = mapPatchAtPosition.normalizedCoordinates;
					position = battle.AttackerSide.LeaderParty.Position;
					Vec2 v = position.ToVec2();
					position = battle.DefenderSide.LeaderParty.Position;
					rec.PatchEncounterDir = (v - position.ToVec2()).Normalized();
					bool flag2 = MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty involvedParty) => involvedParty.Party.IsMobile && (involvedParty.Party.MobileParty.IsCaravan || (involvedParty.Party.Owner != null && involvedParty.Party.Owner.IsMerchant)));
					bool flag3;
					if (MapEvent.PlayerMapEvent.MapEventSettlement == null)
					{
						flag3 = MapEvent.PlayerMapEvent.PartiesOnSide(BattleSideEnum.Defender).Any((MapEventParty involvedParty) => involvedParty.Party.IsMobile && involvedParty.Party.MobileParty.IsVillager);
					}
					else
					{
						flag3 = false;
					}
					bool flag4 = flag3;
					if (flag)
					{
						CampaignMission.OpenNavalBattleMission(rec);
					}
					else if (flag2 || flag4)
					{
						CampaignMission.OpenCaravanBattleMission(rec, flag2);
					}
					else
					{
						CampaignMission.OpenBattleMission(rec);
					}
				}
				PlayerEncounter.StartAttackMission();
				MapEvent.PlayerMapEvent.BeginWait();
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002CCC File Offset: 0x00000ECC
		private static void LordsHallTroopRosterManageDone(TroopRoster selectedTroops)
		{
			MapEvent.PlayerMapEvent.ResetBattleState();
			int wallLevel = PlayerSiege.BesiegedSettlement.Town.GetWallLevel();
			CampaignMission.OpenSiegeLordsHallFightMission(PlayerSiege.BesiegedSettlement.LocationComplex.GetLocationWithId("lordshall").GetSceneName(wallLevel), selectedTroops.ToFlattenedRoster());
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002D1C File Offset: 0x00000F1C
		public static void CheckEnemyAttackableHonorably(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
			{
				return;
			}
			if (PlayerEncounter.PlayerIsDefender)
			{
				return;
			}
			IFaction mapFaction = PlayerEncounter.EncounteredParty.MapFaction;
			if (mapFaction != null && mapFaction.NotAttackableByPlayerUntilTime.IsFuture)
			{
				args.IsEnabled = false;
				args.Tooltip = GameTexts.FindText("str_enemy_not_attackable_tooltip", null);
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002D8C File Offset: 0x00000F8C
		public static bool EncounterOrderAttackCondition(MenuCallbackArgs args)
		{
			MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
			if (playerMapEvent != null)
			{
				args.optionLeaveType = ((!playerMapEvent.IsNavalMapEvent) ? GameMenuOption.LeaveType.OrderTroopsToAttack : GameMenuOption.LeaveType.OrderShipsToAttack);
				MobileParty mobileParty = MapEvent.PlayerMapEvent.PartiesOnSide(PlayerEncounter.Current.OpponentSide)[0].Party.MobileParty;
				if (mobileParty != null && mobileParty.IsInRaftState)
				{
					return false;
				}
				MenuHelper.CheckEnemyAttackableHonorably(args);
				int num = 0;
				foreach (MapEventParty mapEventParty in MobileParty.MainParty.MapEventSide.Parties)
				{
					if (!mapEventParty.Party.IsMobile || !mapEventParty.Party.MobileParty.IsInRaftState)
					{
						num += mapEventParty.Party.MemberRoster.Sum(delegate(TroopRosterElement x)
						{
							if (!x.Character.IsHero)
							{
								return x.Number - x.WoundedNumber;
							}
							if (x.Character == CharacterObject.PlayerCharacter)
							{
								return 0;
							}
							if (!x.Character.HeroObject.IsWounded)
							{
								return 1;
							}
							return 0;
						});
					}
				}
				if (playerMapEvent.HasTroopsOnBothSides() && playerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide) != null && num > 0)
				{
					int num2 = 0;
					if (!MobileParty.MainParty.IsInRaftState)
					{
						num2 = MobileParty.MainParty.MemberRoster.Sum(delegate(TroopRosterElement x)
						{
							if (!x.Character.IsHero)
							{
								return x.Number - x.WoundedNumber;
							}
							if (x.Character == CharacterObject.PlayerCharacter)
							{
								return 0;
							}
							if (!x.Character.HeroObject.IsWounded)
							{
								return 1;
							}
							return 0;
						});
					}
					if (num2 > 0)
					{
						if (MobileParty.MainParty.MapEvent.IsNavalMapEvent)
						{
							MBTextManager.SetTextVariable("SEND_TROOPS_TEXT", "{=NFnS5YqQ}Send ships.", false);
						}
						else
						{
							MBTextManager.SetTextVariable("SEND_TROOPS_TEXT", "{=QfMeoKOm}Send troops.", false);
						}
					}
					else
					{
						MBTextManager.SetTextVariable("SEND_TROOPS_TEXT", "{=jo3UHKMD}Leave it to the others.", false);
					}
					if (playerMapEvent.IsInvulnerable)
					{
						playerMapEvent.IsInvulnerable = false;
					}
					if (!MobilePartyHelper.CanPartyAttackWithCurrentMorale(MobileParty.MainParty))
					{
						args.Tooltip = new TextObject("{=xnRtINwH}Your men lack the courage to continue the battle without you. (Low Morale)", null);
						args.IsEnabled = false;
					}
					else
					{
						IFaction mapFaction = PlayerEncounter.EncounteredParty.MapFaction;
						if (mapFaction == null || mapFaction.NotAttackableByPlayerUntilTime.IsPast)
						{
							args.Tooltip = TooltipHelper.GetSendTroopsPowerContextTooltipForMapEvent();
						}
					}
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002FA4 File Offset: 0x000011A4
		private static void EncounterOrderAttack(TroopRoster selectedTroopsForPlayerSide)
		{
			MapEvent battle = PlayerEncounter.Battle;
			if (PlayerSiege.PlayerSiegeEvent != null)
			{
				ISiegeEventSide siegeEventSide = PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(PlayerSiege.PlayerSide.GetOppositeSide());
				if (siegeEventSide != null)
				{
					if (!siegeEventSide.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Any((PartyBase party) => party.NumberOfHealthyMembers > 0))
					{
						bool flag;
						if (battle != null)
						{
							flag = !battle.GetMapEventSide(battle.GetOtherSide(battle.PlayerSide)).Parties.Any((MapEventParty party) => party.Party.NumberOfHealthyMembers > 0);
						}
						else
						{
							flag = true;
						}
						if (flag)
						{
							PlayerEncounter.Update();
							return;
						}
					}
				}
			}
			PartyBase leaderParty = battle.GetLeaderParty(PartyBase.MainParty.OpponentSide);
			MobileParty mobileParty = MobileParty.MainParty.AttachedTo ?? MobileParty.MainParty;
			SiegeEvent siegeEvent = leaderParty.SiegeEvent;
			if (((siegeEvent != null) ? siegeEvent.BesiegerCamp : null) != null && !leaderParty.SiegeEvent.BesiegerCamp.HasInvolvedPartyForEventType(leaderParty, MapEvent.BattleTypes.Siege) && mobileParty.BesiegerCamp == null)
			{
				mobileParty.BesiegerCamp = leaderParty.SiegeEvent.BesiegerCamp;
			}
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, leaderParty);
			if (PlayerEncounter.Current != null)
			{
				GameMenu.ExitToLast();
				PlayerEncounter.InitSimulation(null, null);
				if (PlayerEncounter.Current != null && PlayerEncounter.Current.BattleSimulation != null)
				{
					((MapState)Game.Current.GameStateManager.ActiveState).StartBattleSimulation();
				}
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00003105 File Offset: 0x00001305
		public static void EncounterOrderAttackConsequence(MenuCallbackArgs args)
		{
			MenuHelper.EncounterOrderAttack(null);
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000310D File Offset: 0x0000130D
		public static void EncounterCaptureTheEnemyOnConsequence(MenuCallbackArgs args)
		{
			MapEvent.PlayerMapEvent.SetOverrideWinner(MapEvent.PlayerMapEvent.PlayerSide);
			PlayerEncounter.Update();
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00003128 File Offset: 0x00001328
		public static void EncounterLeaveConsequence()
		{
			Settlement currentSettlement = MobileParty.MainParty.CurrentSettlement;
			MapEvent mapEvent = ((PlayerEncounter.Battle != null) ? PlayerEncounter.Battle : PlayerEncounter.EncounteredBattle);
			int numberOfInvolvedMen = mapEvent.GetNumberOfInvolvedMen(PartyBase.MainParty.Side);
			Settlement currentSettlement2 = MobileParty.MainParty.CurrentSettlement;
			bool forcePlayerOutFromSettlement;
			if (((currentSettlement2 != null) ? currentSettlement2.SiegeEvent : null) != null)
			{
				Settlement currentSettlement3 = MobileParty.MainParty.CurrentSettlement;
				forcePlayerOutFromSettlement = ((currentSettlement3 != null) ? currentSettlement3.MapFaction : null) != MobileParty.MainParty.MapFaction;
			}
			else
			{
				forcePlayerOutFromSettlement = true;
			}
			PlayerEncounter.Finish(forcePlayerOutFromSettlement);
			if (MobileParty.MainParty.BesiegerCamp != null)
			{
				MobileParty.MainParty.BesiegerCamp = null;
			}
			if (mapEvent != null && !mapEvent.IsFinalized && !mapEvent.IsRaid && numberOfInvolvedMen == PartyBase.MainParty.NumberOfHealthyMembers)
			{
				MapEvent mapEvent2 = mapEvent;
				PlayerEncounter playerEncounter = PlayerEncounter.Current;
				FlattenedTroopRoster[] priorTroops;
				if (playerEncounter == null)
				{
					priorTroops = null;
				}
				else
				{
					BattleSimulation battleSimulation = playerEncounter.BattleSimulation;
					priorTroops = ((battleSimulation != null) ? battleSimulation.SelectedTroops : null);
				}
				mapEvent2.SimulateBattleSetup(priorTroops);
				mapEvent.SimulateBattleRound((PartyBase.MainParty.Side == BattleSideEnum.Attacker) ? 1 : 0, (PartyBase.MainParty.Side == BattleSideEnum.Attacker) ? 0 : 1);
			}
			if (currentSettlement != null)
			{
				EncounterManager.StartSettlementEncounter(MobileParty.MainParty, currentSettlement);
			}
		}

		// Token: 0x06000012 RID: 18 RVA: 0x0000323C File Offset: 0x0000143C
		public static string GetEncounterCultureBackgroundMesh(CultureObject encounterCulture)
		{
			if (string.IsNullOrEmpty((encounterCulture != null) ? encounterCulture.EncounterBackgroundMesh : null))
			{
				Debug.FailedAssert("Background mesh is invalid for current encounter", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Helpers.cs", "GetEncounterCultureBackgroundMesh", 665);
				return string.Empty;
			}
			string text = encounterCulture.EncounterBackgroundMesh;
			if (PlayerEncounter.IsNavalEncounter())
			{
				text += "_naval";
			}
			return text;
		}
	}
}

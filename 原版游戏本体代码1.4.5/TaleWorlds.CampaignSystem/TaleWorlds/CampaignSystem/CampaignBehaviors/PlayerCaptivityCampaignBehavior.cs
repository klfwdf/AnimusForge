using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PlayerCaptivityCampaignBehavior : CampaignBehaviorBase, ICaptivityCampaignBehavior
{
	private const float PlayerExecutionProbability = 0.02f;

	private const float PlayerExecutionRelationLimit = -30f;

	private const int MaxDaysOfImprisonment = 7;

	private bool _isMainHeroExecuted;

	private Hero _mainHeroExecuter;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_isPlayerExecuted", ref _isMainHeroExecuted);
		dataStore.SyncData("_mainHeroExecuter", ref _mainHeroExecuter);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnPrisonerTaken);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
		if (prisoner == Hero.MainHero)
		{
			PlayerEncounter.ProtectPlayerSide(4f);
			if (party != null && party.IsMobile)
			{
				party.MobileParty.Ai.SetDoNotAttackMainParty(12);
			}
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (_isMainHeroExecuted)
		{
			_isMainHeroExecuted = false;
			KillCharacterAction.ApplyByExecution(Hero.MainHero, _mainHeroExecuter);
		}
	}

	private void OnPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		if (prisoner == Hero.MainHero && capturer.LeaderHero != null && (float)capturer.LeaderHero.GetRelation(prisoner) < -30f && MBRandom.RandomFloat <= 0.02f)
		{
			_isMainHeroExecuted = true;
			_mainHeroExecuter = capturer.LeaderHero;
		}
	}

	private Hero FindEnemyPrisonerToSwapWithPlayer()
	{
		IFaction mapFaction = Hero.MainHero.MapFaction;
		IFaction mapFaction2 = PlayerCaptivity.CaptorParty.MapFaction;
		foreach (Settlement settlement in mapFaction.Settlements)
		{
			foreach (CharacterObject prisonerHero in settlement.Party.PrisonerHeroes)
			{
				if (prisonerHero.HeroObject.MapFaction == mapFaction2)
				{
					return prisonerHero.HeroObject;
				}
			}
		}
		return null;
	}

	private void AddGameMenus(CampaignGameStarter gameSystemInitializer)
	{
		gameSystemInitializer.AddGameMenu("menu_captivity_end_no_more_enemies", "{=gOsori1b}Your captors have no more use for you and aren't in a murderous mood, so they let you go.", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_no_more_enemies", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			EndCaptivityAction.ApplyByEscape(Hero.MainHero);
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_by_ally_party_saved", "{=J2Iok9lT}An ally has paid your ransom.", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_by_ally_party_saved", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			EndCaptivityAction.ApplyByReleasedByCompensation(Hero.MainHero);
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_by_party_removed", "{=8gF5qYw5}Your captors have been dispersed, and you are able to escape.", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_by_party_removed", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			if (PlayerCaptivity.CaptorParty.LeaderHero == null && PlayerCaptivity.CaptorParty.MemberRoster.TotalManCount == 0)
			{
				EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.MainHero);
			}
			else
			{
				EndCaptivityAction.ApplyByEscape(Hero.MainHero);
			}
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_wilderness_escape", "{=EVODEPGw}After painful days of being dragged about as a prisoner, you find a chance and escape from your captors!", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_wilderness_escape", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			EndCaptivityAction.ApplyByEscape(Hero.MainHero);
		});
		gameSystemInitializer.AddGameMenu("menu_escape_captivity_during_battle", "{=HYGKcgh6}Your captors engage in a battle. You take advantage of the confusion and escape.", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_escape_captivity_during_battle", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			EndCaptivityAction.ApplyByEscape(Hero.MainHero);
		});
		gameSystemInitializer.AddGameMenu("menu_released_after_battle", "{=GeoTk5b9}Your captors engage in a battle and they lost, you are released after battle.", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_released_after_battle", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			Hero.MainHero.PartyBelongedToAsPrisoner.MobileParty.Ai.SetDoNotAttackMainParty(12);
			PlayerEncounter.ProtectPlayerSide();
			GameMenu.ExitToLast();
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_propose_ransom_wilderness", "{=j5OqFCa6}After painful days of being dragged about as a prisoner, suddenly one of your captors comes near you with an offer; he proposes to free you in return for {MONEY_AMOUNT}{GOLD_ICON} of your hidden wealth. You decide to...", menu_captivity_end_propose_ransom_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_propose_ransom_wilderness", "mno_captivity_end_ransom_accept", "{=buKXELE3}Accept the offer.", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Escape;
			return true;
		}, game_menu_captivity_end_by_ransom_on_consequence);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_propose_ransom_wilderness", "captivity_end_ransom_deny", "{=L4Se89I6}Refuse him, wait for a better offer.", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			return true;
		}, delegate
		{
			GameMenu.ExitToLast();
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_transfer_to_town", "{=ZEvChv7b}Your captors take you to {TOWN_NAME}. You are thrown into the dungeon there...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			MBTextManager.SetTextVariable("TOWN_NAME", PlayerCaptivity.CaptorParty.Settlement.Name);
			PlayerCaptivity.CaptorParty.SetAsCameraFollowParty();
		});
		gameSystemInitializer.AddGameMenuOption("menu_captivity_transfer_to_town", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			GameMenu.ExitToLast();
			GameMenu.ActivateGameMenu("settlement_wait");
			Campaign.Current.TimeControlMode = Campaign.Current.LastTimeControlMode;
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_exchanged_with_prisoner", "{=qoqbaHCE}After days of imprisonment, you are finally set free when your captors exchange you with {PRISONER_LORD.LINK}.", game_menu_captivity_end_by_deal_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_exchanged_with_prisoner", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			Hero.MainHero.PartyBelongedToAsPrisoner.MobileParty.Ai.SetDoNotAttackMainParty(12);
			PlayerEncounter.ProtectPlayerSide();
			GameMenu.ExitToLast();
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_propose_ransom_in_prison", "{=pFzj52dE}You spend long hours in the sunless dank of the dungeon, more than you can count. Suddenly one of your captors enters your cell with an offer; he proposes to free you in return for {MONEY_AMOUNT}{GOLD_ICON} of your hidden wealth. You decide to...", menu_captivity_end_propose_ransom_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_propose_ransom_in_prison", "mno_captivity_end_ransom_accept", "{=buKXELE3}Accept the offer.", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Escape;
			return true;
		}, game_menu_captivity_end_by_ransom_on_consequence);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_propose_ransom_in_prison", "captivity_end_ransom_deny", "{=L4Se89I6}Refuse him, wait for a better offer.", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Wait;
			return true;
		}, delegate
		{
			GameMenu.ExitToLast();
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_castle_remain", "{=BLrwIj7s}More days pass in the darkness of your cell. You get through them as best you can, enduring the kicks and curses of the guards, watching your underfed body waste away more and more...", null);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_castle_remain", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			GameMenu.ExitToLast();
		});
		gameSystemInitializer.AddGameMenu("menu_captivity_end_prison_escape", "{=85kgOyBj}After painful days of being imprisoned in dungeon, you find a chance break free and escape from the settlement!", game_menu_captivity_escape_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_captivity_end_prison_escape", "mno_continue", "{=veWOovVv}Continue...", delegate(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Continue;
			return true;
		}, delegate
		{
			EndCaptivityAction.ApplyByEscape(Hero.MainHero);
		});
	}

	private void game_menu_captivity_escape_on_init(MenuCallbackArgs args)
	{
	}

	private void game_menu_captivity_end_by_deal_on_init(MenuCallbackArgs args)
	{
		StringHelpers.SetCharacterProperties("PRISONER_LORD", FindEnemyPrisonerToSwapWithPlayer().CharacterObject);
	}

	private void game_menu_captivity_end_by_ransom_on_consequence(MenuCallbackArgs args)
	{
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, Campaign.Current.PlayerCaptivity.CurrentRansomAmount);
		Hero leaderHero = PlayerCaptivity.CaptorParty.LeaderHero;
		EndCaptivityAction.ApplyByRansom(Hero.MainHero, leaderHero);
	}

	private void menu_captivity_end_propose_ransom_on_init(MenuCallbackArgs args)
	{
		MBTextManager.SetTextVariable("MONEY_AMOUNT", Campaign.Current.PlayerCaptivity.CurrentRansomAmount);
	}

	public void CheckCaptivityChange(float dt)
	{
		if (PlayerCaptivity.CaptorParty.IsMobile && !PlayerCaptivity.CaptorParty.MobileParty.IsActive)
		{
			GameMenu.SwitchToMenu("menu_captivity_end_by_party_removed");
			return;
		}
		if (PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MapFaction == Hero.MainHero.Clan)
		{
			GameMenu.SwitchToMenu("menu_captivity_end_by_ally_party_saved");
			return;
		}
		if (!FactionManager.IsAtWarAgainstFaction(PlayerCaptivity.CaptorParty.MapFaction, MobileParty.MainParty.MapFaction) && (PlayerCaptivity.CaptorParty.MapFaction == MobileParty.MainParty.MapFaction || (!Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingModerate(PlayerCaptivity.CaptorParty.MapFaction) && !Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(PlayerCaptivity.CaptorParty.MapFaction))))
		{
			GameMenu.SwitchToMenu("menu_captivity_end_no_more_enemies");
			return;
		}
		if (PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement != null && PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.IsTown && PlayerCaptivity.CaptorParty.MapFaction == PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.MapFaction)
		{
			PlayerCaptivity.LastCheckTime = CampaignTime.Now;
			if (Game.Current.GameStateManager.ActiveState is MapState)
			{
				Campaign.Current.LastTimeControlMode = Campaign.Current.TimeControlMode;
			}
			PlayerCaptivity.CaptorParty = PlayerCaptivity.CaptorParty.MobileParty.CurrentSettlement.Party;
			GameMenu.SwitchToMenu("menu_captivity_transfer_to_town");
			return;
		}
		if ((PlayerCaptivity.CaptorParty.IsSettlement && PlayerCaptivity.CaptorParty.Settlement.IsVillage) || (PlayerCaptivity.CaptorParty.IsMobile && (PlayerCaptivity.CaptorParty.MobileParty.IsVillager || PlayerCaptivity.CaptorParty.MobileParty.IsCaravan)))
		{
			GameMenu.SwitchToMenu("menu_captivity_end_no_more_enemies");
			return;
		}
		float playerProgress = Campaign.Current.PlayerProgress;
		float num = (0.4f + playerProgress * 0.4f) * (float)CampaignTime.HoursInDay;
		num *= (Hero.MainHero.PartyBelongedToAsPrisoner.IsSettlement ? 2f : ((Hero.MainHero.PartyBelongedToAsPrisoner.IsMobile && Hero.MainHero.PartyBelongedToAsPrisoner.LeaderHero != null) ? 1.5f : 1f));
		if (!CheckTimeElapsedMoreThanHours(PlayerCaptivity.LastCheckTime, num))
		{
			return;
		}
		PlayerCaptivity.LastCheckTime = CampaignTime.Now;
		if (Campaign.Current.PlayerCaptivity.CountOfOffers == 0)
		{
			Campaign.Current.PlayerCaptivity.SetRansomAmount();
		}
		else
		{
			Campaign.Current.PlayerCaptivity.CurrentRansomAmount = MathF.Max((int)((float)Campaign.Current.PlayerCaptivity.CurrentRansomAmount * 0.8f - (float)Campaign.Current.PlayerCaptivity.CountOfOffers * 0.05f), 1);
		}
		float randomFloat = MBRandom.RandomFloat;
		float num2 = 0f;
		if (PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MapEvent != null)
		{
			int num3 = 0;
			int num4 = 0;
			foreach (PartyBase involvedParty in PlayerCaptivity.CaptorParty.MapEvent.InvolvedParties)
			{
				if (involvedParty.Side == PlayerCaptivity.CaptorParty.Side)
				{
					num3 += involvedParty.MemberRoster.TotalManCount;
				}
				else
				{
					num4 += involvedParty.MemberRoster.TotalManCount;
				}
			}
			if ((float)num3 < (float)num4 * 3f + 1f)
			{
				num2 = 1f - (float)num3 / ((float)num4 * 3f + 1f);
				num2 /= 2f;
			}
		}
		float num5 = ((float)Campaign.Current.PlayerCaptivity.CountOfOffers + 1f) / 8f;
		if (num2 > 0f)
		{
			num5 = MathF.Pow(num5, 1f - num2);
		}
		if (Hero.MainHero.PartyBelongedToAsPrisoner != null)
		{
			if (Hero.MainHero.PartyBelongedToAsPrisoner.IsMobile && Hero.MainHero.PartyBelongedToAsPrisoner.LeaderHero != null)
			{
				num5 *= MathF.Sqrt(num5);
			}
			else if (Hero.MainHero.PartyBelongedToAsPrisoner.IsSettlement)
			{
				num5 = ((!Hero.MainHero.PartyBelongedToAsPrisoner.Settlement.IsHideout) ? (num5 * num5) : (num5 * MathF.Sqrt(num5)));
			}
			if (Hero.MainHero.PartyBelongedToAsPrisoner.IsMobile && !Hero.MainHero.PartyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea && Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.FleetFooted))
			{
				num5 *= 1f + DefaultPerks.Roguery.FleetFooted.SecondaryBonus;
			}
		}
		if (randomFloat < num5)
		{
			if (PlayerCaptivity.CaptorParty.IsMobile && PlayerCaptivity.CaptorParty.MapEvent != null)
			{
				GameMenu.SwitchToMenu("menu_escape_captivity_during_battle");
			}
			else if (Hero.MainHero.CurrentSettlement == null)
			{
				GameMenu.SwitchToMenu("menu_captivity_end_wilderness_escape");
			}
			else
			{
				GameMenu.SwitchToMenu("menu_captivity_end_prison_escape");
			}
			return;
		}
		Campaign.Current.PlayerCaptivity.CountOfOffers++;
		if (randomFloat < 0.5f && Campaign.Current.PlayerCaptivity.CurrentRansomAmount <= Hero.MainHero.Gold && Hero.MainHero.PartyBelongedToAsPrisoner?.MapEvent == null)
		{
			if (Hero.MainHero.CurrentSettlement != null)
			{
				GameMenu.SwitchToMenu("menu_captivity_end_propose_ransom_in_prison");
			}
			else
			{
				GameMenu.SwitchToMenu("menu_captivity_end_propose_ransom_wilderness");
			}
		}
	}

	private bool CheckTimeElapsedMoreThanHours(CampaignTime eventBeginTime, float hoursToWait)
	{
		float elapsedHoursUntilNow = eventBeginTime.ElapsedHoursUntilNow;
		float randomNumber = PlayerCaptivity.RandomNumber;
		return (double)hoursToWait * (0.5 + (double)randomNumber) < (double)elapsedHoursUntilNow;
	}
}

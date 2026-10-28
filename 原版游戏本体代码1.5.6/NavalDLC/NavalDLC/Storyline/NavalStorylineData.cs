using System;
using System.Collections.Generic;
using Helpers;
using NavalDLC.Storyline.CampaignBehaviors;
using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline;

public class NavalStorylineData
{
	public enum NavalStorylineStage
	{
		None = -1,
		Act1,
		Act2,
		Act3Quest1,
		Act3Quest2,
		Act3SpeakToSailors,
		Act3Quest4,
		Act3Quest5,
		Act3SpeakToGunnarAndSister
	}

	public enum NavalStorylineCheckpoint
	{
		None,
		Act1PortMenu,
		Act1PortFightSucceeded,
		Act1CaptivitySucceeded,
		Act2EncounterMenu,
		Act2Finalized,
		Act3Quest1SetPieceEncounterMenu,
		Act3Quest1SetPieceSucceeded,
		Act3Quest2EncounterMenu,
		Act3Quest2Succeeded,
		Act3Quest3EncounterMenu,
		Act3Quest3Succeeded,
		Act3Quest3InterceptedMenu,
		Act3Quest4EncounterMenu,
		Act3Quest4Succeeded,
		Act3Quest5EncounterMenu,
		Act3Quest5MissionMenu,
		Act3Quest5Succeeded
	}

	public enum StorylineCancelDetail
	{
		ByDialogue,
		ByRansom
	}

	public enum NavalStorylineSetPieceBattleMissionTypes
	{
		None = -1,
		Act1,
		Act2,
		Act3Quest1,
		Act3Quest2,
		Act3Quest3,
		Act3Quest4,
		Act3Quest5
	}

	public const string NavalStorylineSpecialQuestType = "NavalStoryline";

	private const string GunnarStringId = "naval_storyline_gangradir";

	private const string BjolgurStringId = "naval_storyline_bjolgur";

	private const string PurigStringId = "naval_storyline_northerner";

	private const string EmiraAlFahdaStringId = "naval_storyline_emira_al_fahda";

	private const string LaharStringId = "naval_storyline_lahar";

	private const string PrusasStringId = "naval_storyline_crusas";

	public const string NavalStoryLineOutOfTownMenuId = "naval_storyline_outside_town";

	public const string NavalStoryLineEncounterBlockingMenuId = "naval_storyline_encounter_blocking";

	public const string NavalStoryLineVirtualPortMenuId = "naval_storyline_virtualport";

	public const string NavalStoryLineEncounterMeetingMenuId = "naval_storyline_encounter_meeting";

	public const string NavalStoryLineEncounterMenuId = "naval_storyline_encounter";

	public const string NavalStoryLineJoinEncounterMenuId = "naval_storyline_join_encounter";

	private const string HomeSettlementStringId = "town_V8";

	private const string Act3Quest1TargetSettlementStringId = "town_N1";

	private const string Act3Quest2TargetSettlementStringId = "town_A1";

	private const string Act3Quest3TargetSettlementStringId = "town_S3";

	private const string Act3Quest4TargetSettlementStringId = "town_V7";

	public const string GunnarsVillageStringId = "village_N1_2";

	public const string InquireAtOsticanCharacterSpawnPointTag = "sp_storyline_npc";

	private Hero _gunnar;

	private Hero _bjolgur;

	private Hero _purig;

	private Hero _lahar;

	private Hero _emiraAlFahda;

	private Hero _prusas;

	private Banner _corsairBanner;

	private Settlement _homeSettlement;

	private Settlement _act3Quest1TargetSettlement;

	private Settlement _act3Quest2TargetSettlement;

	private Settlement _act3Quest3TargetSettlement;

	private Settlement _act3Quest4TargetSettlement;

	public static Hero Gunnar => NavalDLCManager.Instance.NavalStorylineData._gunnar;

	public static Hero Bjolgur => NavalDLCManager.Instance.NavalStorylineData._bjolgur;

	public static Hero Purig => NavalDLCManager.Instance.NavalStorylineData._purig;

	public static Hero Lahar => NavalDLCManager.Instance.NavalStorylineData._lahar;

	public static Hero EmiraAlFahda => NavalDLCManager.Instance.NavalStorylineData._emiraAlFahda;

	public static Hero Prusas => NavalDLCManager.Instance.NavalStorylineData._prusas;

	public static Settlement HomeSettlement => NavalDLCManager.Instance.NavalStorylineData._homeSettlement;

	public static Settlement Act3Quest1TargetSettlement => NavalDLCManager.Instance.NavalStorylineData._act3Quest1TargetSettlement;

	public static Settlement Act3Quest2TargetSettlement => NavalDLCManager.Instance.NavalStorylineData._act3Quest2TargetSettlement;

	public static Settlement Act3Quest3TargetSettlement => NavalDLCManager.Instance.NavalStorylineData._act3Quest3TargetSettlement;

	public static Settlement Act3Quest4TargetSettlement => NavalDLCManager.Instance.NavalStorylineData._act3Quest4TargetSettlement;

	public static Banner CorsairBanner => NavalDLCManager.Instance.NavalStorylineData._corsairBanner;

	public static void OnCheckpointReached(NavalStorylineCheckpoint checkpoint)
	{
		Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.OnCheckpointReached(checkpoint);
	}

	public void Initialize()
	{
		if (!IsNavalStorylineCanceled())
		{
			CacheNavalStorylineSettlements();
			CreateStorylineHero("naval_storyline_gangradir", out _gunnar);
			CreateStorylineHero("naval_storyline_bjolgur", out _bjolgur);
			CreateStorylineHero("naval_storyline_northerner", out _purig);
			CreateStorylineHero("naval_storyline_lahar", out _lahar);
			CreateStorylineHero("naval_storyline_emira_al_fahda", out _emiraAlFahda);
			CreateStorylineHero("naval_storyline_crusas", out _prusas);
			_corsairBanner = new Banner("11.97.166.1528.1528.764.764.1.0.0.500.35.171.555.555.764.764.0.0.0.167.35.171.350.350.764.764.0.0.0");
		}
	}

	private void CacheNavalStorylineSettlements()
	{
		_homeSettlement = Settlement.Find("town_V8");
		_act3Quest1TargetSettlement = Settlement.Find("town_N1");
		_act3Quest2TargetSettlement = Settlement.Find("town_A1");
		_act3Quest3TargetSettlement = Settlement.Find("town_S3");
		_act3Quest4TargetSettlement = Settlement.Find("town_V7");
	}

	private void CreateStorylineHero(string stringId, out Hero hero)
	{
		hero = Campaign.Current.CampaignObjectManager.Find<Hero>(stringId);
		if (hero == null)
		{
			CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(stringId);
			HeroCreator.CreateBasicHero(stringId, characterObject, out hero);
			hero.SetName(characterObject.Name, characterObject.Name);
			CampaignTime randomBirthDayForAge = HeroHelper.GetRandomBirthDayForAge(characterObject.Age);
			hero.SetBirthDay(randomBirthDayForAge);
			hero.SetNewOccupation(Occupation.Special);
			if (characterObject.Culture.StringId == "aserai")
			{
				hero.BornSettlement = Act3Quest2TargetSettlement;
			}
			else
			{
				hero.BornSettlement = HomeSettlement;
			}
			SetHeroText(hero);
		}
		hero.CharacterObject.SetTransferableInPartyScreen(isTransferable: false);
	}

	public static void SetHeroText(Hero hero)
	{
		if (hero == Gunnar)
		{
			TextObject textObject = new TextObject("{=eW6Pbefi}Gunnar of {LAGSHOFN.NAME} is a Nord warrior from the island of Beinland. He won a reputation for courage fighting in a rebellion against {VOLBJORN.NAME}, first king of the Nordvyg, but after the rebels' defeat he made his peace with the victors. He has recently embarked on a campaign against the Sea Hounds, a pirate confederation that has terrorized the northern seas of Calradia.");
			StringHelpers.SetCharacterProperties("VOLBJORN", CharacterObject.Find("dead_lord_7_1"), textObject);
			StringHelpers.SetSettlementProperties("LAGSHOFN", Settlement.Find("village_N1_2"), textObject);
			hero.EncyclopediaText = textObject;
		}
		else if (hero == Purig)
		{
			TextObject textObject2 = new TextObject("{=bbBAWYbu}Purig is a Nord warrior who fought in the rebellion against {VOLBJORN.NAME}, first ruler of the Nordvyg. Following the king's victory he joined with other defeated rebels to form the Sea Hounds, a pirate confederation that is terrorizing the northern seas of Calradia.");
			StringHelpers.SetCharacterProperties("VOLBJORN", CharacterObject.Find("dead_lord_7_1"), textObject2);
			hero.EncyclopediaText = textObject2;
		}
		else if (hero == Lahar)
		{
			TextObject encyclopediaText = new TextObject("{=7y7cF9dC}Lahar is a sea captain and former corsair, now currently in the employ of the merchants of Quyaz.");
			hero.EncyclopediaText = encyclopediaText;
		}
		else if (hero == EmiraAlFahda)
		{
			TextObject encyclopediaText2 = new TextObject("{=MADDAmO5}The Emira al-Fahda is a noblewoman from the city of Quyaz. She fell out with her uncles over an inheritance dispute and turned pirate, allying with the Sea Hounds and ravaging Quyazi shipping.");
			hero.EncyclopediaText = encyclopediaText2;
		}
		else if (hero == Prusas)
		{
			TextObject encyclopediaText3 = new TextObject("{=A0Zr68nk}Salautas Crusas is an imperial merchant who owns sulfur mines in the Gulf of Charas. He uses slaves, which he purchases from the Sea Hounds.");
			hero.EncyclopediaText = encyclopediaText3;
		}
		else if (hero == Bjolgur)
		{
			TextObject encyclopediaText4 = new TextObject("{=8qAnEXE5}Bjolgur of Agilting is a Nord warrior who fought alongside Gunnar in the rebellion against King Volbjorn. After the rebels' defeat he joined up with the Skolderbroda, a mercenary brotherhood.");
			hero.EncyclopediaText = encyclopediaText4;
		}
	}

	public static bool IsNavalStorylineHero(Hero hero)
	{
		if (hero != Gunnar && hero != Purig && hero != Bjolgur && hero != Lahar && hero != EmiraAlFahda)
		{
			return hero == Prusas;
		}
		return true;
	}

	public static void StartNavalStoryline()
	{
		new InquireAtOstican().StartQuest();
	}

	public static bool IsStorylineActivationPossible()
	{
		if (Campaign.Current.IsMainHeroDisguised || MobileParty.MainParty.Army != null)
		{
			return false;
		}
		return !IsWaitingForSistersReturn();
	}

	public static void ActivateNavalStoryline()
	{
		NavalStorylineCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>();
		if (campaignBehavior != null && !campaignBehavior.IsNavalStorylineActive())
		{
			campaignBehavior.ChangeNavalStorylineActivity(activity: true);
		}
	}

	public static void DeactivateNavalStoryline()
	{
		NavalStorylineCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>();
		if (campaignBehavior != null && campaignBehavior.IsNavalStorylineActive())
		{
			campaignBehavior.ChangeNavalStorylineActivity(activity: false);
		}
	}

	public static bool IsMainPartyAllowed()
	{
		Campaign.Current.Models.SettlementAccessModel.CanMainHeroEnterSettlement(Settlement.CurrentSettlement, out var accessDetails);
		if (accessDetails.AccessLevel == SettlementAccessModel.AccessLevel.FullAccess && !Clan.PlayerClan.MapFaction.IsAtWarWith(Settlement.CurrentSettlement.MapFaction))
		{
			if (Settlement.CurrentSettlement.SiegeEvent != null)
			{
				if (!Settlement.CurrentSettlement.SiegeEvent.IsBlockadeActive)
				{
					return MobileParty.MainParty.HasNavalNavigationCapability;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool IsTutorialSkipped()
	{
		return Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.IsTutorialSkipped() ?? false;
	}

	public static bool IsNavalStoryLineActive()
	{
		return Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.IsNavalStorylineActive() ?? false;
	}

	public static bool HasCompletedLast(NavalStorylineStage stage)
	{
		NavalStorylineCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>();
		if (campaignBehavior != null)
		{
			return stage == campaignBehavior.GetNavalStorylineStage();
		}
		return false;
	}

	public static NavalStorylineStage GetStorylineStage()
	{
		return Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.GetNavalStorylineStage() ?? NavalStorylineStage.None;
	}

	public static NavalStorylineSetPieceBattleMissionTypes GetNavalStorylineSetPieceBattleMissionType()
	{
		return Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.GetNavalStorylineSetPieceBattleMissionType() ?? NavalStorylineSetPieceBattleMissionTypes.None;
	}

	public static bool IsWaitingForSistersReturn()
	{
		return Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.IsWaitingForSistersReturn() ?? false;
	}

	public static void SetNavalStorylineSetPieceBattleMissionType(NavalStorylineSetPieceBattleMissionTypes missionType)
	{
		Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.SetNavalStorylineSetPieceBattleMissionType(missionType);
	}

	public static bool IsNavalStorylineCanceled()
	{
		return Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.GetIsNavalStorylineCanceled() ?? true;
	}

	public static void OnStorylineProgress(NavalStorylineQuestBase navalQuest)
	{
		ActivateNavalStoryline();
		FadeToBlack();
		if (navalQuest.Template != null)
		{
			MobileParty.MainParty.InitializeMobilePartyAtPosition(navalQuest.Template, MobileParty.MainParty.Position);
			foreach (Ship ship in MobileParty.MainParty.Ships)
			{
				ship.IsTradeable = false;
				ship.IsUsedByQuest = true;
			}
		}
		AddGunnarToMainParty();
		GiveProvisionsToPlayer();
		MobileParty.MainParty.SetSailAtPosition(Settlement.CurrentSettlement.PortPosition);
		PlayerEncounter.Finish();
	}

	private static void GiveProvisionsToPlayer()
	{
		Campaign.Current.GetCampaignBehavior<NavalStorylineCampaignBehavior>()?.GiveProvisionsToPlayer();
	}

	public static void AddGunnarToMainParty()
	{
		Gunnar.Heal(Gunnar.MaxHitPoints);
		MobileParty.MainParty.MemberRoster.AddToCounts(Gunnar.CharacterObject, 1);
	}

	public static void TeleportMainPartyBackToBase()
	{
		FadeToBlack();
		if (MobileParty.MainParty.CurrentSettlement != HomeSettlement)
		{
			MobileParty.MainParty.Position = (MobileParty.MainParty.HasNavalNavigationCapability ? HomeSettlement.PortPosition : HomeSettlement.GatePosition);
			MobileParty.MainParty.IsCurrentlyAtSea = MobileParty.MainParty.HasNavalNavigationCapability;
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Finish();
			}
			if (MobileParty.MainParty.IsInRaftState)
			{
				RaftStateChangeAction.DeactivateRaftStateForParty(MobileParty.MainParty);
			}
			if (Hero.MainHero.IsPrisoner)
			{
				EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.MainHero);
			}
			if (MobileParty.MainParty.Anchor.IsValid)
			{
				MobileParty.MainParty.Anchor.ResetPosition();
			}
			EncounterManager.StartSettlementEncounter(MobileParty.MainParty, HomeSettlement);
		}
		MobileParty.MainParty.SetMoveModeHold();
		if (GameStateManager.Current.ActiveState is MapState mapState)
		{
			mapState.Handler.TeleportCameraToMainParty();
		}
	}

	public static void TeleportMainHeroAndGunnarBackToBase()
	{
		TeleportMainPartyBackToBase();
		Gunnar.Heal(Gunnar.MaxHitPoints);
		EnterSettlementAction.ApplyForCharacterOnly(Gunnar, HomeSettlement);
		GameMenu.ActivateGameMenu("naval_storyline_outside_town");
	}

	private static void FadeToBlack()
	{
		if (Game.Current.GameStateManager.ActiveState is MapState)
		{
			ScreenFadeController.BeginFadeOutAndIn(0.1f, 0.5f, 0.35f);
		}
	}

	public static MissionInitializerRecord GetNavalMissionInitializerTemplate(string sceneName)
	{
		MissionInitializerRecord result = new MissionInitializerRecord(sceneName);
		result.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.DamageFromPlayerToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
		result.DecalAtlasGroup = 2;
		return result;
	}

	public static void OnPlayerPostponedQuestStart()
	{
		if (!IsMainPartyAllowed())
		{
			Mission.Current?.EndMission();
		}
	}

	public static void OnPlayerAcceptsQuest(Action questAccepted, Action questRejected)
	{
		TextObject textObject = new TextObject("{=1A0ssk7f}If you continue, the following quests will be cancelled: {newline}{newline}{QUESTS}{newline}Are you sure you wish to proceed?");
		IEnumerable<QuestBase> enumerable = Campaign.Current.QuestManager.Quests.WhereQ((QuestBase q) => ShouldIssueQuestCancelled(q));
		int num = enumerable.CountQ();
		if (num > 0)
		{
			TextObject textObject2 = GameTexts.FindText("str_string_newline_string");
			textObject.SetTextVariable("QUESTS", textObject2);
			int num2 = 0;
			foreach (QuestBase item in enumerable)
			{
				textObject2.SetTextVariable("STR1", item.Title);
				if (num2 + 1 == num)
				{
					textObject2.SetTextVariable("STR2", "");
				}
				else
				{
					TextObject textObject3 = GameTexts.FindText("str_string_newline_string");
					textObject2.SetTextVariable("STR2", textObject3);
					textObject2 = textObject3;
				}
				num2++;
			}
			InformationManager.ShowInquiry(new InquiryData("", textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				for (int num3 = Campaign.Current.QuestManager.Quests.Count - 1; num3 >= 0; num3--)
				{
					QuestBase questBase = Campaign.Current.QuestManager.Quests[num3];
					if (ShouldIssueQuestCancelled(questBase))
					{
						questBase.CompleteQuestWithCancel(GameTexts.FindText("str_quest_cancellation_due_to_naval_storyline"));
					}
				}
				questAccepted?.Invoke();
			}, delegate
			{
				questRejected?.Invoke();
			}), pauseGameActiveState: true);
		}
		else
		{
			questAccepted?.Invoke();
		}
	}

	public static bool ShouldIssueQuestCancelled(QuestBase quest)
	{
		if (quest.IsOngoing)
		{
			return !quest.IsSpecialQuest;
		}
		return false;
	}
}

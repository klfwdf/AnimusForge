using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class QuestHelper
{
	public static void AddMapArrowFromPointToTarget(TextObject name, CampaignVec2 sourcePosition, CampaignVec2 targetPosition, float life, float error)
	{
		Vec2 vec = targetPosition.ToVec2() - sourcePosition.ToVec2();
		vec.Normalize();
		vec.x += error * (MBRandom.RandomFloat - 0.5f);
		vec.y += error * (MBRandom.RandomFloat - 0.5f);
		vec.Normalize();
		CampaignVec2 trackPosition = sourcePosition + vec * 4f;
		Campaign.Current.GetCampaignBehavior<IMapTracksCampaignBehavior>()?.AddMapArrow(name, trackPosition, vec, life);
	}

	public static bool CheckGoldForAlternativeSolution(int requiredGold, out TextObject explanation)
	{
		if (Hero.MainHero.Gold < requiredGold)
		{
			explanation = new TextObject("{=jkYQmtIF}You need to have at least {GOLD_AMOUNT}{GOLD_ICON} to pay for the expenses beforehand.");
			explanation.SetTextVariable("GOLD_AMOUNT", requiredGold);
			return false;
		}
		explanation = null;
		return true;
	}

	public static List<SkillObject> GetAlternativeSolutionMeleeSkills()
	{
		return new List<SkillObject>
		{
			DefaultSkills.OneHanded,
			DefaultSkills.TwoHanded,
			DefaultSkills.Polearm
		};
	}

	public static bool CheckRosterForAlternativeSolution(TroopRoster troopRoster, int requiredTroopCount, out TextObject explanation, int minimumTier = 0, bool mountedRequired = false)
	{
		int num = 0;
		foreach (TroopRosterElement item in troopRoster.GetTroopRoster())
		{
			if (!item.Character.IsHero && !item.Character.IsNotTransferableInPartyScreen && (!mountedRequired || item.Character.IsMounted) && (minimumTier == 0 || item.Character.Tier >= minimumTier))
			{
				num += item.Number - item.WoundedNumber;
			}
		}
		if (num < requiredTroopCount)
		{
			if (minimumTier == 0)
			{
				explanation = new TextObject("{=AdkSktd2}You have to send {NUMBER} {?MOUNTED}cavalry {?}{\\?}troops to this quest.");
			}
			else
			{
				explanation = new TextObject("{=Cg3hH8gN}You have to send {NUMBER} {?MOUNTED}cavalry {?}{\\?}troops with at least tier {TIER} to this quest.");
				explanation.SetTextVariable("TIER", minimumTier);
			}
			explanation.SetTextVariable("MOUNTED", mountedRequired ? 1 : 0);
			explanation.SetTextVariable("NUMBER", requiredTroopCount);
			return false;
		}
		explanation = null;
		return true;
	}

	public static List<SkillObject> GetAlternativeSolutionRangedSkills()
	{
		return new List<SkillObject>
		{
			DefaultSkills.Bow,
			DefaultSkills.Crossbow,
			DefaultSkills.Throwing
		};
	}

	public static bool CheckMinorMajorCoercion(QuestBase questToCheck, MapEvent mapEvent, PartyBase attackerParty)
	{
		if ((mapEvent.IsForcingSupplies || mapEvent.IsForcingVolunteers) && attackerParty == PartyBase.MainParty && mapEvent.MapEventSettlement.IsVillage)
		{
			if (!QuestManager.QuestExistInClan(questToCheck, mapEvent.MapEventSettlement.OwnerClan))
			{
				return QuestManager.QuestExistInSettlementNotables(questToCheck, mapEvent.MapEventSettlement);
			}
			return true;
		}
		return false;
	}

	public static void ApplyGenericMinorMajorCoercionConsequences(QuestBase quest, MapEvent mapEvent)
	{
		TextObject textObject = new TextObject("{=tWZ4a8Ih}You are accused in {SETTLEMENT} of a crime and {QUEST_GIVER.LINK} no longer trusts you in this matter.");
		textObject.SetTextVariable("SETTLEMENT", mapEvent.MapEventSettlement.EncyclopediaLinkWithName);
		StringHelpers.SetCharacterProperties("QUEST_GIVER", quest.QuestGiver.CharacterObject, textObject);
		quest.CompleteQuestWithFail(textObject);
		ChangeRelationAction.ApplyPlayerRelation(quest.QuestGiver, -5);
		quest.QuestGiver.AddPower(-10f);
		TraitLevelingHelper.OnIssueSolvedThroughAlternativeSolution(Hero.MainHero, new Tuple<TraitObject, int>[1]
		{
			new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
		});
	}

	public static int GetAveragePriceOfItemInTheWorld(ItemObject item)
	{
		int num = 0;
		int num2 = 0;
		foreach (Settlement item2 in Settlement.All)
		{
			if (item2.IsTown)
			{
				num2 += item2.Town.GetItemPrice(item);
				num++;
			}
			else if (item2.IsVillage)
			{
				num2 += item2.Village.GetItemPrice(item);
				num++;
			}
		}
		return num2 / num;
	}

	public static void CheckWarDeclarationAndFailOrCancelTheQuest(QuestBase questToCheck, IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail, TextObject failLog, TextObject cancelLog, bool forceCancel = false)
	{
		if (questToCheck.QuestGiver.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			if (!forceCancel && DiplomacyHelper.IsWarCausedByPlayer(faction1, faction2, detail))
			{
				questToCheck.CompleteQuestWithFail(failLog);
			}
			else
			{
				questToCheck.CompleteQuestWithCancel(cancelLog);
			}
		}
	}
}

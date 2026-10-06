using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PrisonerRecruitCampaignBehavior : CampaignBehaviorBase
{
	private Dictionary<CharacterObject, float> PrisonerTalkRecords = new Dictionary<CharacterObject, float>();

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("PrisonerTalkRecords", ref PrisonerTalkRecords);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("conversation_prisoner_chat_start", "start", "prisoner_recruit_start_player", "{=k7ebznzr}Yes?", conversation_prisoner_chat_start_on_condition, null);
		campaignGameStarter.AddPlayerLine("conversation_prisoner_chat_player", "prisoner_recruit_start_player", "prisoner_recruit_start_response", "{=ksZXyDJG}Don't do anything stupid, like trying to run away. I will be watching you.", null, null);
		campaignGameStarter.AddDialogLine("conversation_prisoner_chat_response", "prisoner_recruit_start_response", "close_window", "{=Oe1bTJp6}No, I swear I won't.", null, null);
		campaignGameStarter.AddDialogLine("conversation_prisoner_recruit_start_1", "start", "prisoner_recruit_start", "{=!}I'm going to take a chance on you, to give you a chance to walk free, if you like.", conversation_prisoner_recruit_start_on_condition, null);
		campaignGameStarter.AddPlayerLine("conversation_prisoner_recruit_start", "prisoner_recruit_start", "prisoner_recruit", "{=!}Are you willing to join us? To fight alongside us?", null, null);
		campaignGameStarter.AddDialogLine("prisoner_recruit_1", "prisoner_recruit", "close_window", "{=!}Aye. I would do that.", conversation_prisoner_recruit_on_condition, null);
		campaignGameStarter.AddDialogLine("prisoner_recruit_2", "prisoner_recruit", "close_window", "{=!}No. I'm no traitor.", conversation_prisoner_recruit_no_on_condition, null);
		campaignGameStarter.AddDialogLine("prisoner_recruit_3", "prisoner_recruit", "close_window", "{=!}You heard me the first time. You know where to stick your offer.", null, null);
	}

	private bool conversation_prisoner_chat_start_on_condition()
	{
		bool flag = (CharacterObject.OneToOneConversationCharacter.IsHero && (Hero.OneToOneConversationHero.PartyBelongedTo == null || !Hero.OneToOneConversationHero.PartyBelongedTo.IsActive)) || (CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.PrisonGuard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.Guard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.CaravanGuard && MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsMainParty);
		return MobileParty.MainParty.PrisonRoster.Contains(CharacterObject.OneToOneConversationCharacter) && flag;
	}

	private bool conversation_prisoner_recruit_start_on_condition()
	{
		bool flag = !CharacterObject.OneToOneConversationCharacter.IsHero && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.PrisonGuard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.Guard && CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.CaravanGuard && MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsMainParty;
		bool num = MobileParty.MainParty.PrisonRoster.Contains(CharacterObject.OneToOneConversationCharacter);
		if (num && !PrisonerTalkRecords.ContainsKey(CharacterObject.OneToOneConversationCharacter))
		{
			PrisonerTalkRecords.Add(CharacterObject.OneToOneConversationCharacter, -1f);
		}
		return num && flag;
	}

	public bool conversation_prisoner_recruit_on_condition()
	{
		bool flag = false;
		if (PrisonerTalkRecords.TryGetValue(CharacterObject.OneToOneConversationCharacter, out var value) && (value < 0f || Campaign.CurrentTime - value >= 5f))
		{
			flag = MBRandom.RandomInt(MBMath.ClampInt(150 - CharacterObject.PlayerCharacter.GetSkillValue(DefaultSkills.Steward), 1, 150)) < 30;
			if (flag)
			{
				PrisonerTalkRecords.Remove(CharacterObject.OneToOneConversationCharacter);
				int num = MobileParty.MainParty.PrisonRoster.FindIndexOfTroop(CharacterObject.OneToOneConversationCharacter);
				if (num != -1)
				{
					TroopRosterElement elementCopyAtIndex = MobileParty.MainParty.PrisonRoster.GetElementCopyAtIndex(num);
					MobileParty.MainParty.PrisonRoster.AddToCounts(elementCopyAtIndex.Character, -elementCopyAtIndex.Number);
					MobileParty.MainParty.MemberRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number);
				}
			}
		}
		return flag;
	}

	public bool conversation_prisoner_recruit_no_on_condition()
	{
		bool result = false;
		if (PrisonerTalkRecords.TryGetValue(CharacterObject.OneToOneConversationCharacter, out var value) && value < 0f)
		{
			PrisonerTalkRecords[CharacterObject.OneToOneConversationCharacter] = Campaign.CurrentTime;
			result = true;
		}
		return result;
	}
}

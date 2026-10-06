using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics;

public class SearchBodyMissionHandler : MissionLogic
{
	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
		{
			return;
		}
		if (Game.Current.GameStateManager.ActiveState is MissionState)
		{
			if (base.Mission.Mode != MissionMode.Conversation && base.Mission.Mode != MissionMode.Battle && IsSearchable(agent))
			{
				AddItemsToPlayer(agent);
			}
		}
		else
		{
			Debug.FailedAssert("Agent interaction must occur in MissionState.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\SearchBodyMissionHandler.cs", "OnAgentInteraction", 26);
		}
	}

	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (Mission.Current.Mode != MissionMode.Battle && base.Mission.Mode != MissionMode.Duel && base.Mission.Mode != MissionMode.Conversation && IsSearchable(otherAgent))
		{
			return true;
		}
		return false;
	}

	private bool IsSearchable(Agent agent)
	{
		if (!agent.IsActive() && agent.IsHuman && agent.Character.IsHero)
		{
			return true;
		}
		return false;
	}

	private void AddItemsToPlayer(Agent interactedAgent)
	{
		CharacterObject characterObject = (CharacterObject)interactedAgent.Character;
		if (MBRandom.RandomInt(2) == 0)
		{
			characterObject.HeroObject.SpecialItems.Add(MBObjectManager.Instance.GetObject<ItemObject>("leafblade_throwing_knife"));
		}
		else
		{
			characterObject.HeroObject.SpecialItems.Add(MBObjectManager.Instance.GetObject<ItemObject>("falchion_sword_t2"));
			characterObject.HeroObject.SpecialItems.Add(MBObjectManager.Instance.GetObject<ItemObject>("cleaver_sword_t3"));
		}
		foreach (ItemObject specialItem in characterObject.HeroObject.SpecialItems)
		{
			PartyBase.MainParty.ItemRoster.AddToCounts(specialItem, 1);
			MBTextManager.SetTextVariable("ITEM_NAME", specialItem.Name);
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_item_taken").ToString()));
		}
		characterObject.HeroObject.SpecialItems.Clear();
	}
}

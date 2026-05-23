using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000086 RID: 134
	public class SearchBodyMissionHandler : MissionLogic
	{
		// Token: 0x06000533 RID: 1331 RVA: 0x00022C64 File Offset: 0x00020E64
		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				if (Game.Current.GameStateManager.ActiveState is MissionState)
				{
					if (base.Mission.Mode != MissionMode.Conversation && base.Mission.Mode != MissionMode.Battle && this.IsSearchable(agent))
					{
						this.AddItemsToPlayer(agent);
						return;
					}
				}
				else
				{
					Debug.FailedAssert("Agent interaction must occur in MissionState.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\SearchBodyMissionHandler.cs", "OnAgentInteraction", 26);
				}
			}
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x00022CD7 File Offset: 0x00020ED7
		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			return Mission.Current.Mode != MissionMode.Battle && base.Mission.Mode != MissionMode.Duel && base.Mission.Mode != MissionMode.Conversation && this.IsSearchable(otherAgent);
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x00022D0E File Offset: 0x00020F0E
		private bool IsSearchable(Agent agent)
		{
			return !agent.IsActive() && agent.IsHuman && agent.Character.IsHero;
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x00022D30 File Offset: 0x00020F30
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
			foreach (ItemObject itemObject in characterObject.HeroObject.SpecialItems)
			{
				PartyBase.MainParty.ItemRoster.AddToCounts(itemObject, 1);
				MBTextManager.SetTextVariable("ITEM_NAME", itemObject.Name, false);
				InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_item_taken", null).ToString()));
			}
			characterObject.HeroObject.SpecialItems.Clear();
		}
	}
}

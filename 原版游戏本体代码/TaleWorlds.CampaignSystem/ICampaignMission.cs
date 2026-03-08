using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000059 RID: 89
	public interface ICampaignMission
	{
		// Token: 0x170001CB RID: 459
		// (get) Token: 0x060008E1 RID: 2273
		GameState State { get; }

		// Token: 0x170001CC RID: 460
		// (get) Token: 0x060008E2 RID: 2274
		IMissionTroopSupplier AgentSupplier { get; }

		// Token: 0x170001CD RID: 461
		// (get) Token: 0x060008E3 RID: 2275
		// (set) Token: 0x060008E4 RID: 2276
		Location Location { get; set; }

		// Token: 0x170001CE RID: 462
		// (get) Token: 0x060008E5 RID: 2277
		// (set) Token: 0x060008E6 RID: 2278
		Alley LastVisitedAlley { get; set; }

		// Token: 0x170001CF RID: 463
		// (get) Token: 0x060008E7 RID: 2279
		MissionMode Mode { get; }

		// Token: 0x060008E8 RID: 2280
		void SetMissionMode(MissionMode newMode, bool atStart);

		// Token: 0x060008E9 RID: 2281
		void OnCloseEncounterMenu();

		// Token: 0x060008EA RID: 2282
		bool AgentLookingAtAgent(IAgent agent1, IAgent agent2);

		// Token: 0x060008EB RID: 2283
		void OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation);

		// Token: 0x060008EC RID: 2284
		void OnProcessSentence();

		// Token: 0x060008ED RID: 2285
		void OnConversationContinue();

		// Token: 0x060008EE RID: 2286
		bool CheckIfAgentCanFollow(IAgent agent);

		// Token: 0x060008EF RID: 2287
		void AddAgentFollowing(IAgent agent);

		// Token: 0x060008F0 RID: 2288
		bool CheckIfAgentCanUnFollow(IAgent agent);

		// Token: 0x060008F1 RID: 2289
		void RemoveAgentFollowing(IAgent agent);

		// Token: 0x060008F2 RID: 2290
		void OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath);

		// Token: 0x060008F3 RID: 2291
		void OnConversationStart(IAgent agent, bool setActionsInstantly);

		// Token: 0x060008F4 RID: 2292
		void OnConversationEnd(IAgent agent);

		// Token: 0x060008F5 RID: 2293
		void EndMission();

		// Token: 0x060008F6 RID: 2294
		void FadeOutCharacter(CharacterObject characterObject);

		// Token: 0x060008F7 RID: 2295
		void OnGameStateChanged();
	}
}

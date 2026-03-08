using System;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000398 RID: 920
	public interface IMapStateHandler
	{
		// Token: 0x060034CC RID: 13516
		void OnRefreshState();

		// Token: 0x060034CD RID: 13517
		void OnMainPartyEncounter();

		// Token: 0x060034CE RID: 13518
		void OnIncidentStarted(Incident incident);

		// Token: 0x060034CF RID: 13519
		void BeforeTick(float dt);

		// Token: 0x060034D0 RID: 13520
		void Tick(float dt);

		// Token: 0x060034D1 RID: 13521
		void AfterTick(float dt);

		// Token: 0x060034D2 RID: 13522
		void AfterWaitTick(float dt);

		// Token: 0x060034D3 RID: 13523
		void OnIdleTick(float dt);

		// Token: 0x060034D4 RID: 13524
		void OnSignalPeriodicEvents();

		// Token: 0x060034D5 RID: 13525
		void OnExit();

		// Token: 0x060034D6 RID: 13526
		void ResetCamera(bool resetDistance, bool teleportToMainParty);

		// Token: 0x060034D7 RID: 13527
		void TeleportCameraToMainParty();

		// Token: 0x060034D8 RID: 13528
		void FastMoveCameraToMainParty();

		// Token: 0x060034D9 RID: 13529
		bool IsCameraLockedToPlayerParty();

		// Token: 0x060034DA RID: 13530
		void StartCameraAnimation(CampaignVec2 targetPosition, float animationStopDuration);

		// Token: 0x060034DB RID: 13531
		void OnHourlyTick();

		// Token: 0x060034DC RID: 13532
		void OnMenuModeTick(float dt);

		// Token: 0x060034DD RID: 13533
		void OnEnteringMenuMode(MenuContext menuContext);

		// Token: 0x060034DE RID: 13534
		void OnExitingMenuMode();

		// Token: 0x060034DF RID: 13535
		void OnBattleSimulationStarted(BattleSimulation battleSimulation);

		// Token: 0x060034E0 RID: 13536
		void OnBattleSimulationEnded();

		// Token: 0x060034E1 RID: 13537
		void OnGameplayCheatsEnabled();

		// Token: 0x060034E2 RID: 13538
		void OnMapConversationStarts(ConversationCharacterData playerCharacterData, ConversationCharacterData conversationPartnerData);

		// Token: 0x060034E3 RID: 13539
		void OnMapConversationOver();

		// Token: 0x060034E4 RID: 13540
		void OnPlayerSiegeActivated();

		// Token: 0x060034E5 RID: 13541
		void OnPlayerSiegeDeactivated();

		// Token: 0x060034E6 RID: 13542
		void OnSiegeEngineClick(MatrixFrame siegeEngineFrame);

		// Token: 0x060034E7 RID: 13543
		void OnGameLoadFinished();

		// Token: 0x060034E8 RID: 13544
		void OnFadeInAndOut(float fadeOutTime, float blackTime, float fadeInTime);
	}
}

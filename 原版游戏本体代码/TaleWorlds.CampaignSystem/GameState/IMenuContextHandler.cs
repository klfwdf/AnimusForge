using System;
using TaleWorlds.CampaignSystem.Roster;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000392 RID: 914
	public interface IMenuContextHandler
	{
		// Token: 0x06003479 RID: 13433
		void OnBackgroundMeshNameSet(string name);

		// Token: 0x0600347A RID: 13434
		void OnOpenTownManagement();

		// Token: 0x0600347B RID: 13435
		void OnOpenRecruitVolunteers();

		// Token: 0x0600347C RID: 13436
		void OnOpenTournamentLeaderboard();

		// Token: 0x0600347D RID: 13437
		void OnOpenTroopSelection(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount);

		// Token: 0x0600347E RID: 13438
		void OnMenuCreate();

		// Token: 0x0600347F RID: 13439
		void OnMenuActivate();

		// Token: 0x06003480 RID: 13440
		void OnMenuRefresh();

		// Token: 0x06003481 RID: 13441
		void OnHourlyTick();

		// Token: 0x06003482 RID: 13442
		void OnPanelSoundIDSet(string panelSoundID);

		// Token: 0x06003483 RID: 13443
		void OnAmbientSoundIDSet(string ambientSoundID);
	}
}

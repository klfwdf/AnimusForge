using System;
using System.Collections.Generic;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions.Tournaments
{
	// Token: 0x02000027 RID: 39
	internal class ArenaPreloadView : MissionView
	{
		// Token: 0x0600010C RID: 268 RVA: 0x0000CBCC File Offset: 0x0000ADCC
		public override void OnPreMissionTick(float dt)
		{
			if (!this._preloadDone)
			{
				List<BasicCharacterObject> list = new List<BasicCharacterObject>();
				if (Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>() != null)
				{
					foreach (CharacterObject item in ArenaPracticeFightMissionController.GetParticipantCharacters(Settlement.CurrentSettlement))
					{
						list.Add(item);
					}
					list.Add(CharacterObject.PlayerCharacter);
				}
				TournamentBehavior missionBehavior = Mission.Current.GetMissionBehavior<TournamentBehavior>();
				if (missionBehavior != null)
				{
					foreach (CharacterObject item2 in missionBehavior.GetAllPossibleParticipants())
					{
						list.Add(item2);
					}
				}
				this._helperInstance.PreloadCharacters(list);
				this._preloadDone = true;
			}
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000CCB0 File Offset: 0x0000AEB0
		public override void OnSceneRenderingStarted()
		{
			this._helperInstance.WaitForMeshesToBeLoaded();
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0000CCBD File Offset: 0x0000AEBD
		public override void OnMissionStateDeactivated()
		{
			base.OnMissionStateDeactivated();
			this._helperInstance.Clear();
		}

		// Token: 0x04000083 RID: 131
		private readonly PreloadHelper _helperInstance = new PreloadHelper();

		// Token: 0x04000084 RID: 132
		private bool _preloadDone;
	}
}

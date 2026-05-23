using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions
{
	// Token: 0x0200001E RID: 30
	public class MissionPreloadView : MissionView
	{
		// Token: 0x060000C9 RID: 201 RVA: 0x00009DF0 File Offset: 0x00007FF0
		public override void OnPreMissionTick(float dt)
		{
			if (!this._preloadDone)
			{
				List<BasicCharacterObject> list = new List<BasicCharacterObject>();
				foreach (PartyBase partyBase in MapEvent.PlayerMapEvent.InvolvedParties)
				{
					foreach (TroopRosterElement troopRosterElement in partyBase.MemberRoster.GetTroopRoster())
					{
						for (int i = 0; i < troopRosterElement.Number; i++)
						{
							list.Add(troopRosterElement.Character);
						}
					}
				}
				this._helperInstance.PreloadCharacters(list);
				SiegeDeploymentMissionController missionBehavior = base.Mission.GetMissionBehavior<SiegeDeploymentMissionController>();
				if (missionBehavior != null)
				{
					this._helperInstance.PreloadItems(missionBehavior.GetSiegeMissiles());
				}
				this._preloadDone = true;
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00009EE0 File Offset: 0x000080E0
		public override void OnSceneRenderingStarted()
		{
			this._helperInstance.WaitForMeshesToBeLoaded();
		}

		// Token: 0x060000CB RID: 203 RVA: 0x00009EED File Offset: 0x000080ED
		public override void OnMissionStateDeactivated()
		{
			base.OnMissionStateDeactivated();
			this._helperInstance.Clear();
		}

		// Token: 0x060000CC RID: 204 RVA: 0x00009F00 File Offset: 0x00008100
		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			this._helperInstance.Clear();
		}

		// Token: 0x0400007B RID: 123
		private readonly PreloadHelper _helperInstance = new PreloadHelper();

		// Token: 0x0400007C RID: 124
		private bool _preloadDone;
	}
}

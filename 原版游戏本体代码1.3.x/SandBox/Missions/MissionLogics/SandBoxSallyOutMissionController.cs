using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000084 RID: 132
	public class SandBoxSallyOutMissionController : SallyOutMissionController
	{
		// Token: 0x0600052E RID: 1326 RVA: 0x00022BC0 File Offset: 0x00020DC0
		public SandBoxSallyOutMissionController(bool isSallyOutAmbush)
			: base(isSallyOutAmbush)
		{
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x00022BC9 File Offset: 0x00020DC9
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			this._mapEvent = MapEvent.PlayerMapEvent;
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x00022BDC File Offset: 0x00020DDC
		protected override void GetInitialTroopCounts(out int besiegedTotalTroopCount, out int besiegerTotalTroopCount)
		{
			besiegedTotalTroopCount = this._mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
			besiegerTotalTroopCount = this._mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
		}

		// Token: 0x040002C4 RID: 708
		private MapEvent _mapEvent;
	}
}

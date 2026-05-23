using System;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000065 RID: 101
	public class CampaignSiegeStateHandler : MissionLogic
	{
		// Token: 0x17000064 RID: 100
		// (get) Token: 0x060003FE RID: 1022 RVA: 0x00017328 File Offset: 0x00015528
		public bool IsSiege
		{
			get
			{
				return this._mapEvent.IsSiegeAssault;
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060003FF RID: 1023 RVA: 0x00017335 File Offset: 0x00015535
		public bool IsSallyOut
		{
			get
			{
				return this._mapEvent.IsSallyOut;
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000400 RID: 1024 RVA: 0x00017342 File Offset: 0x00015542
		public Settlement Settlement
		{
			get
			{
				return this._mapEvent.MapEventSettlement;
			}
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x0001734F File Offset: 0x0001554F
		public CampaignSiegeStateHandler()
		{
			this._mapEvent = PlayerEncounter.Battle;
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x00017362 File Offset: 0x00015562
		public override void OnRetreatMission()
		{
			this._isRetreat = true;
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0001736B File Offset: 0x0001556B
		public override void OnMissionResultReady(MissionResult missionResult)
		{
			this._defenderVictory = missionResult.BattleState == BattleState.DefenderVictory;
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0001737C File Offset: 0x0001557C
		public override void OnSurrenderMission()
		{
			PlayerEncounter.PlayerSurrender = true;
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x00017384 File Offset: 0x00015584
		protected override void OnEndMission()
		{
			if (this.IsSiege && this._mapEvent.PlayerSide == BattleSideEnum.Attacker && !this._isRetreat && !this._defenderVictory)
			{
				this.Settlement.SetNextSiegeState();
			}
		}

		// Token: 0x0400020F RID: 527
		private readonly MapEvent _mapEvent;

		// Token: 0x04000210 RID: 528
		private bool _isRetreat;

		// Token: 0x04000211 RID: 529
		private bool _defenderVictory;
	}
}

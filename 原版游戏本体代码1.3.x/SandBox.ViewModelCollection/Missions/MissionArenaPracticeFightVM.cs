using System;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions
{
	// Token: 0x0200002D RID: 45
	public class MissionArenaPracticeFightVM : ViewModel
	{
		// Token: 0x0600039D RID: 925 RVA: 0x0000F811 File Offset: 0x0000DA11
		public MissionArenaPracticeFightVM(ArenaPracticeFightMissionController practiceMissionController)
		{
			this._practiceMissionController = practiceMissionController;
			this._mission = practiceMissionController.Mission;
		}

		// Token: 0x0600039E RID: 926 RVA: 0x0000F82C File Offset: 0x0000DA2C
		public void Tick()
		{
			this.IsPlayerPracticing = this._practiceMissionController.IsPlayerPracticing;
			Agent mainAgent = this._mission.MainAgent;
			if (mainAgent != null && mainAgent.IsActive())
			{
				int killCount = this._mission.MainAgent.KillCount;
				GameTexts.SetVariable("BEATEN_OPPONENT_COUNT", killCount);
				this.OpponentsBeatenText = GameTexts.FindText("str_beaten_opponent", null).ToString();
			}
			int remainingOpponentCount = this._practiceMissionController.RemainingOpponentCount;
			GameTexts.SetVariable("REMAINING_OPPONENT_COUNT", remainingOpponentCount);
			this.OpponentsRemainingText = GameTexts.FindText("str_remaining_opponent", null).ToString();
			this.UpdatePrizeText();
		}

		// Token: 0x0600039F RID: 927 RVA: 0x0000F8C8 File Offset: 0x0000DAC8
		public void UpdatePrizeText()
		{
			bool remainingOpponentCount = this._practiceMissionController.RemainingOpponentCount != 0;
			int opponentCountBeatenByPlayer = this._practiceMissionController.OpponentCountBeatenByPlayer;
			int content = 0;
			if (!remainingOpponentCount)
			{
				content = 250;
			}
			else if (opponentCountBeatenByPlayer >= 3)
			{
				if (opponentCountBeatenByPlayer < 6)
				{
					content = 5;
				}
				else if (opponentCountBeatenByPlayer < 10)
				{
					content = 10;
				}
				else if (opponentCountBeatenByPlayer < 20)
				{
					content = 25;
				}
				else
				{
					content = 60;
				}
			}
			GameTexts.SetVariable("DENAR_AMOUNT", content);
			GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			this.PrizeText = GameTexts.FindText("str_earned_denar", null).ToString();
		}

		// Token: 0x17000121 RID: 289
		// (get) Token: 0x060003A0 RID: 928 RVA: 0x0000F94B File Offset: 0x0000DB4B
		// (set) Token: 0x060003A1 RID: 929 RVA: 0x0000F953 File Offset: 0x0000DB53
		[DataSourceProperty]
		public string OpponentsBeatenText
		{
			get
			{
				return this._opponentsBeatenText;
			}
			set
			{
				if (this._opponentsBeatenText != value)
				{
					this._opponentsBeatenText = value;
					base.OnPropertyChangedWithValue<string>(value, "OpponentsBeatenText");
				}
			}
		}

		// Token: 0x17000122 RID: 290
		// (get) Token: 0x060003A2 RID: 930 RVA: 0x0000F976 File Offset: 0x0000DB76
		// (set) Token: 0x060003A3 RID: 931 RVA: 0x0000F97E File Offset: 0x0000DB7E
		[DataSourceProperty]
		public string PrizeText
		{
			get
			{
				return this._prizeText;
			}
			set
			{
				if (this._prizeText != value)
				{
					this._prizeText = value;
					base.OnPropertyChangedWithValue<string>(value, "PrizeText");
				}
			}
		}

		// Token: 0x17000123 RID: 291
		// (get) Token: 0x060003A4 RID: 932 RVA: 0x0000F9A1 File Offset: 0x0000DBA1
		// (set) Token: 0x060003A5 RID: 933 RVA: 0x0000F9A9 File Offset: 0x0000DBA9
		[DataSourceProperty]
		public string OpponentsRemainingText
		{
			get
			{
				return this._opponentsRemainingText;
			}
			set
			{
				if (this._opponentsRemainingText != value)
				{
					this._opponentsRemainingText = value;
					base.OnPropertyChangedWithValue<string>(value, "OpponentsRemainingText");
				}
			}
		}

		// Token: 0x17000124 RID: 292
		// (get) Token: 0x060003A6 RID: 934 RVA: 0x0000F9CC File Offset: 0x0000DBCC
		// (set) Token: 0x060003A7 RID: 935 RVA: 0x0000F9D4 File Offset: 0x0000DBD4
		public bool IsPlayerPracticing
		{
			get
			{
				return this._isPlayerPracticing;
			}
			set
			{
				if (this._isPlayerPracticing != value)
				{
					this._isPlayerPracticing = value;
					base.OnPropertyChangedWithValue(value, "IsPlayerPracticing");
				}
			}
		}

		// Token: 0x040001D7 RID: 471
		private readonly Mission _mission;

		// Token: 0x040001D8 RID: 472
		private readonly ArenaPracticeFightMissionController _practiceMissionController;

		// Token: 0x040001D9 RID: 473
		private string _opponentsBeatenText;

		// Token: 0x040001DA RID: 474
		private string _opponentsRemainingText;

		// Token: 0x040001DB RID: 475
		private bool _isPlayerPracticing;

		// Token: 0x040001DC RID: 476
		private string _prizeText;
	}
}

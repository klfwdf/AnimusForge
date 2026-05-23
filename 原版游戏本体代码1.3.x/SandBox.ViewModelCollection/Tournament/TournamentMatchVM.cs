using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Tournament
{
	// Token: 0x0200000D RID: 13
	public class TournamentMatchVM : ViewModel
	{
		// Token: 0x17000028 RID: 40
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00005A1E File Offset: 0x00003C1E
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x00005A26 File Offset: 0x00003C26
		public TournamentMatch Match { get; private set; }

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000B2 RID: 178 RVA: 0x00005A2F File Offset: 0x00003C2F
		public List<TournamentTeamVM> Teams { get; }

		// Token: 0x060000B3 RID: 179 RVA: 0x00005A38 File Offset: 0x00003C38
		public TournamentMatchVM()
		{
			this.Team1 = new TournamentTeamVM();
			this.Team2 = new TournamentTeamVM();
			this.Team3 = new TournamentTeamVM();
			this.Team4 = new TournamentTeamVM();
			this.Teams = new List<TournamentTeamVM> { this.Team1, this.Team2, this.Team3, this.Team4 };
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00005AC0 File Offset: 0x00003CC0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Teams.ForEach(delegate(TournamentTeamVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00005AF4 File Offset: 0x00003CF4
		public void Initialize()
		{
			foreach (TournamentTeamVM tournamentTeamVM in this.Teams)
			{
				if (tournamentTeamVM.IsValid)
				{
					tournamentTeamVM.Initialize();
				}
			}
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00005B50 File Offset: 0x00003D50
		public void Initialize(TournamentMatch match)
		{
			int num = 0;
			this.Match = match;
			this.IsValid = this.Match != null;
			this.Count = match.Teams.Count<TournamentTeam>();
			foreach (TournamentTeam team in match.Teams)
			{
				this.Teams[num].Initialize(team);
				num++;
			}
			this.State = 0;
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00005BDC File Offset: 0x00003DDC
		public void Refresh(bool forceRefresh)
		{
			if (forceRefresh)
			{
				base.OnPropertyChanged("Count");
			}
			for (int i = 0; i < this.Count; i++)
			{
				TournamentTeamVM tournamentTeamVM = this.Teams[i];
				if (forceRefresh)
				{
					base.OnPropertyChanged("Team" + i + 1);
				}
				tournamentTeamVM.Refresh();
				for (int j = 0; j < tournamentTeamVM.Count; j++)
				{
					TournamentParticipantVM tournamentParticipantVM = tournamentTeamVM.Participants[j];
					tournamentParticipantVM.Score = tournamentParticipantVM.Participant.Score.ToString();
					tournamentParticipantVM.IsQualifiedForNextRound = this.Match.Winners.Contains(tournamentParticipantVM.Participant);
				}
			}
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00005C94 File Offset: 0x00003E94
		public void RefreshActiveMatch()
		{
			for (int i = 0; i < this.Count; i++)
			{
				TournamentTeamVM tournamentTeamVM = this.Teams[i];
				for (int j = 0; j < tournamentTeamVM.Count; j++)
				{
					TournamentParticipantVM tournamentParticipantVM = tournamentTeamVM.Participants[j];
					tournamentParticipantVM.Score = tournamentParticipantVM.Participant.Score.ToString();
				}
			}
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00005CF4 File Offset: 0x00003EF4
		public void Refresh(TournamentMatchVM target)
		{
			base.OnPropertyChanged("Count");
			int num = 0;
			foreach (TournamentTeamVM tournamentTeamVM in from t in this.Teams
				where t.IsValid
				select t)
			{
				base.OnPropertyChanged("Team" + num + 1);
				tournamentTeamVM.Refresh();
				num++;
			}
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00005D90 File Offset: 0x00003F90
		public IEnumerable<TournamentParticipantVM> GetParticipants()
		{
			List<TournamentParticipantVM> list = new List<TournamentParticipantVM>();
			if (this.Team1.IsValid)
			{
				list.AddRange(this.Team1.GetParticipants());
			}
			if (this.Team2.IsValid)
			{
				list.AddRange(this.Team2.GetParticipants());
			}
			if (this.Team3.IsValid)
			{
				list.AddRange(this.Team3.GetParticipants());
			}
			if (this.Team4.IsValid)
			{
				list.AddRange(this.Team4.GetParticipants());
			}
			return list;
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000BB RID: 187 RVA: 0x00005E1C File Offset: 0x0000401C
		// (set) Token: 0x060000BC RID: 188 RVA: 0x00005E24 File Offset: 0x00004024
		[DataSourceProperty]
		public bool IsValid
		{
			get
			{
				return this._isValid;
			}
			set
			{
				if (value != this._isValid)
				{
					this._isValid = value;
					base.OnPropertyChangedWithValue(value, "IsValid");
				}
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000BD RID: 189 RVA: 0x00005E42 File Offset: 0x00004042
		// (set) Token: 0x060000BE RID: 190 RVA: 0x00005E4A File Offset: 0x0000404A
		[DataSourceProperty]
		public int State
		{
			get
			{
				return this._state;
			}
			set
			{
				if (value != this._state)
				{
					this._state = value;
					base.OnPropertyChangedWithValue(value, "State");
				}
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000BF RID: 191 RVA: 0x00005E68 File Offset: 0x00004068
		// (set) Token: 0x060000C0 RID: 192 RVA: 0x00005E70 File Offset: 0x00004070
		[DataSourceProperty]
		public int Count
		{
			get
			{
				return this._count;
			}
			set
			{
				if (value != this._count)
				{
					this._count = value;
					base.OnPropertyChangedWithValue(value, "Count");
				}
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000C1 RID: 193 RVA: 0x00005E8E File Offset: 0x0000408E
		// (set) Token: 0x060000C2 RID: 194 RVA: 0x00005E96 File Offset: 0x00004096
		[DataSourceProperty]
		public TournamentTeamVM Team1
		{
			get
			{
				return this._team1;
			}
			set
			{
				if (value != this._team1)
				{
					this._team1 = value;
					base.OnPropertyChangedWithValue<TournamentTeamVM>(value, "Team1");
				}
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x00005EB4 File Offset: 0x000040B4
		// (set) Token: 0x060000C4 RID: 196 RVA: 0x00005EBC File Offset: 0x000040BC
		[DataSourceProperty]
		public TournamentTeamVM Team2
		{
			get
			{
				return this._team2;
			}
			set
			{
				if (value != this._team2)
				{
					this._team2 = value;
					base.OnPropertyChangedWithValue<TournamentTeamVM>(value, "Team2");
				}
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000C5 RID: 197 RVA: 0x00005EDA File Offset: 0x000040DA
		// (set) Token: 0x060000C6 RID: 198 RVA: 0x00005EE2 File Offset: 0x000040E2
		[DataSourceProperty]
		public TournamentTeamVM Team3
		{
			get
			{
				return this._team3;
			}
			set
			{
				if (value != this._team3)
				{
					this._team3 = value;
					base.OnPropertyChangedWithValue<TournamentTeamVM>(value, "Team3");
				}
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000C7 RID: 199 RVA: 0x00005F00 File Offset: 0x00004100
		// (set) Token: 0x060000C8 RID: 200 RVA: 0x00005F08 File Offset: 0x00004108
		[DataSourceProperty]
		public TournamentTeamVM Team4
		{
			get
			{
				return this._team4;
			}
			set
			{
				if (value != this._team4)
				{
					this._team4 = value;
					base.OnPropertyChangedWithValue<TournamentTeamVM>(value, "Team4");
				}
			}
		}

		// Token: 0x04000053 RID: 83
		private TournamentTeamVM _team1;

		// Token: 0x04000054 RID: 84
		private TournamentTeamVM _team2;

		// Token: 0x04000055 RID: 85
		private TournamentTeamVM _team3;

		// Token: 0x04000056 RID: 86
		private TournamentTeamVM _team4;

		// Token: 0x04000057 RID: 87
		private int _count = -1;

		// Token: 0x04000058 RID: 88
		private int _state = -1;

		// Token: 0x04000059 RID: 89
		private bool _isValid;

		// Token: 0x0200006E RID: 110
		public enum TournamentMatchState
		{
			// Token: 0x0400031F RID: 799
			Unfinished,
			// Token: 0x04000320 RID: 800
			Current,
			// Token: 0x04000321 RID: 801
			Over,
			// Token: 0x04000322 RID: 802
			Active
		}
	}
}

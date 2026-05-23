using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Tournament
{
	// Token: 0x02000010 RID: 16
	public class TournamentTeamVM : ViewModel
	{
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000104 RID: 260 RVA: 0x0000660D File Offset: 0x0000480D
		public List<TournamentParticipantVM> Participants { get; }

		// Token: 0x06000105 RID: 261 RVA: 0x00006618 File Offset: 0x00004818
		public TournamentTeamVM()
		{
			this.Participant1 = new TournamentParticipantVM();
			this.Participant2 = new TournamentParticipantVM();
			this.Participant3 = new TournamentParticipantVM();
			this.Participant4 = new TournamentParticipantVM();
			this.Participant5 = new TournamentParticipantVM();
			this.Participant6 = new TournamentParticipantVM();
			this.Participant7 = new TournamentParticipantVM();
			this.Participant8 = new TournamentParticipantVM();
			this.Participants = new List<TournamentParticipantVM> { this.Participant1, this.Participant2, this.Participant3, this.Participant4, this.Participant5, this.Participant6, this.Participant7, this.Participant8 };
		}

		// Token: 0x06000106 RID: 262 RVA: 0x000066F5 File Offset: 0x000048F5
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Participants.ForEach(delegate(TournamentParticipantVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000107 RID: 263 RVA: 0x00006727 File Offset: 0x00004927
		// (set) Token: 0x06000108 RID: 264 RVA: 0x0000672F File Offset: 0x0000492F
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

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000109 RID: 265 RVA: 0x0000674D File Offset: 0x0000494D
		// (set) Token: 0x0600010A RID: 266 RVA: 0x00006755 File Offset: 0x00004955
		[DataSourceProperty]
		public int Score
		{
			get
			{
				return this._score;
			}
			set
			{
				if (value != this._score)
				{
					this._score = value;
					base.OnPropertyChangedWithValue(value, "Score");
				}
			}
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x0600010B RID: 267 RVA: 0x00006773 File Offset: 0x00004973
		// (set) Token: 0x0600010C RID: 268 RVA: 0x0000677B File Offset: 0x0000497B
		[DataSourceProperty]
		public TournamentParticipantVM Participant1
		{
			get
			{
				return this._participant1;
			}
			set
			{
				if (value != this._participant1)
				{
					this._participant1 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant1");
				}
			}
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600010D RID: 269 RVA: 0x00006799 File Offset: 0x00004999
		// (set) Token: 0x0600010E RID: 270 RVA: 0x000067A1 File Offset: 0x000049A1
		[DataSourceProperty]
		public TournamentParticipantVM Participant2
		{
			get
			{
				return this._participant2;
			}
			set
			{
				if (value != this._participant2)
				{
					this._participant2 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant2");
				}
			}
		}

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600010F RID: 271 RVA: 0x000067BF File Offset: 0x000049BF
		// (set) Token: 0x06000110 RID: 272 RVA: 0x000067C7 File Offset: 0x000049C7
		[DataSourceProperty]
		public TournamentParticipantVM Participant3
		{
			get
			{
				return this._participant3;
			}
			set
			{
				if (value != this._participant3)
				{
					this._participant3 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant3");
				}
			}
		}

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000111 RID: 273 RVA: 0x000067E5 File Offset: 0x000049E5
		// (set) Token: 0x06000112 RID: 274 RVA: 0x000067ED File Offset: 0x000049ED
		[DataSourceProperty]
		public TournamentParticipantVM Participant4
		{
			get
			{
				return this._participant4;
			}
			set
			{
				if (value != this._participant4)
				{
					this._participant4 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant4");
				}
			}
		}

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000113 RID: 275 RVA: 0x0000680B File Offset: 0x00004A0B
		// (set) Token: 0x06000114 RID: 276 RVA: 0x00006813 File Offset: 0x00004A13
		[DataSourceProperty]
		public TournamentParticipantVM Participant5
		{
			get
			{
				return this._participant5;
			}
			set
			{
				if (value != this._participant5)
				{
					this._participant5 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant5");
				}
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000115 RID: 277 RVA: 0x00006831 File Offset: 0x00004A31
		// (set) Token: 0x06000116 RID: 278 RVA: 0x00006839 File Offset: 0x00004A39
		[DataSourceProperty]
		public TournamentParticipantVM Participant6
		{
			get
			{
				return this._participant6;
			}
			set
			{
				if (value != this._participant6)
				{
					this._participant6 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant6");
				}
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000117 RID: 279 RVA: 0x00006857 File Offset: 0x00004A57
		// (set) Token: 0x06000118 RID: 280 RVA: 0x0000685F File Offset: 0x00004A5F
		[DataSourceProperty]
		public TournamentParticipantVM Participant7
		{
			get
			{
				return this._participant7;
			}
			set
			{
				if (value != this._participant7)
				{
					this._participant7 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant7");
				}
			}
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000119 RID: 281 RVA: 0x0000687D File Offset: 0x00004A7D
		// (set) Token: 0x0600011A RID: 282 RVA: 0x00006885 File Offset: 0x00004A85
		[DataSourceProperty]
		public TournamentParticipantVM Participant8
		{
			get
			{
				return this._participant8;
			}
			set
			{
				if (value != this._participant8)
				{
					this._participant8 = value;
					base.OnPropertyChangedWithValue<TournamentParticipantVM>(value, "Participant8");
				}
			}
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x0600011B RID: 283 RVA: 0x000068A3 File Offset: 0x00004AA3
		// (set) Token: 0x0600011C RID: 284 RVA: 0x000068AB File Offset: 0x00004AAB
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

		// Token: 0x0600011D RID: 285 RVA: 0x000068CC File Offset: 0x00004ACC
		public void Initialize()
		{
			this.IsValid = this._team != null;
			for (int i = 0; i < this.Count; i++)
			{
				TournamentParticipant participant = this._team.Participants.ElementAtOrDefault(i);
				this.Participants[i].Refresh(participant, Color.FromUint(this._team.TeamColor));
			}
		}

		// Token: 0x0600011E RID: 286 RVA: 0x0000692D File Offset: 0x00004B2D
		public void Initialize(TournamentTeam team)
		{
			this._team = team;
			this.Count = team.TeamSize;
			this.IsValid = this._team != null;
			this.Initialize();
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00006958 File Offset: 0x00004B58
		public void Refresh()
		{
			this.IsValid = this._team != null;
			base.OnPropertyChanged("Count");
			int num = 0;
			foreach (TournamentParticipantVM tournamentParticipantVM in from p in this.Participants
				where p.IsValid
				select p)
			{
				base.OnPropertyChanged("Participant" + num);
				tournamentParticipantVM.Refresh();
				num++;
			}
		}

		// Token: 0x06000120 RID: 288 RVA: 0x000069FC File Offset: 0x00004BFC
		public IEnumerable<TournamentParticipantVM> GetParticipants()
		{
			if (this.Participant1.IsValid)
			{
				yield return this.Participant1;
			}
			if (this.Participant2.IsValid)
			{
				yield return this.Participant2;
			}
			if (this.Participant3.IsValid)
			{
				yield return this.Participant3;
			}
			if (this.Participant4.IsValid)
			{
				yield return this.Participant4;
			}
			if (this.Participant5.IsValid)
			{
				yield return this.Participant5;
			}
			if (this.Participant6.IsValid)
			{
				yield return this.Participant6;
			}
			if (this.Participant7.IsValid)
			{
				yield return this.Participant7;
			}
			if (this.Participant8.IsValid)
			{
				yield return this.Participant8;
			}
			yield break;
		}

		// Token: 0x04000074 RID: 116
		private TournamentTeam _team;

		// Token: 0x04000076 RID: 118
		private int _count = -1;

		// Token: 0x04000077 RID: 119
		private TournamentParticipantVM _participant1;

		// Token: 0x04000078 RID: 120
		private TournamentParticipantVM _participant2;

		// Token: 0x04000079 RID: 121
		private TournamentParticipantVM _participant3;

		// Token: 0x0400007A RID: 122
		private TournamentParticipantVM _participant4;

		// Token: 0x0400007B RID: 123
		private TournamentParticipantVM _participant5;

		// Token: 0x0400007C RID: 124
		private TournamentParticipantVM _participant6;

		// Token: 0x0400007D RID: 125
		private TournamentParticipantVM _participant7;

		// Token: 0x0400007E RID: 126
		private TournamentParticipantVM _participant8;

		// Token: 0x0400007F RID: 127
		private int _score;

		// Token: 0x04000080 RID: 128
		private bool _isValid;
	}
}

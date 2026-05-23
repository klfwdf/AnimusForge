using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Tournament
{
	// Token: 0x0200000F RID: 15
	public class TournamentRoundVM : ViewModel
	{
		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000E6 RID: 230 RVA: 0x00006299 File Offset: 0x00004499
		// (set) Token: 0x060000E7 RID: 231 RVA: 0x000062A1 File Offset: 0x000044A1
		public TournamentRound Round { get; private set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000E8 RID: 232 RVA: 0x000062AA File Offset: 0x000044AA
		public List<TournamentMatchVM> Matches { get; }

		// Token: 0x060000E9 RID: 233 RVA: 0x000062B4 File Offset: 0x000044B4
		public TournamentRoundVM()
		{
			this.Match1 = new TournamentMatchVM();
			this.Match2 = new TournamentMatchVM();
			this.Match3 = new TournamentMatchVM();
			this.Match4 = new TournamentMatchVM();
			this.Match5 = new TournamentMatchVM();
			this.Match6 = new TournamentMatchVM();
			this.Match7 = new TournamentMatchVM();
			this.Match8 = new TournamentMatchVM();
			this.Matches = new List<TournamentMatchVM> { this.Match1, this.Match2, this.Match3, this.Match4, this.Match5, this.Match6, this.Match7, this.Match8 };
		}

		// Token: 0x060000EA RID: 234 RVA: 0x00006391 File Offset: 0x00004591
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Matches.ForEach(delegate(TournamentMatchVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000EB RID: 235 RVA: 0x000063C3 File Offset: 0x000045C3
		// (set) Token: 0x060000EC RID: 236 RVA: 0x000063CB File Offset: 0x000045CB
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

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000ED RID: 237 RVA: 0x000063E9 File Offset: 0x000045E9
		// (set) Token: 0x060000EE RID: 238 RVA: 0x000063F1 File Offset: 0x000045F1
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000EF RID: 239 RVA: 0x00006414 File Offset: 0x00004614
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x0000641C File Offset: 0x0000461C
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

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x0000643A File Offset: 0x0000463A
		// (set) Token: 0x060000F2 RID: 242 RVA: 0x00006442 File Offset: 0x00004642
		[DataSourceProperty]
		public TournamentMatchVM Match1
		{
			get
			{
				return this._match1;
			}
			set
			{
				if (value != this._match1)
				{
					this._match1 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match1");
				}
			}
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00006460 File Offset: 0x00004660
		// (set) Token: 0x060000F4 RID: 244 RVA: 0x00006468 File Offset: 0x00004668
		[DataSourceProperty]
		public TournamentMatchVM Match2
		{
			get
			{
				return this._match2;
			}
			set
			{
				if (value != this._match2)
				{
					this._match2 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match2");
				}
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x00006486 File Offset: 0x00004686
		// (set) Token: 0x060000F6 RID: 246 RVA: 0x0000648E File Offset: 0x0000468E
		[DataSourceProperty]
		public TournamentMatchVM Match3
		{
			get
			{
				return this._match3;
			}
			set
			{
				if (value != this._match3)
				{
					this._match3 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match3");
				}
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x000064AC File Offset: 0x000046AC
		// (set) Token: 0x060000F8 RID: 248 RVA: 0x000064B4 File Offset: 0x000046B4
		[DataSourceProperty]
		public TournamentMatchVM Match4
		{
			get
			{
				return this._match4;
			}
			set
			{
				if (value != this._match4)
				{
					this._match4 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match4");
				}
			}
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060000F9 RID: 249 RVA: 0x000064D2 File Offset: 0x000046D2
		// (set) Token: 0x060000FA RID: 250 RVA: 0x000064DA File Offset: 0x000046DA
		[DataSourceProperty]
		public TournamentMatchVM Match5
		{
			get
			{
				return this._match5;
			}
			set
			{
				if (value != this._match5)
				{
					this._match5 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match5");
				}
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060000FB RID: 251 RVA: 0x000064F8 File Offset: 0x000046F8
		// (set) Token: 0x060000FC RID: 252 RVA: 0x00006500 File Offset: 0x00004700
		[DataSourceProperty]
		public TournamentMatchVM Match6
		{
			get
			{
				return this._match6;
			}
			set
			{
				if (value != this._match6)
				{
					this._match6 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match6");
				}
			}
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060000FD RID: 253 RVA: 0x0000651E File Offset: 0x0000471E
		// (set) Token: 0x060000FE RID: 254 RVA: 0x00006526 File Offset: 0x00004726
		[DataSourceProperty]
		public TournamentMatchVM Match7
		{
			get
			{
				return this._match7;
			}
			set
			{
				if (value != this._match7)
				{
					this._match7 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match7");
				}
			}
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060000FF RID: 255 RVA: 0x00006544 File Offset: 0x00004744
		// (set) Token: 0x06000100 RID: 256 RVA: 0x0000654C File Offset: 0x0000474C
		[DataSourceProperty]
		public TournamentMatchVM Match8
		{
			get
			{
				return this._match8;
			}
			set
			{
				if (value != this._match8)
				{
					this._match8 = value;
					base.OnPropertyChangedWithValue<TournamentMatchVM>(value, "Match8");
				}
			}
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000656C File Offset: 0x0000476C
		public void Initialize()
		{
			for (int i = 0; i < this.Count; i++)
			{
				this.Matches[i].Initialize();
			}
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000659C File Offset: 0x0000479C
		public void Initialize(TournamentRound round, TextObject name)
		{
			this.IsValid = true;
			this.Round = round;
			this.Count = round.Matches.Length;
			for (int i = 0; i < round.Matches.Length; i++)
			{
				this.Matches[i].Initialize(round.Matches[i]);
			}
			this.Name = name.ToString();
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000065FD File Offset: 0x000047FD
		public IEnumerable<TournamentParticipantVM> GetParticipants()
		{
			foreach (TournamentMatchVM tournamentMatchVM in this.Matches)
			{
				if (tournamentMatchVM.IsValid)
				{
					foreach (TournamentParticipantVM tournamentParticipantVM in tournamentMatchVM.GetParticipants())
					{
						yield return tournamentParticipantVM;
					}
					IEnumerator<TournamentParticipantVM> enumerator2 = null;
				}
			}
			List<TournamentMatchVM>.Enumerator enumerator = default(List<TournamentMatchVM>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x04000069 RID: 105
		private TournamentMatchVM _match1;

		// Token: 0x0400006A RID: 106
		private TournamentMatchVM _match2;

		// Token: 0x0400006B RID: 107
		private TournamentMatchVM _match3;

		// Token: 0x0400006C RID: 108
		private TournamentMatchVM _match4;

		// Token: 0x0400006D RID: 109
		private TournamentMatchVM _match5;

		// Token: 0x0400006E RID: 110
		private TournamentMatchVM _match6;

		// Token: 0x0400006F RID: 111
		private TournamentMatchVM _match7;

		// Token: 0x04000070 RID: 112
		private TournamentMatchVM _match8;

		// Token: 0x04000071 RID: 113
		private int _count = -1;

		// Token: 0x04000072 RID: 114
		private string _name;

		// Token: 0x04000073 RID: 115
		private bool _isValid;
	}
}

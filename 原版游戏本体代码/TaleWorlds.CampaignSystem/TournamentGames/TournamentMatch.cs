using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TournamentGames
{
	// Token: 0x020002D1 RID: 721
	public class TournamentMatch
	{
		// Token: 0x17000982 RID: 2434
		// (get) Token: 0x06002712 RID: 10002 RVA: 0x000A2B13 File Offset: 0x000A0D13
		public IEnumerable<TournamentTeam> Teams
		{
			get
			{
				return this._teams.AsEnumerable<TournamentTeam>();
			}
		}

		// Token: 0x17000983 RID: 2435
		// (get) Token: 0x06002713 RID: 10003 RVA: 0x000A2B20 File Offset: 0x000A0D20
		public IEnumerable<TournamentParticipant> Participants
		{
			get
			{
				return this._participants.AsEnumerable<TournamentParticipant>();
			}
		}

		// Token: 0x17000984 RID: 2436
		// (get) Token: 0x06002714 RID: 10004 RVA: 0x000A2B2D File Offset: 0x000A0D2D
		// (set) Token: 0x06002715 RID: 10005 RVA: 0x000A2B35 File Offset: 0x000A0D35
		public TournamentMatch.MatchState State { get; private set; }

		// Token: 0x17000985 RID: 2437
		// (get) Token: 0x06002716 RID: 10006 RVA: 0x000A2B3E File Offset: 0x000A0D3E
		public IEnumerable<TournamentParticipant> Winners
		{
			get
			{
				return this._winners.AsEnumerable<TournamentParticipant>();
			}
		}

		// Token: 0x17000986 RID: 2438
		// (get) Token: 0x06002717 RID: 10007 RVA: 0x000A2B4B File Offset: 0x000A0D4B
		public bool IsReady
		{
			get
			{
				return this.State == TournamentMatch.MatchState.Ready;
			}
		}

		// Token: 0x06002718 RID: 10008 RVA: 0x000A2B58 File Offset: 0x000A0D58
		public TournamentMatch(int participantCount, int numberOfTeamsPerMatch, int numberOfWinnerParticipants, TournamentGame.QualificationMode qualificationMode)
		{
			this._participants = new List<TournamentParticipant>();
			this._participantCount = participantCount;
			this._teams = new TournamentTeam[numberOfTeamsPerMatch];
			this._winners = new List<TournamentParticipant>();
			this._numberOfWinnerParticipants = numberOfWinnerParticipants;
			this.QualificationMode = qualificationMode;
			this._teamSize = participantCount / numberOfTeamsPerMatch;
			int[] array = new int[] { 119, 118, 120, 121 };
			int num = 0;
			for (int i = 0; i < numberOfTeamsPerMatch; i++)
			{
				this._teams[i] = new TournamentTeam(this._teamSize, BannerManager.GetColor(array[num]), Banner.CreateOneColoredEmptyBanner(array[num]));
				num++;
				num %= 4;
			}
			this.State = TournamentMatch.MatchState.Ready;
		}

		// Token: 0x06002719 RID: 10009 RVA: 0x000A2BFE File Offset: 0x000A0DFE
		public void End()
		{
			this.State = TournamentMatch.MatchState.Finished;
			this._winners = this.GetWinners();
		}

		// Token: 0x0600271A RID: 10010 RVA: 0x000A2C14 File Offset: 0x000A0E14
		public void Start()
		{
			if (this.State != TournamentMatch.MatchState.Started)
			{
				this.State = TournamentMatch.MatchState.Started;
				foreach (TournamentParticipant tournamentParticipant in this.Participants)
				{
					tournamentParticipant.ResetScore();
				}
			}
		}

		// Token: 0x0600271B RID: 10011 RVA: 0x000A2C70 File Offset: 0x000A0E70
		public TournamentParticipant GetParticipant(int uniqueSeed)
		{
			return this._participants.FirstOrDefault((TournamentParticipant p) => p.Descriptor.CompareTo(uniqueSeed) == 0);
		}

		// Token: 0x0600271C RID: 10012 RVA: 0x000A2CA1 File Offset: 0x000A0EA1
		public bool IsParticipantRequired()
		{
			return this._participants.Count < this._participantCount;
		}

		// Token: 0x0600271D RID: 10013 RVA: 0x000A2CB8 File Offset: 0x000A0EB8
		public void AddParticipant(TournamentParticipant participant, bool firstTime)
		{
			this._participants.Add(participant);
			foreach (TournamentTeam tournamentTeam in this.Teams)
			{
				if (tournamentTeam.IsParticipantRequired() && ((participant.Team != null && participant.Team.TeamColor == tournamentTeam.TeamColor) || firstTime))
				{
					tournamentTeam.AddParticipant(participant);
					return;
				}
			}
			foreach (TournamentTeam tournamentTeam2 in this.Teams)
			{
				if (tournamentTeam2.IsParticipantRequired())
				{
					tournamentTeam2.AddParticipant(participant);
					break;
				}
			}
		}

		// Token: 0x0600271E RID: 10014 RVA: 0x000A2D84 File Offset: 0x000A0F84
		public bool IsPlayerParticipating()
		{
			return this.Participants.Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
		}

		// Token: 0x0600271F RID: 10015 RVA: 0x000A2DB0 File Offset: 0x000A0FB0
		public bool IsPlayerWinner()
		{
			if (this.IsPlayerParticipating())
			{
				return this.GetWinners().Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
			}
			return false;
		}

		// Token: 0x06002720 RID: 10016 RVA: 0x000A2DE8 File Offset: 0x000A0FE8
		private List<TournamentParticipant> GetWinners()
		{
			List<TournamentParticipant> list = new List<TournamentParticipant>();
			if (this.QualificationMode == TournamentGame.QualificationMode.IndividualScore)
			{
				List<TournamentParticipant> list2 = (from x in this._participants
					orderby x.Score descending
					select x).Take(this._numberOfWinnerParticipants).ToList<TournamentParticipant>();
				using (List<TournamentParticipant>.Enumerator enumerator = this._participants.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						TournamentParticipant tournamentParticipant = enumerator.Current;
						if (list2.Contains(tournamentParticipant))
						{
							tournamentParticipant.IsAssigned = false;
							list.Add(tournamentParticipant);
						}
					}
					return list;
				}
			}
			if (this.QualificationMode == TournamentGame.QualificationMode.TeamScore)
			{
				IOrderedEnumerable<TournamentTeam> orderedEnumerable = from x in this._teams
					orderby x.Score descending
					select x;
				List<TournamentTeam> list3 = orderedEnumerable.Take(this._numberOfWinnerParticipants / this._teamSize).ToList<TournamentTeam>();
				foreach (TournamentTeam tournamentTeam in this._teams)
				{
					if (list3.Contains(tournamentTeam))
					{
						foreach (TournamentParticipant tournamentParticipant2 in tournamentTeam.Participants)
						{
							tournamentParticipant2.IsAssigned = false;
							list.Add(tournamentParticipant2);
						}
					}
				}
				foreach (TournamentTeam tournamentTeam2 in orderedEnumerable)
				{
					int num = this._numberOfWinnerParticipants - list.Count;
					if (tournamentTeam2.Participants.Count<TournamentParticipant>() >= num)
					{
						IOrderedEnumerable<TournamentParticipant> source = from x in tournamentTeam2.Participants
							orderby x.Score descending
							select x;
						list.AddRange(source.Take(num));
						break;
					}
					list.AddRange(tournamentTeam2.Participants);
				}
			}
			return list;
		}

		// Token: 0x04000B62 RID: 2914
		private readonly int _numberOfWinnerParticipants;

		// Token: 0x04000B63 RID: 2915
		public readonly TournamentGame.QualificationMode QualificationMode;

		// Token: 0x04000B64 RID: 2916
		private readonly TournamentTeam[] _teams;

		// Token: 0x04000B65 RID: 2917
		private readonly List<TournamentParticipant> _participants;

		// Token: 0x04000B67 RID: 2919
		private List<TournamentParticipant> _winners;

		// Token: 0x04000B68 RID: 2920
		private readonly int _participantCount;

		// Token: 0x04000B69 RID: 2921
		private int _teamSize;

		// Token: 0x02000675 RID: 1653
		public enum MatchState
		{
			// Token: 0x040019F0 RID: 6640
			Ready,
			// Token: 0x040019F1 RID: 6641
			Started,
			// Token: 0x040019F2 RID: 6642
			Finished
		}
	}
}

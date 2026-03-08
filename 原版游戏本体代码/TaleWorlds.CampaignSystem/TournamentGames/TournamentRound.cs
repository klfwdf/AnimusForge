using System;

namespace TaleWorlds.CampaignSystem.TournamentGames
{
	// Token: 0x020002D3 RID: 723
	public class TournamentRound
	{
		// Token: 0x1700098E RID: 2446
		// (get) Token: 0x06002732 RID: 10034 RVA: 0x000A30E7 File Offset: 0x000A12E7
		// (set) Token: 0x06002733 RID: 10035 RVA: 0x000A30EF File Offset: 0x000A12EF
		public TournamentMatch[] Matches { get; private set; }

		// Token: 0x1700098F RID: 2447
		// (get) Token: 0x06002734 RID: 10036 RVA: 0x000A30F8 File Offset: 0x000A12F8
		// (set) Token: 0x06002735 RID: 10037 RVA: 0x000A3100 File Offset: 0x000A1300
		public int CurrentMatchIndex { get; private set; }

		// Token: 0x17000990 RID: 2448
		// (get) Token: 0x06002736 RID: 10038 RVA: 0x000A3109 File Offset: 0x000A1309
		public TournamentMatch CurrentMatch
		{
			get
			{
				if (this.CurrentMatchIndex >= this.Matches.Length)
				{
					return null;
				}
				return this.Matches[this.CurrentMatchIndex];
			}
		}

		// Token: 0x06002737 RID: 10039 RVA: 0x000A312C File Offset: 0x000A132C
		public TournamentRound(int participantCount, int numberOfMatches, int numberOfTeamsPerMatch, int numberOfWinnerParticipants, TournamentGame.QualificationMode qualificationMode)
		{
			this.Matches = new TournamentMatch[numberOfMatches];
			this.CurrentMatchIndex = 0;
			int participantCount2 = participantCount / numberOfMatches;
			for (int i = 0; i < numberOfMatches; i++)
			{
				this.Matches[i] = new TournamentMatch(participantCount2, numberOfTeamsPerMatch, numberOfWinnerParticipants / numberOfMatches, qualificationMode);
			}
		}

		// Token: 0x06002738 RID: 10040 RVA: 0x000A3178 File Offset: 0x000A1378
		public void OnMatchEnded()
		{
			int currentMatchIndex = this.CurrentMatchIndex;
			this.CurrentMatchIndex = currentMatchIndex + 1;
		}

		// Token: 0x06002739 RID: 10041 RVA: 0x000A3198 File Offset: 0x000A1398
		public void EndMatch()
		{
			this.CurrentMatch.End();
			int currentMatchIndex = this.CurrentMatchIndex;
			this.CurrentMatchIndex = currentMatchIndex + 1;
		}

		// Token: 0x0600273A RID: 10042 RVA: 0x000A31C0 File Offset: 0x000A13C0
		public void AddParticipant(TournamentParticipant participant, bool firstTime = false)
		{
			foreach (TournamentMatch tournamentMatch in this.Matches)
			{
				if (tournamentMatch.IsParticipantRequired())
				{
					tournamentMatch.AddParticipant(participant, firstTime);
					return;
				}
			}
		}
	}
}

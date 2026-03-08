using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TournamentGames
{
	// Token: 0x020002D4 RID: 724
	public class TournamentTeam
	{
		// Token: 0x17000991 RID: 2449
		// (get) Token: 0x0600273B RID: 10043 RVA: 0x000A31F7 File Offset: 0x000A13F7
		// (set) Token: 0x0600273C RID: 10044 RVA: 0x000A31FF File Offset: 0x000A13FF
		public int TeamSize { get; private set; }

		// Token: 0x17000992 RID: 2450
		// (get) Token: 0x0600273D RID: 10045 RVA: 0x000A3208 File Offset: 0x000A1408
		// (set) Token: 0x0600273E RID: 10046 RVA: 0x000A3210 File Offset: 0x000A1410
		public uint TeamColor { get; private set; }

		// Token: 0x17000993 RID: 2451
		// (get) Token: 0x0600273F RID: 10047 RVA: 0x000A3219 File Offset: 0x000A1419
		// (set) Token: 0x06002740 RID: 10048 RVA: 0x000A3221 File Offset: 0x000A1421
		public Banner TeamBanner { get; private set; }

		// Token: 0x17000994 RID: 2452
		// (get) Token: 0x06002741 RID: 10049 RVA: 0x000A322A File Offset: 0x000A142A
		// (set) Token: 0x06002742 RID: 10050 RVA: 0x000A3232 File Offset: 0x000A1432
		public bool IsPlayerTeam { get; private set; }

		// Token: 0x17000995 RID: 2453
		// (get) Token: 0x06002743 RID: 10051 RVA: 0x000A323B File Offset: 0x000A143B
		public IEnumerable<TournamentParticipant> Participants
		{
			get
			{
				return this._participants.AsEnumerable<TournamentParticipant>();
			}
		}

		// Token: 0x17000996 RID: 2454
		// (get) Token: 0x06002744 RID: 10052 RVA: 0x000A3248 File Offset: 0x000A1448
		public int Score
		{
			get
			{
				int num = 0;
				foreach (TournamentParticipant tournamentParticipant in this._participants)
				{
					num += tournamentParticipant.Score;
				}
				return num;
			}
		}

		// Token: 0x06002745 RID: 10053 RVA: 0x000A32A0 File Offset: 0x000A14A0
		public TournamentTeam(int teamSize, uint teamColor, Banner teamBanner)
		{
			this.TeamColor = teamColor;
			this.TeamBanner = teamBanner;
			this.TeamSize = teamSize;
			this._participants = new List<TournamentParticipant>();
		}

		// Token: 0x06002746 RID: 10054 RVA: 0x000A32C8 File Offset: 0x000A14C8
		public bool IsParticipantRequired()
		{
			return this._participants.Count < this.TeamSize;
		}

		// Token: 0x06002747 RID: 10055 RVA: 0x000A32DD File Offset: 0x000A14DD
		public void AddParticipant(TournamentParticipant participant)
		{
			participant.IsAssigned = true;
			this._participants.Add(participant);
			participant.SetTeam(this);
			if (participant.IsPlayer)
			{
				this.IsPlayerTeam = true;
			}
		}

		// Token: 0x04000B72 RID: 2930
		private List<TournamentParticipant> _participants;
	}
}

using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.TournamentGames
{
	// Token: 0x020002D2 RID: 722
	public class TournamentParticipant
	{
		// Token: 0x17000987 RID: 2439
		// (get) Token: 0x06002721 RID: 10017 RVA: 0x000A3008 File Offset: 0x000A1208
		// (set) Token: 0x06002722 RID: 10018 RVA: 0x000A3010 File Offset: 0x000A1210
		public int Score { get; private set; }

		// Token: 0x17000988 RID: 2440
		// (get) Token: 0x06002723 RID: 10019 RVA: 0x000A3019 File Offset: 0x000A1219
		// (set) Token: 0x06002724 RID: 10020 RVA: 0x000A3021 File Offset: 0x000A1221
		public CharacterObject Character { get; private set; }

		// Token: 0x17000989 RID: 2441
		// (get) Token: 0x06002725 RID: 10021 RVA: 0x000A302A File Offset: 0x000A122A
		// (set) Token: 0x06002726 RID: 10022 RVA: 0x000A3032 File Offset: 0x000A1232
		public UniqueTroopDescriptor Descriptor { get; private set; }

		// Token: 0x1700098A RID: 2442
		// (get) Token: 0x06002727 RID: 10023 RVA: 0x000A303B File Offset: 0x000A123B
		// (set) Token: 0x06002728 RID: 10024 RVA: 0x000A3043 File Offset: 0x000A1243
		public TournamentTeam Team { get; private set; }

		// Token: 0x1700098B RID: 2443
		// (get) Token: 0x06002729 RID: 10025 RVA: 0x000A304C File Offset: 0x000A124C
		// (set) Token: 0x0600272A RID: 10026 RVA: 0x000A3054 File Offset: 0x000A1254
		public Equipment MatchEquipment { get; set; }

		// Token: 0x1700098C RID: 2444
		// (get) Token: 0x0600272B RID: 10027 RVA: 0x000A305D File Offset: 0x000A125D
		// (set) Token: 0x0600272C RID: 10028 RVA: 0x000A3065 File Offset: 0x000A1265
		public bool IsAssigned { get; set; }

		// Token: 0x1700098D RID: 2445
		// (get) Token: 0x0600272D RID: 10029 RVA: 0x000A306E File Offset: 0x000A126E
		public bool IsPlayer
		{
			get
			{
				CharacterObject character = this.Character;
				return character != null && character.IsPlayerCharacter;
			}
		}

		// Token: 0x0600272E RID: 10030 RVA: 0x000A3081 File Offset: 0x000A1281
		public TournamentParticipant(CharacterObject character, UniqueTroopDescriptor descriptor = default(UniqueTroopDescriptor))
		{
			this.Character = character;
			this.Descriptor = (descriptor.IsValid ? descriptor : new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed));
			this.Team = null;
			this.IsAssigned = false;
		}

		// Token: 0x0600272F RID: 10031 RVA: 0x000A30BF File Offset: 0x000A12BF
		public void SetTeam(TournamentTeam team)
		{
			this.Team = team;
		}

		// Token: 0x06002730 RID: 10032 RVA: 0x000A30C8 File Offset: 0x000A12C8
		public int AddScore(int score)
		{
			this.Score += score;
			return this.Score;
		}

		// Token: 0x06002731 RID: 10033 RVA: 0x000A30DE File Offset: 0x000A12DE
		public void ResetScore()
		{
			this.Score = 0;
		}
	}
}

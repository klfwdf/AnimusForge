using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics
{
	// Token: 0x0200002D RID: 45
	public class TournamentBehavior : MissionLogic, ICameraModeLogic
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000158 RID: 344 RVA: 0x000089B7 File Offset: 0x00006BB7
		public TournamentGame TournamentGame
		{
			get
			{
				return this._tournamentGame;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000159 RID: 345 RVA: 0x000089BF File Offset: 0x00006BBF
		// (set) Token: 0x0600015A RID: 346 RVA: 0x000089C7 File Offset: 0x00006BC7
		public TournamentRound[] Rounds { get; private set; }

		// Token: 0x0600015B RID: 347 RVA: 0x000089D0 File Offset: 0x00006BD0
		public SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
		{
			if (!this.IsPlayerParticipating)
			{
				return SpectatorCameraTypes.LockToAnyAgent;
			}
			return SpectatorCameraTypes.Invalid;
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600015C RID: 348 RVA: 0x000089DD File Offset: 0x00006BDD
		// (set) Token: 0x0600015D RID: 349 RVA: 0x000089E5 File Offset: 0x00006BE5
		public bool IsPlayerEliminated { get; private set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600015E RID: 350 RVA: 0x000089EE File Offset: 0x00006BEE
		// (set) Token: 0x0600015F RID: 351 RVA: 0x000089F6 File Offset: 0x00006BF6
		public int CurrentRoundIndex { get; private set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000160 RID: 352 RVA: 0x000089FF File Offset: 0x00006BFF
		// (set) Token: 0x06000161 RID: 353 RVA: 0x00008A07 File Offset: 0x00006C07
		public TournamentMatch LastMatch { get; private set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00008A10 File Offset: 0x00006C10
		public TournamentRound CurrentRound
		{
			get
			{
				return this.Rounds[this.CurrentRoundIndex];
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000163 RID: 355 RVA: 0x00008A1F File Offset: 0x00006C1F
		public TournamentRound NextRound
		{
			get
			{
				if (this.CurrentRoundIndex != 3)
				{
					return this.Rounds[this.CurrentRoundIndex + 1];
				}
				return null;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00008A3B File Offset: 0x00006C3B
		public TournamentMatch CurrentMatch
		{
			get
			{
				return this.CurrentRound.CurrentMatch;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000165 RID: 357 RVA: 0x00008A48 File Offset: 0x00006C48
		// (set) Token: 0x06000166 RID: 358 RVA: 0x00008A50 File Offset: 0x00006C50
		public TournamentParticipant Winner { get; private set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000167 RID: 359 RVA: 0x00008A59 File Offset: 0x00006C59
		// (set) Token: 0x06000168 RID: 360 RVA: 0x00008A61 File Offset: 0x00006C61
		public bool IsPlayerParticipating { get; private set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000169 RID: 361 RVA: 0x00008A6A File Offset: 0x00006C6A
		// (set) Token: 0x0600016A RID: 362 RVA: 0x00008A72 File Offset: 0x00006C72
		public Settlement Settlement { get; private set; }

		// Token: 0x0600016B RID: 363 RVA: 0x00008A7C File Offset: 0x00006C7C
		public TournamentBehavior(TournamentGame tournamentGame, Settlement settlement, ITournamentGameBehavior gameBehavior, bool isPlayerParticipating)
		{
			this.Settlement = settlement;
			this._tournamentGame = tournamentGame;
			this._gameBehavior = gameBehavior;
			this.Rounds = new TournamentRound[4];
			this.CreateParticipants(isPlayerParticipating);
			this.CurrentRoundIndex = -1;
			this.LastMatch = null;
			this.Winner = null;
			this.IsPlayerParticipating = isPlayerParticipating;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00008AD5 File Offset: 0x00006CD5
		public MBList<CharacterObject> GetAllPossibleParticipants()
		{
			return this._tournamentGame.GetParticipantCharacters(this.Settlement, true);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00008AEC File Offset: 0x00006CEC
		private void CreateParticipants(bool includePlayer)
		{
			this._participants = new TournamentParticipant[this._tournamentGame.MaximumParticipantCount];
			MBList<CharacterObject> participantCharacters = this._tournamentGame.GetParticipantCharacters(this.Settlement, includePlayer);
			participantCharacters.Shuffle<CharacterObject>();
			int num = 0;
			while (num < participantCharacters.Count && num < this._tournamentGame.MaximumParticipantCount)
			{
				this._participants[num] = new TournamentParticipant(participantCharacters[num], default(UniqueTroopDescriptor));
				num++;
			}
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00008B64 File Offset: 0x00006D64
		public static void DeleteTournamentSetsExcept(GameEntity selectedSetEntity)
		{
			List<GameEntity> list = Mission.Current.Scene.FindEntitiesWithTag("arena_set").ToList<GameEntity>();
			list.Remove(selectedSetEntity);
			foreach (GameEntity gameEntity in list)
			{
				gameEntity.Remove(93);
			}
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00008BD4 File Offset: 0x00006DD4
		public static void DeleteAllTournamentSets()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("arena_set").ToList<GameEntity>())
			{
				gameEntity.Remove(94);
			}
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00008C3C File Offset: 0x00006E3C
		public override void AfterStart()
		{
			this.CurrentRoundIndex = 0;
			this.CreateTournamentTree();
			this.FillParticipants(this._participants.ToList<TournamentParticipant>());
			this.CalculateBet();
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00008C62 File Offset: 0x00006E62
		public override void OnMissionTick(float dt)
		{
			if (this.CurrentMatch != null && this.CurrentMatch.State == TournamentMatch.MatchState.Started && this._gameBehavior.IsMatchEnded())
			{
				this.EndCurrentMatch(false);
			}
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00008C90 File Offset: 0x00006E90
		public void StartMatch()
		{
			if (this.CurrentMatch.IsPlayerParticipating())
			{
				Campaign.Current.TournamentManager.OnPlayerJoinMatch(this._tournamentGame.GetType());
			}
			this.CurrentMatch.Start();
			base.Mission.SetMissionMode(MissionMode.Tournament, true);
			this._gameBehavior.StartMatch(this.CurrentMatch, this.NextRound == null);
			CampaignEventDispatcher.Instance.OnPlayerStartedTournamentMatch(this.Settlement.Town);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00008D0B File Offset: 0x00006F0B
		public void SkipMatch(bool isLeave = false)
		{
			this.CurrentMatch.Start();
			this._gameBehavior.SkipMatch(this.CurrentMatch);
			this.EndCurrentMatch(isLeave);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00008D30 File Offset: 0x00006F30
		private void EndCurrentMatch(bool isLeave)
		{
			this.LastMatch = this.CurrentMatch;
			this.CurrentRound.EndMatch();
			this._gameBehavior.OnMatchEnded();
			if (this.LastMatch.IsPlayerParticipating())
			{
				if (this.LastMatch.Winners.All((TournamentParticipant x) => x.Character != CharacterObject.PlayerCharacter))
				{
					this.OnPlayerEliminated();
				}
				else
				{
					this.OnPlayerWinMatch();
				}
			}
			if (this.NextRound != null)
			{
				for (;;)
				{
					if (!this.LastMatch.Winners.Any((TournamentParticipant x) => !x.IsAssigned))
					{
						break;
					}
					foreach (TournamentParticipant tournamentParticipant in this.LastMatch.Winners)
					{
						if (!tournamentParticipant.IsAssigned)
						{
							this.NextRound.AddParticipant(tournamentParticipant, false);
							tournamentParticipant.IsAssigned = true;
						}
					}
				}
			}
			if (this.CurrentRound.CurrentMatch == null)
			{
				if (this.CurrentRoundIndex < 3)
				{
					int i = this.CurrentRoundIndex;
					this.CurrentRoundIndex = i + 1;
					this.CalculateBet();
					MissionGameModels missionGameModels = MissionGameModels.Current;
					if (missionGameModels == null)
					{
						return;
					}
					AgentStatCalculateModel agentStatCalculateModel = missionGameModels.AgentStatCalculateModel;
					if (agentStatCalculateModel == null)
					{
						return;
					}
					agentStatCalculateModel.SetAILevelMultiplier(1f + (float)this.CurrentRoundIndex / 3f);
					return;
				}
				else
				{
					MissionGameModels missionGameModels2 = MissionGameModels.Current;
					if (missionGameModels2 != null)
					{
						AgentStatCalculateModel agentStatCalculateModel2 = missionGameModels2.AgentStatCalculateModel;
						if (agentStatCalculateModel2 != null)
						{
							agentStatCalculateModel2.ResetAILevelMultiplier();
						}
					}
					this.CalculateBet();
					MBInformationManager.AddQuickInformation(new TextObject("{=tWzLqegB}Tournament is over.", null), 0, null, null, "");
					this.Winner = this.LastMatch.Winners.FirstOrDefault<TournamentParticipant>();
					if (this.Winner.Character.IsHero)
					{
						if (this.Winner.Character == CharacterObject.PlayerCharacter)
						{
							this.OnPlayerWinTournament();
						}
						Campaign.Current.TournamentManager.GivePrizeToWinner(this._tournamentGame, this.Winner.Character.HeroObject, true);
						Campaign.Current.TournamentManager.AddLeaderboardEntry(this.Winner.Character.HeroObject);
					}
					MBList<CharacterObject> mblist = new MBList<CharacterObject>(this._participants.Length);
					foreach (TournamentParticipant tournamentParticipant2 in this._participants)
					{
						mblist.Add(tournamentParticipant2.Character);
					}
					CampaignEventDispatcher.Instance.OnTournamentFinished(this.Winner.Character, mblist, this.Settlement.Town, this._tournamentGame.Prize);
					if (this.TournamentEnd != null && !isLeave)
					{
						this.TournamentEnd();
					}
				}
			}
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00008FD4 File Offset: 0x000071D4
		public void EndTournamentViaLeave()
		{
			while (this.CurrentMatch != null)
			{
				this.SkipMatch(true);
			}
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00008FE8 File Offset: 0x000071E8
		private void OnPlayerEliminated()
		{
			this.IsPlayerEliminated = true;
			this.BetOdd = 0f;
			if (this.BettedDenars > 0)
			{
				GiveGoldAction.ApplyForCharacterToSettlement(null, Settlement.CurrentSettlement, this.BettedDenars, false);
			}
			this.OverallExpectedDenars = 0;
			CampaignEventDispatcher.Instance.OnPlayerEliminatedFromTournament(this.CurrentRoundIndex, this.Settlement.Town);
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00009044 File Offset: 0x00007244
		private void OnPlayerWinMatch()
		{
			Campaign.Current.TournamentManager.OnPlayerWinMatch(this._tournamentGame.GetType());
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00009060 File Offset: 0x00007260
		private void OnPlayerWinTournament()
		{
			if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
			{
				return;
			}
			if (Hero.MainHero.MapFaction.IsKingdomFaction && Hero.MainHero.MapFaction.Leader != Hero.MainHero)
			{
				GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, 1f);
			}
			if (this.OverallExpectedDenars > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, this.OverallExpectedDenars, false);
			}
			Campaign.Current.TournamentManager.OnPlayerWinTournament(this._tournamentGame.GetType());
		}

		// Token: 0x06000179 RID: 377 RVA: 0x000090E8 File Offset: 0x000072E8
		private void CreateTournamentTree()
		{
			int num = 16;
			int b = (int)MathF.Log((float)this._tournamentGame.MaxTeamSize, 2f);
			for (int i = 0; i < 4; i++)
			{
				int num2 = (int)MathF.Log((float)num, 2f);
				int num3 = MBRandom.RandomInt(1, MathF.Min(MathF.Min(3, num2), this._tournamentGame.MaxTeamNumberPerMatch));
				int num4 = MathF.Min(num2 - num3, b);
				int num5 = MathF.Ceiling(MathF.Log((float)(1 + MBRandom.RandomInt((int)MathF.Pow(2f, (float)num4))), 2f));
				int x = num2 - (num3 + num5);
				this.Rounds[i] = new TournamentRound(num, MathF.PowTwo32(x), MathF.PowTwo32(num3), num / 2, this._tournamentGame.Mode);
				num /= 2;
			}
		}

		// Token: 0x0600017A RID: 378 RVA: 0x000091BC File Offset: 0x000073BC
		private void FillParticipants(List<TournamentParticipant> participants)
		{
			foreach (TournamentParticipant participant in participants)
			{
				this.Rounds[this.CurrentRoundIndex].AddParticipant(participant, true);
			}
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00009218 File Offset: 0x00007418
		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			InquiryData result = null;
			canPlayerLeave = false;
			return result;
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600017C RID: 380 RVA: 0x0000921E File Offset: 0x0000741E
		// (set) Token: 0x0600017D RID: 381 RVA: 0x00009226 File Offset: 0x00007426
		public float BetOdd { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600017E RID: 382 RVA: 0x0000922F File Offset: 0x0000742F
		public int MaximumBetInstance
		{
			get
			{
				return MathF.Min(150, this.PlayerDenars);
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x0600017F RID: 383 RVA: 0x00009241 File Offset: 0x00007441
		// (set) Token: 0x06000180 RID: 384 RVA: 0x00009249 File Offset: 0x00007449
		public int BettedDenars { get; private set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000181 RID: 385 RVA: 0x00009252 File Offset: 0x00007452
		// (set) Token: 0x06000182 RID: 386 RVA: 0x0000925A File Offset: 0x0000745A
		public int OverallExpectedDenars { get; private set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000183 RID: 387 RVA: 0x00009263 File Offset: 0x00007463
		public int PlayerDenars
		{
			get
			{
				return Hero.MainHero.Gold;
			}
		}

		// Token: 0x06000184 RID: 388 RVA: 0x0000926F File Offset: 0x0000746F
		public void PlaceABet(int bet)
		{
			this.BettedDenars += bet;
			this.OverallExpectedDenars += this.GetExpectedDenarsForBet(bet);
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, bet, true);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x000092A0 File Offset: 0x000074A0
		public int GetExpectedDenarsForBet(int bet)
		{
			return (int)(this.BetOdd * (float)bet);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x000092AC File Offset: 0x000074AC
		public int GetMaximumBet()
		{
			int num = 150;
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.DeepPockets))
			{
				num *= (int)DefaultPerks.Roguery.DeepPockets.PrimaryBonus;
			}
			return num;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x000092E0 File Offset: 0x000074E0
		private void CalculateBet()
		{
			if (this.IsPlayerParticipating)
			{
				if (this.CurrentRound.CurrentMatch == null)
				{
					this.BetOdd = 0f;
					return;
				}
				if (this.IsPlayerEliminated || !this.IsPlayerParticipating)
				{
					this.OverallExpectedDenars = 0;
					this.BetOdd = 0f;
					return;
				}
				List<KeyValuePair<Hero, int>> leaderboard = Campaign.Current.TournamentManager.GetLeaderboard();
				int num = 0;
				int num2 = 0;
				for (int i = 0; i < leaderboard.Count; i++)
				{
					if (leaderboard[i].Key == Hero.MainHero)
					{
						num = leaderboard[i].Value;
					}
					if (leaderboard[i].Value > num2)
					{
						num2 = leaderboard[i].Value;
					}
				}
				float num3 = 30f + (float)Hero.MainHero.Level + (float)MathF.Max(0, num * 12 - num2 * 2);
				float num4 = 0f;
				float num5 = 0f;
				float num6 = 0f;
				foreach (TournamentMatch tournamentMatch in this.CurrentRound.Matches)
				{
					foreach (TournamentTeam tournamentTeam in tournamentMatch.Teams)
					{
						float num7 = 0f;
						foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
						{
							if (tournamentParticipant.Character != CharacterObject.PlayerCharacter)
							{
								int num8 = 0;
								if (tournamentParticipant.Character.IsHero)
								{
									for (int k = 0; k < leaderboard.Count; k++)
									{
										if (leaderboard[k].Key == tournamentParticipant.Character.HeroObject)
										{
											num8 = leaderboard[k].Value;
										}
									}
								}
								num7 += (float)(tournamentParticipant.Character.Level + MathF.Max(0, num8 * 8 - num2 * 2));
							}
						}
						if (tournamentTeam.Participants.Any((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter))
						{
							num5 = num7;
							foreach (TournamentTeam tournamentTeam2 in tournamentMatch.Teams)
							{
								if (tournamentTeam != tournamentTeam2)
								{
									foreach (TournamentParticipant tournamentParticipant2 in tournamentTeam2.Participants)
									{
										int num9 = 0;
										if (tournamentParticipant2.Character.IsHero)
										{
											for (int l = 0; l < leaderboard.Count; l++)
											{
												if (leaderboard[l].Key == tournamentParticipant2.Character.HeroObject)
												{
													num9 = leaderboard[l].Value;
												}
											}
										}
										num6 += (float)(tournamentParticipant2.Character.Level + MathF.Max(0, num9 * 8 - num2 * 2));
									}
								}
							}
						}
						num4 += num7;
					}
				}
				float num10 = (num5 + num3) / (num6 + num5 + num3);
				float num11 = num3 / (num5 + num3 + 0.5f * (num4 - (num5 + num6)));
				float num12 = num10 * num11;
				float num13 = MathF.Clamp(MathF.Pow(1f / num12, 0.75f), 1.1f, 4f);
				this.BetOdd = (float)((int)(num13 * 10f)) / 10f;
			}
		}

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000188 RID: 392 RVA: 0x000096FC File Offset: 0x000078FC
		// (remove) Token: 0x06000189 RID: 393 RVA: 0x00009734 File Offset: 0x00007934
		public event Action TournamentEnd;

		// Token: 0x04000066 RID: 102
		public const int RoundCount = 4;

		// Token: 0x04000067 RID: 103
		public const int ParticipantCount = 16;

		// Token: 0x04000068 RID: 104
		public const float EndMatchTimerDuration = 6f;

		// Token: 0x04000069 RID: 105
		public const float CheerTimerDuration = 1f;

		// Token: 0x0400006A RID: 106
		private TournamentGame _tournamentGame;

		// Token: 0x0400006B RID: 107
		private ITournamentGameBehavior _gameBehavior;

		// Token: 0x0400006D RID: 109
		private TournamentParticipant[] _participants;

		// Token: 0x04000075 RID: 117
		private const int MaximumBet = 150;

		// Token: 0x04000076 RID: 118
		public const float MaximumOdd = 4f;
	}
}

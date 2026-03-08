using System;
using psai.net;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions.Sound.Components
{
	// Token: 0x0200002E RID: 46
	public class MusicTournamentMissionView : MissionView, IMusicHandler
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000183 RID: 387 RVA: 0x00012354 File Offset: 0x00010554
		bool IMusicHandler.IsPausable
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00012358 File Offset: 0x00010558
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			MBMusicManager.Current.DeactivateCurrentMode();
			MBMusicManager.Current.ActivateBattleMode();
			MBMusicManager.Current.OnBattleMusicHandlerInit(this);
			this._startTimer = new Timer(Mission.Current.CurrentTime, 3f, true);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x000123A8 File Offset: 0x000105A8
		public override void EarlyStart()
		{
			this._allOneShotSoundEventsAreDisabled = false;
			this._tournamentBehavior = Mission.Current.GetMissionBehavior<TournamentBehavior>();
			this._currentMatch = null;
			this._lastMatch = null;
			this._arenaSoundEntity = base.Mission.Scene.FindEntityWithTag("arena_sound");
			SoundManager.SetGlobalParameter("ArenaIntensity", 0f);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00012404 File Offset: 0x00010604
		public override void OnMissionScreenFinalize()
		{
			SoundManager.SetGlobalParameter("ArenaIntensity", 0f);
			MBMusicManager.Current.DeactivateBattleMode();
			MBMusicManager.Current.OnBattleMusicHandlerFinalize();
		}

		// Token: 0x06000187 RID: 391 RVA: 0x0001242C File Offset: 0x0001062C
		private void CheckIntensityFall()
		{
			PsaiInfo psaiInfo = PsaiCore.Instance.GetPsaiInfo();
			if (psaiInfo.effectiveThemeId >= 0)
			{
				if (float.IsNaN(psaiInfo.currentIntensity))
				{
					MBMusicManager.Current.ChangeCurrentThemeIntensity(MusicParameters.MinIntensity);
					return;
				}
				if (psaiInfo.currentIntensity < MusicParameters.MinIntensity)
				{
					MBMusicManager.Current.ChangeCurrentThemeIntensity(MusicParameters.MinIntensity - psaiInfo.currentIntensity);
				}
			}
		}

		// Token: 0x06000188 RID: 392 RVA: 0x00012494 File Offset: 0x00010694
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (this._fightStarted)
			{
				bool flag = affectedAgent.IsMine || (affectedAgent.RiderAgent != null && affectedAgent.RiderAgent.IsMine);
				Team team = affectedAgent.Team;
				BattleSideEnum battleSideEnum = ((team != null) ? team.Side : BattleSideEnum.None);
				bool flag2;
				if (!flag)
				{
					if (battleSideEnum != BattleSideEnum.None)
					{
						Team playerTeam = Mission.Current.PlayerTeam;
						flag2 = ((playerTeam != null) ? playerTeam.Side : BattleSideEnum.None) == battleSideEnum;
					}
					else
					{
						flag2 = false;
					}
				}
				else
				{
					flag2 = true;
				}
				bool flag3 = flag2;
				if ((affectedAgent.IsHuman && affectedAgent.State != AgentState.Routed) || flag)
				{
					float num = (flag3 ? MusicParameters.FriendlyTroopDeadEffectOnIntensity : MusicParameters.EnemyTroopDeadEffectOnIntensity);
					if (flag)
					{
						num *= MusicParameters.PlayerTroopDeadEffectMultiplierOnIntensity;
					}
					MBMusicManager.Current.ChangeCurrentThemeIntensity(num);
				}
			}
			if (affectedAgent != null && affectorAgent != null && affectorAgent.IsMainAgent && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
			{
				int num2 = 0;
				if (affectedAgent.Team == affectorAgent.Team)
				{
					if (affectedAgent.IsHuman)
					{
						num2 += -30;
					}
					else
					{
						num2 += -20;
					}
				}
				else if (affectedAgent.IsHuman)
				{
					num2++;
					if (affectedAgent.HasMount)
					{
						num2++;
					}
					if (killingBlow.OverrideKillInfo == Agent.KillInfo.Headshot)
					{
						num2 += 3;
					}
					if (killingBlow.IsMissile)
					{
						num2++;
					}
					else
					{
						num2 += 2;
					}
				}
				else if (affectedAgent.RiderAgent != null)
				{
					num2 += 3;
				}
				this.UpdateAudienceIntensity(num2, false);
			}
		}

		// Token: 0x06000189 RID: 393 RVA: 0x000125F4 File Offset: 0x000107F4
		void IMusicHandler.OnUpdated(float dt)
		{
			if (!this._fightStarted && Agent.Main != null && Agent.Main.IsActive() && this._startTimer.Check(Mission.Current.CurrentTime))
			{
				this._fightStarted = true;
				MBMusicManager.Current.StartTheme(MusicTheme.BattleSmall, 0.5f, false);
			}
			if (this._fightStarted)
			{
				this.CheckIntensityFall();
			}
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0001265C File Offset: 0x0001085C
		public override void OnMissionTick(float dt)
		{
			if (this._tournamentBehavior != null)
			{
				if (this._currentMatch != this._tournamentBehavior.CurrentMatch)
				{
					TournamentMatch currentMatch = this._tournamentBehavior.CurrentMatch;
					if (currentMatch != null && currentMatch.IsPlayerParticipating())
					{
						Agent main = Agent.Main;
						if (main != null && main.IsActive())
						{
							this._currentMatch = this._tournamentBehavior.CurrentMatch;
							this.OnTournamentRoundBegin(this._tournamentBehavior.NextRound == null);
						}
					}
				}
				if (this._lastMatch != this._tournamentBehavior.LastMatch)
				{
					this._lastMatch = this._tournamentBehavior.LastMatch;
					if (this._tournamentBehavior.NextRound == null || this._tournamentBehavior.LastMatch.IsPlayerParticipating())
					{
						this.OnTournamentRoundEnd();
					}
				}
			}
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00012724 File Offset: 0x00010924
		public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
		{
			if (affectorAgent != null && affectedAgent != null && affectorAgent.IsMainAgent && affectedAgent.IsHuman && affectedAgent.Position.Distance(affectorAgent.Position) >= 15f && (blow.VictimBodyPart == BoneBodyPartType.Head || blow.VictimBodyPart == BoneBodyPartType.Neck))
			{
				this.UpdateAudienceIntensity(3, false);
			}
		}

		// Token: 0x0600018C RID: 396 RVA: 0x0001277D File Offset: 0x0001097D
		public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
		{
			if (!isCanceled && attacker != null && victim != null && attacker.IsMainAgent && victim.IsHuman && collisionData.IsShieldBroken)
			{
				this.UpdateAudienceIntensity(2, false);
			}
		}

		// Token: 0x0600018D RID: 397 RVA: 0x000127A9 File Offset: 0x000109A9
		public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
		{
			if (!isCanceled && attacker != null && victim != null && attacker.IsMainAgent && victim.IsHuman && collisionData.IsShieldBroken)
			{
				this.UpdateAudienceIntensity(2, false);
			}
		}

		// Token: 0x0600018E RID: 398 RVA: 0x000127D8 File Offset: 0x000109D8
		private void UpdateAudienceIntensity(int intensityChangeAmount, bool isEnd = false)
		{
			MusicTournamentMissionView.ReactionType reactionType;
			if (!isEnd)
			{
				reactionType = ((intensityChangeAmount >= 0) ? MusicTournamentMissionView.ReactionType.Positive : MusicTournamentMissionView.ReactionType.Negative);
			}
			else
			{
				reactionType = MusicTournamentMissionView.ReactionType.End;
			}
			this._currentTournamentIntensity += intensityChangeAmount;
			bool flag = false;
			if (this._currentTournamentIntensity > 60)
			{
				flag = this._arenaIntensityLevel != MusicTournamentMissionView.ArenaIntensityLevel.High;
				this._arenaIntensityLevel = MusicTournamentMissionView.ArenaIntensityLevel.High;
			}
			else if (this._currentTournamentIntensity > 30)
			{
				flag = this._arenaIntensityLevel != MusicTournamentMissionView.ArenaIntensityLevel.Mid;
				this._arenaIntensityLevel = MusicTournamentMissionView.ArenaIntensityLevel.Mid;
			}
			else if (this._currentTournamentIntensity <= 30)
			{
				flag = this._arenaIntensityLevel != MusicTournamentMissionView.ArenaIntensityLevel.Low;
				this._arenaIntensityLevel = MusicTournamentMissionView.ArenaIntensityLevel.Low;
			}
			if (flag)
			{
				SoundManager.SetGlobalParameter("ArenaIntensity", (float)this._arenaIntensityLevel);
			}
			if (!this._allOneShotSoundEventsAreDisabled)
			{
				this.Cheer(reactionType);
			}
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00012888 File Offset: 0x00010A88
		private void Cheer(MusicTournamentMissionView.ReactionType reactionType)
		{
			string text = null;
			switch (reactionType)
			{
			case MusicTournamentMissionView.ReactionType.Positive:
				text = "event:/mission/ambient/arena/reaction";
				break;
			case MusicTournamentMissionView.ReactionType.Negative:
				text = "event:/mission/ambient/arena/negative_reaction";
				break;
			case MusicTournamentMissionView.ReactionType.End:
				text = "event:/mission/ambient/arena/reaction";
				break;
			}
			if (text != null)
			{
				string eventFullName = text;
				Vec3 globalPosition = this._arenaSoundEntity.GlobalPosition;
				SoundManager.StartOneShotEvent(eventFullName, globalPosition);
			}
		}

		// Token: 0x06000190 RID: 400 RVA: 0x000128D9 File Offset: 0x00010AD9
		public void OnTournamentRoundBegin(bool isFinalRound)
		{
			this._isFinalRound = isFinalRound;
			this.UpdateAudienceIntensity(0, false);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x000128EC File Offset: 0x00010AEC
		public void OnTournamentRoundEnd()
		{
			int num = 10;
			if (this._lastMatch.IsPlayerWinner())
			{
				num += 10;
			}
			this.UpdateAudienceIntensity(num, this._isFinalRound);
		}

		// Token: 0x040000DE RID: 222
		private const string ArenaSoundTag = "arena_sound";

		// Token: 0x040000DF RID: 223
		private const string ArenaIntensityParameterId = "ArenaIntensity";

		// Token: 0x040000E0 RID: 224
		private const string ArenaPositiveReactionsSoundId = "event:/mission/ambient/arena/reaction";

		// Token: 0x040000E1 RID: 225
		private const string ArenaNegativeReactionsSoundId = "event:/mission/ambient/arena/negative_reaction";

		// Token: 0x040000E2 RID: 226
		private const string ArenaTournamentEndSoundId = "event:/mission/ambient/arena/reaction";

		// Token: 0x040000E3 RID: 227
		private const int MainAgentKnocksDownAnOpponentBaseIntensityChange = 1;

		// Token: 0x040000E4 RID: 228
		private const int MainAgentKnocksDownAnOpponentHeadShotIntensityChange = 3;

		// Token: 0x040000E5 RID: 229
		private const int MainAgentKnocksDownAnOpponentMountedTargetIntensityChange = 1;

		// Token: 0x040000E6 RID: 230
		private const int MainAgentKnocksDownAnOpponentRangedHitIntensityChange = 1;

		// Token: 0x040000E7 RID: 231
		private const int MainAgentKnocksDownAnOpponentMeleeHitIntensityChange = 2;

		// Token: 0x040000E8 RID: 232
		private const int MainAgentHeadShotFrom15MetersRangeIntensityChange = 3;

		// Token: 0x040000E9 RID: 233
		private const int MainAgentDismountsAnOpponentIntensityChange = 3;

		// Token: 0x040000EA RID: 234
		private const int MainAgentBreaksAShieldIntensityChange = 2;

		// Token: 0x040000EB RID: 235
		private const int MainAgentWinsTournamentRoundIntensityChange = 10;

		// Token: 0x040000EC RID: 236
		private const int RoundEndIntensityChange = 10;

		// Token: 0x040000ED RID: 237
		private const int MainAgentKnocksDownATeamMateIntensityChange = -30;

		// Token: 0x040000EE RID: 238
		private const int MainAgentKnocksDownAFriendlyHorseIntensityChange = -20;

		// Token: 0x040000EF RID: 239
		private int _currentTournamentIntensity;

		// Token: 0x040000F0 RID: 240
		private MusicTournamentMissionView.ArenaIntensityLevel _arenaIntensityLevel;

		// Token: 0x040000F1 RID: 241
		private bool _allOneShotSoundEventsAreDisabled;

		// Token: 0x040000F2 RID: 242
		private TournamentBehavior _tournamentBehavior;

		// Token: 0x040000F3 RID: 243
		private TournamentMatch _currentMatch;

		// Token: 0x040000F4 RID: 244
		private TournamentMatch _lastMatch;

		// Token: 0x040000F5 RID: 245
		private GameEntity _arenaSoundEntity;

		// Token: 0x040000F6 RID: 246
		private bool _isFinalRound;

		// Token: 0x040000F7 RID: 247
		private bool _fightStarted;

		// Token: 0x040000F8 RID: 248
		private Timer _startTimer;

		// Token: 0x02000098 RID: 152
		private enum ArenaIntensityLevel
		{
			// Token: 0x04000306 RID: 774
			None,
			// Token: 0x04000307 RID: 775
			Low,
			// Token: 0x04000308 RID: 776
			Mid,
			// Token: 0x04000309 RID: 777
			High
		}

		// Token: 0x02000099 RID: 153
		private enum ReactionType
		{
			// Token: 0x0400030B RID: 779
			Positive,
			// Token: 0x0400030C RID: 780
			Negative,
			// Token: 0x0400030D RID: 781
			End
		}
	}
}

using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using psai.net;

namespace SandBox.View.Missions.Sound.Components;

public class MusicTournamentMissionView : MissionView, IMusicHandler
{
	private enum ArenaIntensityLevel
	{
		None,
		Low,
		Mid,
		High
	}

	private enum ReactionType
	{
		Positive,
		Negative,
		End
	}

	private const string ArenaSoundTag = "arena_sound";

	private const string ArenaIntensityParameterId = "ArenaIntensity";

	private const string ArenaPositiveReactionsSoundId = "event:/mission/ambient/arena/reaction";

	private const string ArenaNegativeReactionsSoundId = "event:/mission/ambient/arena/negative_reaction";

	private const string ArenaTournamentEndSoundId = "event:/mission/ambient/arena/reaction";

	private const int MainAgentKnocksDownAnOpponentBaseIntensityChange = 1;

	private const int MainAgentKnocksDownAnOpponentHeadShotIntensityChange = 3;

	private const int MainAgentKnocksDownAnOpponentMountedTargetIntensityChange = 1;

	private const int MainAgentKnocksDownAnOpponentRangedHitIntensityChange = 1;

	private const int MainAgentKnocksDownAnOpponentMeleeHitIntensityChange = 2;

	private const int MainAgentHeadShotFrom15MetersRangeIntensityChange = 3;

	private const int MainAgentDismountsAnOpponentIntensityChange = 3;

	private const int MainAgentBreaksAShieldIntensityChange = 2;

	private const int MainAgentWinsTournamentRoundIntensityChange = 10;

	private const int RoundEndIntensityChange = 10;

	private const int MainAgentKnocksDownATeamMateIntensityChange = -30;

	private const int MainAgentKnocksDownAFriendlyHorseIntensityChange = -20;

	private int _currentTournamentIntensity;

	private ArenaIntensityLevel _arenaIntensityLevel;

	private bool _allOneShotSoundEventsAreDisabled;

	private TournamentBehavior _tournamentBehavior;

	private TournamentMatch _currentMatch;

	private TournamentMatch _lastMatch;

	private GameEntity _arenaSoundEntity;

	private bool _isFinalRound;

	private bool _fightStarted;

	private Timer _startTimer;

	bool IMusicHandler.IsPausable => false;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.ActivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerInit(this);
		_startTimer = new Timer(Mission.Current.CurrentTime, 3f);
	}

	public override void EarlyStart()
	{
		_allOneShotSoundEventsAreDisabled = false;
		_tournamentBehavior = Mission.Current.GetMissionBehavior<TournamentBehavior>();
		_currentMatch = null;
		_lastMatch = null;
		_arenaSoundEntity = base.Mission.Scene.FindEntityWithTag("arena_sound");
		SoundManager.SetGlobalParameter("ArenaIntensity", 0f);
	}

	public override void OnMissionScreenFinalize()
	{
		SoundManager.SetGlobalParameter("ArenaIntensity", 0f);
		MBMusicManager.Current.DeactivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerFinalize();
	}

	private void CheckIntensityFall()
	{
		PsaiInfo psaiInfo = PsaiCore.Instance.GetPsaiInfo();
		if (psaiInfo.effectiveThemeId >= 0)
		{
			if (float.IsNaN(psaiInfo.currentIntensity))
			{
				MBMusicManager.Current.ChangeCurrentThemeIntensity(MusicParameters.MinIntensity);
			}
			else if (psaiInfo.currentIntensity < MusicParameters.MinIntensity)
			{
				MBMusicManager.Current.ChangeCurrentThemeIntensity(MusicParameters.MinIntensity - psaiInfo.currentIntensity);
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (_fightStarted)
		{
			bool flag = affectedAgent.IsMine || (affectedAgent.RiderAgent != null && affectedAgent.RiderAgent.IsMine);
			BattleSideEnum battleSideEnum = affectedAgent.Team?.Side ?? BattleSideEnum.None;
			bool flag2 = flag || (battleSideEnum != BattleSideEnum.None && (Mission.Current.PlayerTeam?.Side ?? BattleSideEnum.None) == battleSideEnum);
			if ((affectedAgent.IsHuman && affectedAgent.State != AgentState.Routed) || flag)
			{
				float num = (flag2 ? MusicParameters.FriendlyTroopDeadEffectOnIntensity : MusicParameters.EnemyTroopDeadEffectOnIntensity);
				if (flag)
				{
					num *= MusicParameters.PlayerTroopDeadEffectMultiplierOnIntensity;
				}
				MBMusicManager.Current.ChangeCurrentThemeIntensity(num);
			}
		}
		if (affectedAgent == null || affectorAgent == null || !affectorAgent.IsMainAgent || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
		{
			return;
		}
		int num2 = 0;
		if (affectedAgent.Team == affectorAgent.Team)
		{
			num2 = ((!affectedAgent.IsHuman) ? (num2 + -20) : (num2 + -30));
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
			num2 = ((!killingBlow.IsMissile) ? (num2 + 2) : (num2 + 1));
		}
		else if (affectedAgent.RiderAgent != null)
		{
			num2 += 3;
		}
		UpdateAudienceIntensity(num2);
	}

	void IMusicHandler.OnUpdated(float dt)
	{
		if (!_fightStarted && Agent.Main != null && Agent.Main.IsActive() && _startTimer.Check(Mission.Current.CurrentTime))
		{
			_fightStarted = true;
			MBMusicManager.Current.StartTheme(MusicTheme.BattleSmall, 0.5f);
		}
		if (_fightStarted)
		{
			CheckIntensityFall();
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (_tournamentBehavior == null)
		{
			return;
		}
		if (_currentMatch != _tournamentBehavior.CurrentMatch)
		{
			TournamentMatch currentMatch = _tournamentBehavior.CurrentMatch;
			if (currentMatch != null && currentMatch.IsPlayerParticipating())
			{
				Agent main = Agent.Main;
				if (main != null && main.IsActive())
				{
					_currentMatch = _tournamentBehavior.CurrentMatch;
					OnTournamentRoundBegin(_tournamentBehavior.NextRound == null);
				}
			}
		}
		if (_lastMatch != _tournamentBehavior.LastMatch)
		{
			_lastMatch = _tournamentBehavior.LastMatch;
			if (_tournamentBehavior.NextRound == null || _tournamentBehavior.LastMatch.IsPlayerParticipating())
			{
				OnTournamentRoundEnd();
			}
		}
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent != null && affectedAgent != null && affectorAgent.IsMainAgent && affectedAgent.IsHuman && affectedAgent.Position.Distance(affectorAgent.Position) >= 15f && (blow.VictimBodyPart == BoneBodyPartType.Head || blow.VictimBodyPart == BoneBodyPartType.Neck))
		{
			UpdateAudienceIntensity(3);
		}
	}

	public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		if (!isCanceled && attacker != null && victim != null && attacker.IsMainAgent && victim.IsHuman && collisionData.IsShieldBroken)
		{
			UpdateAudienceIntensity(2);
		}
	}

	public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		if (!isCanceled && attacker != null && victim != null && attacker.IsMainAgent && victim.IsHuman && collisionData.IsShieldBroken)
		{
			UpdateAudienceIntensity(2);
		}
	}

	private void UpdateAudienceIntensity(int intensityChangeAmount, bool isEnd = false)
	{
		ReactionType reactionType = (isEnd ? ReactionType.End : ((intensityChangeAmount < 0) ? ReactionType.Negative : ReactionType.Positive));
		_currentTournamentIntensity += intensityChangeAmount;
		bool flag = false;
		if (_currentTournamentIntensity > 60)
		{
			flag = _arenaIntensityLevel != ArenaIntensityLevel.High;
			_arenaIntensityLevel = ArenaIntensityLevel.High;
		}
		else if (_currentTournamentIntensity > 30)
		{
			flag = _arenaIntensityLevel != ArenaIntensityLevel.Mid;
			_arenaIntensityLevel = ArenaIntensityLevel.Mid;
		}
		else if (_currentTournamentIntensity <= 30)
		{
			flag = _arenaIntensityLevel != ArenaIntensityLevel.Low;
			_arenaIntensityLevel = ArenaIntensityLevel.Low;
		}
		if (flag)
		{
			SoundManager.SetGlobalParameter("ArenaIntensity", (float)_arenaIntensityLevel);
		}
		if (!_allOneShotSoundEventsAreDisabled)
		{
			Cheer(reactionType);
		}
	}

	private void Cheer(ReactionType reactionType)
	{
		string text = null;
		switch (reactionType)
		{
		case ReactionType.Positive:
			text = "event:/mission/ambient/arena/reaction";
			break;
		case ReactionType.Negative:
			text = "event:/mission/ambient/arena/negative_reaction";
			break;
		case ReactionType.End:
			text = "event:/mission/ambient/arena/reaction";
			break;
		}
		if (text != null)
		{
			SoundManager.StartOneShotEvent(text, _arenaSoundEntity.GlobalPosition);
		}
	}

	public void OnTournamentRoundBegin(bool isFinalRound)
	{
		_isFinalRound = isFinalRound;
		UpdateAudienceIntensity(0);
	}

	public void OnTournamentRoundEnd()
	{
		int num = 10;
		if (_lastMatch.IsPlayerWinner())
		{
			num += 10;
		}
		UpdateAudienceIntensity(num, _isFinalRound);
	}
}

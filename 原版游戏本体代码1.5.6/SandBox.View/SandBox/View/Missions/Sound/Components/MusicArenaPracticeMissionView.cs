using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions.Sound.Components;

public class MusicArenaPracticeMissionView : MissionView, IMusicHandler
{
	private enum ArenaIntensityLevel
	{
		None,
		Low,
		Mid,
		High
	}

	private const string ArenaSoundTag = "arena_sound";

	private const string ArenaIntensityParameterId = "ArenaIntensity";

	private const string ArenaPositiveReactionsSoundId = "event:/mission/ambient/arena/reaction";

	private const int MainAgentKnocksDownAnOpponentBaseIntensityChange = 1;

	private const int MainAgentKnocksDownAnOpponentHeadShotIntensityChange = 3;

	private const int MainAgentKnocksDownAnOpponentMountedTargetIntensityChange = 1;

	private const int MainAgentKnocksDownAnOpponentRangedHitIntensityChange = 1;

	private const int MainAgentKnocksDownAnOpponentMeleeHitIntensityChange = 2;

	private const int MainAgentHeadShotFrom15MetersRangeIntensityChange = 3;

	private const int MainAgentDismountsAnOpponentIntensityChange = 3;

	private const int MainAgentBreaksAShieldIntensityChange = 2;

	private int _currentTournamentIntensity;

	private ArenaIntensityLevel _currentArenaIntensityLevel;

	private bool _allOneShotSoundEventsAreDisabled;

	private GameEntity _arenaSoundEntity;

	bool IMusicHandler.IsPausable => false;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.ActivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerInit(this);
	}

	public override void EarlyStart()
	{
		_allOneShotSoundEventsAreDisabled = false;
		_arenaSoundEntity = base.Mission.Scene.FindEntityWithTag("arena_sound");
		SoundManager.SetGlobalParameter("ArenaIntensity", 0f);
	}

	public override void OnMissionScreenFinalize()
	{
		SoundManager.SetGlobalParameter("ArenaIntensity", 0f);
		MBMusicManager.Current.DeactivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerFinalize();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (affectedAgent == null || affectorAgent == null || !affectorAgent.IsMainAgent || (agentState != AgentState.Killed && agentState != AgentState.Unconscious))
		{
			return;
		}
		if (affectedAgent.Team != affectorAgent.Team)
		{
			if (affectedAgent.IsHuman)
			{
				_currentTournamentIntensity++;
				if (affectedAgent.HasMount)
				{
					_currentTournamentIntensity++;
				}
				if (killingBlow.OverrideKillInfo == Agent.KillInfo.Headshot)
				{
					_currentTournamentIntensity += 3;
				}
				if (killingBlow.IsMissile)
				{
					_currentTournamentIntensity++;
				}
				else
				{
					_currentTournamentIntensity += 2;
				}
			}
			else if (affectedAgent.RiderAgent != null)
			{
				_currentTournamentIntensity += 3;
			}
		}
		UpdateAudienceIntensity();
	}

	public override void OnMissionTick(float dt)
	{
	}

	public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	{
		if (affectorAgent != null && affectedAgent != null && affectorAgent.IsMainAgent && affectedAgent.IsHuman && affectedAgent.Position.Distance(affectorAgent.Position) >= 15f && (blow.VictimBodyPart == BoneBodyPartType.Head || blow.VictimBodyPart == BoneBodyPartType.Neck))
		{
			_currentTournamentIntensity += 3;
			UpdateAudienceIntensity();
		}
	}

	public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		if (!isCanceled && attacker != null && victim != null && attacker.IsMainAgent && victim.IsHuman && collisionData.IsShieldBroken)
		{
			_currentTournamentIntensity += 2;
			UpdateAudienceIntensity();
		}
	}

	public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		if (!isCanceled && attacker != null && victim != null && attacker.IsMainAgent && victim.IsHuman && collisionData.IsShieldBroken)
		{
			_currentTournamentIntensity += 2;
			UpdateAudienceIntensity();
		}
	}

	private void UpdateAudienceIntensity()
	{
		bool flag = false;
		if (_currentTournamentIntensity > 60)
		{
			flag = _currentArenaIntensityLevel != ArenaIntensityLevel.High;
			_currentArenaIntensityLevel = ArenaIntensityLevel.High;
		}
		else if (_currentTournamentIntensity > 30)
		{
			flag = _currentArenaIntensityLevel != ArenaIntensityLevel.Mid;
			_currentArenaIntensityLevel = ArenaIntensityLevel.Mid;
		}
		else if (_currentTournamentIntensity <= 30)
		{
			flag = _currentArenaIntensityLevel != ArenaIntensityLevel.Low;
			_currentArenaIntensityLevel = ArenaIntensityLevel.Low;
		}
		if (flag)
		{
			SoundManager.SetGlobalParameter("ArenaIntensity", (float)_currentArenaIntensityLevel);
		}
		if (!_allOneShotSoundEventsAreDisabled)
		{
			Cheer();
		}
	}

	private void Cheer()
	{
		SoundManager.StartOneShotEvent("event:/mission/ambient/arena/reaction", _arenaSoundEntity.GlobalPosition);
	}

	public void OnUpdated(float dt)
	{
	}
}

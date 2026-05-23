using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using psai.net;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Sound;

public class MusicBattleMissionView : MissionView, IMusicHandler
{
	private enum BattleState
	{
		Starting,
		Started,
		TurnedOneSide,
		Ending
	}

	private const float ChargeOrderIntensityIncreaseCooldownInSeconds = 60f;

	private BattleState _battleState;

	private DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;

	private int[] _startingTroopCounts;

	private float _startingBattleRatio;

	private bool _isSiegeBattle;

	private bool _isPaganBattle;

	private MissionTime _nextPossibleTimeToIncreaseIntensityForChargeOrder;

	bool IMusicHandler.IsPausable => false;

	private BattleSideEnum PlayerSide => Mission.Current.PlayerTeam?.Side ?? BattleSideEnum.None;

	public MusicBattleMissionView(bool isSiegeBattle)
	{
		_isSiegeBattle = isSiegeBattle;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionAgentSpawnLogic = Mission.Current.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.ActivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerInit(this);
	}

	public override void OnMissionScreenFinalize()
	{
		MBMusicManager.Current.DeactivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerFinalize();
		base.Mission.PlayerTeam.PlayerOrderController.OnOrderIssued -= PlayerOrderControllerOnOrderIssued;
	}

	public override void AfterStart()
	{
		_nextPossibleTimeToIncreaseIntensityForChargeOrder = MissionTime.Now;
		base.Mission.PlayerTeam.PlayerOrderController.OnOrderIssued += PlayerOrderControllerOnOrderIssued;
	}

	private void PlayerOrderControllerOnOrderIssued(OrderType orderType, IEnumerable<Formation> appliedFormations, OrderController orderController, object[] parameters)
	{
		if ((orderType == OrderType.Charge || orderType == OrderType.ChargeWithTarget) && _nextPossibleTimeToIncreaseIntensityForChargeOrder.IsPast)
		{
			float currentIntensity = PsaiCore.Instance.GetCurrentIntensity();
			float deltaIntensity = currentIntensity * MusicParameters.PlayerChargeEffectMultiplierOnIntensity - currentIntensity;
			MBMusicManager.Current.ChangeCurrentThemeIntensity(deltaIntensity);
			_nextPossibleTimeToIncreaseIntensityForChargeOrder = MissionTime.Now + MissionTime.Seconds(60f);
		}
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

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (_battleState == BattleState.Starting)
		{
			return;
		}
		bool flag = affectedAgent.IsMine || (affectedAgent.RiderAgent != null && affectedAgent.RiderAgent.IsMine);
		BattleSideEnum battleSideEnum = affectedAgent.Team?.Side ?? BattleSideEnum.None;
		bool flag2 = flag || (battleSideEnum != BattleSideEnum.None && (Mission.Current.PlayerTeam?.Side ?? BattleSideEnum.None) == battleSideEnum);
		if (!_isSiegeBattle && affectedAgent.IsHuman && battleSideEnum != BattleSideEnum.None && _battleState == BattleState.Started && _startingTroopCounts.Sum() >= MusicParameters.SmallBattleTreshold && MissionTime.Now.ToSeconds > (double)MusicParameters.BattleTurnsOneSideCooldown && _missionAgentSpawnLogic.NumberOfRemainingTroops == 0)
		{
			int[] array = new int[2] { _missionAgentSpawnLogic.NumberOfActiveDefenderTroops, _missionAgentSpawnLogic.NumberOfActiveAttackerTroops };
			array[(int)battleSideEnum]--;
			MusicTheme musicTheme = MusicTheme.None;
			if (array[0] > 0 && array[1] > 0)
			{
				float num = (float)array[0] / (float)array[1];
				if (num < _startingBattleRatio * MusicParameters.BattleRatioTresholdOnIntensity)
				{
					musicTheme = MBMusicManager.Current.GetBattleTurnsOneSideTheme(base.Mission.MusicCulture, PlayerSide != BattleSideEnum.Defender, _isPaganBattle);
				}
				else if (num > _startingBattleRatio / MusicParameters.BattleRatioTresholdOnIntensity)
				{
					musicTheme = MBMusicManager.Current.GetBattleTurnsOneSideTheme(base.Mission.MusicCulture, PlayerSide == BattleSideEnum.Defender, _isPaganBattle);
				}
			}
			if (musicTheme != MusicTheme.None)
			{
				MBMusicManager.Current.StartTheme(musicTheme, PsaiCore.Instance.GetCurrentIntensity());
				_battleState = BattleState.TurnedOneSide;
			}
		}
		if ((affectedAgent.IsHuman && affectedAgent.State != AgentState.Routed) || flag)
		{
			float num2 = (flag2 ? MusicParameters.FriendlyTroopDeadEffectOnIntensity : MusicParameters.EnemyTroopDeadEffectOnIntensity);
			if (flag)
			{
				num2 *= MusicParameters.PlayerTroopDeadEffectMultiplierOnIntensity;
			}
			MBMusicManager.Current.ChangeCurrentThemeIntensity(num2);
		}
	}

	private void CheckForStarting()
	{
		if (_startingTroopCounts == null)
		{
			_startingTroopCounts = new int[2]
			{
				_missionAgentSpawnLogic.GetTotalNumberOfTroopsForSide(BattleSideEnum.Defender),
				_missionAgentSpawnLogic.GetTotalNumberOfTroopsForSide(BattleSideEnum.Attacker)
			};
			_startingBattleRatio = (float)_startingTroopCounts[0] / (float)_startingTroopCounts[1];
		}
		Vec2 vec = Agent.Main?.Position.AsVec2 ?? Vec2.Invalid;
		bool flag = Mission.Current.PlayerTeam?.FormationsIncludingEmpty.Any((Formation f) => f.CountOfUnits > 0) ?? false;
		float num = float.MaxValue;
		if (flag || vec.IsValid)
		{
			foreach (Formation item in Mission.Current.PlayerEnemyTeam.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits <= 0)
				{
					continue;
				}
				float num2 = float.MaxValue;
				if (!flag && vec.IsValid)
				{
					num2 = vec.DistanceSquared(item.CurrentPosition);
				}
				else if (flag)
				{
					foreach (Formation item2 in Mission.Current.PlayerTeam.FormationsIncludingEmpty)
					{
						if (item2.CountOfUnits > 0)
						{
							float num3 = item2.CurrentPosition.DistanceSquared(item.CurrentPosition);
							if (num2 > num3)
							{
								num2 = num3;
							}
						}
					}
				}
				if (num > num2)
				{
					num = num2;
				}
			}
		}
		int num4 = _startingTroopCounts.Sum();
		bool flag2 = false;
		if (num4 < MusicParameters.SmallBattleTreshold)
		{
			if (num < MusicParameters.SmallBattleDistanceTreshold * MusicParameters.SmallBattleDistanceTreshold)
			{
				flag2 = true;
			}
		}
		else if (num4 < MusicParameters.MediumBattleTreshold)
		{
			if (num < MusicParameters.MediumBattleDistanceTreshold * MusicParameters.MediumBattleDistanceTreshold)
			{
				flag2 = true;
			}
		}
		else if (num4 < MusicParameters.LargeBattleTreshold)
		{
			if (num < MusicParameters.LargeBattleDistanceTreshold * MusicParameters.LargeBattleDistanceTreshold)
			{
				flag2 = true;
			}
		}
		else if (num < MusicParameters.MaxBattleDistanceTreshold * MusicParameters.MaxBattleDistanceTreshold)
		{
			flag2 = true;
		}
		if (flag2)
		{
			float num5 = (float)num4 / 1000f;
			float startIntensity = MusicParameters.DefaultStartIntensity + num5 * MusicParameters.BattleSizeEffectOnStartIntensity + (MBRandom.RandomFloat - 0.5f) * (MusicParameters.RandomEffectMultiplierOnStartIntensity * 2f);
			MusicTheme theme = (_isSiegeBattle ? MBMusicManager.Current.GetSiegeTheme(base.Mission.MusicCulture) : MBMusicManager.Current.GetBattleTheme(base.Mission.MusicCulture, num4, out _isPaganBattle));
			MBMusicManager.Current.StartTheme(theme, startIntensity);
			_battleState = BattleState.Started;
		}
	}

	private void CheckForEnding()
	{
		if (Mission.Current.IsMissionEnding)
		{
			if (Mission.Current.MissionResult != null)
			{
				MusicTheme battleEndTheme = MBMusicManager.Current.GetBattleEndTheme(base.Mission.MusicCulture, Mission.Current.MissionResult.PlayerVictory);
				MBMusicManager.Current.StartTheme(battleEndTheme, PsaiCore.Instance.GetPsaiInfo().currentIntensity, queueEndSegment: true);
				_battleState = BattleState.Ending;
			}
			else
			{
				MBMusicManager.Current.StartTheme(MusicTheme.BattleDefeat, PsaiCore.Instance.GetPsaiInfo().currentIntensity, queueEndSegment: true);
				_battleState = BattleState.Ending;
			}
		}
	}

	void IMusicHandler.OnUpdated(float dt)
	{
		if (_battleState == BattleState.Starting)
		{
			if (base.Mission.MusicCulture == null && Mission.Current.GetMissionBehavior<DeploymentHandler>() == null && _missionAgentSpawnLogic.IsDeploymentOver)
			{
				KeyValuePair<BasicCultureObject, int> keyValuePair = new KeyValuePair<BasicCultureObject, int>(null, -1);
				Dictionary<BasicCultureObject, int> dictionary = new Dictionary<BasicCultureObject, int>();
				foreach (Team team in base.Mission.Teams)
				{
					foreach (Agent activeAgent in team.ActiveAgents)
					{
						BasicCultureObject culture = activeAgent.Character.Culture;
						if (culture != null && culture.IsMainCulture)
						{
							if (!dictionary.ContainsKey(activeAgent.Character.Culture))
							{
								dictionary.Add(activeAgent.Character.Culture, 0);
							}
							dictionary[activeAgent.Character.Culture]++;
							if (dictionary[activeAgent.Character.Culture] > keyValuePair.Value)
							{
								keyValuePair = new KeyValuePair<BasicCultureObject, int>(activeAgent.Character.Culture, dictionary[activeAgent.Character.Culture]);
							}
						}
					}
				}
				if (keyValuePair.Key != null)
				{
					base.Mission.MusicCulture = keyValuePair.Key;
				}
				else
				{
					base.Mission.MusicCulture = Game.Current.PlayerTroop.Culture;
				}
			}
			if (base.Mission.MusicCulture != null)
			{
				CheckForStarting();
			}
		}
		if (_battleState == BattleState.Started || _battleState == BattleState.TurnedOneSide)
		{
			CheckForEnding();
		}
		CheckIntensityFall();
	}
}

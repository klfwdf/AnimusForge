using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using psai.net;

namespace NavalDLC.View;

internal class MusicNavalBattleMissionView : MissionView, IMusicHandler
{
	private enum BattleState
	{
		Starting,
		Started,
		TurnedOneSide,
		Ending
	}

	private enum NavalBattleThemes
	{
		VikingSeaBattle1 = 10241,
		VikingSeaBattle2,
		MediterraneanSeaBattle1,
		Maintheme,
		MediterraneanSeaBattle2
	}

	private const float ChargeOrderIntensityIncreaseCooldownInSeconds = 60f;

	private const float BattleSizeEffectOnStartIntensity = 0.8f;

	private const string CultureSturgia = "sturgia";

	private const string CultureBattania = "battania";

	private const string CultureNord = "nord";

	private BattleState _battleState;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private float _waterStrengthIntensityMultiplier;

	private float _mainAgentBaseHealth;

	private int[] _startingTroopCounts;

	private MissionTime _nextPossibleTimeToIncreaseIntensityForChargeOrder;

	bool IMusicHandler.IsPausable => false;

	private MatrixFrame _listenerGlobalFrame => SoundManager.GetListenerFrame();

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = Mission.Current.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic.ShipSunkEvent += OnShipSunk;
		_navalShipsLogic.ShipRammingEvent += OnShipRamming;
		_navalShipsLogic.ShipHookThrowEvent += OnShipHookThrow;
		_waterStrengthIntensityMultiplier = 1f + MathF.Max(0f, (Mission.Current.Scene.GetWaterStrength() - 3f) * 0.07f);
		_mainAgentBaseHealth = 0f;
		MBMusicManager.Current.DeactivateCurrentMode();
		MBMusicManager.Current.ActivateBattleMode();
		MBMusicManager.Current.OnBattleMusicHandlerInit(this);
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipSunkEvent -= OnShipSunk;
		_navalShipsLogic.ShipRammingEvent -= OnShipRamming;
		_navalShipsLogic.ShipHookThrowEvent -= OnShipHookThrow;
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
			float num = currentIntensity * MusicParameters.PlayerChargeEffectMultiplierOnIntensity - currentIntensity;
			MBMusicManager.Current.ChangeCurrentThemeIntensity(num * _waterStrengthIntensityMultiplier);
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
		if ((affectedAgent.IsHuman && affectedAgent.State != AgentState.Routed) || flag)
		{
			float num = (flag2 ? MusicParameters.FriendlyTroopDeadEffectOnIntensity : MusicParameters.EnemyTroopDeadEffectOnIntensity);
			if (flag)
			{
				num *= MusicParameters.PlayerTroopDeadEffectMultiplierOnIntensity;
			}
			MBMusicManager.Current.ChangeCurrentThemeIntensity(num * _waterStrengthIntensityMultiplier);
		}
	}

	public void OnShipSunk(MissionShip ship)
	{
		float num = _listenerGlobalFrame.origin.DistanceSquared(ship.GameEntity.GlobalPosition);
		if (num < 62500f)
		{
			float num2 = MathF.Max(0.5f - MathF.Sqrt(num) * 0.002f, 0.1f);
			MBMusicManager.Current.ChangeCurrentThemeIntensity(num2 * _waterStrengthIntensityMultiplier);
		}
	}

	public void OnShipRamming(MissionShip rammingShip, MissionShip rammedShip, float damagePercent, bool isFirstImpact, CapsuleData capsuleData, int ramQuality)
	{
		float num = _listenerGlobalFrame.origin.DistanceSquared(rammingShip.GameEntity.GetBodyWorldTransform().origin);
		if (num < 10000f)
		{
			float b = (isFirstImpact ? 0.2f : 0f);
			float num2 = MathF.Max(2f * damagePercent * (1f - MathF.Sqrt(num) * 0.01f), b);
			MBMusicManager.Current.ChangeCurrentThemeIntensity(num2 * _waterStrengthIntensityMultiplier);
		}
	}

	public void OnShipHookThrow(MissionShip hookingShip, MissionShip hookedShip)
	{
		float num = _listenerGlobalFrame.origin.DistanceSquared(hookingShip.GameEntity.GlobalPosition);
		if (num < 10000f)
		{
			float num2 = 0.05f - MathF.Sqrt(num) * 0.0005f;
			MBMusicManager.Current.ChangeCurrentThemeIntensity(num2 * _waterStrengthIntensityMultiplier);
		}
	}

	private void CheckForStarting()
	{
		if (_startingTroopCounts == null || _startingTroopCounts.Sum() == 0)
		{
			_startingTroopCounts = new int[2]
			{
				_navalAgentsLogic.GetNumberOfSpawnedAgents(BattleSideEnum.Defender),
				_navalAgentsLogic.GetNumberOfSpawnedAgents(BattleSideEnum.Attacker)
			};
		}
		float num = (float)_startingTroopCounts.Sum() / 500f;
		float startIntensity = MathF.Max(MusicParameters.DefaultStartIntensity, num * 0.8f) + (MBRandom.RandomFloat - 0.5f) * (MusicParameters.RandomEffectMultiplierOnStartIntensity * 2f);
		NavalBattleThemes navalBattleTheme = GetNavalBattleTheme(base.Mission.MusicCulture);
		MBMusicManager.Current.StartTheme((MusicTheme)navalBattleTheme, startIntensity);
		_battleState = BattleState.Started;
	}

	private NavalBattleThemes GetNavalBattleTheme(BasicCultureObject culture)
	{
		if (culture.StringId == "sturgia" || culture.StringId == "nord" || culture.StringId == "battania")
		{
			if (!((double)MBRandom.NondeterministicRandomFloat > 0.5))
			{
				return NavalBattleThemes.VikingSeaBattle2;
			}
			return NavalBattleThemes.VikingSeaBattle1;
		}
		if (!((double)MBRandom.NondeterministicRandomFloat > 0.5))
		{
			return NavalBattleThemes.MediterraneanSeaBattle2;
		}
		return NavalBattleThemes.MediterraneanSeaBattle1;
	}

	private void CheckForEnding()
	{
		if (Mission.Current.IsMissionEnding)
		{
			if (Mission.Current.MissionResult != null)
			{
				base.Mission.MusicCulture = Mission.Current.GetMissionBehavior<MissionCombatantsLogic>().GetCultureForPlayerSide();
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
			if (base.Mission.MusicCulture == null)
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
					base.Mission.MusicCulture = Mission.Current.GetMissionBehavior<MissionCombatantsLogic>().GetCultureForPlayerSide();
				}
			}
			if (base.Mission.MusicCulture != null)
			{
				CheckForStarting();
			}
		}
		if (_battleState == BattleState.Started && Mission.Current.MainAgent != null && Mission.Current.MainAgent.IsActive())
		{
			float num = 0f;
			if (_mainAgentBaseHealth <= 0.01f)
			{
				_mainAgentBaseHealth = Mission.Current.MainAgent.BaseHealthLimit;
			}
			float num2 = 1f - Mission.Current.MainAgent.Health / _mainAgentBaseHealth;
			_mainAgentBaseHealth = Mission.Current.MainAgent.Health;
			num += num2;
			float lengthSquared = (Mission.Current.MainAgent.GetAverageRealGlobalVelocity() - Mission.Current.MainAgent.AverageVelocity).LengthSquared;
			num += ((lengthSquared > 25f) ? (dt * 0.01f) : 0f);
			if (num > 0f)
			{
				MBMusicManager.Current.ChangeCurrentThemeIntensity(num * _waterStrengthIntensityMultiplier);
			}
		}
		if (_battleState == BattleState.Started || _battleState == BattleState.TurnedOneSide)
		{
			CheckForEnding();
		}
		CheckIntensityFall();
	}
}

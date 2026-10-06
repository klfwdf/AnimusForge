using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.CustomBattle;

public class CPUBenchmarkMissionLogic : MissionLogic
{
	private delegate void MainThreadJobDelegate();

	private enum BattlePhase
	{
		Start,
		ArrowShower,
		MeleePosition,
		Cav1Pos,
		Cav1PosDef,
		CavalryPosition,
		MeleeAttack,
		RangedAdvance,
		CavalryAdvance,
		CavalryCharge,
		CavalryCharge2,
		RangedAdvance2,
		FullCharge
	}

	private enum BenchmarkStatus
	{
		Inactive,
		Active,
		Result,
		SetDefinition
	}

	private const float FormationDistDiff = 20f;

	private const float PressTimeForExit = 0.05f;

	private const float ResultTime = 9f;

	private readonly int _attackerInfCount;

	private readonly int _attackerRangedCount;

	private readonly int _attackerCavCount;

	private readonly int _defenderInfCount;

	private readonly int _defenderCavCount;

	private int _curPath;

	private float _benchmarkExit;

	private bool _benchmarkFinished;

	private static bool _isSiege;

	private float _showResultTime = 92f;

	private Path[] _paths;

	private Path[] _targets;

	private float _cameraSpeed;

	private float _curPathSpeed;

	private float _curPathLenght;

	private float _nextPathSpeed;

	private float _prevPathSpeed;

	private float _cameraPassedDistanceOnPath;

	private DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;

	private bool _formationsSetUp;

	private Formation _defLeftInf;

	private Formation _defMidCav;

	private Formation _defRightInf;

	private Formation _defLeftBInf;

	private Formation _defMidBInf;

	private Formation _defRightBInf;

	private Formation _attLeftInf;

	private Formation _attRightInf;

	private Formation _attLeftRanged;

	private Formation _attRightRanged;

	private Formation _attLeftCav;

	private Formation _attRightCav;

	private Camera _benchmarkCamera;

	private BattlePhase _battlePhase;

	private bool _isCurPhaseInPlay;

	private float _totalTime;

	private bool _benchmarkStarted;

	public CPUBenchmarkMissionLogic(int attackerInfCount, int attackerRangedCount, int attackerCavCount, int defenderInfCount, int defenderCavCount)
	{
		_attackerInfCount = attackerInfCount;
		_attackerRangedCount = attackerRangedCount;
		_attackerCavCount = attackerCavCount;
		_defenderInfCount = defenderInfCount;
		_defenderCavCount = defenderCavCount;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		Utilities.EnableSingleGPUQueryPerFrame();
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		_paths = base.Mission.Scene.GetPathsWithNamePrefix("CameraPath");
		_targets = base.Mission.Scene.GetPathsWithNamePrefix("CameraTarget");
		Array.Sort(_paths, (Path x, Path y) => x.GetName().CompareTo(y.GetName()));
		Array.Sort(_targets, (Path x, Path y) => x.GetName().CompareTo(y.GetName()));
		if (_paths.Length != 0)
		{
			_curPath = 0;
			_cameraPassedDistanceOnPath = 0f;
			string name = _paths[_curPath].GetName();
			int num = name.LastIndexOf('_');
			_curPathSpeed = (_cameraSpeed = float.Parse(name.Substring(num + 1)));
			_curPathLenght = _paths[_curPath].GetTotalLength();
			if (_paths.Length > _curPath + 1)
			{
				string name2 = _paths[_curPath + 1].GetName();
				int num2 = name2.LastIndexOf('_');
				_nextPathSpeed = float.Parse(name2.Substring(num2 + 1));
			}
		}
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.Mission.SetMissionMode(MissionMode.Benchmark, atStart: true);
		if (!_isSiege)
		{
			base.Mission.DefenderTeam.ClearTacticOptions();
			base.Mission.AttackerTeam.ClearTacticOptions();
			base.Mission.DefenderTeam.AddTacticOption(new TacticStop(base.Mission.Teams.Defender));
			base.Mission.AttackerTeam.AddTacticOption(new TacticStop(base.Mission.Teams.Attacker));
		}
	}

	private void SetupFormations()
	{
		if (_isSiege)
		{
			_showResultTime = 295f;
			Mission.Current.MainAgent = Mission.Current.AttackerTeam.ActiveAgents[0];
			Utilities.ConstructMainThreadJob(new MainThreadJobDelegate(Mission.Current.GetMissionBehavior<SiegeDeploymentHandler>().FinishDeployment));
		}
		else
		{
			MatrixFrame globalFrame = base.Mission.Scene.FindEntityWithTag("defend_right").GetGlobalFrame();
			MatrixFrame globalFrame2 = base.Mission.Scene.FindEntityWithTag("defend_mid").GetGlobalFrame();
			MatrixFrame globalFrame3 = base.Mission.Scene.FindEntityWithTag("defend_left").GetGlobalFrame();
			MatrixFrame globalFrame4 = base.Mission.Scene.FindEntityWithTag("attacker_right").GetGlobalFrame();
			MatrixFrame globalFrame5 = base.Mission.Scene.FindEntityWithTag("attacker_mid").GetGlobalFrame();
			MatrixFrame globalFrame6 = base.Mission.Scene.FindEntityWithTag("attacker_left").GetGlobalFrame();
			_defLeftInf = base.Mission.DefenderTeam.GetFormation(FormationClass.Infantry);
			_defMidCav = base.Mission.DefenderTeam.GetFormation(FormationClass.Ranged);
			_defRightInf = base.Mission.DefenderTeam.GetFormation(FormationClass.Cavalry);
			_defLeftBInf = base.Mission.DefenderTeam.GetFormation(FormationClass.HorseArcher);
			_defMidBInf = base.Mission.DefenderTeam.GetFormation(FormationClass.NumberOfDefaultFormations);
			_defRightBInf = base.Mission.DefenderTeam.GetFormation(FormationClass.HeavyInfantry);
			_attLeftInf = base.Mission.AttackerTeam.GetFormation(FormationClass.Infantry);
			_attRightInf = base.Mission.AttackerTeam.GetFormation(FormationClass.Ranged);
			_attLeftRanged = base.Mission.AttackerTeam.GetFormation(FormationClass.Cavalry);
			_attRightRanged = base.Mission.AttackerTeam.GetFormation(FormationClass.HorseArcher);
			_attLeftCav = base.Mission.AttackerTeam.GetFormation(FormationClass.NumberOfDefaultFormations);
			_attRightCav = base.Mission.AttackerTeam.GetFormation(FormationClass.LightCavalry);
			int num = _defenderInfCount / 6;
			float num2 = (float)_defenderInfCount / 3.8f;
			int num3 = 0;
			int num4 = _attackerInfCount / 2;
			int num5 = 0;
			int num6 = _attackerRangedCount / 2;
			int num7 = 0;
			int num8 = _attackerCavCount / 2;
			int num9 = 0;
			foreach (Agent agent in base.Mission.Agents)
			{
				if (agent.Team == null || agent.Character == null)
				{
					continue;
				}
				if (agent.Team.IsDefender)
				{
					if (agent.Character.DefaultFormationClass == FormationClass.Cavalry)
					{
						agent.Formation = _defMidCav;
					}
					else if ((float)num3 < num2)
					{
						num3++;
						agent.Formation = _defLeftInf;
					}
					else if ((float)num3 < num2 * 2f)
					{
						num3++;
						agent.Formation = _defRightInf;
					}
					else if ((float)num3 < num2 * 2f + (float)num)
					{
						num3++;
						agent.Formation = _defLeftBInf;
					}
					else if ((float)num3 < num2 * 2f + (float)(num * 2))
					{
						num3++;
						agent.Formation = _defMidBInf;
					}
					else
					{
						agent.Formation = _defRightBInf;
					}
				}
				else
				{
					if (!agent.Team.IsAttacker)
					{
						continue;
					}
					switch (agent.Character.DefaultFormationClass)
					{
					case FormationClass.Infantry:
						if (num5 < num4)
						{
							num5++;
							agent.Formation = _attLeftInf;
						}
						else
						{
							agent.Formation = _attRightInf;
						}
						break;
					case FormationClass.Ranged:
						if (num7 < num6)
						{
							num7++;
							agent.Formation = _attLeftRanged;
						}
						else
						{
							agent.Formation = _attRightRanged;
						}
						break;
					case FormationClass.Cavalry:
						if (num9 < num8)
						{
							num9++;
							agent.Formation = _attLeftCav;
						}
						else
						{
							agent.Formation = _attRightCav;
						}
						break;
					}
				}
			}
			base.Mission.IsTeleportingAgents = true;
			_defLeftInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_defMidCav.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_defRightInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_defLeftBInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_defMidBInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_defRightBInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_attLeftInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_attRightInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_attLeftRanged.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
			_attRightRanged.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
			_attLeftCav.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_attRightCav.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			_defLeftInf.SetFormOrder(FormOrder.FormOrderCustom(35f));
			_defMidCav.SetFormOrder(FormOrder.FormOrderCustom(30f));
			_defRightInf.SetFormOrder(FormOrder.FormOrderCustom(35f));
			_defLeftBInf.SetFormOrder(FormOrder.FormOrderCustom(25f));
			_defMidBInf.SetFormOrder(FormOrder.FormOrderCustom(25f));
			_defRightBInf.SetFormOrder(FormOrder.FormOrderCustom(25f));
			_attLeftInf.SetFormOrder(FormOrder.FormOrderCustom(25f));
			_attRightInf.SetFormOrder(FormOrder.FormOrderCustom(25f));
			_attLeftRanged.SetFormOrder(FormOrder.FormOrderCustom(50f));
			_attRightRanged.SetFormOrder(FormOrder.FormOrderCustom(50f));
			_attLeftCav.SetFormOrder(FormOrder.FormOrderCustom(30f));
			_attRightCav.SetFormOrder(FormOrder.FormOrderCustom(30f));
			_defLeftInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame3.origin + globalFrame3.rotation.f * 20f * 1.125f + 8f * globalFrame3.rotation.s));
			_defMidCav.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame2.origin - globalFrame2.rotation.f * 20f));
			_defRightInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame.origin + globalFrame.rotation.f * 20f * 1.125f - 8f * globalFrame.rotation.s));
			_defLeftBInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame3.origin - globalFrame3.rotation.s * 10f));
			_defMidBInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame2.origin));
			_defRightBInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame.origin + globalFrame.rotation.s * 10f));
			Vec3 vec = globalFrame5.origin - globalFrame6.origin;
			Vec3 vec2 = globalFrame5.origin - globalFrame4.origin;
			_attLeftInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame6.origin + 0.65f * vec));
			_attRightInf.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame4.origin + 0.65f * vec2));
			_attLeftRanged.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame6.origin + globalFrame6.rotation.f * 20f - 0.3f * vec));
			_attRightRanged.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame4.origin + globalFrame4.rotation.f * 20f - 0.3f * vec2));
			_attLeftCav.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame6.origin - globalFrame6.rotation.f * 20f * 0.1f - globalFrame6.rotation.s * 25f));
			_attRightCav.SetPositioning(new WorldPosition(base.Mission.Scene, globalFrame4.origin - globalFrame4.rotation.f * 20f * 0.1f + globalFrame4.rotation.s * 25f));
			_defLeftInf.SetMovementOrder(MovementOrder.MovementOrderMove(_defLeftInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_defMidCav.SetMovementOrder(MovementOrder.MovementOrderMove(_defMidCav.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_defRightInf.SetMovementOrder(MovementOrder.MovementOrderMove(_defRightInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_defLeftBInf.SetMovementOrder(MovementOrder.MovementOrderMove(_defLeftBInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_defMidBInf.SetMovementOrder(MovementOrder.MovementOrderMove(_defMidBInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_defRightBInf.SetMovementOrder(MovementOrder.MovementOrderMove(_defRightBInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_attLeftInf.SetMovementOrder(MovementOrder.MovementOrderMove(_attLeftInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_attRightInf.SetMovementOrder(MovementOrder.MovementOrderMove(_attRightInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_attLeftRanged.SetMovementOrder(MovementOrder.MovementOrderMove(_attLeftRanged.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_attRightRanged.SetMovementOrder(MovementOrder.MovementOrderMove(_attRightRanged.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_attLeftCav.SetMovementOrder(MovementOrder.MovementOrderMove(_attLeftCav.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			_attRightCav.SetMovementOrder(MovementOrder.MovementOrderMove(_attRightCav.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
			foreach (Formation item in base.Mission.AttackerTeam.FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					item.SetControlledByAI(isControlledByAI: false);
					item.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
				}
			}
			foreach (Formation item2 in base.Mission.DefenderTeam.FormationsIncludingEmpty)
			{
				if (item2.CountOfUnits > 0)
				{
					item2.SetControlledByAI(isControlledByAI: false);
					item2.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
				}
			}
			foreach (Agent agent2 in base.Mission.Agents)
			{
				agent2.SetIsAIPaused(isPaused: true);
			}
		}
		_formationsSetUp = true;
	}

	public override void OnMissionTick(float dt)
	{
		_benchmarkStarted = true;
	}

	protected override void OnEndMission()
	{
		Utilities.SetBenchmarkStatus(0, "");
	}

	public override void OnPreMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (!_benchmarkStarted)
		{
			return;
		}
		if (!_formationsSetUp && (_isSiege || _missionAgentSpawnLogic.IsDeploymentOver))
		{
			SetupFormations();
			Utilities.SetBenchmarkStatus(1, _isSiege ? "#" : "");
		}
		if (_formationsSetUp && !_isSiege)
		{
			Check();
		}
		_totalTime += dt;
		Utilities.SetBenchmarkStatus(3, "Battle Size: " + (_attackerCavCount + _attackerInfCount + _attackerRangedCount) + " (" + base.Mission.AttackerTeam.ActiveAgents.Count + ") vs (" + base.Mission.DefenderTeam.ActiveAgents.Count + ") " + (_defenderCavCount + _defenderInfCount));
		if (_benchmarkExit != 0f && !_benchmarkFinished && _totalTime - _benchmarkExit >= 0.05f)
		{
			Utilities.SetBenchmarkStatus(2, "");
			MouseManager.ShowCursor(show: true);
			_benchmarkFinished = true;
		}
		if (Input.IsKeyPressed(InputKey.Escape) && _benchmarkExit == 0f)
		{
			_benchmarkExit = _totalTime;
		}
		if (Input.IsKeyReleased(InputKey.Escape) && _benchmarkExit != 0f && _totalTime - _benchmarkExit < 0.05f)
		{
			_benchmarkExit = 0f;
		}
		if (!_benchmarkFinished && _totalTime > _showResultTime)
		{
			Utilities.SetBenchmarkStatus(2, "");
			MouseManager.ShowCursor(show: true);
			_benchmarkFinished = true;
			_benchmarkExit = _totalTime;
		}
		if (_benchmarkExit != 0f && _totalTime - _benchmarkExit > 9f)
		{
			Utilities.SetBenchmarkStatus(0, "Battle Size: " + (_attackerCavCount + _attackerInfCount + _attackerRangedCount) + " vs " + (_defenderCavCount + _defenderInfCount));
			Mission.Current.EndMission();
		}
		if (!(ScreenManager.TopScreen is MissionScreen { CombatCamera: var combatCamera } missionScreen))
		{
			return;
		}
		if (combatCamera != null && _curPath < _paths.Length)
		{
			if (_benchmarkCamera == null)
			{
				_benchmarkCamera = Camera.CreateCamera();
				_benchmarkCamera.SetFovHorizontal(combatCamera.HorizontalFov, combatCamera.GetAspectRatio(), combatCamera.Near, combatCamera.Far);
			}
			if (_cameraPassedDistanceOnPath < _curPathLenght && _cameraPassedDistanceOnPath > _curPathLenght / 6f * 5f)
			{
				_cameraSpeed = TaleWorlds.Library.MathF.Lerp(_curPathSpeed, (_curPath != _paths.Length - 1) ? ((_nextPathSpeed + _curPathSpeed) / 2f) : 5f, (_cameraPassedDistanceOnPath - _curPathLenght / 6f * 5f) / (_curPathLenght / 6f));
			}
			if (_cameraPassedDistanceOnPath < _curPathLenght / 6f)
			{
				_cameraSpeed = TaleWorlds.Library.MathF.Lerp((_curPath != 0) ? ((_curPathSpeed + _prevPathSpeed) / 2f) : 5f, _curPathSpeed, _cameraPassedDistanceOnPath / (_curPathLenght / 6f));
			}
			_cameraPassedDistanceOnPath += _cameraSpeed * dt;
			if (_cameraPassedDistanceOnPath >= _paths[_curPath].GetTotalLength() && _curPath != _paths.Length - 1)
			{
				_curPath++;
				_curPathLenght = _paths[_curPath].GetTotalLength();
				_prevPathSpeed = _curPathSpeed;
				_curPathSpeed = _nextPathSpeed;
				_cameraPassedDistanceOnPath = _cameraSpeed * dt;
				if (_paths.Length > _curPath + 1)
				{
					string name = _paths[_curPath + 1].GetName();
					int num = name.LastIndexOf('_');
					_nextPathSpeed = float.Parse(name.Substring(num + 1));
				}
			}
			MatrixFrame frameForDistance = _paths[_curPath].GetFrameForDistance(TaleWorlds.Library.MathF.Min(_paths[_curPath].GetTotalLength(), _cameraPassedDistanceOnPath));
			MatrixFrame frameForDistance2 = _targets[_curPath].GetFrameForDistance(TaleWorlds.Library.MathF.Min(1f, _cameraPassedDistanceOnPath / _paths[_curPath].GetTotalLength()) * _targets[_curPath].GetTotalLength());
			_benchmarkCamera.LookAt(frameForDistance.origin, frameForDistance2.origin, Vec3.Up);
			missionScreen.UpdateFreeCamera(_benchmarkCamera.Frame);
			missionScreen.CustomCamera = missionScreen.CombatCamera;
		}
		if (Utilities.IsBenchmarkQuited())
		{
			Utilities.SetBenchmarkStatus(0, "Battle Size: " + (_attackerCavCount + _attackerInfCount + _attackerRangedCount) + " vs " + (_defenderCavCount + _defenderInfCount));
			Mission.Current.EndMission();
		}
	}

	private void Check()
	{
		float currentTime = base.Mission.CurrentTime;
		if (_battlePhase == BattlePhase.Start && currentTime >= 5f)
		{
			base.Mission.IsTeleportingAgents = false;
			foreach (Agent agent in base.Mission.Agents)
			{
				agent.SetIsAIPaused(isPaused: false);
			}
			_battlePhase = BattlePhase.ArrowShower;
		}
		else
		{
			if (_battlePhase == BattlePhase.Start)
			{
				return;
			}
			if (!_isCurPhaseInPlay)
			{
				Debug.Print("State: " + _battlePhase, 0, Debug.DebugColor.Cyan, 64uL);
				switch (_battlePhase)
				{
				case BattlePhase.ArrowShower:
					_attLeftRanged.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_attRightRanged.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_defLeftBInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_defRightBInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_defMidBInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_defLeftInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
					_defRightInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
					_defLeftInf.SetFormOrder(FormOrder.FormOrderCustom(35f));
					_defRightInf.SetFormOrder(FormOrder.FormOrderCustom(35f));
					_attLeftInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
					_attRightInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
					break;
				case BattlePhase.MeleePosition:
				{
					Vec2 vec = -(_attLeftInf.OrderPosition - _defRightInf.OrderPosition);
					Vec2 vec2 = -(_attRightInf.OrderPosition - _defLeftInf.OrderPosition);
					vec.RotateCCW(0.08726646f);
					vec2.RotateCCW(-0.08726646f);
					WorldPosition position9 = _attLeftInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
					position9.SetVec2(position9.AsVec2 + vec);
					_attLeftInf.SetMovementOrder(MovementOrder.MovementOrderMove(position9));
					WorldPosition position10 = _attRightInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
					position10.SetVec2(position10.AsVec2 + vec2);
					_attRightInf.SetMovementOrder(MovementOrder.MovementOrderMove(position10));
					break;
				}
				case BattlePhase.Cav1Pos:
				{
					Vec2 orderPosition2 = _attLeftRanged.OrderPosition;
					Vec2 direction2 = _attLeftRanged.Direction;
					orderPosition2 -= 15f * direction2;
					direction2.RotateCCW(System.MathF.PI / 2f);
					orderPosition2 += 60f * direction2;
					WorldPosition position8 = _attLeftRanged.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
					position8.SetVec2(orderPosition2);
					_attLeftCav.SetMovementOrder(MovementOrder.MovementOrderMove(position8));
					break;
				}
				case BattlePhase.Cav1PosDef:
				{
					MatrixFrame globalFrame3 = base.Mission.Scene.FindEntityWithTag("defend_right").GetGlobalFrame();
					Vec3 position7 = globalFrame3.origin + 40f * globalFrame3.rotation.s;
					_defMidCav.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position7)));
					break;
				}
				case BattlePhase.CavalryPosition:
				{
					Vec2 orderPosition = _attRightRanged.OrderPosition;
					Vec2 direction = _attRightRanged.Direction;
					orderPosition += 20f * direction;
					direction.RotateCCW(-System.MathF.PI / 2f);
					orderPosition += 80f * direction;
					WorldPosition position6 = _attRightRanged.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
					position6.SetVec2(orderPosition);
					_attRightCav.SetMovementOrder(MovementOrder.MovementOrderMove(position6));
					_attLeftInf.SetMovementOrder(MovementOrder.MovementOrderCharge);
					_attRightInf.SetMovementOrder(MovementOrder.MovementOrderCharge);
					_defLeftBInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					break;
				}
				case BattlePhase.MeleeAttack:
					_defLeftInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_defMidBInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_defRightBInf.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_attLeftInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
					_attRightInf.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
					_attLeftInf.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, _defRightInf.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3())));
					_attRightInf.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, _defLeftInf.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3())));
					break;
				case BattlePhase.RangedAdvance:
				{
					Vec3 position4 = _attLeftRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - 0.15f * (_attLeftRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - _defRightInf.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3());
					Vec3 position5 = _attRightRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - 0.15f * (_attRightRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - _defLeftInf.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3());
					_attLeftRanged.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position4)));
					_attRightRanged.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position5)));
					break;
				}
				case BattlePhase.CavalryAdvance:
				{
					base.Mission.Scene.FindEntityWithTag("attacker_mid").GetGlobalFrame();
					MatrixFrame globalFrame2 = base.Mission.Scene.FindEntityWithTag("defend_right").GetGlobalFrame();
					base.Mission.Scene.FindEntityWithTag("defend_left").GetGlobalFrame();
					Vec3 position3 = globalFrame2.origin + globalFrame2.rotation.s * 68f;
					position3 += 10f * _attLeftRanged.Direction.ToVec3();
					_attLeftCav.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position3)));
					_defMidCav.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position3)));
					break;
				}
				case BattlePhase.CavalryCharge:
				{
					MatrixFrame globalFrame = base.Mission.Scene.FindEntityWithTag("defend_left").GetGlobalFrame();
					_defLeftBInf.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection((_attRightCav.CurrentPosition - _defLeftBInf.CurrentPosition).Normalized()));
					_defLeftBInf.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, globalFrame.origin - globalFrame.rotation.s * 10f)));
					_attRightCav.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(_defLeftBInf));
					_attLeftCav.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(_attLeftInf));
					_defMidCav.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(_attRightInf));
					break;
				}
				case BattlePhase.CavalryCharge2:
					_attRightCav.SetMovementOrder(MovementOrder.MovementOrderMove(_defLeftBInf.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None)));
					_attLeftRanged.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_attLeftRanged.SetMovementOrder(MovementOrder.MovementOrderAdvance);
					_attRightRanged.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
					_attRightRanged.SetMovementOrder(MovementOrder.MovementOrderAdvance);
					break;
				case BattlePhase.RangedAdvance2:
				{
					Vec3 position = _attLeftRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - 0.15f * (_attLeftRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - _defRightInf.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3());
					Vec3 position2 = _attRightRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - 0.15f * (_attRightRanged.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3() - _defLeftInf.GetAveragePositionOfUnits(excludeDetachedUnits: true, excludePlayer: false).ToVec3());
					_attLeftRanged.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position)));
					_attRightRanged.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(base.Mission.Scene, position2)));
					break;
				}
				case BattlePhase.FullCharge:
					foreach (Formation item in base.Mission.AttackerTeam.FormationsIncludingEmpty)
					{
						if (item.CountOfUnits > 0 && item != _attLeftRanged && item != _attRightRanged && item != _attRightCav)
						{
							item.SetMovementOrder(MovementOrder.MovementOrderCharge);
						}
					}
					break;
				}
				_isCurPhaseInPlay = true;
				return;
			}
			switch (_battlePhase)
			{
			case BattlePhase.ArrowShower:
				if (currentTime > 14f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.MeleePosition;
				}
				break;
			case BattlePhase.MeleePosition:
				if (currentTime > 19f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.MeleeAttack;
				}
				break;
			case BattlePhase.MeleeAttack:
				if (currentTime > 19f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.Cav1Pos;
				}
				break;
			case BattlePhase.Cav1Pos:
				if (currentTime > 19f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.Cav1PosDef;
				}
				break;
			case BattlePhase.Cav1PosDef:
				if (currentTime > 24f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.CavalryAdvance;
				}
				break;
			case BattlePhase.CavalryAdvance:
				if (currentTime > 30f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.RangedAdvance;
				}
				break;
			case BattlePhase.RangedAdvance:
				if (currentTime > 60f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.CavalryPosition;
				}
				break;
			case BattlePhase.CavalryPosition:
				if (currentTime > 74.5f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.CavalryCharge;
				}
				break;
			case BattlePhase.CavalryCharge:
				if (currentTime > 92f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.CavalryCharge2;
				}
				break;
			case BattlePhase.CavalryCharge2:
				if (currentTime > 93f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.RangedAdvance2;
				}
				break;
			case BattlePhase.RangedAdvance2:
				if (currentTime > 94f)
				{
					_isCurPhaseInPlay = false;
					_battlePhase = BattlePhase.FullCharge;
				}
				break;
			case BattlePhase.FullCharge:
				break;
			}
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cpu_benchmark_mission", "benchmark")]
	public static string CPUBenchmarkMission(List<string> strings)
	{
		OpenCPUBenchmarkMission("benchmark_battle_11");
		return "Success";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cpu_benchmark", "benchmark")]
	public static string CPUBenchmark(List<string> strings)
	{
		foreach (string @string in strings)
		{
			if (@string == "siege")
			{
				_isSiege = true;
			}
		}
		MBGameManager.StartNewGame(new CustomGameManager());
		return "";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("benchmark_start", "state_string")]
	public static string BenchmarkStateStart(List<string> strings)
	{
		GameState activeState = GameStateManager.Current.ActiveState;
		if (activeState is InitialState)
		{
			MBGameManager.StartNewGame(new CustomGameManager());
		}
		else if (activeState is CustomBattleState)
		{
			GameStateManager.StateActivateCommand = "state_string.benchmark_end";
			if (!_isSiege)
			{
				OpenCPUBenchmarkMission("benchmark_battle_11");
			}
			else
			{
				OpenCPUBenchmarkMission("benchmark_siege");
			}
		}
		return "";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("benchmark_end", "state_string")]
	public static string BenchmarkStateEnd(List<string> strings)
	{
		if (GameStateManager.Current.ActiveState is CustomBattleState)
		{
			GameStateManager.StateActivateCommand = null;
			Game.Current.GameStateManager.PopState();
		}
		return "";
	}

	public static Mission OpenCPUBenchmarkMission(string scene)
	{
		int realBattleSize = BannerlordConfig.GetRealBattleSize();
		IMissionTroopSupplier[] troopSuppliers = new IMissionTroopSupplier[2];
		BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>("empire");
		Banner banner = new Banner("11.4.124.4345.4345.768.768.1.0.0.163.0.5.512.512.769.764.1.0.0");
		Banner banner2 = new Banner("11.45.126.4345.4345.768.768.1.0.0.462.0.13.512.512.769.764.1.0.0");
		CustomBattleCombatant playerParty = new CustomBattleCombatant(new TextObject("{=!}Player Party"), culture, banner);
		CustomBattleCombatant enemyParty = new CustomBattleCombatant(new TextObject("{=!}Enemy Party"), culture, banner2);
		if (!_isSiege)
		{
			int attackerInfCount = realBattleSize / 100 * 18;
			int attackerRangedCount = realBattleSize / 100 * 10;
			int attackerCavCount = realBattleSize / 100 * 8;
			int defenderInfCount = realBattleSize / 100 * 59;
			int defenderCavCount = realBattleSize / 100 * 5;
			playerParty.Side = BattleSideEnum.Attacker;
			playerParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("imperial_legionary"), attackerInfCount);
			playerParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("imperial_palatine_guard"), attackerRangedCount);
			playerParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("imperial_cataphract"), attackerCavCount);
			enemyParty.Side = BattleSideEnum.Defender;
			enemyParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("battanian_wildling"), defenderInfCount);
			enemyParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("battanian_horseman"), defenderCavCount);
			CustomBattleTroopSupplier customBattleTroopSupplier = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral: false, isSallyOut: false);
			troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier;
			CustomBattleTroopSupplier customBattleTroopSupplier2 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false);
			troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier2;
			return MissionState.OpenNew("CPUBenchmarkMission", new MissionInitializerRecord(scene)
			{
				DoNotUseLoadingScreen = false,
				PlayingInCampaignMode = false,
				DecalAtlasGroup = 2
			}, (Mission missionController) => new MissionBehavior[10]
			{
				new MissionCombatantsLogic(null, playerParty, enemyParty, playerParty, Mission.MissionTeamAITypeEnum.FieldBattle, isPlayerSergeant: false),
				new DefaultBattleMissionAgentSpawnLogic(troopSuppliers, BattleSideEnum.Attacker, Mission.BattleSizeType.Battle),
				new BattlePowerCalculationLogic(),
				new CPUBenchmarkMissionSpawnHandler(enemyParty, playerParty),
				new CPUBenchmarkMissionLogic(attackerInfCount, attackerRangedCount, attackerCavCount, defenderInfCount, defenderCavCount),
				new AgentHumanAILogic(),
				new AgentVictoryLogic(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler()
			});
		}
		int num = realBattleSize / 100 * 30;
		int num2 = realBattleSize / 100 * 25;
		int num3 = realBattleSize / 100 * 20;
		int number = realBattleSize / 100 * 25;
		playerParty.Side = BattleSideEnum.Attacker;
		playerParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("commander_1"), 1);
		playerParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("imperial_legionary"), num);
		playerParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("imperial_palatine_guard"), num2);
		enemyParty.Side = BattleSideEnum.Defender;
		enemyParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("commander_2"), 1);
		enemyParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("battanian_wildling"), num3);
		enemyParty.AddCharacter(MBObjectManager.Instance.GetObject<BasicCharacterObject>("battanian_militia_archer"), number);
		CustomBattleTroopSupplier customBattleTroopSupplier3 = new CustomBattleTroopSupplier(playerParty, isPlayerSide: true, isPlayerGeneral: false, isSallyOut: false);
		troopSuppliers[(int)playerParty.Side] = customBattleTroopSupplier3;
		CustomBattleTroopSupplier customBattleTroopSupplier4 = new CustomBattleTroopSupplier(enemyParty, isPlayerSide: false, isPlayerGeneral: false, isSallyOut: false);
		troopSuppliers[(int)enemyParty.Side] = customBattleTroopSupplier4;
		SiegeEngineType type = MBObjectManager.Instance.GetObject<SiegeEngineType>("fire_ballista");
		MBObjectManager.Instance.GetObject<SiegeEngineType>("fire_onager");
		MBObjectManager.Instance.GetObject<SiegeEngineType>("fire_catapult");
		SiegeEngineType type2 = MBObjectManager.Instance.GetObject<SiegeEngineType>("trebuchet");
		SiegeEngineType type3 = MBObjectManager.Instance.GetObject<SiegeEngineType>("ram");
		SiegeEngineType type4 = MBObjectManager.Instance.GetObject<SiegeEngineType>("siege_tower_level2");
		List<MissionSiegeWeapon> list = new List<MissionSiegeWeapon>();
		list.Add(MissionSiegeWeapon.CreateDefaultWeapon(type));
		list.Add(MissionSiegeWeapon.CreateDefaultWeapon(type));
		list.Add(MissionSiegeWeapon.CreateDefaultWeapon(type2));
		list.Add(MissionSiegeWeapon.CreateDefaultWeapon(type2));
		list.Add(MissionSiegeWeapon.CreateDefaultWeapon(type4));
		list.Add(MissionSiegeWeapon.CreateDefaultWeapon(type3));
		List<MissionSiegeWeapon> list2 = new List<MissionSiegeWeapon>();
		list2.Add(MissionSiegeWeapon.CreateDefaultWeapon(type));
		list2.Add(MissionSiegeWeapon.CreateDefaultWeapon(type));
		list2.Add(MissionSiegeWeapon.CreateDefaultWeapon(type));
		list2.Add(MissionSiegeWeapon.CreateDefaultWeapon(type));
		float[] wallHitPointPercentages = new float[2] { 1f, 1f };
		Mission mission = BannerlordMissions.OpenSiegeMissionWithDeployment(scene, MBObjectManager.Instance.GetObject<BasicCharacterObject>("commander_1"), playerParty, enemyParty, isPlayerGeneral: true, wallHitPointPercentages, hasAnySiegeTower: true, list, list2, isPlayerAttacker: true, 3);
		mission.AddMissionBehavior(new CPUBenchmarkMissionLogic(num, num2, 0, num3, 0));
		return mission;
	}
}

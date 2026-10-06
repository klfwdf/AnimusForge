using System.Collections.Generic;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace NavalDLC.View;

internal class NavalAmbientShoutsView : MissionView
{
	private enum Shouts
	{
		AllySinking,
		EnemySinking,
		GettingRammed,
		HooksLaunched,
		HooksLost,
		SailsDead,
		AllyShipGotHooked,
		ShipLowHealth,
		PlayerShipSinking,
		BoardingOrder,
		CutLooseOrder,
		Engaging
	}

	private const float RammingShoutCooldown = 15f;

	private const float HooksTimer = 15f;

	private NavalShipsLogic _navalShipsLogic;

	private NavalAgentsLogic _navalAgentsLogic;

	private readonly Dictionary<MissionShip, float> _shipRammingShoutCooldown = new Dictionary<MissionShip, float>();

	private MissionTimer _hooksLaunchedTimer;

	private MissionTimer _shipGotHookedTimer;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		_navalAgentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		_navalShipsLogic.ShipSunkEvent += OnShipSunk;
		_navalShipsLogic.ShipHookThrowEvent += OnShipHookThrow;
		_navalShipsLogic.SailsDeadEvent += OnSailsDead;
		_navalShipsLogic.ShipLowHealthEvent += OnShipLowHealth;
		_navalShipsLogic.ShipAboutToBeRammedEvent += OnShipAboutToBeRammed;
		_navalShipsLogic.ShipAttachmentLostEvent += OnShipAttachmentLost;
		_navalShipsLogic.BoardingOrderEvent += OnBoardingOrder;
		_navalShipsLogic.CutLooseOrderEvent += OnCutLooseOrder;
		_navalShipsLogic.BridgeConnectedEvent += OnBridgeConnected;
		_hooksLaunchedTimer = new MissionTimer(15f);
		_shipGotHookedTimer = new MissionTimer(15f);
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		_navalShipsLogic.ShipSunkEvent -= OnShipSunk;
		_navalShipsLogic.ShipHookThrowEvent -= OnShipHookThrow;
		_navalShipsLogic.SailsDeadEvent -= OnSailsDead;
		_navalShipsLogic.ShipLowHealthEvent -= OnShipLowHealth;
		_navalShipsLogic.ShipAboutToBeRammedEvent -= OnShipAboutToBeRammed;
		_navalShipsLogic.ShipAttachmentLostEvent -= OnShipAttachmentLost;
		_navalShipsLogic.BoardingOrderEvent -= OnBoardingOrder;
		_navalShipsLogic.CutLooseOrderEvent -= OnCutLooseOrder;
	}

	public void OnShipSunk(MissionShip ship)
	{
		if (ship.Team == null)
		{
			return;
		}
		if (ship.Team.IsPlayerAlly)
		{
			if (IsMainAgentOnTheShip(ship))
			{
				PlayShoutFromShip(Shouts.PlayerShipSinking, ship, 5);
			}
			else
			{
				PlayShoutFromShip(Shouts.AllySinking, Agent.Main?.GetComponent<AgentNavalComponent>().SteppedShip, 5);
			}
		}
		else
		{
			PlayShoutFromShip(Shouts.EnemySinking, Agent.Main?.GetComponent<AgentNavalComponent>().SteppedShip, 5);
		}
	}

	public void OnShipHookThrow(MissionShip hookingShip, MissionShip hookedShip)
	{
		if (hookingShip.Team != null && hookedShip.Team != null)
		{
			bool flag = hookingShip.Team.IsPlayerAlly && !hookedShip.Team.IsPlayerAlly;
			if (flag && _hooksLaunchedTimer.Check(reset: true))
			{
				PlayShoutFromShip(Shouts.HooksLaunched, hookingShip, 3);
			}
			else if (!flag && _shipGotHookedTimer.Check(reset: true))
			{
				PlayShoutFromShip(Shouts.AllyShipGotHooked, hookedShip, 3);
			}
		}
	}

	public void OnSailsDead(MissionShip ship)
	{
		if (IsMainAgentOnTheShip(ship))
		{
			PlayShoutFromShip(Shouts.SailsDead, ship, 5);
		}
	}

	public void OnShipLowHealth(MissionShip ship)
	{
		if (IsMainAgentOnTheShip(ship))
		{
			PlayShoutFromShip(Shouts.ShipLowHealth, ship, 5);
		}
	}

	public void OnCutLooseOrder(MissionShip ship)
	{
		if (IsMainAgentOnTheShip(ship))
		{
			PlayShoutFromShip(Shouts.CutLooseOrder, ship, 3);
		}
	}

	public void OnBoardingOrder(MissionShip boardingShip, MissionShip boardedShip)
	{
		if (IsMainAgentOnTheShip(boardingShip))
		{
			PlayShoutFromShip(Shouts.BoardingOrder, boardingShip, 5);
		}
	}

	public void OnBridgeConnected(MissionShip sourceShip, MissionShip targetShip)
	{
		if (IsMainAgentOnTheShip(sourceShip))
		{
			PlayShoutFromShip(Shouts.BoardingOrder, sourceShip, 5);
		}
		else if (IsMainAgentOnTheShip(targetShip))
		{
			PlayShoutFromShip(Shouts.BoardingOrder, targetShip, 5);
		}
	}

	public void OnShipAboutToBeRammed(MissionShip rammingShip, MissionShip rammedShip, float distance, float speedInRamDirection)
	{
		if (speedInRamDirection > 3f && rammedShip.Team != null && rammingShip.Team != null)
		{
			float currentTime = Mission.Current.CurrentTime;
			if (rammedShip.Team.IsPlayerAlly && (!_shipRammingShoutCooldown.TryGetValue(rammedShip, out var value) || currentTime - value > 15f))
			{
				PlayShoutFromShip(Shouts.GettingRammed, rammedShip, 9);
				_shipRammingShoutCooldown[rammedShip] = currentTime;
			}
			if (rammingShip.Team.IsPlayerAlly && (!_shipRammingShoutCooldown.TryGetValue(rammingShip, out value) || currentTime - value > 15f))
			{
				PlayShoutFromShip(Shouts.GettingRammed, rammingShip, 3);
				_shipRammingShoutCooldown[rammingShip] = currentTime;
			}
		}
	}

	public void OnShipAttachmentLost(MissionShip hookingShip, MissionShip hookedShip)
	{
		if (IsMainAgentOnTheShip(hookingShip) && hookingShip.ComputeActiveShipAttachmentCount() == 1)
		{
			PlayShoutFromShip(Shouts.HooksLost, hookingShip, 3);
		}
		else if (IsMainAgentOnTheShip(hookingShip) && hookedShip.ComputeActiveShipAttachmentCount() == 1)
		{
			PlayShoutFromShip(Shouts.HooksLost, hookedShip, 3);
		}
	}

	private bool IsMainAgentOnTheShip(MissionShip ship)
	{
		if (Agent.Main != null && Agent.Main.IsActive())
		{
			return ship.GetIsAgentOnShip(Agent.Main);
		}
		return false;
	}

	private void PlayShoutFromShip(Shouts shoutType, MissionShip ship, int numberOfAgentsToShout)
	{
		if (ship == null)
		{
			return;
		}
		string eventName = GetEventName(shoutType);
		MBReadOnlyList<Agent> activeAgentsOfShip = _navalAgentsLogic.GetActiveAgentsOfShip(ship);
		if (activeAgentsOfShip != null)
		{
			int count = activeAgentsOfShip.Count;
			for (int i = 0; i < numberOfAgentsToShout && i < count; i++)
			{
				SoundManager.StartOneShotEvent(eventName, activeAgentsOfShip.GetRandomElement().Position);
			}
		}
	}

	private string GetEventName(Shouts shoutType)
	{
		return shoutType switch
		{
			Shouts.AllySinking => "event:/alerts/naval/ally_sunk", 
			Shouts.EnemySinking => "event:/alerts/naval/enemy_sunk", 
			Shouts.GettingRammed => "event:/alerts/naval/getting_rammed", 
			Shouts.HooksLaunched => "event:/alerts/naval/hooks_launch", 
			Shouts.HooksLost => "event:/alerts/naval/hooks_lost", 
			Shouts.SailsDead => "event:/alerts/naval/sails_dead", 
			Shouts.AllyShipGotHooked => "event:/alerts/naval/ship_got_hooked", 
			Shouts.ShipLowHealth => "event:/alerts/naval/ship_low_health", 
			Shouts.PlayerShipSinking => "event:/alerts/naval/ship_sinking", 
			Shouts.BoardingOrder => "event:/alerts/nods/attack", 
			Shouts.CutLooseOrder => "event:/alerts/naval/ship_separate", 
			Shouts.Engaging => "event:/alerts/naval/engaging", 
			_ => "", 
		};
	}
}

using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Handlers;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class MissionOrderDeploymentControllerVM : ViewModel
{
	private DeploymentHandler _deploymentHandler;

	private SiegeDeploymentHandler _siegeDeploymentHandler;

	private InquiryData _siegeDeployQueryData;

	private readonly MissionOrderVM _missionOrder;

	private Mission Mission => Mission.Current;

	public MissionOrderDeploymentControllerVM(MissionOrderVM missionOrder)
	{
		_missionOrder = missionOrder;
		_deploymentHandler = Mission.GetMissionBehavior<DeploymentHandler>();
		if (_deploymentHandler != null)
		{
			_deploymentHandler.OnPlayerSideDeploymentReady += ExecuteDeployPlayerSide;
			if (_deploymentHandler is SiegeDeploymentHandler siegeDeploymentHandler)
			{
				_siegeDeploymentHandler = siegeDeploymentHandler;
				_siegeDeploymentHandler.OnEnemySideDeploymentReady += ExecuteDeployEnemySide;
			}
		}
		_siegeDeployQueryData = new InquiryData(new TextObject("{=TxphX8Uk}Deployment").ToString(), new TextObject("{=LlrlE199}You can still deploy siege engines.{newline}Begin anyway?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate
		{
			_siegeDeploymentHandler.FinishDeployment();
			_missionOrder.TryCloseToggleOrder();
		}, null);
	}

	internal void DeployFormationsOfPlayer()
	{
		if (_siegeDeploymentHandler != null)
		{
			_siegeDeploymentHandler.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam, autoAssignDetachments: false);
		}
		else if (!Mission.IsNavalBattle && !Mission.IsNavalRaidBattle && _deploymentHandler != null)
		{
			_deploymentHandler.AutoDeployTeamUsingDeploymentPlan(Mission.PlayerTeam);
		}
		Mission.Current.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>()?.OnPlayerTeamDeployed();
		if (_siegeDeploymentHandler != null)
		{
			_siegeDeploymentHandler.AutoAssignDetachmentsForDeployment(Mission.PlayerTeam);
		}
	}

	public void ExecuteBeginMission(bool showSiegeMachineInquiry = false)
	{
		if (showSiegeMachineInquiry)
		{
			InformationManager.ShowInquiry(_siegeDeployQueryData);
		}
		else if (_deploymentHandler != null)
		{
			_missionOrder.TryCloseToggleOrder();
			_deploymentHandler.FinishDeployment();
		}
	}

	public void ExecuteAutoDeploy()
	{
		Mission.GetDeploymentPlan<IMissionDeploymentPlan>(out var deploymentPlan);
		deploymentPlan.RemakeDeploymentPlan(Mission.PlayerTeam);
		if (_siegeDeploymentHandler != null)
		{
			_siegeDeploymentHandler.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam);
		}
		else if (_deploymentHandler != null)
		{
			_deploymentHandler.AutoDeployTeamUsingDeploymentPlan(Mission.PlayerTeam);
		}
		if (_deploymentHandler != null)
		{
			_deploymentHandler.HandleGeneralsDeploymentFrames();
		}
	}

	public void ExecuteDeployPlayerSide()
	{
		if (_siegeDeploymentHandler != null)
		{
			Mission.ForceTickOccasionally = true;
			bool isTeleportingAgents = Mission.Current.IsTeleportingAgents;
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = true;
			}
			if (!Mission.IsSallyOutBattle || Mission.PlayerTeam.Side == BattleSideEnum.Attacker)
			{
				DeployFormationsOfPlayer();
				_siegeDeploymentHandler.ForceUpdateAllUnits();
			}
			_missionOrder.OnDeployAll();
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = isTeleportingAgents;
			}
			Mission.ForceTickOccasionally = false;
		}
		else if (_deploymentHandler != null)
		{
			DeployFormationsOfPlayer();
			_deploymentHandler.ForceUpdateAllUnits();
			_missionOrder.OnDeployAll();
		}
	}

	private void ExecuteDeployEnemySide()
	{
		if (_siegeDeploymentHandler != null)
		{
			Mission.ForceTickOccasionally = true;
			bool isTeleportingAgents = Mission.Current.IsTeleportingAgents;
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = true;
			}
			if (!Mission.IsSallyOutBattle || Mission.PlayerTeam.Side == BattleSideEnum.Defender)
			{
				_siegeDeploymentHandler.AutoDeployTeamUsingTeamAI(Mission.PlayerEnemyTeam);
				_siegeDeploymentHandler.ForceUpdateAllUnits();
			}
			_missionOrder.OnDeployAll();
			if (!Mission.IsNavalBattle)
			{
				Mission.IsTeleportingAgents = isTeleportingAgents;
			}
			Mission.ForceTickOccasionally = false;
		}
		else if (_deploymentHandler != null)
		{
			_deploymentHandler.ForceUpdateAllUnits();
			_missionOrder.OnDeployAll();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (_deploymentHandler != null)
		{
			_deploymentHandler.OnPlayerSideDeploymentReady -= ExecuteDeployPlayerSide;
		}
		if (_siegeDeploymentHandler != null)
		{
			_siegeDeploymentHandler.OnEnemySideDeploymentReady -= ExecuteDeployEnemySide;
		}
		_siegeDeploymentHandler = null;
		_siegeDeployQueryData = null;
	}
}

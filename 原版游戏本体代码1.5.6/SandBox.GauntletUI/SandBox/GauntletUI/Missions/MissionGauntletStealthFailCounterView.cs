using SandBox.Missions;
using SandBox.View.Missions;
using SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(MissionStealthFailCounterView))]
public class MissionGauntletStealthFailCounterView : MissionStealthFailCounterView
{
	private GauntletLayer _countdownLayer;

	private MissionStealthFailCounterVM _countdownCounterVM;

	private StealthFailCounterMissionLogic _stealthFailCounterMissionLogic;

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_countdownCounterVM = new MissionStealthFailCounterVM();
		_countdownLayer = new GauntletLayer("MissionStealthFailCounter", 10);
		_countdownLayer.LoadMovie("MissionStealthFailCounter", _countdownCounterVM);
		base.MissionScreen.AddLayer(_countdownLayer);
	}

	public override void AfterStart()
	{
		_stealthFailCounterMissionLogic = base.Mission.GetMissionBehavior<StealthFailCounterMissionLogic>();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		_countdownCounterVM.OnFinalize();
		base.MissionScreen.RemoveLayer(_countdownLayer);
		_countdownLayer = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_stealthFailCounterMissionLogic != null)
		{
			_countdownCounterVM.UpdateFailCounter(_stealthFailCounterMissionLogic.FailCounterElapsedTime, _stealthFailCounterMissionLogic.FailCounterSeconds, _stealthFailCounterMissionLogic.IsActive);
		}
	}
}

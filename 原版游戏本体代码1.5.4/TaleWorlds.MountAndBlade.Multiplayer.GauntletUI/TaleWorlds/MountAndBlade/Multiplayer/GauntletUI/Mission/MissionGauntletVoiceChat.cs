using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MissionMultiplayerVoiceChatUIHandler))]
public class MissionGauntletVoiceChat : MissionView
{
	private MultiplayerVoiceChatVM _dataSource;

	private GauntletLayer _gauntletLayer;

	public MissionGauntletVoiceChat()
	{
		ViewOrderPriority = 60;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_dataSource = new MultiplayerVoiceChatVM(base.Mission);
		_gauntletLayer = new GauntletLayer("MultiplayerVoiceChat", ViewOrderPriority);
		_gauntletLayer.LoadMovie("MultiplayerVoiceChat", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
	}

	public override void OnMissionScreenFinalize()
	{
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_dataSource.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
		base.OnMissionScreenFinalize();
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		_dataSource.OnTick(dt);
	}
}

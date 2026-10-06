namespace TaleWorlds.MountAndBlade;

public class ReplayMissionLogic : MissionLogic
{
	private bool _isMultiplayer;

	public string FileName { get; private set; }

	public ReplayMissionLogic(bool isMultiplayer, string fileName = "")
	{
		if (!string.IsNullOrEmpty(fileName))
		{
			FileName = fileName;
		}
		_isMultiplayer = isMultiplayer;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		if (_isMultiplayer)
		{
			GameNetwork.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
		}
		MBCommon.CurrentGameType = MBCommon.GameType.SingleReplay;
		GameNetwork.InitializeClientSide(null, 0, -1, -1);
		base.Mission.Recorder.RestoreRecordFromFile(FileName);
	}

	public override void OnRemoveBehavior()
	{
		if (_isMultiplayer)
		{
			GameNetwork.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
			GameNetwork.EndReplay();
		}
		GameNetwork.TerminateClientSide();
		base.Mission.Recorder.ClearRecordBuffers();
		base.OnRemoveBehavior();
	}
}

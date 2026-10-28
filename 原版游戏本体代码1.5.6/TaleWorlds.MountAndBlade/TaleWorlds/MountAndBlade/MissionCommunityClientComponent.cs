namespace TaleWorlds.MountAndBlade;

public class MissionCommunityClientComponent : MissionLobbyComponent
{
	private CommunityClient _communityClient;

	private bool _isServerEndedBeforeClientLoaded;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_communityClient = NetworkMain.CommunityClient;
	}

	public void SetServerEndingBeforeClientLoaded(bool isServerEndingBeforeClientLoaded)
	{
		_isServerEndedBeforeClientLoaded = isServerEndingBeforeClientLoaded;
	}

	public override void QuitMission()
	{
		base.QuitMission();
		if (!_isServerEndedBeforeClientLoaded && base.CurrentMultiplayerState != MultiplayerGameState.Ending && _communityClient.IsInGame)
		{
			_communityClient.QuitFromGame();
		}
	}
}

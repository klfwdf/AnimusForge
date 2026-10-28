namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MPVoicePlayerVM : MPPlayerVM
{
	public const int UpdatesRequiredToRemoveForSilence = 30;

	public readonly bool IsMyPeer;

	public int UpdatesSinceSilence;

	public MPVoicePlayerVM(MissionPeer peer)
		: base(peer)
	{
		UpdatesSinceSilence = 0;
		IsMyPeer = peer.IsMine;
	}
}

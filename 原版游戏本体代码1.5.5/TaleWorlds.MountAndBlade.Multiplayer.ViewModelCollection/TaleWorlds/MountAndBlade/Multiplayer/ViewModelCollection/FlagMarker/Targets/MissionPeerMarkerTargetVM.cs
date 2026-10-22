using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

public class MissionPeerMarkerTargetVM : MissionMarkerTargetVM
{
	private const string _partyMemberColor = "#00FF00FF";

	private const string _friendColor = "#FFFF00FF";

	private const string _clanMemberColor = "#00FFFFFF";

	private bool _isFriend;

	public MissionPeer TargetPeer { get; private set; }

	public override Vec3 WorldPosition
	{
		get
		{
			if (TargetPeer?.ControlledAgent != null)
			{
				return TargetPeer.ControlledAgent.Position + new Vec3(0f, 0f, TargetPeer.ControlledAgent.GetEyeGlobalHeight());
			}
			Debug.FailedAssert("No target found!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection\\FlagMarker\\Targets\\MissionPeerMarkerTargetVM.cs", "WorldPosition", 27);
			return Vec3.One;
		}
	}

	protected override float HeightOffset => 0.75f;

	public MissionPeerMarkerTargetVM(MissionPeer peer, bool isFriend)
		: base(MissionMarkerType.Peer)
	{
		TargetPeer = peer;
		_isFriend = isFriend;
		RefreshName();
		SetVisual();
	}

	public void RefreshName()
	{
		base.Name = TargetPeer.DisplayedName;
	}

	private void SetVisual()
	{
		string color = "#FFFFFFFF";
		if (NetworkMain.GameClient.IsInParty && NetworkMain.GameClient.PlayersInParty.Any((PartyPlayerInLobbyClient p) => p.PlayerId.Equals(TargetPeer.Peer.Id)))
		{
			color = "#00FF00FF";
		}
		else if (_isFriend)
		{
			color = "#FFFF00FF";
		}
		else if (NetworkMain.GameClient.IsInClan && NetworkMain.GameClient.PlayersInClan.Any((ClanPlayer p) => p.PlayerId.Equals(TargetPeer.Peer.Id)))
		{
			color = "#00FFFFFF";
		}
		uint color2 = TaleWorlds.Library.Color.ConvertStringToColor("#FFFFFFFF").ToUnsignedInteger();
		uint color3 = TaleWorlds.Library.Color.ConvertStringToColor(color).ToUnsignedInteger();
		RefreshColor(color2, color3);
	}

	public override void UpdateScreenPosition(Camera missionCamera)
	{
		if (TargetPeer?.ControlledAgent != null)
		{
			base.UpdateScreenPosition(missionCamera);
		}
	}
}

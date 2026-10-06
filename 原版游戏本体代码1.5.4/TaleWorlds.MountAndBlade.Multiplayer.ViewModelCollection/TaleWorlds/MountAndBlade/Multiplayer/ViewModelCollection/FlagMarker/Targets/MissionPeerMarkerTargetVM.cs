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

	private bool _showHealth;

	private float _healthLimit;

	private float _currentHealth;

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

	[DataSourceProperty]
	public bool ShowHealth
	{
		get
		{
			return _showHealth;
		}
		set
		{
			if (value != _showHealth)
			{
				_showHealth = value;
				OnPropertyChangedWithValue(value, "ShowHealth");
			}
		}
	}

	[DataSourceProperty]
	public float HealthLimit
	{
		get
		{
			return _healthLimit;
		}
		set
		{
			if (value != _healthLimit)
			{
				_healthLimit = value;
				OnPropertyChangedWithValue(value, "HealthLimit");
			}
		}
	}

	[DataSourceProperty]
	public float CurrentHealth
	{
		get
		{
			return _currentHealth;
		}
		set
		{
			if (value != _currentHealth)
			{
				_currentHealth = value;
				OnPropertyChangedWithValue(value, "CurrentHealth");
			}
		}
	}

	public MissionPeerMarkerTargetVM(MissionPeer peer, bool isFriend)
		: base(MissionMarkerType.Peer)
	{
		TargetPeer = peer;
		_isFriend = isFriend;
		base.Name = peer.DisplayedName;
		SetVisual();
		RefreshHealth();
	}

	private void RefreshHealth()
	{
		Agent agent = TargetPeer?.ControlledAgent;
		if (ShowHealth = MultiplayerSpectatorHelper.ShouldShowBothTeamsData() && agent != null && agent.IsActive())
		{
			HealthLimit = agent.HealthLimit;
			CurrentHealth = agent.Health;
		}
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
			RefreshHealth();
			base.UpdateScreenPosition(missionCamera);
		}
	}
}

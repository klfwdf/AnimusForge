using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public class PartyPlayerInLobbyClient
{
	public PlayerId PlayerId { get; private set; }

	public string Name { get; private set; }

	public bool WaitingInvitation { get; private set; }

	public bool IsPartyLeader { get; private set; }

	public PartyPlayerInLobbyClient(PlayerId playerId, string name, bool isPartyLeader = false)
	{
		PlayerId = playerId;
		Name = name;
		IsPartyLeader = isPartyLeader;
		WaitingInvitation = true;
	}

	public void SetAtParty()
	{
		WaitingInvitation = false;
	}

	public void SetLeader()
	{
		IsPartyLeader = true;
	}

	public void SetMember()
	{
		IsPartyLeader = false;
	}
}

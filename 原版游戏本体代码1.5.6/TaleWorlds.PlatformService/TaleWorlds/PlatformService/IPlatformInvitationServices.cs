using System.Threading.Tasks;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.PlatformService;

public interface IPlatformInvitationServices
{
	Task<bool> OnLogin();

	Task<bool> OnInviteToPlatformSession(PlayerId playerId);

	Task OnLeftParty();

	PlayerId GetInvitationPlayerId();

	Task<(bool Result, ulong InviterPlayerAccountId)> JoinSession();

	Task LeaveSession(bool createNewSession);
}

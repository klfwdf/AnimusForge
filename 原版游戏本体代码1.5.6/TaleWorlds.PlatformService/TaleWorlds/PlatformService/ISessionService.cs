namespace TaleWorlds.PlatformService;

public interface ISessionService
{
	void OnJoinJoinableSession(string connectionString);

	void OnLeaveJoinableSession();
}

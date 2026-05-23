using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

public interface IClient
{
	bool IsInCriticalState { get; }

	long AliveCheckTimeInMiliSeconds { get; }

	ILoginAccessProvider AccessProvider { get; }

	void HandleMessage(Message message);

	void OnConnected();

	void OnCantConnect();

	void OnDisconnected();

	Task<bool> CheckConnection();
}

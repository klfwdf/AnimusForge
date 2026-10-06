using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

public interface IClientSession
{
	int AliveCheckInterval { set; }

	int MaxConsecutiveFailuresBeforeDisconnect { set; }

	string Address { get; set; }

	event MessageHandledDelegate MessageReceived;

	event ConnectedDelegate Connected;

	event DisconnectedDelegate Disconnected;

	event OnCantConnectDelegate ConnectionFailed;

	void Connect();

	void Disconnect();

	void Tick();

	Task<LoginResult> Login(LoginMessage message);

	void SendMessage(Message message);

	Task<CallResult> CallFunction<T>(Message message) where T : FunctionResult;

	Task<bool> CheckConnection();
}

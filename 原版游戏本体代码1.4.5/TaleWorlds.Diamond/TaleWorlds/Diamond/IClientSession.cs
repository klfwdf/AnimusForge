using System.Threading.Tasks;

namespace TaleWorlds.Diamond;

public interface IClientSession
{
	void Connect();

	void Disconnect();

	void Tick();

	Task<LoginResult> Login(LoginMessage message);

	void SendMessage(Message message);

	Task<T> CallFunction<T>(Message message) where T : FunctionResult;

	Task<bool> CheckConnection();
}

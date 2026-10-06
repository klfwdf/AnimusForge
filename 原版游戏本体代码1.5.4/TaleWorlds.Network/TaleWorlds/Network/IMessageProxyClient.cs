using System.Threading.Tasks;

namespace TaleWorlds.Network;

public interface IMessageProxyClient
{
	Task Disconnect();

	Task SystemReset();
}

using System.Threading.Tasks;

namespace TaleWorlds.Network;

public abstract class MessageProxy
{
	public abstract Task Invoke(string methodName, params object[] args);
}

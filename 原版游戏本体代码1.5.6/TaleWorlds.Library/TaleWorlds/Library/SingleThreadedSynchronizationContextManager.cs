using System.Threading;

namespace TaleWorlds.Library;

public static class SingleThreadedSynchronizationContextManager
{
	private static SingleThreadedSynchronizationContext _synchronizationContext;

	public static void Initialize()
	{
		if (_synchronizationContext == null)
		{
			_synchronizationContext = new SingleThreadedSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
		}
	}

	public static void Tick()
	{
		_synchronizationContext.Tick();
	}
}

using System;

namespace TaleWorlds.Library;

public struct TWSharedMutexWriteLock : IDisposable
{
	private readonly TWSharedMutex _mtx;

	public TWSharedMutexWriteLock(TWSharedMutex mtx)
	{
		mtx.EnterWriteLock();
		_mtx = mtx;
	}

	public void Dispose()
	{
		_mtx.ExitWriteLock();
	}
}

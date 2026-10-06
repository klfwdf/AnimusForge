using System;

namespace TaleWorlds.Library;

public struct TWSharedMutexReadLock : IDisposable
{
	private readonly TWSharedMutex _mtx;

	public TWSharedMutexReadLock(TWSharedMutex mtx)
	{
		mtx.EnterReadLock();
		_mtx = mtx;
	}

	public void Dispose()
	{
		_mtx.ExitReadLock();
	}
}

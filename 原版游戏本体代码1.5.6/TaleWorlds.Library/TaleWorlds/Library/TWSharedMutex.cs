using System.Threading;

namespace TaleWorlds.Library;

public class TWSharedMutex
{
	private int _readerCount;

	private int _writerFlag;

	private int _writeRequests;

	public bool IsReadLockHeld => Volatile.Read(ref _readerCount) > 0;

	public bool IsWriteLockHeld => Volatile.Read(ref _writerFlag) > 0;

	public void EnterReadLock()
	{
		while (true)
		{
			if (Volatile.Read(ref _writerFlag) == 1 || Volatile.Read(ref _writeRequests) > 0)
			{
				Thread.SpinWait(4);
				continue;
			}
			Interlocked.Increment(ref _readerCount);
			if (Volatile.Read(ref _writerFlag) == 0 && Volatile.Read(ref _writeRequests) == 0)
			{
				break;
			}
			Interlocked.Decrement(ref _readerCount);
		}
	}

	public void EnterWriteLock()
	{
		Interlocked.Increment(ref _writeRequests);
		while (Interlocked.CompareExchange(ref _writerFlag, 1, 0) != 0)
		{
			Thread.SpinWait(4);
		}
		while (Volatile.Read(ref _readerCount) > 0)
		{
			Thread.SpinWait(4);
		}
		Interlocked.Decrement(ref _writeRequests);
	}

	public void ExitReadLock()
	{
		Interlocked.Decrement(ref _readerCount);
	}

	public void ExitWriteLock()
	{
		Volatile.Write(ref _writerFlag, 0);
	}
}

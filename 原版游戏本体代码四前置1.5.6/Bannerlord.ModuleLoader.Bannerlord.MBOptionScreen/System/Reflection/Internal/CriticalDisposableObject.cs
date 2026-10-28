using System.Runtime.ConstrainedExecution;

namespace System.Reflection.Internal;

internal abstract class CriticalDisposableObject : CriticalFinalizerObject, IDisposable
{
	protected abstract void Release();

	public void Dispose()
	{
		Release();
		GC.SuppressFinalize(this);
	}

	~CriticalDisposableObject()
	{
		Release();
	}
}

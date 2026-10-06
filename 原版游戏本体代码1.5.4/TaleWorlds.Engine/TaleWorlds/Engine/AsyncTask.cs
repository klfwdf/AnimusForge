using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglManaged_concurrent_task")]
public sealed class AsyncTask : NativeObject, ITask
{
	internal AsyncTask(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public static AsyncTask CreateWithDelegate(ManagedDelegate function, bool isBackground)
	{
		return EngineApplicationInterface.IAsyncTask.CreateWithDelegate(function, isBackground);
	}

	void ITask.Invoke()
	{
		EngineApplicationInterface.IAsyncTask.Invoke(base.Pointer);
	}

	void ITask.Wait()
	{
		EngineApplicationInterface.IAsyncTask.Wait(base.Pointer);
	}
}

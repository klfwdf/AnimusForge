using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;

namespace ManagedCallbacks;

public class CallbackManager : ICallbackManager
{
	public void Initialize()
	{
		CoreCallbacksGenerated.Initialize();
	}

	public Delegate[] GetDelegates()
	{
		return CoreCallbacksGenerated.Delegates;
	}

	public Dictionary<string, object> GetScriptingInterfaceObjects()
	{
		return ScriptingInterfaceObjects.GetObjects();
	}

	public void SetFunctionPointer(int id, IntPtr pointer)
	{
		ScriptingInterfaceObjects.SetFunctionPointer(id, pointer);
	}

	public void CheckSharedStructureSizes()
	{
	}
}

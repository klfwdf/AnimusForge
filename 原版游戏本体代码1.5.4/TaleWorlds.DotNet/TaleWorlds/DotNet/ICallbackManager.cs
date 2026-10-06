using System;
using System.Collections.Generic;

namespace TaleWorlds.DotNet;

public interface ICallbackManager
{
	void Initialize();

	Delegate[] GetDelegates();

	Dictionary<string, object> GetScriptingInterfaceObjects();

	void SetFunctionPointer(int id, IntPtr pointer);

	void CheckSharedStructureSizes();
}

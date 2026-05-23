using System;
using System.Collections.Generic;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000014 RID: 20
	public interface ICallbackManager
	{
		// Token: 0x06000063 RID: 99
		void Initialize();

		// Token: 0x06000064 RID: 100
		Delegate[] GetDelegates();

		// Token: 0x06000065 RID: 101
		Dictionary<string, object> GetScriptingInterfaceObjects();

		// Token: 0x06000066 RID: 102
		void SetFunctionPointer(int id, IntPtr pointer);

		// Token: 0x06000067 RID: 103
		void CheckSharedStructureSizes();
	}
}

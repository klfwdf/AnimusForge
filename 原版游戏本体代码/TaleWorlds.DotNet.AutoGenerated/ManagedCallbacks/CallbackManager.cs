using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000003 RID: 3
	public class CallbackManager : ICallbackManager
	{
		// Token: 0x06000006 RID: 6 RVA: 0x00002071 File Offset: 0x00000271
		public void Initialize()
		{
			LibraryCallbacksGenerated.Initialize();
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002078 File Offset: 0x00000278
		public Delegate[] GetDelegates()
		{
			return LibraryCallbacksGenerated.Delegates;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000207F File Offset: 0x0000027F
		public Dictionary<string, object> GetScriptingInterfaceObjects()
		{
			return ScriptingInterfaceObjects.GetObjects();
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002086 File Offset: 0x00000286
		public void SetFunctionPointer(int id, IntPtr pointer)
		{
			ScriptingInterfaceObjects.SetFunctionPointer(id, pointer);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000208F File Offset: 0x0000028F
		public void CheckSharedStructureSizes()
		{
		}
	}
}

using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;

namespace ManagedCallbacks
{
	// Token: 0x02000005 RID: 5
	public class CallbackManager : ICallbackManager
	{
		// Token: 0x06000008 RID: 8 RVA: 0x00002081 File Offset: 0x00000281
		public void Initialize()
		{
			EngineCallbacksGenerated.Initialize();
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002088 File Offset: 0x00000288
		public Delegate[] GetDelegates()
		{
			return EngineCallbacksGenerated.Delegates;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000208F File Offset: 0x0000028F
		public Dictionary<string, object> GetScriptingInterfaceObjects()
		{
			return ScriptingInterfaceObjects.GetObjects();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002096 File Offset: 0x00000296
		public void SetFunctionPointer(int id, IntPtr pointer)
		{
			ScriptingInterfaceObjects.SetFunctionPointer(id, pointer);
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000209F File Offset: 0x0000029F
		public void CheckSharedStructureSizes()
		{
		}
	}
}

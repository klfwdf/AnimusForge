using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200002D RID: 45
	[ApplicationInterfaceBase]
	internal interface IAsyncTask
	{
		// Token: 0x060004F9 RID: 1273
		[EngineMethod("create_with_function", false, null, false)]
		AsyncTask CreateWithDelegate(ManagedDelegate function, bool isBackground);

		// Token: 0x060004FA RID: 1274
		[EngineMethod("invoke", false, null, false)]
		void Invoke(UIntPtr Pointer);

		// Token: 0x060004FB RID: 1275
		[EngineMethod("wait", false, null, false)]
		void Wait(UIntPtr Pointer);
	}
}

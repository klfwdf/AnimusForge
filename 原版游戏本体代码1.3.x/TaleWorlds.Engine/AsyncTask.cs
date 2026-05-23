using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000006 RID: 6
	[EngineClass("rglManaged_concurrent_task")]
	public sealed class AsyncTask : NativeObject, ITask
	{
		// Token: 0x0600000D RID: 13 RVA: 0x00002310 File Offset: 0x00000510
		internal AsyncTask(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000231F File Offset: 0x0000051F
		public static AsyncTask CreateWithDelegate(ManagedDelegate function, bool isBackground)
		{
			return EngineApplicationInterface.IAsyncTask.CreateWithDelegate(function, isBackground);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000232D File Offset: 0x0000052D
		void ITask.Invoke()
		{
			EngineApplicationInterface.IAsyncTask.Invoke(base.Pointer);
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000233F File Offset: 0x0000053F
		void ITask.Wait()
		{
			EngineApplicationInterface.IAsyncTask.Wait(base.Pointer);
		}
	}
}

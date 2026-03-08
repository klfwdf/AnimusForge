using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x02000016 RID: 22
	public class EngineCallback : ManagedFromNativeCallback
	{
		// Token: 0x060000AD RID: 173 RVA: 0x00003BB1 File Offset: 0x00001DB1
		public EngineCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
			: base(conditionals, isMultiThreadCallable)
		{
		}
	}
}

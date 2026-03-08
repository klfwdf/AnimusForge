using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000020 RID: 32
	public class LibraryCallback : ManagedFromNativeCallback
	{
		// Token: 0x06000093 RID: 147 RVA: 0x000030C8 File Offset: 0x000012C8
		public LibraryCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
			: base(conditionals, isMultiThreadCallable)
		{
		}
	}
}

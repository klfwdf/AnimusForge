using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.Engine
{
	// Token: 0x0200006A RID: 106
	public abstract class MessageManagerBase : DotNetObject
	{
		// Token: 0x060009E0 RID: 2528
		[EngineCallback(null, false)]
		protected internal abstract void PostWarningLine(string text);

		// Token: 0x060009E1 RID: 2529
		[EngineCallback(null, false)]
		protected internal abstract void PostSuccessLine(string text);

		// Token: 0x060009E2 RID: 2530
		[EngineCallback(null, false)]
		protected internal abstract void PostMessageLineFormatted(string text, uint color);

		// Token: 0x060009E3 RID: 2531
		[EngineCallback(null, false)]
		protected internal abstract void PostMessageLine(string text, uint color);
	}
}

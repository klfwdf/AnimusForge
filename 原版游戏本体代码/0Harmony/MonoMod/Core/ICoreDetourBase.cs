using System;

namespace MonoMod.Core
{
	// Token: 0x020004DC RID: 1244
	internal interface ICoreDetourBase : IDisposable
	{
		// Token: 0x170005F2 RID: 1522
		// (get) Token: 0x06001B9C RID: 7068
		bool IsApplied { get; }

		// Token: 0x06001B9D RID: 7069
		void Apply();

		// Token: 0x06001B9E RID: 7070
		void Undo();
	}
}

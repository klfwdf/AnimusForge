using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.Core
{
	// Token: 0x020000DA RID: 218
	public class TutorialContextChangedEvent : EventBase
	{
		// Token: 0x170003D7 RID: 983
		// (get) Token: 0x06000B3D RID: 2877 RVA: 0x0002471E File Offset: 0x0002291E
		// (set) Token: 0x06000B3E RID: 2878 RVA: 0x00024726 File Offset: 0x00022926
		public TutorialContexts NewContext { get; private set; }

		// Token: 0x06000B3F RID: 2879 RVA: 0x0002472F File Offset: 0x0002292F
		public TutorialContextChangedEvent(TutorialContexts newContext)
		{
			this.NewContext = newContext;
		}
	}
}

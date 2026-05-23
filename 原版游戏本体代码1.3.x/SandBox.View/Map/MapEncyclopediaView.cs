using System;

namespace SandBox.View.Map
{
	// Token: 0x0200004C RID: 76
	public class MapEncyclopediaView : MapView
	{
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600028F RID: 655 RVA: 0x00017DA9 File Offset: 0x00015FA9
		// (set) Token: 0x06000290 RID: 656 RVA: 0x00017DB1 File Offset: 0x00015FB1
		public bool IsEncyclopediaOpen { get; protected set; }

		// Token: 0x06000291 RID: 657 RVA: 0x00017DBA File Offset: 0x00015FBA
		public virtual void CloseEncyclopedia()
		{
		}
	}
}

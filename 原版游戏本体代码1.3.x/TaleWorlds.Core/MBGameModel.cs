using System;

namespace TaleWorlds.Core
{
	// Token: 0x020000AD RID: 173
	public abstract class MBGameModel<T> : GameModel where T : GameModel
	{
		// Token: 0x1700031C RID: 796
		// (get) Token: 0x0600091D RID: 2333 RVA: 0x0001DF1A File Offset: 0x0001C11A
		// (set) Token: 0x0600091E RID: 2334 RVA: 0x0001DF22 File Offset: 0x0001C122
		private protected T BaseModel { protected get; private set; }

		// Token: 0x0600091F RID: 2335 RVA: 0x0001DF2B File Offset: 0x0001C12B
		public void Initialize(T baseModel)
		{
			this.BaseModel = baseModel;
		}
	}
}

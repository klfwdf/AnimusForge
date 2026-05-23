using System;
using System.Collections.Generic;

namespace TaleWorlds.Core
{
	// Token: 0x0200001C RID: 28
	public class BasicGameModels : GameModelsManager
	{
		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000184 RID: 388 RVA: 0x0000693C File Offset: 0x00004B3C
		// (set) Token: 0x06000185 RID: 389 RVA: 0x00006944 File Offset: 0x00004B44
		public RidingModel RidingModel { get; private set; }

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000186 RID: 390 RVA: 0x0000694D File Offset: 0x00004B4D
		// (set) Token: 0x06000187 RID: 391 RVA: 0x00006955 File Offset: 0x00004B55
		public ItemCategorySelector ItemCategorySelector { get; private set; }

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000188 RID: 392 RVA: 0x0000695E File Offset: 0x00004B5E
		// (set) Token: 0x06000189 RID: 393 RVA: 0x00006966 File Offset: 0x00004B66
		public ItemValueModel ItemValueModel { get; private set; }

		// Token: 0x0600018A RID: 394 RVA: 0x0000696F File Offset: 0x00004B6F
		public BasicGameModels(IEnumerable<GameModel> inputComponents)
			: base(inputComponents)
		{
			this.RidingModel = base.GetGameModel<RidingModel>();
			this.ItemCategorySelector = base.GetGameModel<ItemCategorySelector>();
			this.ItemValueModel = base.GetGameModel<ItemValueModel>();
		}
	}
}

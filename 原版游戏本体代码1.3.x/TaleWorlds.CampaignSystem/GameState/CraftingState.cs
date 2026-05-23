using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x0200038C RID: 908
	public class CraftingState : GameState
	{
		// Token: 0x17000C67 RID: 3175
		// (get) Token: 0x0600345F RID: 13407 RVA: 0x000D5FDC File Offset: 0x000D41DC
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C68 RID: 3176
		// (get) Token: 0x06003460 RID: 13408 RVA: 0x000D5FDF File Offset: 0x000D41DF
		// (set) Token: 0x06003461 RID: 13409 RVA: 0x000D5FE7 File Offset: 0x000D41E7
		public Crafting CraftingLogic { get; private set; }

		// Token: 0x17000C69 RID: 3177
		// (get) Token: 0x06003462 RID: 13410 RVA: 0x000D5FF0 File Offset: 0x000D41F0
		// (set) Token: 0x06003463 RID: 13411 RVA: 0x000D5FF8 File Offset: 0x000D41F8
		public ICraftingStateHandler Handler
		{
			get
			{
				return this._handler;
			}
			set
			{
				this._handler = value;
			}
		}

		// Token: 0x06003464 RID: 13412 RVA: 0x000D6001 File Offset: 0x000D4201
		public void InitializeLogic(Crafting newCraftingLogic, bool isReplacingWeaponClass = false)
		{
			this.CraftingLogic = newCraftingLogic;
			if (this._handler != null)
			{
				if (isReplacingWeaponClass)
				{
					this._handler.OnCraftingLogicRefreshed();
					return;
				}
				this._handler.OnCraftingLogicInitialized();
			}
		}

		// Token: 0x04000EEF RID: 3823
		private ICraftingStateHandler _handler;
	}
}

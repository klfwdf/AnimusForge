using System;
using Helpers;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameState
{
	// Token: 0x02000393 RID: 915
	public class InventoryState : PlayerGameState
	{
		// Token: 0x17000C70 RID: 3184
		// (get) Token: 0x06003484 RID: 13444 RVA: 0x000D6134 File Offset: 0x000D4334
		public override bool IsMenuState
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000C71 RID: 3185
		// (get) Token: 0x06003485 RID: 13445 RVA: 0x000D6137 File Offset: 0x000D4337
		// (set) Token: 0x06003486 RID: 13446 RVA: 0x000D613F File Offset: 0x000D433F
		public InventoryLogic InventoryLogic { get; set; }

		// Token: 0x17000C72 RID: 3186
		// (get) Token: 0x06003487 RID: 13447 RVA: 0x000D6148 File Offset: 0x000D4348
		// (set) Token: 0x06003488 RID: 13448 RVA: 0x000D6150 File Offset: 0x000D4350
		public InventoryScreenHelper.InventoryMode InventoryMode { get; set; }

		// Token: 0x17000C73 RID: 3187
		// (get) Token: 0x06003489 RID: 13449 RVA: 0x000D6159 File Offset: 0x000D4359
		// (set) Token: 0x0600348A RID: 13450 RVA: 0x000D6161 File Offset: 0x000D4361
		public Action DoneLogicExtrasDelegate { get; set; }

		// Token: 0x17000C74 RID: 3188
		// (get) Token: 0x0600348B RID: 13451 RVA: 0x000D616A File Offset: 0x000D436A
		// (set) Token: 0x0600348C RID: 13452 RVA: 0x000D6172 File Offset: 0x000D4372
		public IInventoryStateHandler Handler { get; set; }
	}
}

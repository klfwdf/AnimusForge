using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000D8 RID: 216
	public class TransferCommandResult
	{
		// Token: 0x170005BA RID: 1466
		// (get) Token: 0x060014D3 RID: 5331 RVA: 0x00060080 File Offset: 0x0005E280
		public Equipment ResultSideEquipment
		{
			get
			{
				switch (this.ResultSide)
				{
				case InventoryLogic.InventorySide.CivilianEquipment:
				{
					CharacterObject transferCharacter = this.TransferCharacter;
					if (transferCharacter == null)
					{
						return null;
					}
					return transferCharacter.FirstCivilianEquipment;
				}
				case InventoryLogic.InventorySide.BattleEquipment:
				{
					CharacterObject transferCharacter2 = this.TransferCharacter;
					if (transferCharacter2 == null)
					{
						return null;
					}
					return transferCharacter2.FirstBattleEquipment;
				}
				case InventoryLogic.InventorySide.StealthEquipment:
				{
					CharacterObject transferCharacter3 = this.TransferCharacter;
					if (transferCharacter3 == null)
					{
						return null;
					}
					return transferCharacter3.FirstStealthEquipment;
				}
				default:
					return null;
				}
			}
		}

		// Token: 0x170005BB RID: 1467
		// (get) Token: 0x060014D4 RID: 5332 RVA: 0x000600E1 File Offset: 0x0005E2E1
		// (set) Token: 0x060014D5 RID: 5333 RVA: 0x000600E9 File Offset: 0x0005E2E9
		public CharacterObject TransferCharacter { get; private set; }

		// Token: 0x170005BC RID: 1468
		// (get) Token: 0x060014D6 RID: 5334 RVA: 0x000600F2 File Offset: 0x0005E2F2
		// (set) Token: 0x060014D7 RID: 5335 RVA: 0x000600FA File Offset: 0x0005E2FA
		public InventoryLogic.InventorySide ResultSide { get; private set; }

		// Token: 0x170005BD RID: 1469
		// (get) Token: 0x060014D8 RID: 5336 RVA: 0x00060103 File Offset: 0x0005E303
		// (set) Token: 0x060014D9 RID: 5337 RVA: 0x0006010B File Offset: 0x0005E30B
		public ItemRosterElement EffectedItemRosterElement { get; private set; }

		// Token: 0x170005BE RID: 1470
		// (get) Token: 0x060014DA RID: 5338 RVA: 0x00060114 File Offset: 0x0005E314
		// (set) Token: 0x060014DB RID: 5339 RVA: 0x0006011C File Offset: 0x0005E31C
		public int EffectedNumber { get; private set; }

		// Token: 0x170005BF RID: 1471
		// (get) Token: 0x060014DC RID: 5340 RVA: 0x00060125 File Offset: 0x0005E325
		// (set) Token: 0x060014DD RID: 5341 RVA: 0x0006012D File Offset: 0x0005E32D
		public int FinalNumber { get; private set; }

		// Token: 0x170005C0 RID: 1472
		// (get) Token: 0x060014DE RID: 5342 RVA: 0x00060136 File Offset: 0x0005E336
		// (set) Token: 0x060014DF RID: 5343 RVA: 0x0006013E File Offset: 0x0005E33E
		public EquipmentIndex EffectedEquipmentIndex { get; private set; }

		// Token: 0x060014E0 RID: 5344 RVA: 0x00060147 File Offset: 0x0005E347
		public TransferCommandResult()
		{
		}

		// Token: 0x060014E1 RID: 5345 RVA: 0x0006014F File Offset: 0x0005E34F
		public TransferCommandResult(InventoryLogic.InventorySide resultSide, ItemRosterElement effectedItemRosterElement, int effectedNumber, int finalNumber, EquipmentIndex effectedEquipmentIndex, CharacterObject transferCharacter)
		{
			this.ResultSide = resultSide;
			this.EffectedItemRosterElement = effectedItemRosterElement;
			this.EffectedNumber = effectedNumber;
			this.FinalNumber = finalNumber;
			this.EffectedEquipmentIndex = effectedEquipmentIndex;
			this.TransferCharacter = transferCharacter;
		}
	}
}

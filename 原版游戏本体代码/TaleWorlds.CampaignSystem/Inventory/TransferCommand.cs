using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Inventory
{
	// Token: 0x020000D9 RID: 217
	public struct TransferCommand
	{
		// Token: 0x170005C1 RID: 1473
		// (get) Token: 0x060014E2 RID: 5346 RVA: 0x00060184 File Offset: 0x0005E384
		public Equipment FromSideEquipment
		{
			get
			{
				switch (this.FromSide)
				{
				case InventoryLogic.InventorySide.CivilianEquipment:
				{
					CharacterObject character = this.Character;
					if (character == null)
					{
						return null;
					}
					return character.FirstCivilianEquipment;
				}
				case InventoryLogic.InventorySide.BattleEquipment:
				{
					CharacterObject character2 = this.Character;
					if (character2 == null)
					{
						return null;
					}
					return character2.FirstBattleEquipment;
				}
				case InventoryLogic.InventorySide.StealthEquipment:
				{
					CharacterObject character3 = this.Character;
					if (character3 == null)
					{
						return null;
					}
					return character3.FirstStealthEquipment;
				}
				default:
					return null;
				}
			}
		}

		// Token: 0x170005C2 RID: 1474
		// (get) Token: 0x060014E3 RID: 5347 RVA: 0x000601E8 File Offset: 0x0005E3E8
		public Equipment ToSideEquipment
		{
			get
			{
				switch (this.ToSide)
				{
				case InventoryLogic.InventorySide.CivilianEquipment:
				{
					CharacterObject character = this.Character;
					if (character == null)
					{
						return null;
					}
					return character.FirstCivilianEquipment;
				}
				case InventoryLogic.InventorySide.BattleEquipment:
				{
					CharacterObject character2 = this.Character;
					if (character2 == null)
					{
						return null;
					}
					return character2.FirstBattleEquipment;
				}
				case InventoryLogic.InventorySide.StealthEquipment:
				{
					CharacterObject character3 = this.Character;
					if (character3 == null)
					{
						return null;
					}
					return character3.FirstStealthEquipment;
				}
				default:
					return null;
				}
			}
		}

		// Token: 0x170005C3 RID: 1475
		// (get) Token: 0x060014E4 RID: 5348 RVA: 0x00060249 File Offset: 0x0005E449
		// (set) Token: 0x060014E5 RID: 5349 RVA: 0x00060251 File Offset: 0x0005E451
		public InventoryLogic.InventorySide FromSide { get; private set; }

		// Token: 0x170005C4 RID: 1476
		// (get) Token: 0x060014E6 RID: 5350 RVA: 0x0006025A File Offset: 0x0005E45A
		// (set) Token: 0x060014E7 RID: 5351 RVA: 0x00060262 File Offset: 0x0005E462
		public InventoryLogic.InventorySide ToSide { get; private set; }

		// Token: 0x170005C5 RID: 1477
		// (get) Token: 0x060014E8 RID: 5352 RVA: 0x0006026B File Offset: 0x0005E46B
		// (set) Token: 0x060014E9 RID: 5353 RVA: 0x00060273 File Offset: 0x0005E473
		public EquipmentIndex FromEquipmentIndex { get; private set; }

		// Token: 0x170005C6 RID: 1478
		// (get) Token: 0x060014EA RID: 5354 RVA: 0x0006027C File Offset: 0x0005E47C
		// (set) Token: 0x060014EB RID: 5355 RVA: 0x00060284 File Offset: 0x0005E484
		public EquipmentIndex ToEquipmentIndex { get; private set; }

		// Token: 0x170005C7 RID: 1479
		// (get) Token: 0x060014EC RID: 5356 RVA: 0x0006028D File Offset: 0x0005E48D
		// (set) Token: 0x060014ED RID: 5357 RVA: 0x00060295 File Offset: 0x0005E495
		public int Amount { get; private set; }

		// Token: 0x170005C8 RID: 1480
		// (get) Token: 0x060014EE RID: 5358 RVA: 0x0006029E File Offset: 0x0005E49E
		// (set) Token: 0x060014EF RID: 5359 RVA: 0x000602A6 File Offset: 0x0005E4A6
		public ItemRosterElement ElementToTransfer { get; private set; }

		// Token: 0x170005C9 RID: 1481
		// (get) Token: 0x060014F0 RID: 5360 RVA: 0x000602AF File Offset: 0x0005E4AF
		// (set) Token: 0x060014F1 RID: 5361 RVA: 0x000602B7 File Offset: 0x0005E4B7
		public CharacterObject Character { get; private set; }

		// Token: 0x060014F2 RID: 5362 RVA: 0x000602C0 File Offset: 0x0005E4C0
		public static TransferCommand Transfer(int amount, InventoryLogic.InventorySide fromSide, InventoryLogic.InventorySide toSide, ItemRosterElement elementToTransfer, EquipmentIndex fromEquipmentIndex, EquipmentIndex toEquipmentIndex, CharacterObject character)
		{
			return new TransferCommand
			{
				FromSide = fromSide,
				ToSide = toSide,
				ElementToTransfer = elementToTransfer,
				FromEquipmentIndex = fromEquipmentIndex,
				ToEquipmentIndex = toEquipmentIndex,
				Character = character,
				Amount = amount
			};
		}
	}
}

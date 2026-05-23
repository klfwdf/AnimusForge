using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200007E RID: 126
	public class DefaultItems
	{
		// Token: 0x170003EC RID: 1004
		// (get) Token: 0x06001069 RID: 4201 RVA: 0x0004E3F0 File Offset: 0x0004C5F0
		private static DefaultItems Instance
		{
			get
			{
				return Campaign.Current.DefaultItems;
			}
		}

		// Token: 0x170003ED RID: 1005
		// (get) Token: 0x0600106A RID: 4202 RVA: 0x0004E3FC File Offset: 0x0004C5FC
		public static ItemObject Grain
		{
			get
			{
				return DefaultItems.Instance._itemGrain;
			}
		}

		// Token: 0x170003EE RID: 1006
		// (get) Token: 0x0600106B RID: 4203 RVA: 0x0004E408 File Offset: 0x0004C608
		public static ItemObject Planks
		{
			get
			{
				return DefaultItems.Instance._itemPlanks;
			}
		}

		// Token: 0x170003EF RID: 1007
		// (get) Token: 0x0600106C RID: 4204 RVA: 0x0004E414 File Offset: 0x0004C614
		public static ItemObject Felt
		{
			get
			{
				return DefaultItems.Instance._itemFelt;
			}
		}

		// Token: 0x170003F0 RID: 1008
		// (get) Token: 0x0600106D RID: 4205 RVA: 0x0004E420 File Offset: 0x0004C620
		public static ItemObject Meat
		{
			get
			{
				return DefaultItems.Instance._itemMeat;
			}
		}

		// Token: 0x170003F1 RID: 1009
		// (get) Token: 0x0600106E RID: 4206 RVA: 0x0004E42C File Offset: 0x0004C62C
		public static ItemObject Hides
		{
			get
			{
				return DefaultItems.Instance._itemHides;
			}
		}

		// Token: 0x170003F2 RID: 1010
		// (get) Token: 0x0600106F RID: 4207 RVA: 0x0004E438 File Offset: 0x0004C638
		public static ItemObject Tools
		{
			get
			{
				return DefaultItems.Instance._itemTools;
			}
		}

		// Token: 0x170003F3 RID: 1011
		// (get) Token: 0x06001070 RID: 4208 RVA: 0x0004E444 File Offset: 0x0004C644
		public static ItemObject IronOre
		{
			get
			{
				return DefaultItems.Instance._itemIronOre;
			}
		}

		// Token: 0x170003F4 RID: 1012
		// (get) Token: 0x06001071 RID: 4209 RVA: 0x0004E450 File Offset: 0x0004C650
		public static ItemObject HardWood
		{
			get
			{
				return DefaultItems.Instance._itemHardwood;
			}
		}

		// Token: 0x170003F5 RID: 1013
		// (get) Token: 0x06001072 RID: 4210 RVA: 0x0004E45C File Offset: 0x0004C65C
		public static ItemObject Charcoal
		{
			get
			{
				return DefaultItems.Instance._itemCharcoal;
			}
		}

		// Token: 0x170003F6 RID: 1014
		// (get) Token: 0x06001073 RID: 4211 RVA: 0x0004E468 File Offset: 0x0004C668
		public static ItemObject IronIngot1
		{
			get
			{
				return DefaultItems.Instance._itemIronIngot1;
			}
		}

		// Token: 0x170003F7 RID: 1015
		// (get) Token: 0x06001074 RID: 4212 RVA: 0x0004E474 File Offset: 0x0004C674
		public static ItemObject IronIngot2
		{
			get
			{
				return DefaultItems.Instance._itemIronIngot2;
			}
		}

		// Token: 0x170003F8 RID: 1016
		// (get) Token: 0x06001075 RID: 4213 RVA: 0x0004E480 File Offset: 0x0004C680
		public static ItemObject IronIngot3
		{
			get
			{
				return DefaultItems.Instance._itemIronIngot3;
			}
		}

		// Token: 0x170003F9 RID: 1017
		// (get) Token: 0x06001076 RID: 4214 RVA: 0x0004E48C File Offset: 0x0004C68C
		public static ItemObject IronIngot4
		{
			get
			{
				return DefaultItems.Instance._itemIronIngot4;
			}
		}

		// Token: 0x170003FA RID: 1018
		// (get) Token: 0x06001077 RID: 4215 RVA: 0x0004E498 File Offset: 0x0004C698
		public static ItemObject IronIngot5
		{
			get
			{
				return DefaultItems.Instance._itemIronIngot5;
			}
		}

		// Token: 0x170003FB RID: 1019
		// (get) Token: 0x06001078 RID: 4216 RVA: 0x0004E4A4 File Offset: 0x0004C6A4
		public static ItemObject IronIngot6
		{
			get
			{
				return DefaultItems.Instance._itemIronIngot6;
			}
		}

		// Token: 0x170003FC RID: 1020
		// (get) Token: 0x06001079 RID: 4217 RVA: 0x0004E4B0 File Offset: 0x0004C6B0
		public static ItemObject Trash
		{
			get
			{
				return DefaultItems.Instance._itemTrash;
			}
		}

		// Token: 0x0600107A RID: 4218 RVA: 0x0004E4BC File Offset: 0x0004C6BC
		public DefaultItems()
		{
			this.RegisterAll();
		}

		// Token: 0x0600107B RID: 4219 RVA: 0x0004E4CC File Offset: 0x0004C6CC
		private void RegisterAll()
		{
			this._itemGrain = this.Create("grain");
			this._itemFelt = this.Create("felt");
			this._itemPlanks = this.Create("planks");
			this._itemMeat = this.Create("meat");
			this._itemHides = this.Create("hides");
			this._itemTools = this.Create("tools");
			this._itemIronOre = this.Create("iron");
			this._itemHardwood = this.Create("hardwood");
			this._itemCharcoal = this.Create("charcoal");
			this._itemIronIngot1 = this.Create("ironIngot1");
			this._itemIronIngot2 = this.Create("ironIngot2");
			this._itemIronIngot3 = this.Create("ironIngot3");
			this._itemIronIngot4 = this.Create("ironIngot4");
			this._itemIronIngot5 = this.Create("ironIngot5");
			this._itemIronIngot6 = this.Create("ironIngot6");
			this._itemTrash = this.Create("trash");
			this.InitializeAll();
		}

		// Token: 0x0600107C RID: 4220 RVA: 0x0004E5EF File Offset: 0x0004C7EF
		private ItemObject Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<ItemObject>(new ItemObject(stringId));
		}

		// Token: 0x0600107D RID: 4221 RVA: 0x0004E608 File Offset: 0x0004C808
		private void InitializeAll()
		{
			ItemObject.InitializeTradeGood(this._itemGrain, new TextObject("{=Itv3fgJm}Grain{@Plural}loads of grain{\\@}", null), "merchandise_grain", DefaultItemCategories.Grain, 10, 10f, ItemObject.ItemTypeEnum.Goods, true);
			ItemObject.InitializeTradeGood(this._itemMeat, new TextObject("{=LmwhFv5p}Meat{@Plural}loads of meat{\\@}", null), "merchandise_meat", DefaultItemCategories.Meat, 30, 10f, ItemObject.ItemTypeEnum.Goods, true);
			ItemObject.InitializeTradeGood(this._itemPlanks, new TextObject("{=5ac8Boz1}Planks{@Plural}loads of planks{\\@}", null), "bd_planks_a", DefaultItemCategories.Planks, 180, 10f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemFelt, new TextObject("{=hNwjpCVP}Felt{@Plural}rolls of felt{\\@}", null), "merchandise_hides_b", DefaultItemCategories.Felt, 230, 10f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemHides, new TextObject("{=4kvKQuXM}Hides{@Plural}loads of hide{\\@}", null), "merchandise_hides_b", DefaultItemCategories.Hides, 50, 10f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemTools, new TextObject("{=n3cjEB0X}Tools{@Plural}loads of tools{\\@}", null), "bd_pickaxe_b", DefaultItemCategories.Tools, 250, 10f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronOre, new TextObject("{=Kw6BkhIf}Iron Ore{@Plural}loads of iron ore{\\@}", null), "iron_ore", DefaultItemCategories.Iron, 50, 10f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemHardwood, new TextObject("{=ExjMoUiT}Hardwood{@Plural}hardwood logs{\\@}", null), "hardwood", DefaultItemCategories.Wood, 25, 10f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemCharcoal, new TextObject("{=iQadPYNe}Charcoal{@Plural}loads of charcoal{\\@}", null), "charcoal", DefaultItemCategories.Wood, 50, 5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronIngot1, new TextObject("{=gOpodlt1}Crude Iron{@Plural}loads of crude iron{\\@}", null), "crude_iron", DefaultItemCategories.Iron, 20, 0.5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronIngot2, new TextObject("{=7HvtT8bm}Wrought Iron{@Plural}loads of wrought iron{\\@}", null), "wrought_iron", DefaultItemCategories.Iron, 30, 0.5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronIngot3, new TextObject("{=XHmmbnbB}Iron{@Plural}loads of iron{\\@}", null), "iron_a", DefaultItemCategories.Iron, 60, 0.5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronIngot4, new TextObject("{=UfuLKuaI}Steel{@Plural}loads of steel{\\@}", null), "steel", DefaultItemCategories.Iron, 100, 0.5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronIngot5, new TextObject("{=azjMBa86}Fine Steel{@Plural}loads of fine steel{\\@}", null), "fine_steel", DefaultItemCategories.Iron, 160, 0.5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemIronIngot6, new TextObject("{=vLVAfcta}Thamaskene Steel{@Plural}loads of thamaskene steel{\\@}", null), "thamaskene_steel", DefaultItemCategories.Iron, 260, 0.5f, ItemObject.ItemTypeEnum.Goods, false);
			ItemObject.InitializeTradeGood(this._itemTrash, new TextObject("{=ZvZN6UkU}Trash Item", null), "iron_ore", DefaultItemCategories.Unassigned, 1, 1f, ItemObject.ItemTypeEnum.Goods, false);
		}

		// Token: 0x040004D5 RID: 1237
		private const float TradeGoodWeight = 10f;

		// Token: 0x040004D6 RID: 1238
		private const float HalfWeight = 5f;

		// Token: 0x040004D7 RID: 1239
		private const float IngotWeight = 0.5f;

		// Token: 0x040004D8 RID: 1240
		private const float TrashWeight = 1f;

		// Token: 0x040004D9 RID: 1241
		private const int IngotValue = 20;

		// Token: 0x040004DA RID: 1242
		private const int TrashValue = 1;

		// Token: 0x040004DB RID: 1243
		private ItemObject _itemGrain;

		// Token: 0x040004DC RID: 1244
		private ItemObject _itemPlanks;

		// Token: 0x040004DD RID: 1245
		private ItemObject _itemFelt;

		// Token: 0x040004DE RID: 1246
		private ItemObject _itemMeat;

		// Token: 0x040004DF RID: 1247
		private ItemObject _itemHides;

		// Token: 0x040004E0 RID: 1248
		private ItemObject _itemTools;

		// Token: 0x040004E1 RID: 1249
		private ItemObject _itemIronOre;

		// Token: 0x040004E2 RID: 1250
		private ItemObject _itemHardwood;

		// Token: 0x040004E3 RID: 1251
		private ItemObject _itemCharcoal;

		// Token: 0x040004E4 RID: 1252
		private ItemObject _itemIronIngot1;

		// Token: 0x040004E5 RID: 1253
		private ItemObject _itemIronIngot2;

		// Token: 0x040004E6 RID: 1254
		private ItemObject _itemIronIngot3;

		// Token: 0x040004E7 RID: 1255
		private ItemObject _itemIronIngot4;

		// Token: 0x040004E8 RID: 1256
		private ItemObject _itemIronIngot5;

		// Token: 0x040004E9 RID: 1257
		private ItemObject _itemIronIngot6;

		// Token: 0x040004EA RID: 1258
		private ItemObject _itemTrash;
	}
}

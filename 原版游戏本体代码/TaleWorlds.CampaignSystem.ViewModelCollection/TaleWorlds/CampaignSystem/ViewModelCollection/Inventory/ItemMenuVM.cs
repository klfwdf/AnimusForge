using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x02000091 RID: 145
	public class ItemMenuVM : ViewModel
	{
		// Token: 0x06000C75 RID: 3189 RVA: 0x00032A00 File Offset: 0x00030C00
		public ItemMenuVM(Action<ItemVM, int> resetComparedItems, InventoryLogic inventoryLogic, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags, Func<EquipmentIndex, SPItemVM> getEquipmentAtIndex)
		{
			this._resetComparedItems = resetComparedItems;
			this._inventoryLogic = inventoryLogic;
			this._comparedItemProperties = new MBBindingList<ItemMenuTooltipPropertyVM>();
			this._targetItemProperties = new MBBindingList<ItemMenuTooltipPropertyVM>();
			this._getItemUsageSetFlags = getItemUsageSetFlags;
			this._getEquipmentAtIndex = getEquipmentAtIndex;
			this.TargetItemFlagList = new MBBindingList<ItemFlagVM>();
			this.ComparedItemFlagList = new MBBindingList<ItemFlagVM>();
			this.AlternativeUsages = new MBBindingList<StringItemWithHintVM>();
			this._tradeRumorsBehavior = Campaign.Current.GetCampaignBehavior<ITradeRumorCampaignBehavior>();
		}

		// Token: 0x06000C76 RID: 3190 RVA: 0x00032D00 File Offset: 0x00030F00
		public void SetItem(SPItemVM item, InventoryLogic.InventorySide currentEquipmentMode, ItemVM comparedItem = null, BasicCharacterObject character = null, int alternativeUsageIndex = 0)
		{
			this.IsInitializationOver = false;
			this._character = character;
			bool flag = item != this._targetItem;
			bool flag2;
			if (comparedItem == this._comparedItem)
			{
				int lastComparedItemVersion = this._lastComparedItemVersion;
				ItemVM comparedItem2 = this._comparedItem;
				int? num = ((comparedItem2 != null) ? new int?(comparedItem2.Version) : null);
				flag2 = !((lastComparedItemVersion == num.GetValueOrDefault()) & (num != null));
			}
			else
			{
				flag2 = true;
			}
			bool flag3 = flag2;
			if (flag)
			{
				this._targetItem = item;
				this.IsPlayerItem = item.InventorySide == InventoryLogic.InventorySide.PlayerInventory;
				this.ImageIdentifier = item.ImageIdentifier;
				this.ItemName = item.ItemDescription;
				this.AlternativeUsages.Clear();
			}
			if (flag3)
			{
				this._comparedItem = comparedItem;
				ItemVM comparedItem3 = this._comparedItem;
				this.IsComparing = ((comparedItem3 != null) ? comparedItem3.ItemRosterElement.EquipmentElement.Item : null) != null;
				this.IsStealthModeActive = currentEquipmentMode == InventoryLogic.InventorySide.StealthEquipment;
				ItemVM comparedItem4 = this._comparedItem;
				this.ComparedImageIdentifier = ((comparedItem4 != null) ? comparedItem4.ImageIdentifier : null);
				ItemVM comparedItem5 = this._comparedItem;
				this.ComparedItemName = ((comparedItem5 != null) ? comparedItem5.ItemRosterElement.EquipmentElement.GetModifiedItemName().ToString() : null);
				ItemVM comparedItem6 = this._comparedItem;
				this._lastComparedItemVersion = ((comparedItem6 != null) ? comparedItem6.Version : 0);
			}
			this.RefreshItemTooltips(item, comparedItem, alternativeUsageIndex);
			this.IsInitializationOver = true;
			Game game = Game.Current;
			if (game == null)
			{
				return;
			}
			game.EventManager.TriggerEvent<InventoryItemInspectedEvent>(new InventoryItemInspectedEvent(item.ItemRosterElement, item.InventorySide));
		}

		// Token: 0x06000C77 RID: 3191 RVA: 0x00032E7C File Offset: 0x0003107C
		private void RefreshItemTooltips(ItemVM item, ItemVM comparedItem, int alternativeUsageIndex = 0)
		{
			this.TargetItemProperties.Clear();
			this.TargetItemFlagList.Clear();
			this.ComparedItemProperties.Clear();
			this.ComparedItemFlagList.Clear();
			this.SetGeneralComponentTooltip();
			Town town = this._inventoryLogic.CurrentSettlementComponent as Town;
			EquipmentElement equipmentElement;
			if (town != null && Game.Current.IsDevelopmentMode)
			{
				MBBindingList<ItemMenuTooltipPropertyVM> targetItemProperties = this.TargetItemProperties;
				string definition = "Category:";
				equipmentElement = item.ItemRosterElement.EquipmentElement;
				this.CreateProperty(targetItemProperties, definition, equipmentElement.Item.ItemCategory.GetName().ToString(), 0, null);
				MBBindingList<ItemMenuTooltipPropertyVM> targetItemProperties2 = this.TargetItemProperties;
				string definition2 = "Supply:";
				TownMarketData marketData = town.MarketData;
				equipmentElement = item.ItemRosterElement.EquipmentElement;
				this.CreateProperty(targetItemProperties2, definition2, marketData.GetSupply(equipmentElement.Item.ItemCategory).ToString(), 0, null);
				MBBindingList<ItemMenuTooltipPropertyVM> targetItemProperties3 = this.TargetItemProperties;
				string definition3 = "Demand:";
				TownMarketData marketData2 = town.MarketData;
				equipmentElement = item.ItemRosterElement.EquipmentElement;
				this.CreateProperty(targetItemProperties3, definition3, marketData2.GetDemand(equipmentElement.Item.ItemCategory).ToString(), 0, null);
				MBBindingList<ItemMenuTooltipPropertyVM> targetItemProperties4 = this.TargetItemProperties;
				string definition4 = "Price Index:";
				TownMarketData marketData3 = town.MarketData;
				equipmentElement = item.ItemRosterElement.EquipmentElement;
				this.CreateProperty(targetItemProperties4, definition4, marketData3.GetPriceFactor(equipmentElement.Item.ItemCategory).ToString(), 0, null);
			}
			equipmentElement = item.ItemRosterElement.EquipmentElement;
			if (equipmentElement.Item.HasArmorComponent)
			{
				this.SetArmorComponentTooltip();
			}
			else
			{
				equipmentElement = item.ItemRosterElement.EquipmentElement;
				if (equipmentElement.Item.WeaponComponent != null)
				{
					equipmentElement = this._targetItem.ItemRosterElement.EquipmentElement;
					this.SetWeaponComponentTooltip(equipmentElement, alternativeUsageIndex, EquipmentElement.Invalid, -1);
				}
				else
				{
					equipmentElement = item.ItemRosterElement.EquipmentElement;
					if (equipmentElement.Item.HasHorseComponent)
					{
						this.SetHorseComponentTooltip();
					}
					else
					{
						equipmentElement = item.ItemRosterElement.EquipmentElement;
						if (equipmentElement.Item.IsFood)
						{
							this.SetFoodTooltip();
						}
					}
				}
			}
			equipmentElement = item.ItemRosterElement.EquipmentElement;
			if (InventoryScreenHelper.GetInventoryItemTypeOfItem(equipmentElement.Item) == InventoryScreenHelper.InventoryItemType.Goods)
			{
				this.SetMerchandiseComponentTooltip();
			}
			if (this.IsComparing && !Input.IsGamepadActive)
			{
				for (EquipmentIndex equipmentIndex = this._comparedItem.ItemType + 1; equipmentIndex != this._comparedItem.ItemType; equipmentIndex = (equipmentIndex + 1) % EquipmentIndex.NumEquipmentSetSlots)
				{
					SPItemVM spitemVM = this._getEquipmentAtIndex(equipmentIndex);
					if (spitemVM != null)
					{
						equipmentElement = spitemVM.ItemRosterElement.EquipmentElement;
						ItemObject item2 = equipmentElement.Item;
						equipmentElement = comparedItem.ItemRosterElement.EquipmentElement;
						if (ItemHelper.CheckComparability(item2, equipmentElement.Item))
						{
							TextObject textObject = new TextObject("{=8fqFGxD9}Press {KEY} to compare with: {ITEM}", null);
							textObject.SetTextVariable("KEY", GameTexts.FindText("str_game_key_text", "anyalt"));
							textObject.SetTextVariable("ITEM", spitemVM.ItemDescription);
							this.CreateProperty(this.TargetItemProperties, "", textObject.ToString(), 0, null);
							this.CreateProperty(this.ComparedItemProperties, "", "", 0, null);
							return;
						}
					}
				}
			}
		}

		// Token: 0x06000C78 RID: 3192 RVA: 0x00033190 File Offset: 0x00031390
		private int CompareValues(float currentValue, float comparedValue)
		{
			int num = (int)(currentValue * 10f);
			int num2 = (int)(comparedValue * 10f);
			if ((num != 0 && (float)MathF.Abs(num) <= MathF.Abs(currentValue)) || (num2 != 0 && (float)MathF.Abs(num2) <= MathF.Abs(comparedValue)))
			{
				return 0;
			}
			return this.CompareValues(num, num2);
		}

		// Token: 0x06000C79 RID: 3193 RVA: 0x000331E5 File Offset: 0x000313E5
		private int CompareValues(int currentValue, int comparedValue)
		{
			if (this._comparedItem == null || currentValue == comparedValue)
			{
				return 0;
			}
			if (currentValue <= comparedValue)
			{
				return -1;
			}
			return 1;
		}

		// Token: 0x06000C7A RID: 3194 RVA: 0x000331FC File Offset: 0x000313FC
		private void AlternativeUsageIndexUpdated()
		{
			if (this.AlternativeUsageIndex < 0 || !this.IsInitializationOver)
			{
				return;
			}
			if (this._targetItem.ItemRosterElement.EquipmentElement.Item.WeaponComponent != null)
			{
				WeaponComponentData weaponComponentData = this._targetItem.ItemRosterElement.EquipmentElement.Item.Weapons[this.AlternativeUsageIndex];
				EquipmentElement equipmentElement;
				int num;
				this.GetComparedWeapon(weaponComponentData.WeaponDescriptionId, out equipmentElement, out num);
				if (!equipmentElement.IsEmpty)
				{
					this.RefreshItemTooltips(this._targetItem, this._comparedItem, this.AlternativeUsageIndex);
					return;
				}
				this._resetComparedItems(this._targetItem, this.AlternativeUsageIndex);
			}
		}

		// Token: 0x06000C7B RID: 3195 RVA: 0x000332AC File Offset: 0x000314AC
		private void GetComparedWeapon(string weaponUsageId, out EquipmentElement comparedWeapon, out int comparedUsageIndex)
		{
			comparedWeapon = EquipmentElement.Invalid;
			comparedUsageIndex = -1;
			int num;
			if (this.IsComparing && this._comparedItem != null && ItemHelper.IsWeaponComparableWithUsage(this._comparedItem.ItemRosterElement.EquipmentElement.Item, weaponUsageId, out num))
			{
				comparedWeapon = this._comparedItem.ItemRosterElement.EquipmentElement;
				comparedUsageIndex = num;
			}
		}

		// Token: 0x06000C7C RID: 3196 RVA: 0x00033314 File Offset: 0x00031514
		private void SetGeneralComponentTooltip()
		{
			if (this._targetItem.ItemCost >= 0)
			{
				if (this._targetItem.ItemRosterElement.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Goods || this._targetItem.ItemRosterElement.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Animal || this._targetItem.ItemRosterElement.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Horse)
				{
					Town town = this._inventoryLogic.CurrentSettlementComponent as Town;
					Village village;
					if (town == null && (village = this._inventoryLogic.CurrentSettlementComponent as Village) != null && village.TradeBound != null)
					{
						town = village.TradeBound.Town;
					}
					if (town == null)
					{
						town = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, null);
					}
					float num = ((town != null) ? TownHelpers.CalculatePriceDeviationRatio(town, this._targetItem.ItemRosterElement.EquipmentElement) : 1f);
					GameTexts.SetVariable("PERCENTAGE", MathF.Round(MathF.Abs(num * 100f)));
					if (num > 0.3f)
					{
						this._costProperty = this.CreateColoredProperty(this.TargetItemProperties, "", this._targetItem.ItemCost + this.GoldIcon, UIColors.NegativeIndicator, 1, new HintViewModel(GameTexts.FindText("str_inventory_cost_higher", null), null), TooltipProperty.TooltipPropertyFlags.Cost);
					}
					else if (num < -0.2f)
					{
						this._costProperty = this.CreateColoredProperty(this.TargetItemProperties, "", this._targetItem.ItemCost + this.GoldIcon, UIColors.PositiveIndicator, 1, new HintViewModel(GameTexts.FindText("str_inventory_cost_lower", null), null), TooltipProperty.TooltipPropertyFlags.Cost);
					}
					else
					{
						this._costProperty = this.CreateColoredProperty(this.TargetItemProperties, "", this._targetItem.ItemCost + this.GoldIcon, UIColors.Gold, 1, new HintViewModel(GameTexts.FindText("str_inventory_cost_normal", null), null), TooltipProperty.TooltipPropertyFlags.Cost);
					}
				}
				else
				{
					this._costProperty = this.CreateColoredProperty(this.TargetItemProperties, "", this._targetItem.ItemCost + this.GoldIcon, UIColors.Gold, 1, null, TooltipProperty.TooltipPropertyFlags.Cost);
				}
			}
			if (this.IsComparing)
			{
				this.CreateColoredProperty(this.ComparedItemProperties, "", this._comparedItem.ItemCost + this.GoldIcon, UIColors.Gold, 2, null, TooltipProperty.TooltipPropertyFlags.Cost);
			}
			if (Game.Current.IsDevelopmentMode)
			{
				if (this._targetItem.ItemRosterElement.EquipmentElement.Item.Culture != null)
				{
					this.CreateColoredProperty(this.TargetItemProperties, "Culture: ", this._targetItem.ItemRosterElement.EquipmentElement.Item.Culture.StringId, UIColors.Gold, 0, null, TooltipProperty.TooltipPropertyFlags.None);
				}
				else
				{
					this.CreateColoredProperty(this.TargetItemProperties, "Culture: ", "No Culture", UIColors.Gold, 0, null, TooltipProperty.TooltipPropertyFlags.None);
				}
				this.CreateColoredProperty(this.TargetItemProperties, "ID: ", this._targetItem.ItemRosterElement.EquipmentElement.Item.StringId, UIColors.Gold, 0, null, TooltipProperty.TooltipPropertyFlags.None);
			}
			float equipmentWeightMultiplier = 1f;
			CharacterObject characterObject;
			bool flag = (characterObject = this._character as CharacterObject) != null && characterObject.GetPerkValue(DefaultPerks.Athletics.FormFittingArmor);
			SPItemVM spitemVM = this._getEquipmentAtIndex(this._targetItem.ItemType);
			bool flag2 = spitemVM != null && spitemVM.ItemType != EquipmentIndex.None && spitemVM.ItemType != EquipmentIndex.HorseHarness && this._targetItem.ItemRosterElement.EquipmentElement.Item.HasArmorComponent;
			if (flag && flag2)
			{
				equipmentWeightMultiplier += DefaultPerks.Athletics.FormFittingArmor.PrimaryBonus;
			}
			this.AddFloatProperty(this._weightText, (EquipmentElement x) => x.GetEquipmentElementWeight() * equipmentWeightMultiplier, true);
			ItemObject item = this._targetItem.ItemRosterElement.EquipmentElement.Item;
			if (item.RelevantSkill != null && (item.Difficulty > 0 || (this.IsComparing && this._comparedItem.ItemRosterElement.EquipmentElement.Item.Difficulty > 0)))
			{
				this.AddSkillRequirement(this._targetItem, this.TargetItemProperties, false);
				if (this.IsComparing)
				{
					this.AddSkillRequirement(this._comparedItem, this.ComparedItemProperties, true);
				}
			}
			this.AddGeneralItemFlags(this.TargetItemFlagList, item);
			if (this.IsComparing)
			{
				this.AddGeneralItemFlags(this.ComparedItemFlagList, this._comparedItem.ItemRosterElement.EquipmentElement.Item);
			}
		}

		// Token: 0x06000C7D RID: 3197 RVA: 0x000337EC File Offset: 0x000319EC
		private void AddSkillRequirement(ItemVM itemVm, MBBindingList<ItemMenuTooltipPropertyVM> itemProperties, bool isComparison)
		{
			ItemObject item = itemVm.ItemRosterElement.EquipmentElement.Item;
			string text = "";
			if (item.Difficulty > 0)
			{
				text = item.RelevantSkill.Name.ToString();
				text += " ";
				text += item.Difficulty.ToString();
			}
			string definition = "";
			if (!isComparison)
			{
				definition = this._requiresText.ToString();
			}
			this.CreateColoredProperty(itemProperties, definition, text, this.GetColorFromBool(this._character == null || CharacterHelper.CanUseItemBasedOnSkill(this._character, itemVm.ItemRosterElement.EquipmentElement)), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x06000C7E RID: 3198 RVA: 0x00033898 File Offset: 0x00031A98
		private void AddGeneralItemFlags(MBBindingList<ItemFlagVM> list, ItemObject item)
		{
			if (item.IsUniqueItem)
			{
				list.Add(new ItemFlagVM("GeneralFlagIcons\\unique", GameTexts.FindText("str_inventory_flag_unique", null)));
			}
			if (item.IsCivilian)
			{
				list.Add(new ItemFlagVM("GeneralFlagIcons\\civillian", GameTexts.FindText("str_inventory_flag_civillian", null)));
			}
			if (item.IsStealthItem)
			{
				list.Add(new ItemFlagVM("GeneralFlagIcons\\stealth", GameTexts.FindText("str_inventory_flag_stealth", null)));
			}
			if (item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale))
			{
				list.Add(new ItemFlagVM("GeneralFlagIcons\\male_only", GameTexts.FindText("str_inventory_flag_male_only", null)));
			}
			if (item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))
			{
				list.Add(new ItemFlagVM("GeneralFlagIcons\\female_only", GameTexts.FindText("str_inventory_flag_female_only", null)));
			}
		}

		// Token: 0x06000C7F RID: 3199 RVA: 0x00033968 File Offset: 0x00031B68
		private void AddFoodItemFlags(MBBindingList<ItemFlagVM> list, ItemObject item)
		{
			list.Add(new ItemFlagVM("GoodsFlagIcons\\consumable", GameTexts.FindText("str_inventory_flag_consumable", null)));
		}

		// Token: 0x06000C80 RID: 3200 RVA: 0x00033988 File Offset: 0x00031B88
		private void AddWeaponItemFlags(MBBindingList<ItemFlagVM> list, WeaponComponentData weapon)
		{
			if (weapon == null)
			{
				Debug.FailedAssert("Trying to add flags for a null weapon", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\ItemMenuVM.cs", "AddWeaponItemFlags", 429);
				return;
			}
			ItemObject.ItemUsageSetFlags itemUsageFlags = this._getItemUsageSetFlags(weapon);
			foreach (ValueTuple<string, TextObject> valueTuple in CampaignUIHelper.GetFlagDetailsForWeapon(weapon, itemUsageFlags, this._character as CharacterObject))
			{
				list.Add(new ItemFlagVM(valueTuple.Item1, valueTuple.Item2));
			}
		}

		// Token: 0x06000C81 RID: 3201 RVA: 0x00033A24 File Offset: 0x00031C24
		private Color GetColorFromBool(bool booleanValue)
		{
			if (!booleanValue)
			{
				return UIColors.NegativeIndicator;
			}
			return UIColors.PositiveIndicator;
		}

		// Token: 0x06000C82 RID: 3202 RVA: 0x00033A34 File Offset: 0x00031C34
		private void SetFoodTooltip()
		{
			this.CreateColoredProperty(this.TargetItemProperties, "", this._foodText.ToString(), this.ConsumableColor, 1, null, TooltipProperty.TooltipPropertyFlags.None);
			this.AddFoodItemFlags(this.TargetItemFlagList, this._targetItem.ItemRosterElement.EquipmentElement.Item);
		}

		// Token: 0x06000C83 RID: 3203 RVA: 0x00033A8C File Offset: 0x00031C8C
		private void SetHorseComponentTooltip()
		{
			HorseComponent horseComponent = this._targetItem.ItemRosterElement.EquipmentElement.Item.HorseComponent;
			HorseComponent horse = (this.IsComparing ? this._comparedItem.ItemRosterElement.EquipmentElement.Item.HorseComponent : null);
			this.CreateProperty(this.TargetItemProperties, this._typeText.ToString(), GameTexts.FindText("str_inventory_type_" + (int)this._targetItem.ItemRosterElement.EquipmentElement.Item.Type, null).ToString(), 0, null);
			this.AddHorseItemFlags(this.TargetItemFlagList, this._targetItem.ItemRosterElement.EquipmentElement.Item, horseComponent);
			if (this.IsComparing)
			{
				this.CreateProperty(this.ComparedItemProperties, " ", GameTexts.FindText("str_inventory_type_" + (int)this._comparedItem.ItemRosterElement.EquipmentElement.Item.Type, null).ToString(), 0, null);
				this.AddHorseItemFlags(this.ComparedItemFlagList, this._comparedItem.ItemRosterElement.EquipmentElement.Item, horse);
			}
			if (this._targetItem.ItemRosterElement.EquipmentElement.Item.IsMountable)
			{
				this.AddIntProperty(this._horseTierText, (int)(this._targetItem.ItemRosterElement.EquipmentElement.Item.Tier + 1), (this.IsComparing && this._comparedItem != null) ? new int?((int)(this._comparedItem.ItemRosterElement.EquipmentElement.Item.Tier + 1)) : null);
				this.AddIntProperty(this._chargeDamageText, this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedMountCharge(EquipmentElement.Invalid), (this.IsComparing && this._comparedItem != null) ? new int?(this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedMountCharge(EquipmentElement.Invalid)) : null);
				this.AddIntProperty(this._speedText, this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedMountSpeed(EquipmentElement.Invalid), (this.IsComparing && this._comparedItem != null) ? new int?(this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedMountSpeed(EquipmentElement.Invalid)) : null);
				this.AddIntProperty(this._maneuverText, this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedMountManeuver(EquipmentElement.Invalid), (this.IsComparing && this._comparedItem != null) ? new int?(this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedMountManeuver(EquipmentElement.Invalid)) : null);
				this.AddIntProperty(this._hitPointsText, this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedMountHitPoints(), (this.IsComparing && this._comparedItem != null) ? new int?(this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedMountHitPoints()) : null);
				if (this._targetItem.ItemRosterElement.EquipmentElement.Item.HasHorseComponent && this._targetItem.ItemRosterElement.EquipmentElement.Item.HorseComponent.IsMount)
				{
					this.AddComparableStringProperty(this._horseTypeText, (EquipmentElement x) => x.Item.ItemCategory.GetName().ToString(), (EquipmentElement x) => this.GetHorseCategoryValue(x.Item.ItemCategory));
				}
			}
		}

		// Token: 0x06000C84 RID: 3204 RVA: 0x00033E64 File Offset: 0x00032064
		private void AddHorseItemFlags(MBBindingList<ItemFlagVM> list, ItemObject item, HorseComponent horse)
		{
			if (!horse.IsLiveStock)
			{
				if (item.ItemCategory == DefaultItemCategories.PackAnimal)
				{
					list.Add(new ItemFlagVM("MountFlagIcons\\weight_carrying_mount", GameTexts.FindText("str_inventory_flag_carrying_mount", null)));
				}
				else
				{
					list.Add(new ItemFlagVM("MountFlagIcons\\speed_mount", GameTexts.FindText("str_inventory_flag_speed_mount", null)));
				}
			}
			if (this._inventoryLogic.IsSlaughterable(item))
			{
				list.Add(new ItemFlagVM("MountFlagIcons\\slaughterable", GameTexts.FindText("str_inventory_flag_slaughterable", null)));
			}
		}

		// Token: 0x06000C85 RID: 3205 RVA: 0x00033EE8 File Offset: 0x000320E8
		private void SetMerchandiseComponentTooltip()
		{
			if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
			{
				if (this._tradeRumorsBehavior == null)
				{
					return;
				}
				IEnumerable<TradeRumor> tradeRumors = this._tradeRumorsBehavior.TradeRumors;
				bool flag = true;
				IMarketData marketData = this._inventoryLogic.MarketData;
				foreach (TradeRumor tradeRumor in from x in tradeRumors
					orderby x.SellPrice descending, x.BuyPrice descending
					select x)
				{
					bool flag2 = false;
					bool flag3 = false;
					if (this._targetItem.ItemRosterElement.EquipmentElement.Item == tradeRumor.ItemCategory)
					{
						if ((float)tradeRumor.BuyPrice < 0.9f * (float)marketData.GetPrice(tradeRumor.ItemCategory, MobileParty.MainParty, true, this._inventoryLogic.OtherParty))
						{
							flag3 = true;
						}
						if ((float)tradeRumor.SellPrice > 1.1f * (float)marketData.GetPrice(tradeRumor.ItemCategory, MobileParty.MainParty, false, this._inventoryLogic.OtherParty))
						{
							flag2 = true;
						}
						if ((Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != tradeRumor.Settlement) && this._targetItem.ItemRosterElement.EquipmentElement.Item == tradeRumor.ItemCategory && (flag3 || flag2))
						{
							if (flag)
							{
								this.CreateColoredProperty(this.TargetItemProperties, "", this._tradeRumorsText.ToString(), this.TitleColor, 1, null, TooltipProperty.TooltipPropertyFlags.None);
								if (this.IsComparing)
								{
									this.CreateProperty(this.ComparedItemProperties, "", "", 0, null);
									this.CreateProperty(this.ComparedItemProperties, "", "", 0, null);
								}
								flag = false;
							}
							MBTextManager.SetTextVariable("SETTLEMENT_NAME", tradeRumor.Settlement.Name, false);
							MBTextManager.SetTextVariable("SELL_PRICE", tradeRumor.SellPrice);
							MBTextManager.SetTextVariable("BUY_PRICE", tradeRumor.BuyPrice);
							float alpha = this.CalculateTradeRumorOldnessFactor(tradeRumor);
							Color color = new Color(this.TitleColor.Red, this.TitleColor.Green, this.TitleColor.Blue, alpha);
							TextObject textObject = (flag3 ? GameTexts.FindText("str_trade_rumors_text_buy", null) : GameTexts.FindText("str_trade_rumors_text_sell", null));
							this.CreateColoredProperty(this.TargetItemProperties, "", textObject.ToString(), color, 0, null, TooltipProperty.TooltipPropertyFlags.None);
							if (this.IsComparing)
							{
								this.CreateProperty(this.ComparedItemProperties, "", "", 0, null);
							}
						}
					}
				}
			}
		}

		// Token: 0x06000C86 RID: 3206 RVA: 0x000341B0 File Offset: 0x000323B0
		private float CalculateTradeRumorOldnessFactor(TradeRumor rumor)
		{
			return MathF.Clamp((float)((int)rumor.RumorEndTime.RemainingDaysFromNow) / 5f, 0.5f, 1f);
		}

		// Token: 0x06000C87 RID: 3207 RVA: 0x000341E4 File Offset: 0x000323E4
		private void UpdateAlternativeUsages(EquipmentElement targetWeapon)
		{
			List<StringItemWithHintVM> list = new List<StringItemWithHintVM>();
			foreach (WeaponComponentData weaponComponentData in targetWeapon.Item.Weapons)
			{
				if (CampaignUIHelper.IsItemUsageApplicable(weaponComponentData))
				{
					list.Add(new StringItemWithHintVM(GameTexts.FindText("str_weapon_usage", weaponComponentData.WeaponDescriptionId).ToString(), GameTexts.FindText("str_inventory_alternative_usage_hint", null)));
				}
			}
			for (int i = this.AlternativeUsages.Count - 1; i >= 0; i--)
			{
				StringItemWithHintVM oldUsage = this.AlternativeUsages[i];
				if (!list.Any((StringItemWithHintVM x) => x.Text == oldUsage.Text))
				{
					this.AlternativeUsages.RemoveAt(i);
				}
			}
			using (List<StringItemWithHintVM>.Enumerator enumerator2 = list.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					StringItemWithHintVM newUsage = enumerator2.Current;
					if (!this.AlternativeUsages.Any((StringItemWithHintVM x) => x.Text == newUsage.Text))
					{
						this.AlternativeUsages.Add(newUsage);
					}
				}
			}
		}

		// Token: 0x06000C88 RID: 3208 RVA: 0x00034334 File Offset: 0x00032534
		private void SetWeaponComponentTooltip(in EquipmentElement targetWeapon, int targetWeaponUsageIndex, EquipmentElement comparedWeapon, int comparedWeaponUsageIndex)
		{
			EquipmentElement equipmentElement = targetWeapon;
			WeaponComponentData weaponWithUsageIndex = equipmentElement.Item.GetWeaponWithUsageIndex(targetWeaponUsageIndex);
			if (this.IsComparing && this._comparedItem != null && comparedWeapon.IsEmpty)
			{
				this.GetComparedWeapon(weaponWithUsageIndex.WeaponDescriptionId, out comparedWeapon, out comparedWeaponUsageIndex);
			}
			WeaponComponentData weaponComponentData = (comparedWeapon.IsEmpty ? null : comparedWeapon.Item.GetWeaponWithUsageIndex(comparedWeaponUsageIndex));
			this.AddWeaponItemFlags(this.TargetItemFlagList, weaponWithUsageIndex);
			if (this.IsComparing)
			{
				this.AddWeaponItemFlags(this.ComparedItemFlagList, weaponComponentData);
			}
			this.UpdateAlternativeUsages(targetWeapon);
			this.AlternativeUsageIndex = targetWeaponUsageIndex;
			this.CreateProperty(this.TargetItemProperties, this._classText.ToString(), GameTexts.FindText("str_inventory_weapon", weaponWithUsageIndex.WeaponClass.ToString()).ToString(), 0, null);
			if (!comparedWeapon.IsEmpty)
			{
				this.CreateProperty(this.ComparedItemProperties, " ", GameTexts.FindText("str_inventory_weapon", weaponComponentData.WeaponClass.ToString()).ToString(), 0, null);
			}
			else if (this.IsComparing)
			{
				this.CreateProperty(this.ComparedItemProperties, "", "", 0, null);
			}
			equipmentElement = targetWeapon;
			if (equipmentElement.Item.BannerComponent == null)
			{
				int value = 0;
				if (!comparedWeapon.IsEmpty)
				{
					value = (int)(comparedWeapon.Item.Tier + 1);
				}
				TextObject weaponTierText = this._weaponTierText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(weaponTierText, (int)(equipmentElement.Item.Tier + 1), new int?(value));
			}
			ItemObject.ItemTypeEnum itemTypeFromWeaponClass = WeaponComponentData.GetItemTypeFromWeaponClass(weaponWithUsageIndex.WeaponClass);
			ItemObject.ItemTypeEnum itemTypeEnum = ((!comparedWeapon.IsEmpty) ? WeaponComponentData.GetItemTypeFromWeaponClass(weaponWithUsageIndex.WeaponClass) : ItemObject.ItemTypeEnum.Invalid);
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.OneHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.TwoHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Polearm || itemTypeEnum == ItemObject.ItemTypeEnum.OneHandedWeapon || itemTypeEnum == ItemObject.ItemTypeEnum.TwoHandedWeapon || itemTypeEnum == ItemObject.ItemTypeEnum.Polearm)
			{
				if (weaponWithUsageIndex.SwingDamageType != DamageTypes.Invalid)
				{
					TextObject swingSpeedText = this._swingSpeedText;
					equipmentElement = targetWeapon;
					this.AddIntProperty(swingSpeedText, equipmentElement.GetModifiedSwingSpeedForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedSwingSpeedForUsage(comparedWeaponUsageIndex)));
					this.AddSwingDamageProperty(this._swingDamageText, targetWeapon, targetWeaponUsageIndex, comparedWeapon, comparedWeaponUsageIndex);
				}
				if (weaponWithUsageIndex.ThrustDamageType != DamageTypes.Invalid)
				{
					TextObject thrustSpeedText = this._thrustSpeedText;
					equipmentElement = targetWeapon;
					this.AddIntProperty(thrustSpeedText, equipmentElement.GetModifiedThrustSpeedForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedThrustSpeedForUsage(comparedWeaponUsageIndex)));
					this.AddThrustDamageProperty(this._thrustDamageText, targetWeapon, targetWeaponUsageIndex, comparedWeapon, comparedWeaponUsageIndex);
				}
				this.AddIntProperty(this._lengthText, weaponWithUsageIndex.WeaponLength, (weaponComponentData != null) ? new int?(weaponComponentData.WeaponLength) : null);
				TextObject handlingText = this._handlingText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(handlingText, equipmentElement.GetModifiedHandlingForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedHandlingForUsage(comparedWeaponUsageIndex)));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Thrown || itemTypeEnum == ItemObject.ItemTypeEnum.Thrown)
			{
				this.AddIntProperty(this._weaponLengthText, weaponWithUsageIndex.WeaponLength, (weaponComponentData != null) ? new int?(weaponComponentData.WeaponLength) : null);
				this.AddMissileDamageProperty(this._damageText, targetWeapon, targetWeaponUsageIndex, comparedWeapon, comparedWeaponUsageIndex);
				TextObject missileSpeedText = this._missileSpeedText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(missileSpeedText, equipmentElement.GetModifiedMissileSpeedForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedMissileSpeedForUsage(comparedWeaponUsageIndex)));
				this.AddIntProperty(this._accuracyText, weaponWithUsageIndex.Accuracy, (weaponComponentData != null) ? new int?(weaponComponentData.Accuracy) : null);
				TextObject stackAmountText = this._stackAmountText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(stackAmountText, (int)equipmentElement.GetModifiedStackCountForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?((int)comparedWeapon.GetModifiedStackCountForUsage(comparedWeaponUsageIndex)));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Shield || itemTypeEnum == ItemObject.ItemTypeEnum.Shield)
			{
				TextObject speedText = this._speedText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(speedText, equipmentElement.GetModifiedSwingSpeedForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedSwingSpeedForUsage(comparedWeaponUsageIndex)));
				TextObject hitPointsText = this._hitPointsText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(hitPointsText, (int)equipmentElement.GetModifiedMaximumHitPointsForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?((int)comparedWeapon.GetModifiedMaximumHitPointsForUsage(comparedWeaponUsageIndex)));
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Bow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Sling || itemTypeEnum == ItemObject.ItemTypeEnum.Bow || itemTypeEnum == ItemObject.ItemTypeEnum.Crossbow || itemTypeEnum == ItemObject.ItemTypeEnum.Sling)
			{
				TextObject speedText2 = this._speedText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(speedText2, equipmentElement.GetModifiedSwingSpeedForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedSwingSpeedForUsage(comparedWeaponUsageIndex)));
				this.AddThrustDamageProperty(this._damageText, targetWeapon, targetWeaponUsageIndex, comparedWeapon, comparedWeaponUsageIndex);
				this.AddIntProperty(this._accuracyText, weaponWithUsageIndex.Accuracy, (weaponComponentData != null) ? new int?(weaponComponentData.Accuracy) : null);
				TextObject missileSpeedText2 = this._missileSpeedText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(missileSpeedText2, equipmentElement.GetModifiedMissileSpeedForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?(comparedWeapon.GetModifiedMissileSpeedForUsage(comparedWeaponUsageIndex)));
				if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow || itemTypeEnum == ItemObject.ItemTypeEnum.Crossbow)
				{
					TextObject ammoLimitText = this._ammoLimitText;
					int maxDataValue = (int)weaponWithUsageIndex.MaxDataValue;
					short? num = ((weaponComponentData != null) ? new short?(weaponComponentData.MaxDataValue) : null);
					this.AddIntProperty(ammoLimitText, maxDataValue, (num != null) ? new int?((int)num.GetValueOrDefault()) : null);
				}
			}
			if (weaponWithUsageIndex.IsAmmo || (weaponComponentData != null && weaponComponentData.IsAmmo))
			{
				if ((itemTypeFromWeaponClass != ItemObject.ItemTypeEnum.Arrows && itemTypeFromWeaponClass != ItemObject.ItemTypeEnum.Bolts && itemTypeFromWeaponClass != ItemObject.ItemTypeEnum.SlingStones) || (weaponComponentData != null && itemTypeEnum != ItemObject.ItemTypeEnum.Arrows && itemTypeEnum != ItemObject.ItemTypeEnum.Bolts && itemTypeEnum != ItemObject.ItemTypeEnum.SlingStones))
				{
					this.AddIntProperty(this._accuracyText, weaponWithUsageIndex.Accuracy, (weaponComponentData != null) ? new int?(weaponComponentData.Accuracy) : null);
				}
				this.AddThrustDamageProperty(this._damageText, targetWeapon, targetWeaponUsageIndex, comparedWeapon, comparedWeaponUsageIndex);
				TextObject stackAmountText2 = this._stackAmountText;
				equipmentElement = targetWeapon;
				this.AddIntProperty(stackAmountText2, (int)equipmentElement.GetModifiedStackCountForUsage(targetWeaponUsageIndex), comparedWeapon.IsEmpty ? null : new int?((int)comparedWeapon.GetModifiedStackCountForUsage(comparedWeaponUsageIndex)));
			}
			equipmentElement = targetWeapon;
			ItemObject item = equipmentElement.Item;
			if (item == null || !item.HasBannerComponent)
			{
				ItemObject item2 = comparedWeapon.Item;
				if (item2 == null || !item2.HasBannerComponent)
				{
					goto IL_6B9;
				}
			}
			Func<EquipmentElement, string> valueAsStringFunc = delegate(EquipmentElement x)
			{
				ItemObject item3 = x.Item;
				bool flag;
				if (item3 == null)
				{
					flag = null != null;
				}
				else
				{
					BannerComponent bannerComponent = item3.BannerComponent;
					flag = ((bannerComponent != null) ? bannerComponent.BannerEffect : null) != null;
				}
				if (flag)
				{
					GameTexts.SetVariable("RANK", x.Item.BannerComponent.BannerEffect.Name);
					string content = string.Empty;
					if (x.Item.BannerComponent.BannerEffect.IncrementType == EffectIncrementType.AddFactor)
					{
						GameTexts.FindText("str_NUMBER_percent", null).SetTextVariable("NUMBER", ((int)Math.Abs(x.Item.BannerComponent.GetBannerEffectBonus() * 100f)).ToString());
						object obj;
						content = obj.ToString();
					}
					else if (x.Item.BannerComponent.BannerEffect.IncrementType == EffectIncrementType.Add)
					{
						content = x.Item.BannerComponent.GetBannerEffectBonus().ToString();
					}
					GameTexts.SetVariable("NUMBER", content);
					return GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
				}
				return this._noneText.ToString();
			};
			this.AddComparableStringProperty(this._bannerEffectText, valueAsStringFunc, (EquipmentElement x) => 0);
			IL_6B9:
			this.AddDonationXpTooltip();
		}

		// Token: 0x170003FF RID: 1023
		// (get) Token: 0x06000C89 RID: 3209 RVA: 0x00034A00 File Offset: 0x00032C00
		// (set) Token: 0x06000C8A RID: 3210 RVA: 0x00034A08 File Offset: 0x00032C08
		[DataSourceProperty]
		public bool IsComparing
		{
			get
			{
				return this._isComparing;
			}
			set
			{
				if (value != this._isComparing)
				{
					this._isComparing = value;
					base.OnPropertyChangedWithValue(value, "IsComparing");
				}
			}
		}

		// Token: 0x06000C8B RID: 3211 RVA: 0x00034A28 File Offset: 0x00032C28
		private void AddIntProperty(TextObject description, int targetValue, int? comparedValue)
		{
			string value = targetValue.ToString();
			if (this.IsComparing && comparedValue != null)
			{
				string value2 = comparedValue.Value.ToString();
				int result = this.CompareValues(targetValue, comparedValue.Value);
				this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				this.CreateColoredProperty(this.ComparedItemProperties, " ", value2, this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x17000400 RID: 1024
		// (get) Token: 0x06000C8C RID: 3212 RVA: 0x00034ACA File Offset: 0x00032CCA
		// (set) Token: 0x06000C8D RID: 3213 RVA: 0x00034AD2 File Offset: 0x00032CD2
		[DataSourceProperty]
		public bool IsPlayerItem
		{
			get
			{
				return this._isPlayerItem;
			}
			set
			{
				if (value != this._isPlayerItem)
				{
					this._isPlayerItem = value;
					base.OnPropertyChangedWithValue(value, "IsPlayerItem");
				}
			}
		}

		// Token: 0x06000C8E RID: 3214 RVA: 0x00034AF0 File Offset: 0x00032CF0
		private void AddFloatProperty(TextObject description, Func<EquipmentElement, float> func, bool reversedCompare = false)
		{
			float targetValue = func(this._targetItem.ItemRosterElement.EquipmentElement);
			float? comparedValue = null;
			if (this.IsComparing && this._comparedItem != null)
			{
				comparedValue = new float?(func(this._comparedItem.ItemRosterElement.EquipmentElement));
			}
			this.AddFloatProperty(description, targetValue, comparedValue, reversedCompare);
		}

		// Token: 0x17000401 RID: 1025
		// (get) Token: 0x06000C8F RID: 3215 RVA: 0x00034B53 File Offset: 0x00032D53
		// (set) Token: 0x06000C90 RID: 3216 RVA: 0x00034B5B File Offset: 0x00032D5B
		[DataSourceProperty]
		public ItemImageIdentifierVM ImageIdentifier
		{
			get
			{
				return this._imageIdentifier;
			}
			set
			{
				if (value != this._imageIdentifier)
				{
					this._imageIdentifier = value;
					base.OnPropertyChangedWithValue<ItemImageIdentifierVM>(value, "ImageIdentifier");
				}
			}
		}

		// Token: 0x06000C91 RID: 3217 RVA: 0x00034B7C File Offset: 0x00032D7C
		private void AddFloatProperty(TextObject description, float targetValue, float? comparedValue, bool reversedCompare = false)
		{
			string formattedItemPropertyText = CampaignUIHelper.GetFormattedItemPropertyText(targetValue, false);
			if (this.IsComparing && comparedValue != null)
			{
				string formattedItemPropertyText2 = CampaignUIHelper.GetFormattedItemPropertyText(comparedValue.Value, false);
				int num = this.CompareValues(targetValue, comparedValue.Value);
				if (reversedCompare)
				{
					num *= -1;
				}
				this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), formattedItemPropertyText, this.GetColorFromComparison(num, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				this.CreateColoredProperty(this.ComparedItemProperties, " ", formattedItemPropertyText2, this.GetColorFromComparison(num, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), formattedItemPropertyText, this.GetColorFromComparison(0, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x17000402 RID: 1026
		// (get) Token: 0x06000C92 RID: 3218 RVA: 0x00034C24 File Offset: 0x00032E24
		// (set) Token: 0x06000C93 RID: 3219 RVA: 0x00034C2C File Offset: 0x00032E2C
		[DataSourceProperty]
		public ItemImageIdentifierVM ComparedImageIdentifier
		{
			get
			{
				return this._comparedImageIdentifier;
			}
			set
			{
				if (value != this._comparedImageIdentifier)
				{
					this._comparedImageIdentifier = value;
					base.OnPropertyChangedWithValue<ItemImageIdentifierVM>(value, "ComparedImageIdentifier");
				}
			}
		}

		// Token: 0x06000C94 RID: 3220 RVA: 0x00034C4C File Offset: 0x00032E4C
		private void AddComparableStringProperty(TextObject description, Func<EquipmentElement, string> valueAsStringFunc, Func<EquipmentElement, int> valueAsIntFunc)
		{
			string value = valueAsStringFunc(this._targetItem.ItemRosterElement.EquipmentElement);
			int currentValue = valueAsIntFunc(this._targetItem.ItemRosterElement.EquipmentElement);
			if (this.IsComparing && this._comparedItem != null)
			{
				int comparedValue = valueAsIntFunc(this._comparedItem.ItemRosterElement.EquipmentElement);
				int result = this.CompareValues(currentValue, comparedValue);
				this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				this.CreateColoredProperty(this.ComparedItemProperties, " ", valueAsStringFunc(this._comparedItem.ItemRosterElement.EquipmentElement), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x17000403 RID: 1027
		// (get) Token: 0x06000C95 RID: 3221 RVA: 0x00034D29 File Offset: 0x00032F29
		// (set) Token: 0x06000C96 RID: 3222 RVA: 0x00034D31 File Offset: 0x00032F31
		[DataSourceProperty]
		public int TransactionTotalCost
		{
			get
			{
				return this._transactionTotalCost;
			}
			set
			{
				if (value != this._transactionTotalCost)
				{
					this._transactionTotalCost = value;
					base.OnPropertyChangedWithValue(value, "TransactionTotalCost");
				}
			}
		}

		// Token: 0x06000C97 RID: 3223 RVA: 0x00034D50 File Offset: 0x00032F50
		private void AddSwingDamageProperty(TextObject description, in EquipmentElement targetWeapon, int targetWeaponUsageIndex, in EquipmentElement comparedWeapon, int comparedWeaponUsageIndex)
		{
			EquipmentElement equipmentElement = targetWeapon;
			int modifiedSwingDamageForUsage = equipmentElement.GetModifiedSwingDamageForUsage(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			WeaponComponentData weaponWithUsageIndex = equipmentElement.Item.GetWeaponWithUsageIndex(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			string value = ItemHelper.GetSwingDamageText(weaponWithUsageIndex, equipmentElement.ItemModifier).ToString();
			if (this.IsComparing)
			{
				equipmentElement = comparedWeapon;
				if (!equipmentElement.IsEmpty)
				{
					equipmentElement = comparedWeapon;
					int modifiedSwingDamageForUsage2 = equipmentElement.GetModifiedSwingDamageForUsage(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					WeaponComponentData weaponWithUsageIndex2 = equipmentElement.Item.GetWeaponWithUsageIndex(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					string value2 = ItemHelper.GetSwingDamageText(weaponWithUsageIndex2, equipmentElement.ItemModifier).ToString();
					int result = this.CompareValues(modifiedSwingDamageForUsage, modifiedSwingDamageForUsage2);
					this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					this.CreateColoredProperty(this.ComparedItemProperties, " ", value2, this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					return;
				}
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x17000404 RID: 1028
		// (get) Token: 0x06000C98 RID: 3224 RVA: 0x00034E66 File Offset: 0x00033066
		// (set) Token: 0x06000C99 RID: 3225 RVA: 0x00034E6E File Offset: 0x0003306E
		[DataSourceProperty]
		public bool IsInitializationOver
		{
			get
			{
				return this._isInitializationOver;
			}
			set
			{
				if (value != this._isInitializationOver)
				{
					this._isInitializationOver = value;
					base.OnPropertyChangedWithValue(value, "IsInitializationOver");
				}
			}
		}

		// Token: 0x06000C9A RID: 3226 RVA: 0x00034E8C File Offset: 0x0003308C
		private void AddMissileDamageProperty(TextObject description, in EquipmentElement targetWeapon, int targetWeaponUsageIndex, in EquipmentElement comparedWeapon, int comparedWeaponUsageIndex)
		{
			EquipmentElement equipmentElement = targetWeapon;
			int modifiedMissileDamageForUsage = equipmentElement.GetModifiedMissileDamageForUsage(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			WeaponComponentData weaponWithUsageIndex = equipmentElement.Item.GetWeaponWithUsageIndex(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			string value = ItemHelper.GetMissileDamageText(weaponWithUsageIndex, equipmentElement.ItemModifier).ToString();
			if (this.IsComparing)
			{
				equipmentElement = comparedWeapon;
				if (!equipmentElement.IsEmpty)
				{
					equipmentElement = comparedWeapon;
					int modifiedMissileDamageForUsage2 = equipmentElement.GetModifiedMissileDamageForUsage(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					WeaponComponentData weaponWithUsageIndex2 = equipmentElement.Item.GetWeaponWithUsageIndex(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					string value2 = ItemHelper.GetMissileDamageText(weaponWithUsageIndex2, equipmentElement.ItemModifier).ToString();
					int result = this.CompareValues(modifiedMissileDamageForUsage, modifiedMissileDamageForUsage2);
					this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					this.CreateColoredProperty(this.ComparedItemProperties, " ", value2, this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					return;
				}
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x17000405 RID: 1029
		// (get) Token: 0x06000C9B RID: 3227 RVA: 0x00034FA2 File Offset: 0x000331A2
		// (set) Token: 0x06000C9C RID: 3228 RVA: 0x00034FAA File Offset: 0x000331AA
		[DataSourceProperty]
		public string ItemName
		{
			get
			{
				return this._itemName;
			}
			set
			{
				if (value != this._itemName)
				{
					this._itemName = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemName");
				}
			}
		}

		// Token: 0x06000C9D RID: 3229 RVA: 0x00034FD0 File Offset: 0x000331D0
		private void AddThrustDamageProperty(TextObject description, in EquipmentElement targetWeapon, int targetWeaponUsageIndex, in EquipmentElement comparedWeapon, int comparedWeaponUsageIndex)
		{
			EquipmentElement equipmentElement = targetWeapon;
			int modifiedThrustDamageForUsage = equipmentElement.GetModifiedThrustDamageForUsage(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			WeaponComponentData weaponWithUsageIndex = equipmentElement.Item.GetWeaponWithUsageIndex(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			string value = ItemHelper.GetThrustDamageText(weaponWithUsageIndex, equipmentElement.ItemModifier).ToString();
			if (this.IsComparing)
			{
				equipmentElement = comparedWeapon;
				if (!equipmentElement.IsEmpty)
				{
					equipmentElement = comparedWeapon;
					int modifiedThrustDamageForUsage2 = equipmentElement.GetModifiedThrustDamageForUsage(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					WeaponComponentData weaponWithUsageIndex2 = equipmentElement.Item.GetWeaponWithUsageIndex(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					string value2 = ItemHelper.GetThrustDamageText(weaponWithUsageIndex2, equipmentElement.ItemModifier).ToString();
					int result = this.CompareValues(modifiedThrustDamageForUsage, modifiedThrustDamageForUsage2);
					this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					this.CreateColoredProperty(this.ComparedItemProperties, " ", value2, this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					return;
				}
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		// Token: 0x17000406 RID: 1030
		// (get) Token: 0x06000C9E RID: 3230 RVA: 0x000350E6 File Offset: 0x000332E6
		// (set) Token: 0x06000C9F RID: 3231 RVA: 0x000350EE File Offset: 0x000332EE
		[DataSourceProperty]
		public string ComparedItemName
		{
			get
			{
				return this._comparedItemName;
			}
			set
			{
				if (value != this._comparedItemName)
				{
					this._comparedItemName = value;
					base.OnPropertyChangedWithValue<string>(value, "ComparedItemName");
				}
			}
		}

		// Token: 0x06000CA0 RID: 3232 RVA: 0x00035114 File Offset: 0x00033314
		private void SetArmorComponentTooltip()
		{
			int value = 0;
			if (this._comparedItem != null && this._comparedItem.ItemRosterElement.EquipmentElement.Item != null)
			{
				value = (int)(this._comparedItem.ItemRosterElement.EquipmentElement.Item.Tier + 1);
			}
			this.AddIntProperty(this._armorTierText, (int)(this._targetItem.ItemRosterElement.EquipmentElement.Item.Tier + 1), new int?(value));
			this.CreateProperty(this.TargetItemProperties, this._typeText.ToString(), GameTexts.FindText("str_inventory_type_" + (int)this._targetItem.ItemRosterElement.EquipmentElement.Item.Type, null).ToString(), 0, null);
			if (this.IsComparing)
			{
				this.CreateProperty(this.ComparedItemProperties, " ", GameTexts.FindText("str_inventory_type_" + (int)this._targetItem.ItemRosterElement.EquipmentElement.Item.Type, null).ToString(), 0, null);
			}
			ArmorComponent armorComponent = this._targetItem.ItemRosterElement.EquipmentElement.Item.ArmorComponent;
			ArmorComponent armorComponent2 = (this.IsComparing ? this._comparedItem.ItemRosterElement.EquipmentElement.Item.ArmorComponent : null);
			if (armorComponent.HeadArmor != 0 || (this.IsComparing && armorComponent2.HeadArmor != 0))
			{
				int result = (this.IsComparing ? this.CompareValues(this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedHeadArmor(), this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedHeadArmor()) : 0);
				this.CreateColoredProperty(this.TargetItemProperties, this._headArmorText.ToString(), this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedHeadArmor().ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				if (this.IsComparing)
				{
					this.CreateColoredProperty(this.ComparedItemProperties, " ", this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedHeadArmor().ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				}
			}
			if (armorComponent.BodyArmor != 0 || (this.IsComparing && this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedBodyArmor() != 0))
			{
				if (this._targetItem.ItemType == EquipmentIndex.HorseHarness)
				{
					int result = (this.IsComparing ? this.CompareValues(this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedMountBodyArmor(), this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedMountBodyArmor()) : 0);
					this.CreateColoredProperty(this.TargetItemProperties, this._horseArmorText.ToString(), this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedMountBodyArmor().ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					if (this.IsComparing)
					{
						this.CreateColoredProperty(this.ComparedItemProperties, " ", this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedMountBodyArmor().ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					}
				}
				else
				{
					int result = (this.IsComparing ? this.CompareValues(this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedBodyArmor(), this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedBodyArmor()) : 0);
					this.CreateColoredProperty(this.TargetItemProperties, this._bodyArmorText.ToString(), this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedBodyArmor().ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					if (this.IsComparing)
					{
						this.CreateColoredProperty(this.ComparedItemProperties, " ", this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedBodyArmor().ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					}
				}
			}
			if (this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedLegArmor() != 0 || (this.IsComparing && this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedLegArmor() != 0))
			{
				int result = (this.IsComparing ? this.CompareValues(this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedLegArmor(), this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedLegArmor()) : 0);
				this.CreateColoredProperty(this.TargetItemProperties, this._legArmorText.ToString(), this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedLegArmor().ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				if (this.IsComparing)
				{
					this.CreateColoredProperty(this.ComparedItemProperties, " ", this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedLegArmor().ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				}
			}
			if (this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedArmArmor() != 0 || (this.IsComparing && this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedArmArmor() != 0))
			{
				int result = (this.IsComparing ? this.CompareValues(this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedArmArmor(), this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedArmArmor()) : 0);
				this.CreateColoredProperty(this.TargetItemProperties, this._armArmorText.ToString(), this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedArmArmor().ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				if (this.IsComparing)
				{
					this.CreateColoredProperty(this.ComparedItemProperties, " ", this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedArmArmor().ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				}
			}
			if (this.IsStealthModeActive)
			{
				int result = (this.IsComparing ? this.CompareValues(this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedStealthFactor(), this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedStealthFactor()) : 0);
				this.CreateColoredProperty(this.TargetItemProperties, this._stealthBonusText.ToString(), this._targetItem.ItemRosterElement.EquipmentElement.GetModifiedStealthFactor().ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				if (this.IsComparing)
				{
					this.CreateColoredProperty(this.ComparedItemProperties, " ", this._comparedItem.ItemRosterElement.EquipmentElement.GetModifiedStealthFactor().ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				}
			}
			this.AddDonationXpTooltip();
		}

		// Token: 0x17000407 RID: 1031
		// (get) Token: 0x06000CA1 RID: 3233 RVA: 0x00035846 File Offset: 0x00033A46
		// (set) Token: 0x06000CA2 RID: 3234 RVA: 0x0003584E File Offset: 0x00033A4E
		[DataSourceProperty]
		public bool IsStealthModeActive
		{
			get
			{
				return this._isStealthModeActive;
			}
			set
			{
				if (value != this._isStealthModeActive)
				{
					this._isStealthModeActive = value;
					base.OnPropertyChangedWithValue(value, "IsStealthModeActive");
				}
			}
		}

		// Token: 0x17000408 RID: 1032
		// (get) Token: 0x06000CA3 RID: 3235 RVA: 0x0003586C File Offset: 0x00033A6C
		// (set) Token: 0x06000CA4 RID: 3236 RVA: 0x00035874 File Offset: 0x00033A74
		[DataSourceProperty]
		public MBBindingList<ItemMenuTooltipPropertyVM> TargetItemProperties
		{
			get
			{
				return this._targetItemProperties;
			}
			set
			{
				if (value != this._targetItemProperties)
				{
					this._targetItemProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ItemMenuTooltipPropertyVM>>(value, "TargetItemProperties");
				}
			}
		}

		// Token: 0x06000CA5 RID: 3237 RVA: 0x00035894 File Offset: 0x00033A94
		private void AddDonationXpTooltip()
		{
			ItemDiscardModel itemDiscardModel = Campaign.Current.Models.ItemDiscardModel;
			int xpBonusForDiscardingItem = itemDiscardModel.GetXpBonusForDiscardingItem(this._targetItem.ItemRosterElement.EquipmentElement.Item, 1);
			int num = (this.IsComparing ? itemDiscardModel.GetXpBonusForDiscardingItem(this._comparedItem.ItemRosterElement.EquipmentElement.Item, 1) : 0);
			if (xpBonusForDiscardingItem > 0 || (this.IsComparing && num > 0))
			{
				InventoryLogic inventoryLogic = this._inventoryLogic;
				if (inventoryLogic != null && inventoryLogic.IsDiscardDonating)
				{
					MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_inventory_donation_item_hint", null).ToString(), false);
					int result = (this.IsComparing ? this.CompareValues(xpBonusForDiscardingItem, num) : 0);
					this.CreateColoredProperty(this.TargetItemProperties, GameTexts.FindText("str_LEFT_colon", null).ToString(), xpBonusForDiscardingItem.ToString(), this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					if (this.IsComparing)
					{
						this.CreateColoredProperty(this.ComparedItemProperties, " ", num.ToString(), this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					}
				}
			}
		}

		// Token: 0x17000409 RID: 1033
		// (get) Token: 0x06000CA6 RID: 3238 RVA: 0x000359B6 File Offset: 0x00033BB6
		// (set) Token: 0x06000CA7 RID: 3239 RVA: 0x000359BE File Offset: 0x00033BBE
		[DataSourceProperty]
		public MBBindingList<ItemMenuTooltipPropertyVM> ComparedItemProperties
		{
			get
			{
				return this._comparedItemProperties;
			}
			set
			{
				if (value != this._comparedItemProperties)
				{
					this._comparedItemProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ItemMenuTooltipPropertyVM>>(value, "ComparedItemProperties");
				}
			}
		}

		// Token: 0x06000CA8 RID: 3240 RVA: 0x000359DC File Offset: 0x00033BDC
		private Color GetColorFromComparison(int result, bool isCompared)
		{
			if (result != -1)
			{
				if (result != 1)
				{
					return Colors.Black;
				}
				if (!isCompared)
				{
					return UIColors.PositiveIndicator;
				}
				return UIColors.NegativeIndicator;
			}
			else
			{
				if (!isCompared)
				{
					return UIColors.NegativeIndicator;
				}
				return UIColors.PositiveIndicator;
			}
		}

		// Token: 0x1700040A RID: 1034
		// (get) Token: 0x06000CA9 RID: 3241 RVA: 0x00035A0B File Offset: 0x00033C0B
		// (set) Token: 0x06000CAA RID: 3242 RVA: 0x00035A13 File Offset: 0x00033C13
		[DataSourceProperty]
		public MBBindingList<ItemFlagVM> TargetItemFlagList
		{
			get
			{
				return this._targetItemFlagList;
			}
			set
			{
				if (value != this._targetItemFlagList)
				{
					this._targetItemFlagList = value;
					base.OnPropertyChangedWithValue<MBBindingList<ItemFlagVM>>(value, "TargetItemFlagList");
				}
			}
		}

		// Token: 0x06000CAB RID: 3243 RVA: 0x00035A34 File Offset: 0x00033C34
		private int GetHorseCategoryValue(ItemCategory itemCategory)
		{
			if (itemCategory.IsAnimal)
			{
				if (itemCategory == DefaultItemCategories.PackAnimal)
				{
					return 1;
				}
				if (itemCategory == DefaultItemCategories.Horse)
				{
					return 2;
				}
				if (itemCategory == DefaultItemCategories.WarHorse)
				{
					return 3;
				}
				if (itemCategory == DefaultItemCategories.NobleHorse)
				{
					return 4;
				}
			}
			Debug.FailedAssert("This horse item category is not defined", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Inventory\\ItemMenuVM.cs", "GetHorseCategoryValue", 1446);
			return -1;
		}

		// Token: 0x1700040B RID: 1035
		// (get) Token: 0x06000CAC RID: 3244 RVA: 0x00035A8B File Offset: 0x00033C8B
		// (set) Token: 0x06000CAD RID: 3245 RVA: 0x00035A93 File Offset: 0x00033C93
		[DataSourceProperty]
		public MBBindingList<ItemFlagVM> ComparedItemFlagList
		{
			get
			{
				return this._comparedItemFlagList;
			}
			set
			{
				if (value != this._comparedItemFlagList)
				{
					this._comparedItemFlagList = value;
					base.OnPropertyChangedWithValue<MBBindingList<ItemFlagVM>>(value, "ComparedItemFlagList");
				}
			}
		}

		// Token: 0x06000CAE RID: 3246 RVA: 0x00035AB4 File Offset: 0x00033CB4
		private ItemMenuTooltipPropertyVM CreateProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetList, string definition, string value, int textHeight = 0, HintViewModel hint = null)
		{
			ItemMenuTooltipPropertyVM itemMenuTooltipPropertyVM = new ItemMenuTooltipPropertyVM(definition, value, textHeight, false, hint);
			targetList.Add(itemMenuTooltipPropertyVM);
			return itemMenuTooltipPropertyVM;
		}

		// Token: 0x1700040C RID: 1036
		// (get) Token: 0x06000CAF RID: 3247 RVA: 0x00035AD6 File Offset: 0x00033CD6
		// (set) Token: 0x06000CB0 RID: 3248 RVA: 0x00035ADE File Offset: 0x00033CDE
		[DataSourceProperty]
		public int AlternativeUsageIndex
		{
			get
			{
				return this._alternativeUsageIndex;
			}
			set
			{
				if (value != this._alternativeUsageIndex)
				{
					this._alternativeUsageIndex = value;
					base.OnPropertyChangedWithValue(value, "AlternativeUsageIndex");
					this.AlternativeUsageIndexUpdated();
				}
			}
		}

		// Token: 0x06000CB1 RID: 3249 RVA: 0x00035B04 File Offset: 0x00033D04
		private ItemMenuTooltipPropertyVM CreateColoredProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetList, string definition, string value, Color color, int textHeight = 0, HintViewModel hint = null, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			if (color == Colors.Black)
			{
				this.CreateProperty(targetList, definition, value, textHeight, hint);
				return null;
			}
			ItemMenuTooltipPropertyVM itemMenuTooltipPropertyVM = new ItemMenuTooltipPropertyVM(definition, value, textHeight, color, false, hint, propertyFlags);
			targetList.Add(itemMenuTooltipPropertyVM);
			return itemMenuTooltipPropertyVM;
		}

		// Token: 0x1700040D RID: 1037
		// (get) Token: 0x06000CB2 RID: 3250 RVA: 0x00035B48 File Offset: 0x00033D48
		// (set) Token: 0x06000CB3 RID: 3251 RVA: 0x00035B50 File Offset: 0x00033D50
		[DataSourceProperty]
		public MBBindingList<StringItemWithHintVM> AlternativeUsages
		{
			get
			{
				return this._alternativeUsages;
			}
			set
			{
				if (value != this._alternativeUsages)
				{
					this._alternativeUsages = value;
					base.OnPropertyChangedWithValue<MBBindingList<StringItemWithHintVM>>(value, "AlternativeUsages");
				}
			}
		}

		// Token: 0x06000CB4 RID: 3252 RVA: 0x00035B70 File Offset: 0x00033D70
		public void SetTransactionCost(int getItemTotalPrice, int maxIndividualPrice)
		{
			this.TransactionTotalCost = getItemTotalPrice;
			if (this._targetItem.ItemCost == maxIndividualPrice)
			{
				this._costProperty.ValueLabel = this._targetItem.ItemCost + this.GoldIcon;
				return;
			}
			if (this._targetItem.ItemCost < maxIndividualPrice)
			{
				this._costProperty.ValueLabel = string.Concat(new object[]
				{
					this._targetItem.ItemCost,
					" - ",
					maxIndividualPrice,
					this.GoldIcon
				});
				return;
			}
			this._costProperty.ValueLabel = string.Concat(new object[]
			{
				maxIndividualPrice,
				" - ",
				this._targetItem.ItemCost,
				this.GoldIcon
			});
		}

		// Token: 0x0400058B RID: 1419
		private readonly TextObject _swingDamageText = GameTexts.FindText("str_swing_damage", null);

		// Token: 0x0400058C RID: 1420
		private readonly TextObject _swingSpeedText = new TextObject("{=345a87fcc69f626ae3916939ef2fc135}Swing Speed: ", null);

		// Token: 0x0400058D RID: 1421
		private readonly TextObject _weaponTierText = new TextObject("{=weaponTier}Weapon Tier: ", null);

		// Token: 0x0400058E RID: 1422
		private readonly TextObject _armorTierText = new TextObject("{=armorTier}Armor Tier: ", null);

		// Token: 0x0400058F RID: 1423
		private readonly TextObject _horseTierText = new TextObject("{=mountTier}Mount Tier: ", null);

		// Token: 0x04000590 RID: 1424
		private readonly TextObject _horseTypeText = new TextObject("{=9sxECG6e}Mount Type: ", null);

		// Token: 0x04000591 RID: 1425
		private readonly TextObject _chargeDamageText = new TextObject("{=c7638a0869219ae845de0f660fd57a9d}Charge Damage: ", null);

		// Token: 0x04000592 RID: 1426
		private readonly TextObject _hitPointsText = GameTexts.FindText("str_hit_points", null);

		// Token: 0x04000593 RID: 1427
		private readonly TextObject _speedText = new TextObject("{=74dc1908cb0b990e80fb977b5a0ef10d}Speed: ", null);

		// Token: 0x04000594 RID: 1428
		private readonly TextObject _maneuverText = new TextObject("{=3025020b83b218707499f0de3135ed0a}Maneuver: ", null);

		// Token: 0x04000595 RID: 1429
		private readonly TextObject _thrustSpeedText = GameTexts.FindText("str_thrust_speed", null);

		// Token: 0x04000596 RID: 1430
		private readonly TextObject _thrustDamageText = GameTexts.FindText("str_thrust_damage", null);

		// Token: 0x04000597 RID: 1431
		private readonly TextObject _lengthText = GameTexts.FindText("str_crafting_stat", "WeaponReach");

		// Token: 0x04000598 RID: 1432
		private readonly TextObject _weightText = GameTexts.FindText("str_weight_text", null);

		// Token: 0x04000599 RID: 1433
		private readonly TextObject _handlingText = new TextObject("{=ca8b1e8956057b831dfc665f54bae4b0}Handling: ", null);

		// Token: 0x0400059A RID: 1434
		private readonly TextObject _weaponLengthText = new TextObject("{=5fa36d2798479803b4518a64beb4d732}Weapon Length: ", null);

		// Token: 0x0400059B RID: 1435
		private readonly TextObject _damageText = new TextObject("{=c9c5dfed2ca6bcb7a73d905004c97b23}Damage: ", null);

		// Token: 0x0400059C RID: 1436
		private readonly TextObject _missileSpeedText = GameTexts.FindText("str_missile_speed", null);

		// Token: 0x0400059D RID: 1437
		private readonly TextObject _accuracyText = new TextObject("{=5dec16fa0be433ade3c4cb0074ef366d}Accuracy: ", null);

		// Token: 0x0400059E RID: 1438
		private readonly TextObject _stackAmountText = new TextObject("{=05fdfc6e238429753ef282f2ce97c1f8}Stack Amount: ", null);

		// Token: 0x0400059F RID: 1439
		private readonly TextObject _ammoLimitText = new TextObject("{=6adabc1f82216992571c3e22abc164d7}Ammo Limit: ", null);

		// Token: 0x040005A0 RID: 1440
		private readonly TextObject _requiresText = new TextObject("{=154a34f8caccfc833238cc89d38861e8}Requires: ", null);

		// Token: 0x040005A1 RID: 1441
		private readonly TextObject _foodText = new TextObject("{=qSi4DlT4}Food", null);

		// Token: 0x040005A2 RID: 1442
		private readonly TextObject _partyMoraleText = new TextObject("{=a241aacb1780599430c79fd9f667b67f}Party Morale: ", null);

		// Token: 0x040005A3 RID: 1443
		private readonly TextObject _typeText = new TextObject("{=08abd5af7774d311cadc3ed900b47754}Type: ", null);

		// Token: 0x040005A4 RID: 1444
		private readonly TextObject _tradeRumorsText = new TextObject("{=f2971dc587a9777223ad2d7be236fb05}Trade Rumors", null);

		// Token: 0x040005A5 RID: 1445
		private readonly TextObject _classText = new TextObject("{=8cad4a279770f269c4bb0dc7a357ee1e}Class: ", null);

		// Token: 0x040005A6 RID: 1446
		private readonly TextObject _headArmorText = GameTexts.FindText("str_head_armor", null);

		// Token: 0x040005A7 RID: 1447
		private readonly TextObject _horseArmorText = new TextObject("{=305cf7f98458b22e9af72b60a131714f}Horse Armor: ", null);

		// Token: 0x040005A8 RID: 1448
		private readonly TextObject _bodyArmorText = GameTexts.FindText("str_body_armor", null);

		// Token: 0x040005A9 RID: 1449
		private readonly TextObject _legArmorText = GameTexts.FindText("str_leg_armor", null);

		// Token: 0x040005AA RID: 1450
		private readonly TextObject _armArmorText = new TextObject("{=cf61cce254c7dca65be9bebac7fb9bf5}Arm Armor: ", null);

		// Token: 0x040005AB RID: 1451
		private readonly TextObject _stealthBonusText = new TextObject("{=YJkAqExw}Stealth Bonus: ", null);

		// Token: 0x040005AC RID: 1452
		private readonly TextObject _bannerEffectText = new TextObject("{=DbXZjPdf}Banner Effect: ", null);

		// Token: 0x040005AD RID: 1453
		private readonly TextObject _noneText = new TextObject("{=koX9okuG}None", null);

		// Token: 0x040005AE RID: 1454
		private readonly string GoldIcon = "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\"/>";

		// Token: 0x040005AF RID: 1455
		private readonly Color ConsumableColor = Color.FromUint(4290873921U);

		// Token: 0x040005B0 RID: 1456
		private readonly Color TitleColor = Color.FromUint(4293446041U);

		// Token: 0x040005B1 RID: 1457
		private TooltipProperty _costProperty;

		// Token: 0x040005B2 RID: 1458
		private InventoryLogic _inventoryLogic;

		// Token: 0x040005B3 RID: 1459
		private Action<ItemVM, int> _resetComparedItems;

		// Token: 0x040005B4 RID: 1460
		private readonly Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> _getItemUsageSetFlags;

		// Token: 0x040005B5 RID: 1461
		private readonly Func<EquipmentIndex, SPItemVM> _getEquipmentAtIndex;

		// Token: 0x040005B6 RID: 1462
		private int _lastComparedItemVersion;

		// Token: 0x040005B7 RID: 1463
		private ItemVM _targetItem;

		// Token: 0x040005B8 RID: 1464
		private bool _isComparing;

		// Token: 0x040005B9 RID: 1465
		private bool _isStealthModeActive;

		// Token: 0x040005BA RID: 1466
		private ItemVM _comparedItem;

		// Token: 0x040005BB RID: 1467
		private bool _isPlayerItem;

		// Token: 0x040005BC RID: 1468
		private BasicCharacterObject _character;

		// Token: 0x040005BD RID: 1469
		private ItemImageIdentifierVM _imageIdentifier;

		// Token: 0x040005BE RID: 1470
		private ItemImageIdentifierVM _comparedImageIdentifier;

		// Token: 0x040005BF RID: 1471
		private string _itemName;

		// Token: 0x040005C0 RID: 1472
		private string _comparedItemName;

		// Token: 0x040005C1 RID: 1473
		private MBBindingList<ItemMenuTooltipPropertyVM> _comparedItemProperties;

		// Token: 0x040005C2 RID: 1474
		private MBBindingList<ItemMenuTooltipPropertyVM> _targetItemProperties;

		// Token: 0x040005C3 RID: 1475
		private bool _isInitializationOver;

		// Token: 0x040005C4 RID: 1476
		private int _transactionTotalCost = -1;

		// Token: 0x040005C5 RID: 1477
		private MBBindingList<ItemFlagVM> _targetItemFlagList;

		// Token: 0x040005C6 RID: 1478
		private MBBindingList<ItemFlagVM> _comparedItemFlagList;

		// Token: 0x040005C7 RID: 1479
		private int _alternativeUsageIndex;

		// Token: 0x040005C8 RID: 1480
		private MBBindingList<StringItemWithHintVM> _alternativeUsages;

		// Token: 0x040005C9 RID: 1481
		private ITradeRumorCampaignBehavior _tradeRumorsBehavior;
	}
}

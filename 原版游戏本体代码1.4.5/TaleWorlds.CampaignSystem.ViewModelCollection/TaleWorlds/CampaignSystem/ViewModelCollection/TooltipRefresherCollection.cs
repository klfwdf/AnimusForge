using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public static class TooltipRefresherCollection
{
	private static readonly IEqualityComparer<(ItemCategory, int)> itemCategoryDistinctComparer = new CampaignUIHelper.ProductInputOutputEqualityComparer();

	private static string ExtendKeyId = "ExtendModifier";

	private static string FollowModifierKeyId = "FollowModifier";

	private static string MapClickKeyId = "MapClick";

	public static void RefreshExplainedNumberTooltip(RundownTooltipVM explainedNumberTooltip, object[] args)
	{
		explainedNumberTooltip.IsActive = explainedNumberTooltip.IsInitializedProperly;
		if (!explainedNumberTooltip.IsActive)
		{
			return;
		}
		Func<ExplainedNumber> func = args[0] as Func<ExplainedNumber>;
		Func<ExplainedNumber> func2 = args[1] as Func<ExplainedNumber>;
		explainedNumberTooltip.Lines.Clear();
		Func<ExplainedNumber> func3 = ((explainedNumberTooltip.IsExtended && func2 != null) ? func2 : func);
		if (func3 == null)
		{
			return;
		}
		ExplainedNumber explainedNumber = func3();
		explainedNumberTooltip.CurrentExpectedChange = explainedNumber.ResultNumber;
		foreach (var line in explainedNumber.GetLines())
		{
			explainedNumberTooltip.Lines.Add(new RundownLineVM(line.name, line.number));
		}
	}

	public static void RefreshTrackTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Track track = args[0] as Track;
		propertyBasedTooltipVM.Mode = 1;
		MapTrackModel mapTrackModel = Campaign.Current.Models.MapTrackModel;
		if (mapTrackModel == null)
		{
			return;
		}
		TextObject textObject = mapTrackModel.TrackTitle(track);
		propertyBasedTooltipVM.AddProperty("", textObject.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		foreach (var item in mapTrackModel.GetTrackDescription(track))
		{
			propertyBasedTooltipVM.AddProperty(item.Item1?.ToString(), item.Item2);
		}
	}

	public static void RefreshHeroTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Hero hero = args[0] as Hero;
		bool flag = (bool)args[1];
		StringHelpers.SetCharacterProperties("NPC", hero.CharacterObject);
		TextObject disableReason;
		bool num = CampaignUIHelper.IsHeroInformationHidden(hero, out disableReason);
		if (hero.IsEnemy(Hero.MainHero))
		{
			propertyBasedTooltipVM.Mode = 3;
		}
		else if (hero == Hero.MainHero || hero.IsFriend(Hero.MainHero))
		{
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			propertyBasedTooltipVM.Mode = 1;
		}
		propertyBasedTooltipVM.AddProperty("", hero.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (!hero.IsNotable && !hero.IsWanderer)
		{
			if (hero.Clan?.Kingdom != null)
			{
				propertyBasedTooltipVM.AddProperty("", CampaignUIHelper.GetHeroKingdomRank(hero));
			}
			if (Game.Current.IsDevelopmentMode && hero.Clan?.Leader == hero)
			{
				propertyBasedTooltipVM.AddProperty("DEBUG Clan Leader", "");
			}
			if (Game.Current.IsDevelopmentMode && hero.Clan?.Kingdom != null)
			{
				if (hero == hero.MapFaction?.Leader && hero.Clan?.Kingdom != null)
				{
					propertyBasedTooltipVM.AddProperty("DEBUG Kingdom Gold", hero.Clan.Kingdom.KingdomBudgetWallet.ToString());
				}
				propertyBasedTooltipVM.AddProperty("DEBUG Gold", hero.Gold.ToString());
				if (Game.Current.IsDevelopmentMode && hero.Clan != null && hero.Clan.IsUnderMercenaryService)
				{
					propertyBasedTooltipVM.AddProperty("DEBUG Mercenary Award", hero.Clan.MercenaryAwardMultiplier.ToString());
				}
				if (Game.Current.IsDevelopmentMode && hero.Clan?.Leader == hero)
				{
					propertyBasedTooltipVM.AddProperty("DEBUG Debt To Kingdom", hero.Clan.DebtToKingdom.ToString());
				}
			}
			if (Game.Current.IsDevelopmentMode && hero.PartyBelongedTo != null && !hero.IsSpecial)
			{
				propertyBasedTooltipVM.AddProperty("DEBUG Party Size", hero.PartyBelongedTo.MemberRoster.TotalManCount + "/" + hero.PartyBelongedTo.Party.PartySizeLimit);
				propertyBasedTooltipVM.AddProperty("DEBUG Party Position", (int)hero.PartyBelongedTo.Position.X + "," + (int)hero.PartyBelongedTo.Position.Y);
				propertyBasedTooltipVM.AddProperty("DEBUG Party Wage", hero.PartyBelongedTo.TotalWage.ToString());
			}
			if (Game.Current.IsDevelopmentMode && hero.PartyBelongedTo != null)
			{
				propertyBasedTooltipVM.AddProperty("DEBUG Party Morale", hero.PartyBelongedTo.Morale.ToString());
			}
			if (Game.Current.IsDevelopmentMode && hero.PartyBelongedTo != null)
			{
				propertyBasedTooltipVM.AddProperty("DEBUG Starving", hero.PartyBelongedTo.Party.IsStarving.ToString());
			}
			if (Game.Current.IsDevelopmentMode && hero.MapFaction?.Leader != null && hero != hero.MapFaction.Leader)
			{
				propertyBasedTooltipVM.AddProperty("DEBUG King Relation", hero.GetRelation(hero.MapFaction.Leader).ToString());
			}
			if (Game.Current.IsDevelopmentMode && hero.PartyBelongedToAsPrisoner != null)
			{
				propertyBasedTooltipVM.AddProperty("DEBUG Prisoner at", hero.PartyBelongedToAsPrisoner.Name.ToString());
			}
		}
		if (hero.Clan != null)
		{
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_clan").ToString(), hero.Clan.Name.ToString());
		}
		propertyBasedTooltipVM.AddProperty("", "", -1);
		if (!num)
		{
			List<TextObject> list = new List<TextObject>();
			foreach (Settlement item2 in Settlement.All)
			{
				if (item2.IsTown)
				{
					Town town = item2.Town;
					list.AddRange(from x in town.Workshops
						where x.Owner == hero && !x.WorkshopType.IsHidden
						select x.WorkshopType.Name);
				}
				if (!item2.IsTown && !item2.IsVillage)
				{
					continue;
				}
				foreach (Alley alley in item2.Alleys)
				{
					if (alley.Owner == hero)
					{
						MBTextManager.SetTextVariable("RANK", alley.Name);
						MBTextManager.SetTextVariable("NUMBER", Campaign.Current.Models.AlleyModel.GetTroopsOfAIOwnedAlley(alley).TotalManCount);
						TextObject item = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis");
						list.Add(item);
					}
				}
			}
			MBTextManager.SetTextVariable("PROPERTIES", CampaignUIHelper.GetCommaSeparatedText(new TextObject("{=VZjxs5Dt}Owner of "), list));
			string value = new TextObject("{=j8uZBakZ}{PROPERTIES}").ToString();
			if (list.Count > 0)
			{
				propertyBasedTooltipVM.AddProperty("", value, 0, TooltipProperty.TooltipPropertyFlags.MultiLine);
			}
			TextObject textObject = new TextObject("{=C2qpwFq5}Owner of {SETTLEMENTS}");
			IEnumerable<TextObject> enumerable = from x in Settlement.FindAll((Settlement x) => x.IsFortification && x.OwnerClan != null && x.OwnerClan.Leader == hero)
				select x.Name;
			MBTextManager.SetTextVariable("SETTLEMENTS", CampaignUIHelper.GetCommaSeparatedText(null, enumerable));
			if (enumerable.Count() > 0)
			{
				propertyBasedTooltipVM.AddProperty("", textObject.ToString(), 0, TooltipProperty.TooltipPropertyFlags.MultiLine);
			}
			if (hero.OwnedCaravans.Count > 0)
			{
				TextObject empty = TextObject.GetEmpty();
				Settlement currentSettlement = hero.CurrentSettlement;
				empty = ((currentSettlement == null || !currentSettlement.HasPort) ? new TextObject("{=TEkWkzbH}Owned Caravans: {CARAVAN_COUNT}") : new TextObject("{=03G5GPec}Owned Convoys: {CARAVAN_COUNT}"));
				empty.SetTextVariable("CARAVAN_COUNT", hero.OwnedCaravans.Count);
				propertyBasedTooltipVM.AddProperty("", empty.ToString());
			}
			if (hero.GovernorOf != null)
			{
				MBTextManager.SetTextVariable("STR1", new TextObject("{=jQdBl4hf}Governor of "));
				MBTextManager.SetTextVariable("STR2", hero.GovernorOf.Name);
				TextObject textObject2 = GameTexts.FindText("str_STR1_STR2");
				propertyBasedTooltipVM.AddProperty("", textObject2.ToString);
			}
			if (hero != Hero.MainHero)
			{
				MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_relation"));
				string definition = GameTexts.FindText("str_LEFT_ONLY").ToString();
				propertyBasedTooltipVM.AddProperty(definition, ((int)hero.GetRelationWithPlayer()).ToString());
			}
		}
		if (hero.HomeSettlement != null)
		{
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_home"));
			string definition2 = GameTexts.FindText("str_LEFT_ONLY").ToString();
			propertyBasedTooltipVM.AddProperty(definition2, hero.HomeSettlement.Name.ToString());
		}
		if (hero.IsNotable)
		{
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_notable_power"));
			string definition3 = GameTexts.FindText("str_LEFT_colon").ToString();
			MBTextManager.SetTextVariable("RANK", Campaign.Current.Models.NotablePowerModel.GetPowerRankName(hero).ToString());
			MBTextManager.SetTextVariable("NUMBER", ((int)hero.Power).ToString());
			string value2 = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis").ToString();
			propertyBasedTooltipVM.AddProperty(definition3, value2);
			if (Game.Current.IsDevelopmentMode)
			{
				propertyBasedTooltipVM.AddProperty("", "");
				ExplainedNumber explainedNumber = Campaign.Current.Models.NotablePowerModel.CalculateDailyPowerChangeForHero(hero, includeDescriptions: true);
				propertyBasedTooltipVM.AddProperty("[DEV] Daily Power Change", explainedNumber.ResultNumber.ToString("+0.##;-0.##;0"), 0, TooltipProperty.TooltipPropertyFlags.RundownResult);
				propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
				foreach (var (text, num2) in explainedNumber.GetLines())
				{
					propertyBasedTooltipVM.AddProperty("[DEV] " + text, num2.ToString("+0.##;-0.##;0"));
				}
				propertyBasedTooltipVM.AddProperty("", "");
			}
		}
		MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_type"));
		string definition4 = GameTexts.FindText("str_LEFT_ONLY").ToString();
		propertyBasedTooltipVM.AddProperty(definition4, HeroHelper.GetCharacterTypeName(hero).ToString());
		if (hero.CurrentSettlement != null && LocationComplex.Current != null && hero.CurrentSettlement == Hero.MainHero.CurrentSettlement && LocationComplex.Current.GetLocationOfCharacter(hero) != null)
		{
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_location"));
			string definition5 = GameTexts.FindText("str_LEFT_ONLY").ToString();
			propertyBasedTooltipVM.AddProperty(definition5, LocationComplex.Current.GetLocationOfCharacter(hero).DoorName.ToString());
		}
		if (hero.CurrentSettlement != null && hero.IsNotable && hero.SupporterOf != null)
		{
			MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_tooltip_label_supporter_of"));
			string definition6 = GameTexts.FindText("str_LEFT_ONLY").ToString();
			propertyBasedTooltipVM.AddProperty(definition6, hero.SupporterOf.Name.ToString());
		}
		if (flag)
		{
			List<(CampaignUIHelper.IssueQuestFlags, TextObject, TextObject)> questStateOfHero = CampaignUIHelper.GetQuestStateOfHero(hero);
			for (int num3 = 0; num3 < questStateOfHero.Count; num3++)
			{
				string questExplanationOfHero = CampaignUIHelper.GetQuestExplanationOfHero(questStateOfHero[num3].Item1);
				if (!string.IsNullOrEmpty(questExplanationOfHero))
				{
					propertyBasedTooltipVM.AddProperty("", "", -1);
					propertyBasedTooltipVM.AddProperty(questExplanationOfHero, questStateOfHero[num3].Item2.ToString());
				}
			}
		}
		if (!hero.IsAlive)
		{
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_dead").ToString());
		}
	}

	public static void RefreshInventoryTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		InventoryLogic inventoryLogic = args[0] as InventoryLogic;
		propertyBasedTooltipVM.Mode = 0;
		List<(ItemRosterElement, int)> soldItems = inventoryLogic.GetSoldItems();
		List<(ItemRosterElement, int)> boughtItems = inventoryLogic.GetBoughtItems();
		TextObject textObject = new TextObject("{=bPFjmYCI}{SHOP_NAME} x {SHOP_DIFFERENCE_COUNT}");
		TextObject textObject2 = new TextObject("{=lxwGbRwu}x {SHOP_DIFFERENCE_COUNT}");
		TextObject textObject3 = (inventoryLogic.IsTrading ? textObject : textObject2);
		int num = 0;
		int num2 = 40;
		foreach (var item2 in soldItems)
		{
			if (num == num2)
			{
				break;
			}
			ItemRosterElement itemRosterElement;
			(itemRosterElement, _) = item2;
			textObject3.SetTextVariable("SHOP_NAME", itemRosterElement.EquipmentElement.GetModifiedItemName());
			(itemRosterElement, _) = item2;
			textObject3.SetTextVariable("SHOP_DIFFERENCE_COUNT", itemRosterElement.Amount);
			if (inventoryLogic.IsTrading)
			{
				string definition = textObject3.ToString();
				int item = item2.Item2;
				propertyBasedTooltipVM.AddColoredProperty(definition, "+" + item, UIColors.PositiveIndicator);
			}
			else
			{
				(itemRosterElement, _) = item2;
				propertyBasedTooltipVM.AddColoredProperty(itemRosterElement.EquipmentElement.GetModifiedItemName().ToString(), textObject3.ToString(), UIColors.NegativeIndicator);
			}
			num++;
		}
		foreach (var item3 in boughtItems)
		{
			if (num == num2)
			{
				break;
			}
			ItemRosterElement itemRosterElement;
			(itemRosterElement, _) = item3;
			textObject3.SetTextVariable("SHOP_NAME", itemRosterElement.EquipmentElement.GetModifiedItemName());
			(itemRosterElement, _) = item3;
			textObject3.SetTextVariable("SHOP_DIFFERENCE_COUNT", itemRosterElement.Amount);
			if (inventoryLogic.IsTrading)
			{
				propertyBasedTooltipVM.AddColoredProperty(textObject3.ToString(), (-item3.Item2).ToString(), UIColors.NegativeIndicator);
			}
			else
			{
				(itemRosterElement, _) = item3;
				propertyBasedTooltipVM.AddColoredProperty(itemRosterElement.EquipmentElement.GetModifiedItemName().ToString(), textObject3.ToString(), UIColors.PositiveIndicator);
			}
			num++;
		}
		if (num == num2)
		{
			int num3 = soldItems.Count + boughtItems.Count - num;
			if (num3 > 0)
			{
				TextObject textObject4 = new TextObject("{=OpsiBFCu}... and {COUNT} more items.");
				textObject4.SetTextVariable("COUNT", num3);
				propertyBasedTooltipVM.AddProperty("", textObject4.ToString());
			}
		}
	}

	public static void RefreshCraftingPartTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		WeaponDesignElement weaponDesignElement = args[0] as WeaponDesignElement;
		propertyBasedTooltipVM.Mode = 0;
		propertyBasedTooltipVM.AddProperty("", weaponDesignElement.CraftingPiece.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		TextObject textObject = GameTexts.FindText("str_crafting_piece_type", weaponDesignElement.CraftingPiece.PieceType.ToString());
		propertyBasedTooltipVM.AddProperty("", textObject.ToString());
		propertyBasedTooltipVM.AddProperty(new TextObject("{=Oo3fkeab}Difficulty: ").ToString(), Campaign.Current.Models.SmithingModel.GetCraftingPartDifficulty(weaponDesignElement.CraftingPiece).ToString());
		propertyBasedTooltipVM.AddProperty(new TextObject("{=XUtiwiYP}Length: ").ToString(), TaleWorlds.Library.MathF.Round(weaponDesignElement.CraftingPiece.Length * 100f, 2).ToString());
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_weight_text").ToString(), TaleWorlds.Library.MathF.Round(weaponDesignElement.CraftingPiece.Weight, 2).ToString());
		if (weaponDesignElement.CraftingPiece.PieceType == CraftingPiece.PieceTypes.Blade)
		{
			if (weaponDesignElement.CraftingPiece.BladeData.SwingDamageType != DamageTypes.Invalid)
			{
				DamageTypes swingDamageType = weaponDesignElement.CraftingPiece.BladeData.SwingDamageType;
				MBTextManager.SetTextVariable("SWING_DAMAGE_FACTOR", TaleWorlds.Library.MathF.Round(weaponDesignElement.CraftingPiece.BladeData.SwingDamageFactor, 2) + " " + GameTexts.FindText("str_damage_types", swingDamageType.ToString()).ToString()[0].ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=nYYUQQm0}Swing Damage Factor ").ToString(), new TextObject("{=aTdrjrEh}{SWING_DAMAGE_FACTOR}").ToString());
			}
			if (weaponDesignElement.CraftingPiece.BladeData.ThrustDamageType != DamageTypes.Invalid)
			{
				DamageTypes thrustDamageType = weaponDesignElement.CraftingPiece.BladeData.ThrustDamageType;
				MBTextManager.SetTextVariable("THRUST_DAMAGE_FACTOR", TaleWorlds.Library.MathF.Round(weaponDesignElement.CraftingPiece.BladeData.ThrustDamageFactor, 2) + " " + GameTexts.FindText("str_damage_types", thrustDamageType.ToString()).ToString()[0].ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=KTKBKmvp}Thrust Damage Factor ").ToString(), new TextObject("{=DNq9bdvV}{THRUST_DAMAGE_FACTOR}").ToString());
			}
		}
		if (weaponDesignElement.CraftingPiece.ArmorBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=7Xynf4IA}Hand Armor").ToString(), weaponDesignElement.CraftingPiece.ArmorBonus);
		}
		if (weaponDesignElement.CraftingPiece.SwingDamageBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=QeToaiLt}Swing Damage").ToString(), weaponDesignElement.CraftingPiece.SwingDamageBonus);
		}
		if (weaponDesignElement.CraftingPiece.SwingSpeedBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=sVZaIPoQ}Swing Speed").ToString(), weaponDesignElement.CraftingPiece.SwingSpeedBonus);
		}
		if (weaponDesignElement.CraftingPiece.ThrustDamageBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=dO95yR9b}Thrust Damage").ToString(), weaponDesignElement.CraftingPiece.ThrustDamageBonus);
		}
		if (weaponDesignElement.CraftingPiece.ThrustSpeedBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=4uMWNDoi}Thrust Speed").ToString(), weaponDesignElement.CraftingPiece.ThrustSpeedBonus);
		}
		if (weaponDesignElement.CraftingPiece.HandlingBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=oibdTnXP}Handling").ToString(), weaponDesignElement.CraftingPiece.HandlingBonus);
		}
		if (weaponDesignElement.CraftingPiece.AccuracyBonus != 0)
		{
			propertyBasedTooltipVM.AddModifierProperty(new TextObject("{=TAnabTdy}Accuracy").ToString(), weaponDesignElement.CraftingPiece.AccuracyBonus);
		}
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=hr4MuPnt}Required Materials").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", string.Empty, -1, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		foreach (var item2 in weaponDesignElement.CraftingPiece.MaterialsUsed)
		{
			ItemObject craftingMaterialItem = Campaign.Current.Models.SmithingModel.GetCraftingMaterialItem(item2.Item1);
			if (craftingMaterialItem != null)
			{
				string definition = craftingMaterialItem.Name.ToString();
				int item = item2.Item2;
				propertyBasedTooltipVM.AddProperty(definition, item.ToString());
			}
		}
	}

	public static void RefreshCharacterTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		CharacterObject characterObject = args[0] as CharacterObject;
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty("", characterObject.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		TextObject textObject = GameTexts.FindText("str_party_troop_tier");
		textObject.SetTextVariable("TIER_LEVEL", characterObject.Tier);
		propertyBasedTooltipVM.AddProperty("", textObject.ToString());
		if (characterObject.UpgradeTargets.Length != 0)
		{
			GameTexts.SetVariable("XP_AMOUNT", characterObject.GetUpgradeXpCost(PartyBase.MainParty, 0));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_required_xp_to_upgrade").ToString());
		}
		if (characterObject.TroopWage > 0)
		{
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_wage"));
			GameTexts.SetVariable("STR1", characterObject.TroopWage);
			GameTexts.SetVariable("STR2", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			GameTexts.SetVariable("RIGHT", GameTexts.FindText("str_STR1_space_STR2"));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString());
		}
		propertyBasedTooltipVM.AddProperty("", "");
		propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_skills").ToString());
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		foreach (SkillObject item in Skills.All)
		{
			if (characterObject.GetSkillValue(item) > 0)
			{
				propertyBasedTooltipVM.AddProperty(item.Name.ToString(), characterObject.GetSkillValue(item).ToString());
			}
		}
	}

	public static void RefreshItemTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		EquipmentElement? equipmentElement = args[0] as EquipmentElement?;
		ItemObject item = equipmentElement.Value.Item;
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty("", item.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=zMMqgxb1}Type").ToString(), GameTexts.FindText("str_inventory_type_" + (int)item.Type).ToString());
		propertyBasedTooltipVM.AddProperty(" ", " ");
		if (Game.Current.IsDevelopmentMode)
		{
			if (item.Culture != null)
			{
				propertyBasedTooltipVM.AddProperty("Culture: ", item.Culture.StringId);
			}
			else
			{
				propertyBasedTooltipVM.AddProperty("Culture: ", "No Culture");
			}
			propertyBasedTooltipVM.AddProperty("ID: ", item.StringId);
		}
		if (item.RelevantSkill != null && item.Difficulty > 0)
		{
			propertyBasedTooltipVM.AddProperty(new TextObject("{=dWYm9GsC}Requires").ToString(), " ");
			propertyBasedTooltipVM.AddProperty(" ", " ", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
			propertyBasedTooltipVM.AddProperty(item.RelevantSkill.Name.ToString(), item.Difficulty.ToString());
			propertyBasedTooltipVM.AddProperty(" ", " ");
		}
		propertyBasedTooltipVM.AddProperty(new TextObject("{=4Dd2xgPm}Weight").ToString(), item.Weight.ToString());
		string text = "";
		if (item.IsUniqueItem)
		{
			if (text != string.Empty)
			{
				TextObject textObject = GameTexts.FindText("str_STR1_space_STR2");
				textObject.SetTextVariable("STR1", text);
				textObject.SetTextVariable("STR2", GameTexts.FindText("str_inventory_flag_unique").ToString());
				text = textObject.ToString();
			}
			else
			{
				text = GameTexts.FindText("str_inventory_flag_unique").ToString();
			}
		}
		if (item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByFemale))
		{
			if (text != string.Empty)
			{
				TextObject textObject2 = GameTexts.FindText("str_STR1_space_STR2");
				textObject2.SetTextVariable("STR1", text);
				textObject2.SetTextVariable("STR2", GameTexts.FindText("str_inventory_flag_male_only").ToString());
				text = textObject2.ToString();
			}
			else
			{
				text = GameTexts.FindText("str_inventory_flag_male_only").ToString();
			}
		}
		if (item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))
		{
			if (text != string.Empty)
			{
				TextObject textObject3 = GameTexts.FindText("str_STR1_space_STR2");
				textObject3.SetTextVariable("STR1", text);
				textObject3.SetTextVariable("STR2", GameTexts.FindText("str_inventory_flag_female_only").ToString());
				text = textObject3.ToString();
			}
			else
			{
				text = GameTexts.FindText("str_inventory_flag_female_only").ToString();
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			propertyBasedTooltipVM.AddProperty(new TextObject("{=eHVq6yDa}Item Properties").ToString(), text);
		}
		if (item.HasArmorComponent)
		{
			if (Campaign.Current != null)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=US7UmBbt}Armor Tier").ToString(), ((int)(item.Tier + 1)).ToString());
			}
			if (item.ArmorComponent.HeadArmor != 0)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=O3dhjtOS}Head Armor").ToString(), equipmentElement.Value.GetModifiedHeadArmor().ToString());
			}
			if (item.ArmorComponent.BodyArmor != 0)
			{
				if (item.Type == ItemObject.ItemTypeEnum.HorseHarness)
				{
					propertyBasedTooltipVM.AddProperty(new TextObject("{=kftE5nvv}Horse Armor").ToString(), equipmentElement.Value.GetModifiedMountBodyArmor().ToString());
				}
				else
				{
					propertyBasedTooltipVM.AddProperty(new TextObject("{=HkfY3Ds5}Body Armor").ToString(), equipmentElement.Value.GetModifiedBodyArmor().ToString());
				}
			}
			if (item.ArmorComponent.ArmArmor != 0)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=kx7q8ybD}Arm Armor").ToString(), equipmentElement.Value.GetModifiedArmArmor().ToString());
			}
			if (item.ArmorComponent.LegArmor != 0)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=eIws123Z}Leg Armor").ToString(), equipmentElement.Value.GetModifiedLegArmor().ToString());
			}
		}
		else if (item.WeaponComponent != null && item.Weapons.Count > 0)
		{
			int num = ((item.Weapons.Count > 1 && propertyBasedTooltipVM.IsExtended) ? 1 : 0);
			WeaponComponentData weaponComponentData = item.Weapons[num];
			propertyBasedTooltipVM.AddProperty(new TextObject("{=sqdzHOPe}Class").ToString(), GameTexts.FindText("str_inventory_weapon", weaponComponentData.WeaponClass.ToString()).ToString());
			if (Campaign.Current != null)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=hn9TPqhK}Weapon Tier").ToString(), ((int)(item.Tier + 1)).ToString());
			}
			ItemObject.ItemTypeEnum itemTypeFromWeaponClass = WeaponComponentData.GetItemTypeFromWeaponClass(weaponComponentData.WeaponClass);
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.OneHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.TwoHandedWeapon || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Polearm)
			{
				if (weaponComponentData.SwingDamageType != DamageTypes.Invalid)
				{
					propertyBasedTooltipVM.AddProperty(new TextObject("{=sVZaIPoQ}Swing Speed").ToString(), equipmentElement.Value.GetModifiedSwingSpeedForUsage(num).ToString());
					propertyBasedTooltipVM.AddProperty(new TextObject("{=QeToaiLt}Swing Damage").ToString(), equipmentElement.Value.GetModifiedSwingDamageForUsage(num).ToString());
				}
				if (weaponComponentData.ThrustDamageType != DamageTypes.Invalid)
				{
					propertyBasedTooltipVM.AddProperty(new TextObject("{=4uMWNDoi}Thrust Speed").ToString(), equipmentElement.Value.GetModifiedThrustSpeedForUsage(num).ToString());
					propertyBasedTooltipVM.AddProperty(new TextObject("{=dO95yR9b}Thrust Damage").ToString(), equipmentElement.Value.GetModifiedThrustDamageForUsage(num).ToString());
				}
				propertyBasedTooltipVM.AddProperty(new TextObject("{=ZcybPatO}Weapon Length").ToString(), weaponComponentData.WeaponLength.ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=oibdTnXP}Handling").ToString(), weaponComponentData.Handling.ToString());
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Thrown)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=ZcybPatO}Weapon Length").ToString(), weaponComponentData.WeaponLength.ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=s31DnnAf}Damage").ToString(), ItemHelper.GetMissileDamageText(weaponComponentData, equipmentElement.Value.ItemModifier).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=bAqDnkaT}Missile Speed").ToString(), equipmentElement.Value.GetModifiedMissileSpeedForUsage(num).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=TAnabTdy}Accuracy").ToString(), weaponComponentData.Accuracy.ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=twtbH1zv}Stack Amount").ToString(), equipmentElement.Value.GetModifiedStackCountForUsage(num).ToString());
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Shield)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=6GSXsdeX}Speed").ToString(), equipmentElement.Value.GetModifiedSwingSpeedForUsage(num).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=oBbiVeKE}Hit Points").ToString(), equipmentElement.Value.GetModifiedMaximumHitPointsForUsage(num).ToString());
			}
			if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Bow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow || itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Sling)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=6GSXsdeX}Speed").ToString(), equipmentElement.Value.GetModifiedSwingSpeedForUsage(num).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=s31DnnAf}Damage").ToString(), ItemHelper.GetThrustDamageText(weaponComponentData, equipmentElement.Value.ItemModifier).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=TAnabTdy}Accuracy").ToString(), weaponComponentData.Accuracy.ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=bAqDnkaT}Missile Speed").ToString(), equipmentElement.Value.GetModifiedMissileSpeedForUsage(num).ToString());
				if (itemTypeFromWeaponClass == ItemObject.ItemTypeEnum.Crossbow)
				{
					propertyBasedTooltipVM.AddProperty(new TextObject("{=cnmRwV4s}Ammo Limit").ToString(), weaponComponentData.MaxDataValue.ToString());
				}
			}
			if (weaponComponentData.IsAmmo)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=TAnabTdy}Accuracy").ToString(), weaponComponentData.Accuracy.ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=s31DnnAf}Damage").ToString(), ItemHelper.GetThrustDamageText(weaponComponentData, equipmentElement.Value.ItemModifier).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=twtbH1zv}Stack Amount").ToString(), equipmentElement.Value.GetModifiedStackCountForUsage(num).ToString());
			}
			if (item.Weapons.Any(delegate(WeaponComponentData x)
			{
				string weaponDescriptionId = x.WeaponDescriptionId;
				return weaponDescriptionId != null && weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0;
			}))
			{
				propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_inventory_flag_couchable").ToString());
			}
			if (item.Weapons.Any(delegate(WeaponComponentData x)
			{
				string weaponDescriptionId = x.WeaponDescriptionId;
				return weaponDescriptionId != null && weaponDescriptionId.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0;
			}))
			{
				propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_inventory_flag_braceable").ToString());
			}
		}
		else if (item.HasHorseComponent)
		{
			if (item.HorseComponent.IsMount)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=8BlMRMiR}Horse Tier").ToString(), ((int)(item.Tier + 1)).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=Mfbc4rQR}Charge Damage").ToString(), equipmentElement.Value.GetModifiedMountCharge(in EquipmentElement.Invalid).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=6GSXsdeX}Speed").ToString(), equipmentElement.Value.GetModifiedMountSpeed(in EquipmentElement.Invalid).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=rg7OuWS2}Maneuver").ToString(), equipmentElement.Value.GetModifiedMountManeuver(in EquipmentElement.Invalid).ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=oBbiVeKE}Hit Points").ToString(), equipmentElement.Value.GetModifiedMountHitPoints().ToString());
				propertyBasedTooltipVM.AddProperty(new TextObject("{=ZUgoQ1Ws}Horse Type").ToString(), item.ItemCategory.GetName().ToString());
			}
		}
		else if (item.HasFoodComponent)
		{
			if (item.FoodComponent.MoraleBonus > 0)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=myMbtwXi}Morale Bonus").ToString(), item.FoodComponent.MoraleBonus.ToString());
			}
			if (item.IsFood)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=qSi4DlT4}Food").ToString(), " ");
			}
		}
		if (item == null || !item.HasBannerComponent)
		{
			return;
		}
		TextObject textObject5;
		if (item?.BannerComponent?.BannerEffect != null)
		{
			GameTexts.SetVariable("RANK", item.BannerComponent.BannerEffect.Name);
			string content = string.Empty;
			if (item.BannerComponent.BannerEffect.IncrementType == EffectIncrementType.AddFactor)
			{
				TextObject textObject4 = GameTexts.FindText("str_NUMBER_percent");
				textObject4.SetTextVariable("NUMBER", ((int)Math.Abs(item.BannerComponent.GetBannerEffectBonus() * 100f)).ToString());
				content = textObject4.ToString();
			}
			else if (item.BannerComponent.BannerEffect.IncrementType == EffectIncrementType.Add)
			{
				content = item.BannerComponent.GetBannerEffectBonus().ToString();
			}
			GameTexts.SetVariable("NUMBER", content);
			textObject5 = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis");
		}
		else
		{
			textObject5 = new TextObject("{=koX9okuG}None");
		}
		propertyBasedTooltipVM.AddProperty(new TextObject("{=DbXZjPdf}Banner Effect: ").ToString(), textObject5.ToString());
	}

	public static void RefreshBuildingTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Building building = args[0] as Building;
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty("", building.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (building.BuildingType.IsDailyProject)
		{
			propertyBasedTooltipVM.AddProperty("", new TextObject("{=bd7oAQq6}Daily").ToString());
		}
		else
		{
			propertyBasedTooltipVM.AddProperty(new TextObject("{=IJdjwXvn}Current Level: ").ToString(), building.CurrentLevel.ToString());
		}
		propertyBasedTooltipVM.AddProperty("", building.Explanation.ToString(), 0, TooltipProperty.TooltipPropertyFlags.MultiLine);
		propertyBasedTooltipVM.AddProperty("", building.GetBonusExplanation().ToString());
	}

	public static void RefreshAnchorTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		AnchorPoint anchorPoint = args[0] as AnchorPoint;
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty("", anchorPoint.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
	}

	public static void RefreshWorkshopTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Workshop workshop = args[0] as Workshop;
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty("", workshop.WorkshopType.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=qRqnrtdX}Owner").ToString(), workshop.Owner.Name.ToString());
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=xtt9Oxer}Productions").ToString(), " ");
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		IEnumerable<(ItemCategory, int)> enumerable = workshop.WorkshopType.Productions.SelectMany((WorkshopType.Production p) => p.Inputs).Distinct(itemCategoryDistinctComparer);
		IEnumerable<(ItemCategory, int)> enumerable2 = workshop.WorkshopType.Productions.SelectMany((WorkshopType.Production p) => p.Outputs).Distinct(itemCategoryDistinctComparer);
		if (enumerable.Any())
		{
			propertyBasedTooltipVM.AddProperty(new TextObject("{=omXaC1am}Material").ToString(), " ");
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
			foreach (var item in enumerable)
			{
				propertyBasedTooltipVM.AddProperty(" ", item.Item1.GetName().ToString());
			}
			propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		}
		if (!enumerable2.Any())
		{
			return;
		}
		propertyBasedTooltipVM.AddProperty(new TextObject("{=bGyrPe8c}Production").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
		foreach (var item2 in enumerable2)
		{
			propertyBasedTooltipVM.AddProperty(" ", item2.Item1.GetName().ToString());
		}
	}

	public static void RefreshEncounterTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		int num = (int)args[0];
		List<MobileParty> list = new List<MobileParty> { MobileParty.MainParty };
		List<MobileParty> list2 = new List<MobileParty> { PlayerEncounter.EncounteredParty.MobileParty };
		PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
		List<MobileParty> parties = null;
		if (num == 0)
		{
			parties = list;
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			parties = list2;
			propertyBasedTooltipVM.Mode = 3;
		}
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		foreach (MobileParty item in parties)
		{
			for (int i = 0; i < item.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = item.MemberRoster.GetElementCopyAtIndex(i);
				troopRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
			}
			for (int j = 0; j < item.PrisonRoster.Count; j++)
			{
				TroopRosterElement elementCopyAtIndex2 = item.PrisonRoster.GetElementCopyAtIndex(j);
				troopRoster2.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber);
			}
		}
		Func<TroopRoster> funcToDoBeforeLambda = delegate
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			foreach (MobileParty item2 in parties)
			{
				for (int k = 0; k < item2.MemberRoster.Count; k++)
				{
					TroopRosterElement elementCopyAtIndex3 = item2.MemberRoster.GetElementCopyAtIndex(k);
					troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber);
				}
			}
			return troopRoster3;
		};
		Func<TroopRoster> funcToDoBeforeLambda2 = delegate
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			foreach (MobileParty item3 in parties)
			{
				for (int k = 0; k < item3.PrisonRoster.Count; k++)
				{
					TroopRosterElement elementCopyAtIndex3 = item3.PrisonRoster.GetElementCopyAtIndex(k);
					troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber);
				}
			}
			return troopRoster3;
		};
		bool flag = false;
		foreach (MobileParty item4 in parties)
		{
			flag = flag || item4.IsInspected;
			propertyBasedTooltipVM.AddProperty("", item4.Name.ToString(), 1);
			if (item4.Name.ToString() != item4.MapFaction?.Name.ToString())
			{
				propertyBasedTooltipVM.AddProperty("", item4.MapFaction?.Name.ToString() ?? "");
			}
		}
		if (troopRoster.Count > 0)
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster, GameTexts.FindText("str_map_tooltip_troops"), flag, funcToDoBeforeLambda);
		}
		if (troopRoster2.Count > 0)
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster2, GameTexts.FindText("str_map_tooltip_prisoners"), flag, funcToDoBeforeLambda2);
		}
		if (!Campaign.Current.IsMapTooltipLongForm && !propertyBasedTooltipVM.IsExtended)
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString());
		}
	}

	public static void RefreshSiegeEventTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		SiegeEvent siegeEvent = args[0] as SiegeEvent;
		if (!siegeEvent.ReadyToBeRemoved)
		{
			propertyBasedTooltipVM.Mode = 4;
			TooltipProperty.TooltipPropertyFlags tooltipPropertyFlags = TooltipProperty.TooltipPropertyFlags.None;
			TooltipProperty.TooltipPropertyFlags tooltipPropertyFlags2 = TooltipProperty.TooltipPropertyFlags.None;
			tooltipPropertyFlags = (FactionManager.IsAtWarAgainstFaction(siegeEvent.BesiegerCamp.MapFaction, PartyBase.MainParty.MapFaction) ? TooltipProperty.TooltipPropertyFlags.WarFirstEnemy : ((siegeEvent.BesiegerCamp.MapFaction != PartyBase.MainParty.MapFaction && !DiplomacyHelper.IsSameFactionAndNotEliminated(siegeEvent.BesiegerCamp.MapFaction, PartyBase.MainParty.MapFaction)) ? TooltipProperty.TooltipPropertyFlags.WarFirstNeutral : TooltipProperty.TooltipPropertyFlags.WarFirstAlly));
			tooltipPropertyFlags2 = (FactionManager.IsAtWarAgainstFaction(siegeEvent.BesiegedSettlement.MapFaction, PartyBase.MainParty.MapFaction) ? TooltipProperty.TooltipPropertyFlags.WarSecondEnemy : ((siegeEvent.BesiegedSettlement.MapFaction != PartyBase.MainParty.MapFaction && !DiplomacyHelper.IsSameFactionAndNotEliminated(siegeEvent.BesiegedSettlement.MapFaction, PartyBase.MainParty.MapFaction)) ? TooltipProperty.TooltipPropertyFlags.WarSecondNeutral : TooltipProperty.TooltipPropertyFlags.WarSecondAlly));
			propertyBasedTooltipVM.AddProperty("", "", 1, tooltipPropertyFlags | tooltipPropertyFlags2);
			if (siegeEvent.GetCurrentBattleType() == MapEvent.BattleTypes.Siege)
			{
				TextObject textObject = new TextObject("{=43HYUImy}{SETTLEMENT}'s Siege");
				textObject.SetTextVariable("SETTLEMENT", siegeEvent.BesiegedSettlement.Name);
				propertyBasedTooltipVM.AddProperty("", textObject.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
			}
			propertyBasedTooltipVM.AddProperty("", "", -1);
			MBList<PartyBase> parties = new MBReadOnlyList<PartyBase>(siegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType()).Where((PartyBase x) => !x.IsSettlement).ToMBList();
			MBList<PartyBase> parties2 = new MBReadOnlyList<PartyBase>(siegeEvent.BesiegedSettlement.GetInvolvedPartiesForEventType()).Where((PartyBase x) => !x.IsSettlement).ToMBList();
			AddEncounterParties(propertyBasedTooltipVM, parties, parties2, propertyBasedTooltipVM.IsExtended);
			if (!propertyBasedTooltipVM.IsExtended)
			{
				propertyBasedTooltipVM.AddProperty("", "", -1);
				GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
				propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString());
			}
		}
	}

	public static void RefreshMapEventTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		MapEvent mapEvent = args[0] as MapEvent;
		propertyBasedTooltipVM.Mode = 4;
		TooltipProperty.TooltipPropertyFlags tooltipPropertyFlags = TooltipProperty.TooltipPropertyFlags.None;
		TooltipProperty.TooltipPropertyFlags tooltipPropertyFlags2 = TooltipProperty.TooltipPropertyFlags.None;
		tooltipPropertyFlags = (FactionManager.IsAtWarAgainstFaction(mapEvent.AttackerSide.LeaderParty.MapFaction, PartyBase.MainParty.MapFaction) ? TooltipProperty.TooltipPropertyFlags.WarFirstEnemy : ((mapEvent.AttackerSide.LeaderParty.MapFaction != PartyBase.MainParty.MapFaction && !DiplomacyHelper.IsSameFactionAndNotEliminated(mapEvent.AttackerSide.LeaderParty.MapFaction, PartyBase.MainParty.MapFaction)) ? TooltipProperty.TooltipPropertyFlags.WarFirstNeutral : TooltipProperty.TooltipPropertyFlags.WarFirstAlly));
		tooltipPropertyFlags2 = (FactionManager.IsAtWarAgainstFaction(mapEvent.DefenderSide.LeaderParty.MapFaction, PartyBase.MainParty.MapFaction) ? TooltipProperty.TooltipPropertyFlags.WarSecondEnemy : ((mapEvent.DefenderSide.LeaderParty.MapFaction != PartyBase.MainParty.MapFaction && !DiplomacyHelper.IsSameFactionAndNotEliminated(mapEvent.DefenderSide.LeaderParty.MapFaction, PartyBase.MainParty.MapFaction)) ? TooltipProperty.TooltipPropertyFlags.WarSecondNeutral : TooltipProperty.TooltipPropertyFlags.WarSecondAlly));
		propertyBasedTooltipVM.AddProperty("", "", 1, tooltipPropertyFlags | tooltipPropertyFlags2);
		if (mapEvent.IsSiegeAssault)
		{
			TextObject textObject = new TextObject("{=43HYUImy}{SETTLEMENT}'s Siege");
			textObject.SetTextVariable("SETTLEMENT", mapEvent.MapEventSettlement.Name);
			propertyBasedTooltipVM.AddProperty("", textObject.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		else if (mapEvent.IsRaid)
		{
			TextObject textObject2 = new TextObject("{=T9bndUYP}{SETTLEMENT}'s Raid");
			textObject2.SetTextVariable("SETTLEMENT", mapEvent.MapEventSettlement.Name);
			propertyBasedTooltipVM.AddProperty("", textObject2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		else if (mapEvent.IsNavalMapEvent)
		{
			TextObject textObject3 = new TextObject("{=lr2UaD9m}Naval Battle");
			propertyBasedTooltipVM.AddProperty("", textObject3.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		else
		{
			TextObject textObject4 = new TextObject("{=CnsIzaWo}Field Battle");
			propertyBasedTooltipVM.AddProperty("", textObject4.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		propertyBasedTooltipVM.AddProperty("", "", -1);
		MBList<MapEventParty> parties = (from x in mapEvent.PartiesOnSide(BattleSideEnum.Attacker)
			where !x.Party.IsSettlement
			select x).ToMBList();
		MBList<MapEventParty> parties2 = (from x in mapEvent.PartiesOnSide(BattleSideEnum.Defender)
			where !x.Party.IsSettlement
			select x).ToMBList();
		AddEncounterParties(propertyBasedTooltipVM, parties, parties2, propertyBasedTooltipVM.IsExtended);
		if (!propertyBasedTooltipVM.IsExtended)
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString());
		}
	}

	public static void RefreshSettlementTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Settlement settlement = args[0] as Settlement;
		PartyBase settlementAsParty = settlement.Party;
		if (settlementAsParty == null)
		{
			return;
		}
		if (FactionManager.IsAtWarAgainstFaction(settlementAsParty.MapFaction, PartyBase.MainParty.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 3;
		}
		else if (settlementAsParty.MapFaction == PartyBase.MainParty.MapFaction || DiplomacyHelper.IsSameFactionAndNotEliminated(settlementAsParty.MapFaction, PartyBase.MainParty.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			propertyBasedTooltipVM.Mode = 1;
		}
		if (Game.Current.IsDevelopmentMode)
		{
			string text = settlement.Name.ToString();
			int upgradeLevel = 1;
			string text2 = "";
			if (settlement.IsHideout)
			{
				text2 = settlement.LocationComplex.GetScene("hideout_center", upgradeLevel);
				propertyBasedTooltipVM.AddProperty("", text + "( id: " + settlementAsParty.Id + ")\n(Scene: " + text2 + ")", 1);
			}
			else
			{
				if (settlement.IsFortification)
				{
					upgradeLevel = settlement.Town.GetWallLevel();
					text2 = settlement.LocationComplex.GetScene("center", upgradeLevel);
				}
				else if (settlement.IsVillage)
				{
					text2 = settlement.LocationComplex.GetScene("village_center", upgradeLevel);
				}
				propertyBasedTooltipVM.AddProperty("", text + " (" + text2 + ")", 0, TooltipProperty.TooltipPropertyFlags.Title);
			}
			if (settlement.IsFortification)
			{
				propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
				string text3 = "[DEBUG WALL DATA]\n";
				text3 = text3 + "Current wall level: " + settlement.Town.GetWallLevel() + "\n";
				text3 = text3 + "Current wall hp: " + settlement.SettlementTotalWallHitPoints + "\n";
				text3 = text3 + "Max wall hp: " + settlement.MaxWallHitPoints + "\n";
				propertyBasedTooltipVM.AddProperty("", text3, 0, TooltipProperty.TooltipPropertyFlags.Title);
			}
		}
		else
		{
			propertyBasedTooltipVM.AddProperty("", settlement.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		TextObject disableReason;
		bool flag = !CampaignUIHelper.IsSettlementInformationHidden(settlement, out disableReason);
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_owner").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		TextObject textObject = new TextObject("{=!}{PARTY_OWNERS_FACTION}");
		TextObject variable = ((settlement.OwnerClan == null) ? new TextObject("{=3PzgpFGq}Neutral") : settlement.OwnerClan.Name);
		textObject.SetTextVariable("PARTY_OWNERS_FACTION", variable);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_clan").ToString(), textObject.ToString());
		if (settlementAsParty.MapFaction != null)
		{
			TextObject textObject2 = new TextObject("{=!}{MAP_FACTION}");
			textObject2.SetTextVariable("MAP_FACTION", settlementAsParty.MapFaction?.Name ?? new TextObject("{=!}ERROR"));
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_faction").ToString(), textObject2.ToString());
		}
		if (settlement.Culture != null && !TextObject.IsNullOrEmpty(settlement.Culture.Name))
		{
			TextObject textObject3 = new TextObject("{=!}{CULTURE}");
			textObject3.SetTextVariable("CULTURE", settlement.Culture.Name);
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_culture").ToString(), textObject3.ToString());
		}
		if (flag)
		{
			if (settlementAsParty.IsSettlement && (settlementAsParty.Settlement.IsVillage || settlementAsParty.Settlement.IsTown || settlementAsParty.Settlement.IsCastle))
			{
				propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_information").ToString(), " ");
				propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
			}
			if (settlement.IsFortification)
			{
				int wallLevel = settlementAsParty.Settlement.Town.GetWallLevel();
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_wall_level").ToString(), wallLevel.ToString());
			}
			if (settlement.IsFortification)
			{
				Func<string> value = delegate
				{
					int num6 = (int)settlementAsParty.Settlement.Town.FoodChange;
					int variable2 = (int)settlementAsParty.Settlement.Town.FoodStocks;
					TextObject textObject6 = new TextObject("{=Jyfkahka}{VALUE} ({?POSITIVE}+{?}{\\?}{DELTA_VALUE})");
					textObject6.SetTextVariable("VALUE", variable2);
					textObject6.SetTextVariable("POSITIVE", (num6 > 0) ? 1 : 0);
					textObject6.SetTextVariable("DELTA_VALUE", num6);
					return textObject6.ToString();
				};
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_food_stocks").ToString(), value);
			}
			if (settlement.IsVillage || settlement.IsFortification)
			{
				Func<string> value2 = delegate
				{
					float num6 = float.Parse($"{(settlementAsParty.Settlement.IsFortification ? settlementAsParty.Settlement.Town.ProsperityChange : settlementAsParty.Settlement.Village.HearthChange):0.00}");
					int variable2 = (int)(settlementAsParty.Settlement.IsFortification ? settlementAsParty.Settlement.Town.Prosperity : settlementAsParty.Settlement.Village.Hearth);
					TextObject textObject6 = new TextObject("{=Jyfkahka}{VALUE} ({?POSITIVE}+{?}{\\?}{DELTA_VALUE})");
					textObject6.SetTextVariable("VALUE", variable2);
					textObject6.SetTextVariable("POSITIVE", (num6 > 0f) ? 1 : 0);
					textObject6.SetTextVariable("DELTA_VALUE", num6);
					return textObject6.ToString();
				};
				propertyBasedTooltipVM.AddProperty(settlementAsParty.Settlement.IsFortification ? GameTexts.FindText("str_map_tooltip_prosperity").ToString() : GameTexts.FindText("str_map_tooltip_hearths").ToString(), value2);
			}
			if (settlement.IsFortification)
			{
				Func<string> value3 = delegate
				{
					TextObject textObject6 = new TextObject("{=Jyfkahka}{VALUE} ({?POSITIVE}+{?}{\\?}{DELTA_VALUE})");
					textObject6.SetTextVariable("VALUE", settlement.Town.Loyalty);
					textObject6.SetTextVariable("POSITIVE", (settlement.Town.LoyaltyChange > 0f) ? 1 : 0);
					textObject6.SetTextVariable("DELTA_VALUE", settlement.Town.LoyaltyChange);
					return textObject6.ToString();
				};
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_loyalty").ToString(), value3);
				Func<string> value4 = delegate
				{
					TextObject textObject6 = new TextObject("{=Jyfkahka}{VALUE} ({?POSITIVE}+{?}{\\?}{DELTA_VALUE})");
					textObject6.SetTextVariable("VALUE", settlement.Town.Security);
					textObject6.SetTextVariable("POSITIVE", (settlement.Town.SecurityChange > 0f) ? 1 : 0);
					textObject6.SetTextVariable("DELTA_VALUE", settlement.Town.SecurityChange);
					return textObject6.ToString();
				};
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_security").ToString(), value4);
			}
		}
		if (settlement.IsVillage)
		{
			string definition = GameTexts.FindText("str_bound_settlement").ToString();
			string value5 = settlementAsParty.Settlement.Village.Bound.Name.ToString();
			propertyBasedTooltipVM.AddProperty(definition, value5);
			if (settlementAsParty.Settlement.Village.TradeBound != null)
			{
				string definition2 = GameTexts.FindText("str_trade_bound_settlement").ToString();
				string value6 = settlementAsParty.Settlement.Village.TradeBound.Name.ToString();
				propertyBasedTooltipVM.AddProperty(definition2, value6);
			}
			ItemObject primaryProduction = settlementAsParty.Settlement.Village.VillageType.PrimaryProduction;
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_primary_production").ToString(), primaryProduction.Name.ToString());
		}
		if (settlement.BoundVillages.Count > 0)
		{
			string definition3 = GameTexts.FindText("str_bound_village").ToString();
			IEnumerable<TextObject> texts = settlementAsParty.Settlement.BoundVillages.Select((Village x) => x.Name);
			propertyBasedTooltipVM.AddProperty(definition3, CampaignUIHelper.GetCommaNewlineSeparatedText(TextObject.GetEmpty(), texts).ToString());
			if (propertyBasedTooltipVM.IsExtended && settlement.IsTown && settlement.Town.TradeBoundVillages.Count > 0)
			{
				string definition4 = GameTexts.FindText("str_trade_bound_village").ToString();
				IEnumerable<TextObject> texts2 = settlement.Town.TradeBoundVillages.Select((Village x) => x.Name);
				propertyBasedTooltipVM.AddProperty(definition4, CampaignUIHelper.GetCommaNewlineSeparatedText(TextObject.GetEmpty(), texts2).ToString());
			}
		}
		if (Game.Current.IsDevelopmentMode && settlement.IsTown)
		{
			propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
			propertyBasedTooltipVM.AddProperty("[DEV] " + GameTexts.FindText("str_shops").ToString(), " ");
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
			int num = 1;
			Workshop[] workshops = settlementAsParty.Settlement.Town.Workshops;
			foreach (Workshop workshop in workshops)
			{
				if (workshop.WorkshopType != null)
				{
					propertyBasedTooltipVM.AddProperty("[DEV] Shop " + num, workshop.WorkshopType.Name.ToString());
					num++;
				}
			}
		}
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		TroopRoster.CreateDummyTroopRoster();
		Func<TroopRoster> func = delegate
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			foreach (MobileParty party in settlement.Parties)
			{
				if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, settlementAsParty.MapFaction) && (!(party.Aggressiveness < 0.01f) || party.IsGarrison || party.IsMilitia) && !party.IsMainParty)
				{
					for (int i = 0; i < party.MemberRoster.Count; i++)
					{
						TroopRosterElement elementCopyAtIndex = party.MemberRoster.GetElementCopyAtIndex(i);
						troopRoster3.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
					}
				}
			}
			return troopRoster3;
		};
		Func<TroopRoster> func2 = delegate
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			foreach (MobileParty party2 in settlement.Parties)
			{
				if (!party2.IsMainParty && !FactionManager.IsAtWarAgainstFaction(party2.MapFaction, settlementAsParty.MapFaction))
				{
					for (int i = 0; i < party2.PrisonRoster.Count; i++)
					{
						TroopRosterElement elementCopyAtIndex = party2.PrisonRoster.GetElementCopyAtIndex(i);
						troopRoster3.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
					}
				}
			}
			for (int j = 0; j < settlementAsParty.PrisonRoster.Count; j++)
			{
				TroopRosterElement elementCopyAtIndex2 = settlementAsParty.PrisonRoster.GetElementCopyAtIndex(j);
				troopRoster3.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber);
			}
			return troopRoster3;
		};
		troopRoster2 = func2();
		if (propertyBasedTooltipVM.IsExtended)
		{
			troopRoster = func();
			if (troopRoster.Count > 0)
			{
				AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster, GameTexts.FindText("str_map_tooltip_troops"), flag, func);
			}
		}
		else
		{
			propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
			if (!settlement.IsHideout && flag)
			{
				List<MobileParty> list = new List<MobileParty>();
				Town town = settlement.Town;
				bool flag2 = town == null || !town.InRebelliousState;
				for (int num3 = 0; num3 < settlement.Parties.Count; num3++)
				{
					MobileParty mobileParty = settlement.Parties[num3];
					bool flag3 = flag2 && mobileParty.IsMilitia;
					if (DiplomacyHelper.IsSameFactionAndNotEliminated(settlementAsParty.MapFaction, mobileParty.MapFaction) && (mobileParty.IsLordParty || flag3 || mobileParty.IsGarrison))
					{
						list.Add(mobileParty);
					}
				}
				list.Sort(CampaignUIHelper.MobilePartyPrecedenceComparerInstance);
				List<MobileParty> list2 = settlement.Parties.Where((MobileParty p) => !p.IsLordParty && !p.IsMilitia && !p.IsGarrison).ToList();
				list2.Sort(CampaignUIHelper.MobilePartyPrecedenceComparerInstance);
				if (list.Count > 0)
				{
					int num4 = list.Sum((MobileParty p) => p.Party.NumberOfHealthyMembers);
					int num5 = list.Sum((MobileParty p) => p.Party.NumberOfWoundedTotalMembers);
					string value7 = num4 + ((num5 > 0) ? ("+" + num5 + GameTexts.FindText("str_party_nameplate_wounded_abbr").ToString()) : "");
					propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_defenders").ToString(), value7);
					propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
					foreach (MobileParty item in list)
					{
						propertyBasedTooltipVM.AddProperty(item.Name.ToString(), CampaignUIHelper.GetPartyNameplateText(item, includeAttachedParties: false));
					}
					propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
				}
				if (list2.Count > 0)
				{
					propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
					foreach (MobileParty item2 in list2)
					{
						propertyBasedTooltipVM.AddProperty(item2.Name.ToString(), CampaignUIHelper.GetPartyNameplateText(item2, includeAttachedParties: false));
					}
				}
			}
			else
			{
				string value8 = GameTexts.FindText("str_missing_info_indicator").ToString();
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_parties").ToString(), value8);
			}
		}
		if (!settlement.IsHideout && troopRoster2.Count > 0 && flag)
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster2, GameTexts.FindText("str_map_tooltip_prisoners"), flag, func2);
		}
		if (settlement.IsFortification && settlement.Town.InRebelliousState)
		{
			propertyBasedTooltipVM.AddProperty(string.Empty, GameTexts.FindText("str_settlement_rebellious_state").ToString(), -1);
		}
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		if (!settlement.IsHideout && !propertyBasedTooltipVM.IsExtended && flag)
		{
			TextObject textObject4 = GameTexts.FindText("str_map_tooltip_info");
			textObject4.SetTextVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty(string.Empty, textObject4.ToString(), -1);
		}
		if (Campaign.Current.Models.EncounterModel.CanMainHeroDoParleyWithParty(settlementAsParty, out disableReason))
		{
			TextObject textObject5 = new TextObject("{=uEeLvYXT}Press '{MODIFIER_KEY}' + '{CLICK_KEY}' to parley.");
			textObject5.SetTextVariable("MODIFIER_KEY", propertyBasedTooltipVM.GetKeyText(FollowModifierKeyId));
			textObject5.SetTextVariable("CLICK_KEY", propertyBasedTooltipVM.GetKeyText(MapClickKeyId));
			propertyBasedTooltipVM.AddProperty(string.Empty, textObject5.ToString(), -1);
		}
	}

	public static void RefreshMobilePartyTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		MobileParty mobileParty = args[0] as MobileParty;
		bool flag = (bool)args[1];
		bool flag2 = (bool)args[2];
		if (mobileParty == null)
		{
			return;
		}
		if (FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, PartyBase.MainParty.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 3;
		}
		else if (mobileParty.MapFaction == PartyBase.MainParty.MapFaction || DiplomacyHelper.IsSameFactionAndNotEliminated(mobileParty.MapFaction, PartyBase.MainParty.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			propertyBasedTooltipVM.Mode = 1;
		}
		if (Game.Current.IsDevelopmentMode)
		{
			propertyBasedTooltipVM.AddProperty("", string.Concat(mobileParty.Name, " (", mobileParty.Id, ")"), 1, TooltipProperty.TooltipPropertyFlags.Title);
		}
		else
		{
			propertyBasedTooltipVM.AddProperty("", mobileParty.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		bool isInspected = mobileParty.IsInspected;
		if (mobileParty.IsDisorganized)
		{
			TextObject hoursAndDaysTextFromHourValue = CampaignUIHelper.GetHoursAndDaysTextFromHourValue(TaleWorlds.Library.MathF.Ceiling(mobileParty.DisorganizedUntilTime.RemainingHoursFromNow));
			TextObject textObject = new TextObject("{=BbLTwhsA}Disorganized for {REMAINING_TIME}");
			textObject.SetTextVariable("REMAINING_TIME", hoursAndDaysTextFromHourValue.ToString());
			propertyBasedTooltipVM.AddProperty("", textObject.ToString());
			propertyBasedTooltipVM.AddProperty("", "", -1);
		}
		if (isInspected)
		{
			propertyBasedTooltipVM.AddProperty("", CampaignUIHelper.GetMobilePartyBehaviorText(mobileParty));
		}
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_owner").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan != null && mobileParty.LeaderHero.Clan != mobileParty.MapFaction)
		{
			TextObject textObject2 = new TextObject("{=oUhd9YhP}{PARTY_LEADERS_FACTION}");
			textObject2.SetTextVariable("PARTY_LEADERS_FACTION", mobileParty.LeaderHero.Clan.Name);
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_clan").ToString(), textObject2.ToString());
		}
		if (mobileParty.MapFaction != null)
		{
			TextObject textObject3 = new TextObject("{=!}{MAP_FACTION}");
			textObject3.SetTextVariable("MAP_FACTION", mobileParty.MapFaction?.Name ?? new TextObject("{=!}ERROR"));
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_faction").ToString(), textObject3.ToString());
		}
		if (isInspected)
		{
			propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_information").ToString(), " ");
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_speed").ToString(), CampaignUIHelper.FloatToString(mobileParty.Speed));
			if (propertyBasedTooltipVM.IsExtended)
			{
				TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_terrain").ToString(), GameTexts.FindText("str_terrain_types", faceTerrainType.ToString()).ToString());
			}
		}
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		TroopRoster.CreateDummyTroopRoster();
		Func<TroopRoster> func = delegate
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			for (int i = 0; i < mobileParty.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(i);
				troopRoster3.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
			}
			return troopRoster3;
		};
		Func<TroopRoster> func2 = delegate
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			for (int i = 0; i < mobileParty.PrisonRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = mobileParty.PrisonRoster.GetElementCopyAtIndex(i);
				troopRoster3.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
			}
			return troopRoster3;
		};
		troopRoster = func();
		troopRoster2 = func2();
		if (troopRoster.Count > 0 && !mobileParty.IsInfoHidden)
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster, GameTexts.FindText("str_map_tooltip_troops"), flag || isInspected || !flag2, func);
		}
		if (troopRoster2.Count > 0 && !mobileParty.IsInfoHidden && (isInspected || flag))
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster2, GameTexts.FindText("str_map_tooltip_prisoners"), isInspected || !flag2, func2);
		}
		if (mobileParty.Ships.Count > 0 && !mobileParty.IsInfoHidden)
		{
			AddPartyShipProperties(propertyBasedTooltipVM, new MBList<MobileParty> { mobileParty }, flag, flag2);
		}
		if (!propertyBasedTooltipVM.IsExtended)
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString(), -1);
		}
	}

	public static void RefreshArmyTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Army army = args[0] as Army;
		bool flag = (bool)args[1];
		bool flag2 = (bool)args[2];
		MobileParty leaderParty = army.LeaderParty;
		if (leaderParty == null)
		{
			return;
		}
		if (FactionManager.IsAtWarAgainstFaction(leaderParty.MapFaction, PartyBase.MainParty.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 3;
		}
		else if (army.Kingdom == PartyBase.MainParty.MapFaction || DiplomacyHelper.IsSameFactionAndNotEliminated(leaderParty.MapFaction, PartyBase.MainParty.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			propertyBasedTooltipVM.Mode = 1;
		}
		if (Game.Current.IsDevelopmentMode)
		{
			propertyBasedTooltipVM.AddProperty("", string.Concat(army.Name, " (", army.LeaderParty.Id, ")"), 1, TooltipProperty.TooltipPropertyFlags.Title);
		}
		else
		{
			propertyBasedTooltipVM.AddProperty("", army.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		}
		if (leaderParty.IsInspected || !flag2)
		{
			propertyBasedTooltipVM.AddProperty("", CampaignUIHelper.GetMobilePartyBehaviorText(leaderParty));
		}
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_owner").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		if (army.Kingdom != null)
		{
			TextObject textObject = new TextObject("{=!}{MAP_FACTION}");
			textObject.SetTextVariable("MAP_FACTION", army.Kingdom?.Name ?? new TextObject("{=!}ERROR"));
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_faction").ToString(), textObject.ToString());
		}
		if (leaderParty.IsInspected || !flag2)
		{
			propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_information").ToString(), " ");
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_speed").ToString(), CampaignUIHelper.FloatToString(leaderParty.Speed));
			if (propertyBasedTooltipVM.IsExtended)
			{
				TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(leaderParty.CurrentNavigationFace);
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_terrain").ToString(), GameTexts.FindText("str_terrain_types", faceTerrainType.ToString()).ToString());
			}
		}
		TroopRoster troopRoster = GetTempRoster();
		TroopRoster troopRoster2 = GetTempPrisonerRoster();
		if (troopRoster.Count > 0)
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster, GameTexts.FindText("str_map_tooltip_troops"), flag || leaderParty.IsInspected, GetTempRoster);
		}
		if (troopRoster2.Count > 0 && (leaderParty.IsInspected || flag || !flag2))
		{
			AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster2, GameTexts.FindText("str_map_tooltip_prisoners"), leaderParty.IsInspected || !flag2, GetTempPrisonerRoster);
		}
		MBList<MobileParty> mBList = new MBList<MobileParty>();
		mBList.Add(leaderParty);
		mBList.AddRange(leaderParty.AttachedParties);
		if (mBList.Any((MobileParty x) => x.Ships.Count > 0))
		{
			AddPartyShipProperties(propertyBasedTooltipVM, mBList, flag, flag2);
		}
		if (!propertyBasedTooltipVM.IsExtended)
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString(), -1);
		}
		TroopRoster GetTempPrisonerRoster()
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			for (int i = 0; i < army.LeaderParty.PrisonRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = army.LeaderParty.PrisonRoster.GetElementCopyAtIndex(i);
				troopRoster3.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
			}
			foreach (MobileParty attachedParty in army.LeaderParty.AttachedParties)
			{
				for (int j = 0; j < attachedParty.PrisonRoster.Count; j++)
				{
					TroopRosterElement elementCopyAtIndex2 = attachedParty.PrisonRoster.GetElementCopyAtIndex(j);
					troopRoster3.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber);
				}
			}
			return troopRoster3;
		}
		TroopRoster GetTempRoster()
		{
			TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
			for (int i = 0; i < army.LeaderParty.MemberRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = army.LeaderParty.MemberRoster.GetElementCopyAtIndex(i);
				troopRoster3.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
			}
			foreach (MobileParty attachedParty2 in army.LeaderParty.AttachedParties)
			{
				for (int j = 0; j < attachedParty2.MemberRoster.Count; j++)
				{
					TroopRosterElement elementCopyAtIndex2 = attachedParty2.MemberRoster.GetElementCopyAtIndex(j);
					troopRoster3.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber);
				}
			}
			return troopRoster3;
		}
	}

	public static void RefreshClanTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Clan clan = args[0] as Clan;
		propertyBasedTooltipVM.AddProperty("", clan.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (FactionManager.IsAtWarAgainstFaction(clan.MapFaction, Hero.MainHero.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 3;
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_kingdom_at_war").ToString());
			propertyBasedTooltipVM.AddProperty("", "", -1);
		}
		else if (DiplomacyHelper.IsSameFactionAndNotEliminated(clan.MapFaction, Hero.MainHero.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			propertyBasedTooltipVM.Mode = 1;
		}
		if (clan.IsEliminated)
		{
			propertyBasedTooltipVM.AddProperty("", new TextObject("{=SlubkZ1A}Eliminated").ToString());
			return;
		}
		propertyBasedTooltipVM.AddProperty(new TextObject("{=SrfYbg3x}Leader").ToString(), clan.Leader.Name.ToString());
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=tTLvo8sM}Clan Tier").ToString(), clan.Tier.ToString());
		propertyBasedTooltipVM.AddProperty(new TextObject("{=ODEnkg0o}Clan Strength").ToString(), TaleWorlds.Library.MathF.Round(clan.CurrentTotalStrength).ToString());
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_wealth").ToString(), CampaignUIHelper.GetClanWealthStatusText(clan));
		int count = clan.Fiefs.Count;
		List<TextObject> list = new List<TextObject>();
		foreach (IFaction item in Campaign.Current.Factions.OrderBy((IFaction x) => !x.IsKingdomFaction).ThenBy((IFaction f) => f.Name.ToString()))
		{
			if (FactionManager.IsAtWarAgainstFaction(clan.MapFaction, item.MapFaction) && !item.MapFaction.IsBanditFaction && !list.Contains(item.MapFaction.Name))
			{
				list.Add(item.MapFaction.Name);
			}
		}
		if (propertyBasedTooltipVM.IsExtended)
		{
			if (count > 0)
			{
				propertyBasedTooltipVM.AddProperty("", "", -1);
				IEnumerable<TextObject> texts = from x in clan.Fiefs
					orderby x.IsCastle, s.IsTown
					select x.Name;
				propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_settlements").ToString(), CampaignUIHelper.GetCommaNewlineSeparatedText(TextObject.GetEmpty(), texts).ToString());
			}
			if (list.Count > 0)
			{
				propertyBasedTooltipVM.AddProperty("", "", -1);
				propertyBasedTooltipVM.AddProperty(new TextObject("{=zZlWRZjO}Wars").ToString(), CampaignUIHelper.GetCommaNewlineSeparatedText(TextObject.GetEmpty(), list).ToString());
			}
		}
		if (!propertyBasedTooltipVM.IsExtended && (count > 0 || list.Count > 0))
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString(), -1);
		}
	}

	public static void RefreshKingdomTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		Kingdom kingdom = args[0] as Kingdom;
		propertyBasedTooltipVM.AddProperty("", kingdom.Name.ToString(), 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (FactionManager.IsAtWarAgainstFaction(kingdom.MapFaction, Hero.MainHero.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 3;
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_kingdom_at_war").ToString());
			propertyBasedTooltipVM.AddProperty("", "", -1);
		}
		else if (DiplomacyHelper.IsSameFactionAndNotEliminated(kingdom.MapFaction, Hero.MainHero.MapFaction))
		{
			propertyBasedTooltipVM.Mode = 2;
		}
		else
		{
			propertyBasedTooltipVM.Mode = 1;
		}
		if (kingdom.IsEliminated)
		{
			propertyBasedTooltipVM.AddProperty("", new TextObject("{=SlubkZ1A}Eliminated").ToString());
			return;
		}
		propertyBasedTooltipVM.AddProperty(new TextObject("{=SrfYbg3x}Leader").ToString(), kingdom.Leader.Name.ToString());
		int count = kingdom.Clans.Count;
		List<TextObject> list = new List<TextObject>();
		foreach (IFaction item in Campaign.Current.Factions.OrderBy((IFaction x) => !x.IsKingdomFaction).ThenBy((IFaction f) => f.Name.ToString()))
		{
			if (FactionManager.IsAtWarAgainstFaction(kingdom.MapFaction, item.MapFaction) && !item.MapFaction.IsBanditFaction && !list.Contains(item.MapFaction.Name))
			{
				list.Add(item.MapFaction.Name);
			}
		}
		if (propertyBasedTooltipVM.IsExtended)
		{
			if (count > 0)
			{
				propertyBasedTooltipVM.AddProperty("", "", -1);
				IEnumerable<TextObject> texts = kingdom.Clans.Select((Clan x) => x.Name);
				propertyBasedTooltipVM.AddProperty(new TextObject("{=bfQLwMUp}Clans").ToString(), CampaignUIHelper.GetCommaNewlineSeparatedText(TextObject.GetEmpty(), texts).ToString());
			}
			if (list.Count > 0)
			{
				propertyBasedTooltipVM.AddProperty("", "", -1);
				propertyBasedTooltipVM.AddProperty(new TextObject("{=zZlWRZjO}Wars").ToString(), CampaignUIHelper.GetCommaNewlineSeparatedText(TextObject.GetEmpty(), list).ToString());
			}
		}
		if (!propertyBasedTooltipVM.IsExtended && (count > 0 || list.Count > 0))
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(ExtendKeyId));
			propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info").ToString(), -1);
		}
	}

	private static void AddEncounterParties(PropertyBasedTooltipVM propertyBasedTooltipVM, MBReadOnlyList<PartyBase> parties1, MBReadOnlyList<PartyBase> parties2, bool isExtended)
	{
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.BattleMode);
		for (int i = 0; i < parties1.Count || i < parties2.Count; i++)
		{
			MBTextManager.SetTextVariable("PARTY_1S_MEMBERS", "");
			MBTextManager.SetTextVariable("PARTY_2S_MEMBERS", "");
			if (i < parties1.Count)
			{
				MBTextManager.SetTextVariable("PARTY_1S_MEMBERS", parties1[i].Name);
			}
			if (i < parties2.Count)
			{
				MBTextManager.SetTextVariable("PARTY_2S_MEMBERS", parties2[i].Name);
			}
			propertyBasedTooltipVM.AddProperty(new TextObject("{=CExQ40Ux}{PARTY_1S_MEMBERS}   ").ToString(), new TextObject("{=OTaPfaJl}{PARTY_2S_MEMBERS}   ").ToString());
		}
		if (parties1.Count > 0 && parties2.Count > 0 && parties1[0]?.MapFaction != null && parties2[0]?.MapFaction != null)
		{
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
			MBTextManager.SetTextVariable("PARTY_1S_MEMBERS", parties1[0].MapFaction.Name);
			MBTextManager.SetTextVariable("PARTY_2S_MEMBERS", parties2[0].MapFaction.Name);
			propertyBasedTooltipVM.AddProperty(new TextObject("{=CExQ40Ux}{PARTY_1S_MEMBERS}   ").ToString(), new TextObject("{=OTaPfaJl}{PARTY_2S_MEMBERS}   ").ToString());
		}
		int lastHeroIndex = 0;
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (PartyBase item in parties1)
		{
			for (int j = 0; j < item.MemberRoster.Count; j++)
			{
				TroopRosterElement elementCopyAtIndex = item.MemberRoster.GetElementCopyAtIndex(j);
				if (elementCopyAtIndex.Character.IsHero)
				{
					troopRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
					lastHeroIndex++;
				}
				else
				{
					troopRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
				}
			}
		}
		lastHeroIndex = 0;
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		foreach (PartyBase item2 in parties2)
		{
			for (int k = 0; k < item2.MemberRoster.Count; k++)
			{
				TroopRosterElement elementCopyAtIndex2 = item2.MemberRoster.GetElementCopyAtIndex(k);
				if (elementCopyAtIndex2.Character.IsHero)
				{
					troopRoster2.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
					lastHeroIndex++;
				}
				else
				{
					troopRoster2.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber);
				}
			}
		}
		Func<string> func = () => "";
		Func<string> func2 = () => "";
		if (troopRoster.Count > 0)
		{
			func = delegate
			{
				TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
				lastHeroIndex = 0;
				foreach (PartyBase item3 in parties1)
				{
					for (int l = 0; l < item3.MemberRoster.Count; l++)
					{
						TroopRosterElement elementCopyAtIndex3 = item3.MemberRoster.GetElementCopyAtIndex(l);
						if (elementCopyAtIndex3.Character.IsHero)
						{
							troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
							lastHeroIndex++;
						}
						else
						{
							troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber);
						}
					}
				}
				TextObject textObject = new TextObject("{=QlbkxoSp} {TOOLTIP_TROOPS} ({PARTY_SIZE})");
				textObject.SetTextVariable("TOOLTIP_TROOPS", GameTexts.FindText("str_map_tooltip_troops"));
				textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(troopRoster3.TotalManCount - troopRoster3.TotalWounded, troopRoster3.TotalWounded, isInspected: true));
				return textObject.ToString();
			};
		}
		if (troopRoster2.Count > 0)
		{
			func2 = delegate
			{
				TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
				lastHeroIndex = 0;
				foreach (PartyBase item4 in parties2)
				{
					for (int l = 0; l < item4.MemberRoster.Count; l++)
					{
						TroopRosterElement elementCopyAtIndex3 = item4.MemberRoster.GetElementCopyAtIndex(l);
						if (elementCopyAtIndex3.Character.IsHero)
						{
							troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
							lastHeroIndex++;
						}
						else
						{
							troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber);
						}
					}
				}
				TextObject textObject = new TextObject("{=QlbkxoSp} {TOOLTIP_TROOPS} ({PARTY_SIZE})");
				textObject.SetTextVariable("TOOLTIP_TROOPS", GameTexts.FindText("str_map_tooltip_troops"));
				textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(troopRoster3.TotalManCount - troopRoster3.TotalWounded, troopRoster3.TotalWounded, isInspected: true));
				return textObject.ToString();
			};
		}
		if (func().Length != 0 && func2().Length != 0)
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			propertyBasedTooltipVM.AddProperty(func, func2);
		}
		if (isExtended)
		{
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
			for (int num = 0; num < troopRoster.Count || num < troopRoster2.Count; num++)
			{
				string blankString = new TextObject("{=!} ").ToString();
				Func<string> definition = () => blankString;
				Func<string> value = () => blankString;
				if (num < troopRoster.Count)
				{
					CharacterObject character = troopRoster.GetElementCopyAtIndex(num).Character;
					definition = delegate
					{
						lastHeroIndex = 0;
						foreach (PartyBase item5 in parties1)
						{
							for (int l = 0; l < item5.MemberRoster.Count; l++)
							{
								TroopRosterElement elementCopyAtIndex3 = item5.MemberRoster.GetElementCopyAtIndex(l);
								if (elementCopyAtIndex3.Character == character)
								{
									TextObject textObject;
									if (elementCopyAtIndex3.Character.IsHero)
									{
										textObject = new TextObject("{=W1tsTWZv} {PARTY_MEMBER.LINK} ({MEMBER_HEALTH}%)");
										textObject.SetTextVariable("MEMBER_HEALTH", elementCopyAtIndex3.Character.HeroObject.HitPoints * 100 / elementCopyAtIndex3.Character.MaxHitPoints());
									}
									else
									{
										textObject = new TextObject("{=vLaBJFGy} {PARTY_MEMBER.LINK} ({PARTY_SIZE})");
										textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(elementCopyAtIndex3.Number - elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.WoundedNumber, isInspected: true));
									}
									StringHelpers.SetCharacterProperties("PARTY_MEMBER", elementCopyAtIndex3.Character, textObject);
									return textObject.ToString();
								}
							}
						}
						return blankString;
					};
				}
				if (num < troopRoster2.Count)
				{
					CharacterObject character2 = troopRoster2.GetElementCopyAtIndex(num).Character;
					value = delegate
					{
						lastHeroIndex = 0;
						foreach (PartyBase item6 in parties2)
						{
							for (int l = 0; l < item6.MemberRoster.Count; l++)
							{
								TroopRosterElement elementCopyAtIndex3 = item6.MemberRoster.GetElementCopyAtIndex(l);
								if (character2 == elementCopyAtIndex3.Character)
								{
									TextObject textObject;
									if (character2.IsHero)
									{
										textObject = new TextObject("{=PS02CqPu} {PARTY_MEMBER.LINK} (Health: {MEMBER_HEALTH}%)");
										textObject.SetTextVariable("MEMBER_HEALTH", elementCopyAtIndex3.Character.HeroObject.HitPoints * 100 / elementCopyAtIndex3.Character.MaxHitPoints());
									}
									else
									{
										textObject = new TextObject("{=vLaBJFGy} {PARTY_MEMBER.LINK} ({PARTY_SIZE})");
										textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(elementCopyAtIndex3.Number - elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.WoundedNumber, isInspected: true));
									}
									StringHelpers.SetCharacterProperties("PARTY_MEMBER", elementCopyAtIndex3.Character, textObject);
									return textObject.ToString();
								}
							}
						}
						return blankString;
					};
				}
				propertyBasedTooltipVM.AddProperty(definition, value);
			}
		}
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.BattleModeOver);
	}

	private static void AddEncounterParties(PropertyBasedTooltipVM propertyBasedTooltipVM, MBReadOnlyList<MapEventParty> parties1, MBReadOnlyList<MapEventParty> parties2, bool isExtended)
	{
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.BattleMode);
		for (int i = 0; i < parties1.Count || i < parties2.Count; i++)
		{
			MBTextManager.SetTextVariable("PARTY_1S_MEMBERS", "");
			MBTextManager.SetTextVariable("PARTY_2S_MEMBERS", "");
			if (i < parties1.Count)
			{
				MBTextManager.SetTextVariable("PARTY_1S_MEMBERS", parties1[i].Party.Name);
			}
			if (i < parties2.Count)
			{
				MBTextManager.SetTextVariable("PARTY_2S_MEMBERS", parties2[i].Party.Name);
			}
			propertyBasedTooltipVM.AddProperty(new TextObject("{=CExQ40Ux}{PARTY_1S_MEMBERS}   ").ToString(), new TextObject("{=OTaPfaJl}{PARTY_2S_MEMBERS}   ").ToString());
		}
		if (parties1.Count > 0 && parties2.Count > 0 && parties1[0]?.Party?.MapFaction != null && parties2[0]?.Party?.MapFaction != null)
		{
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
			MBTextManager.SetTextVariable("PARTY_1S_MEMBERS", parties1[0].Party.MapFaction.Name);
			MBTextManager.SetTextVariable("PARTY_2S_MEMBERS", parties2[0].Party.MapFaction.Name);
			propertyBasedTooltipVM.AddProperty(new TextObject("{=CExQ40Ux}{PARTY_1S_MEMBERS}   ").ToString(), new TextObject("{=OTaPfaJl}{PARTY_2S_MEMBERS}   ").ToString());
		}
		bool isInfoHiddenLeft = false;
		bool isInfoHiddenRight = false;
		int lastHeroIndex = 0;
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		foreach (MapEventParty item in parties1)
		{
			MobileParty mobileParty = item.Party.MobileParty;
			if (mobileParty == null || !mobileParty.IsInfoHidden)
			{
				for (int j = 0; j < item.Party.MemberRoster.Count; j++)
				{
					TroopRosterElement elementCopyAtIndex = item.Party.MemberRoster.GetElementCopyAtIndex(j);
					if (elementCopyAtIndex.Character.IsHero)
					{
						troopRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
						lastHeroIndex++;
					}
					else
					{
						troopRoster.AddToCounts(elementCopyAtIndex.Character, elementCopyAtIndex.Number, insertAtFront: false, elementCopyAtIndex.WoundedNumber);
					}
				}
			}
			else
			{
				isInfoHiddenLeft = true;
			}
		}
		lastHeroIndex = 0;
		TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
		foreach (MapEventParty item2 in parties2)
		{
			MobileParty mobileParty2 = item2.Party.MobileParty;
			if (mobileParty2 == null || !mobileParty2.IsInfoHidden)
			{
				for (int k = 0; k < item2.Party.MemberRoster.Count; k++)
				{
					TroopRosterElement elementCopyAtIndex2 = item2.Party.MemberRoster.GetElementCopyAtIndex(k);
					if (elementCopyAtIndex2.Character.IsHero)
					{
						troopRoster2.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
						lastHeroIndex++;
					}
					else
					{
						troopRoster2.AddToCounts(elementCopyAtIndex2.Character, elementCopyAtIndex2.Number, insertAtFront: false, elementCopyAtIndex2.WoundedNumber);
					}
				}
			}
			else
			{
				isInfoHiddenRight = true;
			}
		}
		Func<string> func = () => "";
		Func<string> func2 = () => "";
		if (troopRoster.Count > 0)
		{
			func = delegate
			{
				TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
				lastHeroIndex = 0;
				foreach (MapEventParty item3 in parties1)
				{
					MobileParty mobileParty3 = item3.Party.MobileParty;
					if (mobileParty3 == null || !mobileParty3.IsInfoHidden)
					{
						for (int l = 0; l < item3.Party.MemberRoster.Count; l++)
						{
							TroopRosterElement elementCopyAtIndex3 = item3.Party.MemberRoster.GetElementCopyAtIndex(l);
							if (elementCopyAtIndex3.Character.IsHero)
							{
								troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
								lastHeroIndex++;
							}
							else
							{
								troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber);
							}
						}
					}
				}
				TextObject textObject = new TextObject("{=QlbkxoSp} {TOOLTIP_TROOPS} ({PARTY_SIZE})");
				if (isInfoHiddenLeft)
				{
					textObject = ((troopRoster3.TotalManCount <= 0) ? new TextObject("{=CLvFTdTn} {TOOLTIP_TROOPS} (?)") : new TextObject("{=ZGjnsItg} {TOOLTIP_TROOPS} ({PARTY_SIZE}) + ?"));
				}
				textObject.SetTextVariable("TOOLTIP_TROOPS", GameTexts.FindText("str_map_tooltip_troops"));
				textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(troopRoster3.TotalManCount - troopRoster3.TotalWounded, troopRoster3.TotalWounded, isInspected: true));
				return textObject.ToString();
			};
		}
		if (troopRoster2.Count > 0)
		{
			func2 = delegate
			{
				TroopRoster troopRoster3 = TroopRoster.CreateDummyTroopRoster();
				lastHeroIndex = 0;
				foreach (MapEventParty item4 in parties2)
				{
					MobileParty mobileParty3 = item4.Party.MobileParty;
					if (mobileParty3 == null || !mobileParty3.IsInfoHidden)
					{
						for (int l = 0; l < item4.Party.MemberRoster.Count; l++)
						{
							TroopRosterElement elementCopyAtIndex3 = item4.Party.MemberRoster.GetElementCopyAtIndex(l);
							if (elementCopyAtIndex3.Character.IsHero)
							{
								troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber, 0, removeDepleted: true, lastHeroIndex);
								lastHeroIndex++;
							}
							else
							{
								troopRoster3.AddToCounts(elementCopyAtIndex3.Character, elementCopyAtIndex3.Number, insertAtFront: false, elementCopyAtIndex3.WoundedNumber);
							}
						}
					}
				}
				TextObject textObject = new TextObject("{=QlbkxoSp} {TOOLTIP_TROOPS} ({PARTY_SIZE})");
				if (isInfoHiddenRight)
				{
					textObject = ((troopRoster3.TotalManCount <= 0) ? new TextObject("{=CLvFTdTn} {TOOLTIP_TROOPS} (?)") : new TextObject("{=ZGjnsItg} {TOOLTIP_TROOPS} ({PARTY_SIZE}) + ?"));
				}
				textObject.SetTextVariable("TOOLTIP_TROOPS", GameTexts.FindText("str_map_tooltip_troops"));
				textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(troopRoster3.TotalManCount - troopRoster3.TotalWounded, troopRoster3.TotalWounded, isInspected: true));
				return textObject.ToString();
			};
		}
		if (func().Length != 0 && func2().Length != 0)
		{
			propertyBasedTooltipVM.AddProperty("", "", -1);
			propertyBasedTooltipVM.AddProperty(func, func2);
		}
		if (isExtended)
		{
			string blankString = new TextObject("{=!} ").ToString();
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
			for (int num = 0; num < troopRoster.Count || num < troopRoster2.Count; num++)
			{
				Func<string> definition = () => blankString;
				Func<string> value = () => blankString;
				if (num < troopRoster.Count)
				{
					CharacterObject character = troopRoster.GetElementCopyAtIndex(num).Character;
					definition = delegate
					{
						lastHeroIndex = 0;
						foreach (MapEventParty item5 in parties1)
						{
							for (int l = 0; l < item5.Party.MemberRoster.Count; l++)
							{
								TroopRosterElement elementCopyAtIndex3 = item5.Party.MemberRoster.GetElementCopyAtIndex(l);
								if (elementCopyAtIndex3.Character == character)
								{
									TextObject textObject;
									if (elementCopyAtIndex3.Character.IsHero)
									{
										textObject = new TextObject("{=W1tsTWZv} {PARTY_MEMBER.LINK} ({MEMBER_HEALTH}%)");
										textObject.SetTextVariable("MEMBER_HEALTH", elementCopyAtIndex3.Character.HeroObject.HitPoints * 100 / elementCopyAtIndex3.Character.MaxHitPoints());
									}
									else
									{
										textObject = new TextObject("{=vLaBJFGy} {PARTY_MEMBER.LINK} ({PARTY_SIZE})");
										textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(elementCopyAtIndex3.Number - elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.WoundedNumber, isInspected: true));
									}
									StringHelpers.SetCharacterProperties("PARTY_MEMBER", elementCopyAtIndex3.Character, textObject);
									return textObject.ToString();
								}
							}
						}
						return blankString;
					};
				}
				else if (num == troopRoster.Count && isInfoHiddenLeft)
				{
					definition = () => "?";
				}
				if (num < troopRoster2.Count)
				{
					CharacterObject character2 = troopRoster2.GetElementCopyAtIndex(num).Character;
					value = delegate
					{
						lastHeroIndex = 0;
						foreach (MapEventParty item6 in parties2)
						{
							for (int l = 0; l < item6.Party.MemberRoster.Count; l++)
							{
								TroopRosterElement elementCopyAtIndex3 = item6.Party.MemberRoster.GetElementCopyAtIndex(l);
								if (character2 == elementCopyAtIndex3.Character)
								{
									TextObject textObject;
									if (character2.IsHero)
									{
										textObject = new TextObject("{=PS02CqPu} {PARTY_MEMBER.LINK} (Health: {MEMBER_HEALTH}%)");
										textObject.SetTextVariable("MEMBER_HEALTH", elementCopyAtIndex3.Character.HeroObject.HitPoints * 100 / elementCopyAtIndex3.Character.MaxHitPoints());
									}
									else
									{
										textObject = new TextObject("{=vLaBJFGy} {PARTY_MEMBER.LINK} ({PARTY_SIZE})");
										textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(elementCopyAtIndex3.Number - elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.WoundedNumber, isInspected: true));
									}
									StringHelpers.SetCharacterProperties("PARTY_MEMBER", elementCopyAtIndex3.Character, textObject);
									return textObject.ToString();
								}
							}
						}
						return blankString;
					};
				}
				else if (num == troopRoster2.Count && isInfoHiddenRight)
				{
					value = () => "?";
				}
				propertyBasedTooltipVM.AddProperty(definition, value);
			}
			if ((troopRoster.Count >= troopRoster2.Count && isInfoHiddenLeft) || (troopRoster2.Count >= troopRoster.Count && isInfoHiddenRight))
			{
				Func<string> definition2 = () => blankString;
				Func<string> value2 = () => blankString;
				if (troopRoster.Count >= troopRoster2.Count && isInfoHiddenLeft)
				{
					definition2 = () => "?";
				}
				if (troopRoster2.Count >= troopRoster.Count && isInfoHiddenRight)
				{
					value2 = () => "?";
				}
				propertyBasedTooltipVM.AddProperty(definition2, value2);
			}
		}
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.BattleModeOver);
	}

	private static void AddPartyTroopProperties(PropertyBasedTooltipVM propertyBasedTooltipVM, TroopRoster troopRoster, TextObject title, bool isInspected, Func<TroopRoster> funcToDoBeforeLambda = null)
	{
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(title.ToString(), delegate
		{
			TroopRoster troopRoster2 = ((funcToDoBeforeLambda != null) ? funcToDoBeforeLambda() : troopRoster);
			int num4 = 0;
			int num5 = 0;
			for (int i = 0; i < troopRoster2.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex3 = troopRoster2.GetElementCopyAtIndex(i);
				num4 += elementCopyAtIndex3.Number - elementCopyAtIndex3.WoundedNumber;
				num5 += elementCopyAtIndex3.WoundedNumber;
			}
			TextObject textObject3 = new TextObject("{=iXXTONWb} ({PARTY_SIZE})");
			textObject3.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(num4, num5, isInspected));
			return textObject3.ToString();
		});
		if (isInspected)
		{
			propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		}
		if (isInspected)
		{
			Dictionary<FormationClass, Tuple<int, int>> dictionary = new Dictionary<FormationClass, Tuple<int, int>>();
			for (int num = 0; num < troopRoster.Count; num++)
			{
				TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(num);
				if (dictionary.ContainsKey(elementCopyAtIndex.Character.DefaultFormationClass))
				{
					Tuple<int, int> tuple = dictionary[elementCopyAtIndex.Character.DefaultFormationClass];
					dictionary[elementCopyAtIndex.Character.DefaultFormationClass] = new Tuple<int, int>(tuple.Item1 + elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber, tuple.Item2 + elementCopyAtIndex.WoundedNumber);
				}
				else
				{
					dictionary.Add(elementCopyAtIndex.Character.DefaultFormationClass, new Tuple<int, int>(elementCopyAtIndex.Number - elementCopyAtIndex.WoundedNumber, elementCopyAtIndex.WoundedNumber));
				}
			}
			foreach (KeyValuePair<FormationClass, Tuple<int, int>> item in dictionary.OrderBy((KeyValuePair<FormationClass, Tuple<int, int>> x) => x.Key))
			{
				TextObject textObject = new TextObject("{=Dqydb21E} {PARTY_SIZE}");
				textObject.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(item.Value.Item1, item.Value.Item2, isInspected: true));
				TextObject textObject2 = GameTexts.FindText("str_troop_type_name", item.Key.GetName());
				propertyBasedTooltipVM.AddProperty(textObject2.ToString(), textObject.ToString());
			}
		}
		if (!(propertyBasedTooltipVM.IsExtended && isInspected))
		{
			return;
		}
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_troop_types").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
		for (int num2 = 0; num2 < troopRoster.Count; num2++)
		{
			TroopRosterElement elementCopyAtIndex2 = troopRoster.GetElementCopyAtIndex(num2);
			if (!elementCopyAtIndex2.Character.IsHero)
			{
				continue;
			}
			CharacterObject hero = elementCopyAtIndex2.Character;
			propertyBasedTooltipVM.AddProperty(elementCopyAtIndex2.Character.Name.ToString(), delegate
			{
				TroopRoster troopRoster2 = ((funcToDoBeforeLambda != null) ? funcToDoBeforeLambda() : troopRoster);
				int num4 = troopRoster2.FindIndexOfTroop(hero);
				if (num4 == -1)
				{
					return string.Empty;
				}
				TroopRosterElement elementCopyAtIndex3 = troopRoster2.GetElementCopyAtIndex(num4);
				TextObject textObject3 = GameTexts.FindText("str_NUMBER_percent");
				textObject3.SetTextVariable("NUMBER", elementCopyAtIndex3.Character.HeroObject.HitPoints * 100 / elementCopyAtIndex3.Character.MaxHitPoints());
				return textObject3.ToString();
			});
		}
		for (int num3 = 0; num3 < troopRoster.Count; num3++)
		{
			int index = num3;
			CharacterObject character = troopRoster.GetElementCopyAtIndex(index).Character;
			if (character.IsHero)
			{
				continue;
			}
			propertyBasedTooltipVM.AddProperty(character.Name.ToString(), delegate
			{
				TroopRoster troopRoster2 = ((funcToDoBeforeLambda != null) ? funcToDoBeforeLambda() : troopRoster);
				int num4 = troopRoster2.FindIndexOfTroop(character);
				if (num4 != -1)
				{
					if (num4 > troopRoster2.Count)
					{
						return string.Empty;
					}
					TroopRosterElement elementCopyAtIndex3 = troopRoster2.GetElementCopyAtIndex(num4);
					if (elementCopyAtIndex3.Character == null)
					{
						return string.Empty;
					}
					CharacterObject character2 = elementCopyAtIndex3.Character;
					if (character2 != null && !character2.IsHero)
					{
						TextObject textObject3 = new TextObject("{=!}{PARTY_SIZE}");
						textObject3.SetTextVariable("PARTY_SIZE", PartyBaseHelper.GetPartySizeText(elementCopyAtIndex3.Number - elementCopyAtIndex3.WoundedNumber, elementCopyAtIndex3.WoundedNumber, isInspected: true));
						return textObject3.ToString();
					}
				}
				return string.Empty;
			});
		}
	}

	private static void AddPartyShipProperties(PropertyBasedTooltipVM propertyBasedTooltipVM, MBList<MobileParty> parties, bool openedFromMenuLayout, bool checkForMapVisibility)
	{
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		Dictionary<ShipHull.ShipType, int> dictionary = new Dictionary<ShipHull.ShipType, int>();
		Dictionary<ShipHull, int> dictionary2 = new Dictionary<ShipHull, int>();
		int num = 0;
		bool flag = parties.Any((MobileParty x) => x.IsInspected);
		foreach (MobileParty party in parties)
		{
			if (party.Ships.Count <= 0)
			{
				continue;
			}
			num += party.Ships.Count;
			if (!(openedFromMenuLayout || flag) && checkForMapVisibility)
			{
				continue;
			}
			for (int num2 = 0; num2 < party.Ships.Count; num2++)
			{
				Ship ship = party.Ships[num2];
				ShipHull shipHull = ship.ShipHull;
				ShipHull.ShipType type = ship.ShipHull.Type;
				if (dictionary.ContainsKey(type))
				{
					dictionary[type]++;
				}
				else
				{
					dictionary[type] = 1;
				}
				if (dictionary2.ContainsKey(shipHull))
				{
					dictionary2[shipHull]++;
				}
				else
				{
					dictionary2[shipHull] = 1;
				}
			}
		}
		string shipSizeText = PartyBaseHelper.GetShipSizeText(num, openedFromMenuLayout || flag || !checkForMapVisibility);
		TextObject textObject = GameTexts.FindText("str_men_count_in_paranthesis_wo_wounded");
		textObject.SetTextVariable("NUMBER_OF_MEN", shipSizeText);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_map_tooltip_ships").ToString(), textObject.ToString());
		if (!(openedFromMenuLayout || flag) && checkForMapVisibility)
		{
			return;
		}
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
		foreach (KeyValuePair<ShipHull.ShipType, int> item in dictionary.OrderBy((KeyValuePair<ShipHull.ShipType, int> x) => x.Key))
		{
			TextObject textObject2 = new TextObject("{=Dqydb21E} {PARTY_SIZE}");
			textObject2.SetTextVariable("PARTY_SIZE", item.Value);
			TextObject textObject3 = GameTexts.FindText("str_ship_type", item.Key.ToString().ToLower());
			propertyBasedTooltipVM.AddProperty(textObject3.ToString(), textObject2.ToString());
		}
		if (!propertyBasedTooltipVM.IsExtended)
		{
			return;
		}
		propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
		propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_ship_types").ToString(), " ");
		propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
		foreach (KeyValuePair<ShipHull, int> item2 in dictionary2)
		{
			ShipHull key = item2.Key;
			int value = item2.Value;
			propertyBasedTooltipVM.AddProperty(key.Name.ToString(), value.ToString());
		}
	}

	public static void RefreshMapMarkerTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		MapMarker mapMarker = args[0] as MapMarker;
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty("", mapMarker.Name.ToString, 0, TooltipProperty.TooltipPropertyFlags.Title);
	}
}

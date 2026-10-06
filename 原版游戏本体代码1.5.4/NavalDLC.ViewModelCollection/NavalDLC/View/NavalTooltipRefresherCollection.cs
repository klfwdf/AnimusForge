using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.View;

public static class NavalTooltipRefresherCollection
{
	private static string ExtendKeyId = "ExtendModifier";

	private static string FollowModifierKeyId = "FollowModifier";

	private static string MapClickKeyId = "MapClick";

	public static void RefreshShipTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		if (args == null || args.Length == 0)
		{
			Debug.FailedAssert("Invalid ship arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshShipTooltip", 28);
			return;
		}
		if (!(args[0] is Ship ship))
		{
			Debug.FailedAssert("Invalid ship arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshShipTooltip", 34);
			return;
		}
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty(ship.Name.ToString(), string.Empty, 0, TooltipProperty.TooltipPropertyFlags.Title);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=wEmx6fZi}Hull").ToString(), ship.ShipHull.Name.ToString());
		if (ship.Owner != null)
		{
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_owner").ToString(), ship.Owner.Name.ToString());
		}
		propertyBasedTooltipVM.AddProperty(new TextObject("{=sqdzHOPe}Class").ToString(), GameTexts.FindText("str_ship_type", ship.ShipHull.Type.ToString().ToLowerInvariant()).ToString());
		string value = GameTexts.FindText("str_LEFT_over_RIGHT_no_space").SetTextVariable("LEFT", (int)ship.HitPoints).SetTextVariable("RIGHT", (int)ship.MaxHitPoints)
			.ToString();
		propertyBasedTooltipVM.AddProperty(new TextObject("{=UbZL2BJQ}Hitpoints").ToString(), value);
		int num = ship.TotalCrewCapacity - ship.MainDeckCrewCapacity;
		propertyBasedTooltipVM.AddProperty(value: (num <= 0) ? ship.TotalCrewCapacity.ToString() : new TextObject("{=r2fvxfwZ}{TOTAL} ({MAIN_DECK}+{RESERVE})").SetTextVariable("TOTAL", ship.TotalCrewCapacity.ToString()).SetTextVariable("MAIN_DECK", ship.MainDeckCrewCapacity.ToString()).SetTextVariable("RESERVE", num.ToString())
			.ToString(), definition: new TextObject("{=oqVVGxgb}Crew Capacity").ToString());
	}

	public static void RefreshShipHullTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		if (args == null || args.Length == 0)
		{
			Debug.FailedAssert("Invalid ship hull arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshShipHullTooltip", 74);
			return;
		}
		if (!(args[0] is ShipHull shipHull))
		{
			Debug.FailedAssert("Invalid ship hull arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshShipHullTooltip", 80);
			return;
		}
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty(shipHull.Name.ToString(), string.Empty, 0, TooltipProperty.TooltipPropertyFlags.Title);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=sqdzHOPe}Class").ToString(), GameTexts.FindText("str_ship_type", shipHull.Type.ToString().ToLowerInvariant()).ToString());
		propertyBasedTooltipVM.AddProperty(new TextObject("{=UbZL2BJQ}Hitpoints").ToString(), shipHull.MaxHitPoints.ToString());
		int num = shipHull.TotalCrewCapacity - shipHull.MainDeckCrewCapacity;
		propertyBasedTooltipVM.AddProperty(value: (num <= 0) ? shipHull.TotalCrewCapacity.ToString() : new TextObject("{=r2fvxfwZ}{TOTAL} ({MAIN_DECK}+{RESERVE})").SetTextVariable("TOTAL", shipHull.TotalCrewCapacity.ToString()).SetTextVariable("MAIN_DECK", shipHull.MainDeckCrewCapacity.ToString()).SetTextVariable("RESERVE", num.ToString())
			.ToString(), definition: new TextObject("{=oqVVGxgb}Crew Capacity").ToString());
	}

	public static void RefreshShipPieceTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		if (args == null || args.Length == 0)
		{
			Debug.FailedAssert("Invalid ship piece arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshShipPieceTooltip", 112);
			return;
		}
		if (!(args[0] is ShipUpgradePiece shipUpgradePiece))
		{
			Debug.FailedAssert("Invalid ship piece arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshShipPieceTooltip", 119);
			return;
		}
		bool flag = false;
		if (args.Length > 1 && args[1] is bool flag2)
		{
			flag = flag2;
		}
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty(shipUpgradePiece.GetName().ToString(), "", 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (flag)
		{
			if (shipUpgradePiece.RequiredCulture1 != null && shipUpgradePiece.RequiredCulture2 != null)
			{
				TextObject commaSeparatedText = CampaignUIHelper.GetCommaSeparatedText(null, new TextObject[2]
				{
					shipUpgradePiece.RequiredCulture1.Name,
					shipUpgradePiece.RequiredCulture2.Name
				});
				propertyBasedTooltipVM.AddProperty(new TextObject("{=n0R6yfth}Required Cultures").ToString(), commaSeparatedText.ToString());
			}
			else if (shipUpgradePiece.RequiredCulture1 != null || shipUpgradePiece.RequiredCulture2 != null)
			{
				BasicCultureObject basicCultureObject = shipUpgradePiece.RequiredCulture1 ?? shipUpgradePiece.RequiredCulture2;
				propertyBasedTooltipVM.AddProperty(new TextObject("{=11c9lb6E}Required Culture").ToString(), basicCultureObject.Name.ToString());
			}
			propertyBasedTooltipVM.AddProperty(new TextObject("{=gGWVrUPh}Required Port Level").ToString(), shipUpgradePiece.RequiredPortLevel.ToString());
			return;
		}
		TextObject textObject = GameTexts.FindText("str_plus_with_number");
		if (shipUpgradePiece.SeaWorthinessBonus != 0)
		{
			textObject.SetTextVariable("NUMBER", shipUpgradePiece.SeaWorthinessBonus);
			propertyBasedTooltipVM.AddProperty(new TextObject("{=cN03zpII}Seaworthiness").ToString(), textObject.ToString());
		}
		if (shipUpgradePiece.AdditionalAmmoBonus != 0)
		{
			textObject.SetTextVariable("NUMBER", shipUpgradePiece.AdditionalAmmoBonus);
			propertyBasedTooltipVM.AddProperty(new TextObject("{=pJz8SBGB}Additional Ammo Bonus").ToString(), textObject.ToString());
		}
		if (shipUpgradePiece.ArcherQuiverBonus != 0)
		{
			textObject.SetTextVariable("NUMBER", shipUpgradePiece.ArcherQuiverBonus);
			propertyBasedTooltipVM.AddProperty(new TextObject("{=EqJiCbQL}Quivers").ToString(), textObject.ToString());
		}
		if (shipUpgradePiece.ThrowingWeaponStackBonus != 0)
		{
			textObject.SetTextVariable("NUMBER", shipUpgradePiece.ThrowingWeaponStackBonus);
			propertyBasedTooltipVM.AddProperty(new TextObject("{=bbAzBnhC}Throwing Weapon Stacks").ToString(), textObject.ToString());
		}
		TextObject textObject2 = GameTexts.FindText("str_NUMBER_percent");
		if (shipUpgradePiece.CrewCapacityBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.CrewCapacityBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=oqVVGxgb}Crew Capacity").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.ShipWeightBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.ShipWeightBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=4Dd2xgPm}Weight").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.DecreaseForwardDragMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.DecreaseForwardDragMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=AOpCa0ZB}Top Speed").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.CampaignSpeedBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.CampaignSpeedBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=DbERaPfF}Travel Speed").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.MaxHitPointsBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.MaxHitPointsBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=lfEJZZfG}Ship Hitpoints").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.MaxSailHitPointsBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.MaxSailHitPointsBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=EAnQtOuG}Sail Hitpoints").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.CrewShieldHitPointsBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.CrewShieldHitPointsBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=4ZbgDw60}Crew Shield Hitpoints").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.InventoryCapacityBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.InventoryCapacityBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=IE1KbkaH}Cargo Capacity").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.MaxOarPowerBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.MaxOarPowerBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=VLugPMkM}Oar Speed").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.MaxOarForceBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.MaxOarForceBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=gOM8Eibs}Oar Power").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.SailForceBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.SailForceBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=ruAdMru6}Sail Power").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.CrewMeleeDamageBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.CrewMeleeDamageBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=vGqCgA6v}Crew Melee Damage").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.SailRotationSpeedBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.SailRotationSpeedBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=idjVMLKe}Sail Rotation Speed").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.RudderSurfaceAreaBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.RudderSurfaceAreaBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=b6dbh1uN}Rudder Effectiveness").ToString(), textObject2.ToString());
		}
		if (shipUpgradePiece.MaxRudderForceBonusMultiplier != 0f)
		{
			textObject2.SetTextVariable("NUMBER", (shipUpgradePiece.MaxRudderForceBonusMultiplier * 100f).ToString("#"));
			propertyBasedTooltipVM.AddProperty(new TextObject("{=djdlcniG}Rudder Power").ToString(), textObject2.ToString());
		}
	}

	public static void RefreshFigureheadTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		if (args == null || args.Length == 0 || !(args[0] is Figurehead figurehead))
		{
			Debug.FailedAssert("Invalid arguments for figurehead tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshFigureheadTooltip", 295);
			return;
		}
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty(figurehead.Name.ToString(), "", 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (figurehead.Culture != null)
		{
			propertyBasedTooltipVM.AddProperty(GameTexts.FindText("str_culture").ToString(), figurehead.Culture.Name.ToString());
		}
		StringHelpers.SetEffectIncrementTypeTextVariable("EFFECT_AMOUNT", figurehead.Description, figurehead.EffectAmount, figurehead.EffectIncrementType);
		propertyBasedTooltipVM.AddProperty(new TextObject("{=opVqBNLh}Effect").ToString(), figurehead.Description.ToString());
	}

	public static void RefreshAnchorPointTooltip(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
	{
		if (args == null || args.Length == 0 || !(args[0] is AnchorPoint anchorPoint))
		{
			Debug.FailedAssert("Invalid anchor arguments for tooltip", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshAnchorPointTooltip", 319);
			return;
		}
		if (!anchorPoint.IsValid)
		{
			Debug.FailedAssert("Anchor tooltip should not be visible when its not at a valid position", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.ViewModelCollection\\NavalTooltipRefresherCollection.cs", "RefreshAnchorPointTooltip", 325);
			return;
		}
		propertyBasedTooltipVM.Mode = 1;
		propertyBasedTooltipVM.AddProperty(anchorPoint.Name.ToString(), "", 0, TooltipProperty.TooltipPropertyFlags.Title);
		if (anchorPoint.IsMovingToPoint)
		{
			return;
		}
		MBReadOnlyList<Settlement> all = Settlement.All;
		Settlement settlement = null;
		for (int i = 0; i < all.Count; i++)
		{
			if (all[i].HasPort && anchorPoint.IsAtSettlement(all[i]))
			{
				settlement = all[i];
				break;
			}
		}
		if (settlement != null)
		{
			TextObject textObject = new TextObject("{=a6vEx1tM}Anchored at {SETTLEMENT}").SetTextVariable("SETTLEMENT", settlement.Name.ToString());
			propertyBasedTooltipVM.AddProperty("", textObject.ToString(), 0, TooltipProperty.TooltipPropertyFlags.MultiLine);
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
			Building building = settlement.Town?.GetShipyard();
			if (building != null)
			{
				propertyBasedTooltipVM.AddProperty(new TextObject("{=NfhYN9yt}Shipyard Level").ToString(), building.CurrentLevel.ToString());
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
					float num6 = (settlementAsParty.Settlement.IsFortification ? settlementAsParty.Settlement.Town.ProsperityChange : settlementAsParty.Settlement.Village.HearthChange);
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
		if (!settlement.IsHideout && propertyBasedTooltipVM.IsExtended)
		{
			troopRoster = func();
			if (troopRoster.Count > 0)
			{
				AddPartyTroopProperties(propertyBasedTooltipVM, troopRoster, GameTexts.FindText("str_map_tooltip_troops"), flag, func);
			}
		}
		else if (!settlement.IsHideout)
		{
			propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1);
			if (flag)
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
}

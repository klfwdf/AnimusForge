using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.SceneInformationPopupTypes
{
	// Token: 0x020000B1 RID: 177
	public static class CampaignSceneNotificationHelper
	{
		// Token: 0x06001388 RID: 5000 RVA: 0x0005A900 File Offset: 0x00058B00
		public static SceneNotificationData.SceneNotificationCharacter GetBodyguardOfCulture(CultureObject culture)
		{
			string stringId = culture.StringId;
			uint num = <PrivateImplementationDetails>.ComputeStringHash(stringId);
			string objectName;
			if (num <= 2848701557U)
			{
				if (num != 744444005U)
				{
					if (num != 1759932477U)
					{
						if (num == 2848701557U)
						{
							if (stringId == "khuzait")
							{
								objectName = "khuzait_khans_guard";
								goto IL_118;
							}
						}
					}
					else if (stringId == "battania")
					{
						objectName = "battanian_fian_champion";
						goto IL_118;
					}
				}
				else if (stringId == "empire")
				{
					objectName = "imperial_legionary";
					goto IL_118;
				}
			}
			else if (num <= 3015521580U)
			{
				if (num != 2894801972U)
				{
					if (num == 3015521580U)
					{
						if (stringId == "aserai")
						{
							objectName = "mamluke_palace_guard";
							goto IL_118;
						}
					}
				}
				else if (stringId == "nord")
				{
					objectName = "nord_huscarl";
					goto IL_118;
				}
			}
			else if (num != 3311783860U)
			{
				if (num == 4214512470U)
				{
					if (stringId == "vlandia")
					{
						objectName = "vlandian_banner_knight";
						goto IL_118;
					}
				}
			}
			else if (stringId == "sturgia")
			{
				objectName = "druzhinnik_champion";
				goto IL_118;
			}
			objectName = "fighter_sturgia";
			IL_118:
			return new SceneNotificationData.SceneNotificationCharacter(MBObjectManager.Instance.GetObject<CharacterObject>(objectName), null, default(BodyProperties), false, uint.MaxValue, uint.MaxValue, false);
		}

		// Token: 0x06001389 RID: 5001 RVA: 0x0005AA44 File Offset: 0x00058C44
		public static void RemoveWeaponsFromEquipment(ref Equipment equipment, bool removeHelmet = false, bool removeShoulder = false)
		{
			for (int i = 0; i < 5; i++)
			{
				equipment[i] = EquipmentElement.Invalid;
			}
			if (removeHelmet)
			{
				equipment[5] = EquipmentElement.Invalid;
			}
			if (removeShoulder)
			{
				equipment[9] = EquipmentElement.Invalid;
			}
		}

		// Token: 0x0600138A RID: 5002 RVA: 0x0005AA8C File Offset: 0x00058C8C
		public static string GetChildStageEquipmentIDFromCulture(CultureObject childCulture)
		{
			string stringId = childCulture.StringId;
			uint num = <PrivateImplementationDetails>.ComputeStringHash(stringId);
			if (num <= 2848701557U)
			{
				if (num != 744444005U)
				{
					if (num != 1759932477U)
					{
						if (num == 2848701557U)
						{
							if (stringId == "khuzait")
							{
								return "comingofage_kid_khu_cutscene_template";
							}
						}
					}
					else if (stringId == "battania")
					{
						return "comingofage_kid_bat_cutscene_template";
					}
				}
				else if (stringId == "empire")
				{
					return "comingofage_kid_emp_cutscene_template";
				}
			}
			else if (num <= 3015521580U)
			{
				if (num != 2894801972U)
				{
					if (num == 3015521580U)
					{
						if (stringId == "aserai")
						{
							return "comingofage_kid_ase_cutscene_template";
						}
					}
				}
				else if (stringId == "nord")
				{
					return "comingofage_kid_nord_cutscene_template";
				}
			}
			else if (num != 3311783860U)
			{
				if (num == 4214512470U)
				{
					if (stringId == "vlandia")
					{
						return "comingofage_kid_vla_cutscene_template";
					}
				}
			}
			else if (stringId == "sturgia")
			{
				return "comingofage_kid_stu_cutscene_template";
			}
			return "comingofage_kid_emp_cutscene_template";
		}

		// Token: 0x0600138B RID: 5003 RVA: 0x0005AB9C File Offset: 0x00058D9C
		public static CharacterObject GetRandomTroopForCulture(CultureObject culture)
		{
			string objectName = "imperial_recruit";
			if (culture != null)
			{
				List<CharacterObject> list = new List<CharacterObject>();
				if (culture.BasicTroop != null)
				{
					list.Add(culture.BasicTroop);
				}
				if (culture.EliteBasicTroop != null)
				{
					list.Add(culture.EliteBasicTroop);
				}
				if (culture.MeleeMilitiaTroop != null)
				{
					list.Add(culture.MeleeMilitiaTroop);
				}
				if (culture.MeleeEliteMilitiaTroop != null)
				{
					list.Add(culture.MeleeEliteMilitiaTroop);
				}
				if (culture.RangedMilitiaTroop != null)
				{
					list.Add(culture.RangedMilitiaTroop);
				}
				if (culture.RangedEliteMilitiaTroop != null)
				{
					list.Add(culture.RangedEliteMilitiaTroop);
				}
				if (list.Count > 0)
				{
					return list[MBRandom.RandomInt(list.Count)];
				}
			}
			return Game.Current.ObjectManager.GetObject<CharacterObject>(objectName);
		}

		// Token: 0x0600138C RID: 5004 RVA: 0x0005AC5E File Offset: 0x00058E5E
		public static IEnumerable<Hero> GetMilitaryAudienceForHero(Hero hero, bool includeClanLeader = true, bool onlyClanMembers = false)
		{
			if (hero.Clan != null)
			{
				if (includeClanLeader)
				{
					Hero leader = hero.Clan.Leader;
					if (leader != null && leader.IsAlive && hero != hero.Clan.Leader)
					{
						yield return hero.Clan.Leader;
					}
				}
				IOrderedEnumerable<Hero> orderedEnumerable = from h in hero.Clan.Heroes
					orderby h.Level
					select h;
				foreach (Hero hero2 in orderedEnumerable)
				{
					if (hero2 != hero.Clan.Leader && hero2.IsAlive && !hero2.IsChild && hero2 != hero)
					{
						yield return hero2;
					}
				}
				IEnumerator<Hero> enumerator = null;
			}
			if (!onlyClanMembers)
			{
				IOrderedEnumerable<Hero> orderedEnumerable2 = from h in Hero.AllAliveHeroes
					orderby CharacterRelationManager.GetHeroRelation(hero, h)
					select h;
				foreach (Hero hero3 in orderedEnumerable2)
				{
					bool flag = hero3 != null && hero3.Clan != hero.Clan;
					if (hero3.IsFriend(hero3) && hero3.IsLord && !hero3.IsChild && hero3 != hero && !flag)
					{
						yield return hero3;
					}
				}
				IEnumerator<Hero> enumerator = null;
			}
			yield break;
			yield break;
		}

		// Token: 0x0600138D RID: 5005 RVA: 0x0005AC7C File Offset: 0x00058E7C
		public static IEnumerable<Hero> GetMilitaryAudienceForKingdom(Kingdom kingdom, bool includeKingdomLeader = true)
		{
			if (includeKingdomLeader)
			{
				Hero leader = kingdom.Leader;
				if (leader != null && leader.IsAlive)
				{
					yield return kingdom.Leader;
				}
			}
			Hero leader2 = kingdom.Leader;
			IOrderedEnumerable<Hero> orderedEnumerable;
			if (leader2 == null)
			{
				orderedEnumerable = null;
			}
			else
			{
				orderedEnumerable = from h in leader2.Clan.Heroes.WhereQ((Hero h) => h != h.Clan.Kingdom.Leader)
					orderby h.GetRelationWithPlayer()
					select h;
			}
			IOrderedEnumerable<Hero> orderedEnumerable2 = orderedEnumerable;
			foreach (Hero hero in orderedEnumerable2)
			{
				if (!hero.IsChild && hero != Hero.MainHero && hero.IsAlive)
				{
					yield return hero;
				}
			}
			IEnumerator<Hero> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x0600138E RID: 5006 RVA: 0x0005AC94 File Offset: 0x00058E94
		public static TextObject GetFormalDayAndSeasonText(CampaignTime time)
		{
			TextObject textObject = new TextObject("{=CpsPq6WD}the {DAY_ORDINAL} day of {SEASON_NAME}", null);
			TextObject variable = GameTexts.FindText("str_season_" + time.GetSeasonOfYear, null);
			TextObject variable2 = GameTexts.FindText("str_ordinal_number", (time.GetDayOfSeason + 1).ToString());
			textObject.SetTextVariable("SEASON_NAME", variable);
			textObject.SetTextVariable("DAY_ORDINAL", variable2);
			return textObject;
		}

		// Token: 0x0600138F RID: 5007 RVA: 0x0005AD00 File Offset: 0x00058F00
		public static TextObject GetFormalNameForKingdom(Kingdom kingdom)
		{
			TextObject informalName;
			if (!GameTexts.TryGetText("str_kingdom_formal_name", out informalName, kingdom.StringId))
			{
				informalName = kingdom.InformalName;
			}
			return informalName;
		}

		// Token: 0x06001390 RID: 5008 RVA: 0x0005AD2C File Offset: 0x00058F2C
		public static SceneNotificationData.SceneNotificationCharacter CreateNotificationCharacterFromHero(Hero hero, Equipment overridenEquipment = null, bool useCivilian = false, BodyProperties overriddenBodyProperties = default(BodyProperties), uint overriddenColor1 = 4294967295U, uint overriddenColor2 = 4294967295U, bool useHorse = false)
		{
			if (overriddenColor1 == 4294967295U)
			{
				IFaction mapFaction = hero.MapFaction;
				overriddenColor1 = ((mapFaction != null) ? mapFaction.Color : hero.CharacterObject.Culture.Color);
			}
			if (overriddenColor2 == 4294967295U)
			{
				IFaction mapFaction2 = hero.MapFaction;
				overriddenColor2 = ((mapFaction2 != null) ? mapFaction2.Color2 : hero.CharacterObject.Culture.Color2);
			}
			if (overridenEquipment == null)
			{
				overridenEquipment = (useCivilian ? hero.CivilianEquipment : hero.BattleEquipment);
			}
			return new SceneNotificationData.SceneNotificationCharacter(hero.CharacterObject, overridenEquipment, overriddenBodyProperties, useCivilian, overriddenColor1, overriddenColor2, useHorse);
		}

		// Token: 0x06001391 RID: 5009 RVA: 0x0005ADB4 File Offset: 0x00058FB4
		public static SceneNotificationData.SceneNotificationShip CreateNotificationShipFromShip(Ship ship)
		{
			List<ShipVisualSlotInfo> shipVisualSlotInfos = ship.GetShipVisualSlotInfos();
			PartyBase owner = ship.Owner;
			uint? num;
			if (owner == null)
			{
				num = null;
			}
			else
			{
				IFaction mapFaction = owner.MapFaction;
				num = ((mapFaction != null) ? new uint?(mapFaction.Color) : null);
			}
			uint sailColor = num ?? uint.MaxValue;
			PartyBase owner2 = ship.Owner;
			uint? num2;
			if (owner2 == null)
			{
				num2 = null;
			}
			else
			{
				IFaction mapFaction2 = owner2.MapFaction;
				num2 = ((mapFaction2 != null) ? new uint?(mapFaction2.Color2) : null);
			}
			uint sailColor2 = num2 ?? uint.MaxValue;
			int randomValue = ship.RandomValue;
			float shipHitPointRatio = (ship.MaxHitPoints.ApproximatelyEqualsTo(0f, 1E-05f) ? 0f : (ship.HitPoints / ship.MaxHitPoints));
			MissionShipObject @object = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.ShipHull.MissionShipObjectId);
			if (@object != null)
			{
				Debug.FailedAssert(string.Format("Ship ({0}) does not have a valid mission ship object id", ship), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\SceneInformationPopupTypes\\CampaignSceneNotificationHelper.cs", "CreateNotificationShipFromShip", 261);
				return new SceneNotificationData.SceneNotificationShip("", shipVisualSlotInfos, shipHitPointRatio, sailColor, sailColor2, randomValue);
			}
			return new SceneNotificationData.SceneNotificationShip(@object.Prefab, shipVisualSlotInfos, shipHitPointRatio, sailColor, sailColor2, randomValue);
		}

		// Token: 0x06001392 RID: 5010 RVA: 0x0005AEF0 File Offset: 0x000590F0
		public static SceneNotificationData.SceneNotificationShip CreateNotificationShipFromShip(Ship ship, float hitPointRatio)
		{
			List<ShipVisualSlotInfo> shipVisualSlotInfos = ship.GetShipVisualSlotInfos();
			PartyBase owner = ship.Owner;
			uint? num;
			if (owner == null)
			{
				num = null;
			}
			else
			{
				IFaction mapFaction = owner.MapFaction;
				num = ((mapFaction != null) ? new uint?(mapFaction.Color) : null);
			}
			uint sailColor = num ?? uint.MaxValue;
			PartyBase owner2 = ship.Owner;
			uint? num2;
			if (owner2 == null)
			{
				num2 = null;
			}
			else
			{
				IFaction mapFaction2 = owner2.MapFaction;
				num2 = ((mapFaction2 != null) ? new uint?(mapFaction2.Color2) : null);
			}
			uint sailColor2 = num2 ?? uint.MaxValue;
			int randomValue = ship.RandomValue;
			hitPointRatio = MathF.Clamp(hitPointRatio, 0f, 1f);
			MissionShipObject @object = MBObjectManager.Instance.GetObject<MissionShipObject>(ship.ShipHull.MissionShipObjectId);
			if (@object != null)
			{
				Debug.FailedAssert(string.Format("Ship ({0}) does not have a valid mission ship object id", ship), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\SceneInformationPopupTypes\\CampaignSceneNotificationHelper.cs", "CreateNotificationShipFromShip", 279);
				return new SceneNotificationData.SceneNotificationShip("", shipVisualSlotInfos, hitPointRatio, sailColor, sailColor2, randomValue);
			}
			return new SceneNotificationData.SceneNotificationShip(@object.Prefab, shipVisualSlotInfos, hitPointRatio, sailColor, sailColor2, randomValue);
		}

		// Token: 0x06001393 RID: 5011 RVA: 0x0005B00E File Offset: 0x0005920E
		public static ItemObject GetDefaultHorseItem()
		{
			return Game.Current.ObjectManager.GetObjectTypeList<ItemObject>().First((ItemObject i) => i.ItemType == ItemObject.ItemTypeEnum.Horse && i.HasHorseComponent && i.HorseComponent.IsMount && i.HorseComponent.Monster.StringId == "horse");
		}
	}
}

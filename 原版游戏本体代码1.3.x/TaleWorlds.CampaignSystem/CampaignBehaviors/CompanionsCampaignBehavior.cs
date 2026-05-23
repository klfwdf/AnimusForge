using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003E0 RID: 992
	public class CompanionsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E09 RID: 3593
		// (get) Token: 0x06003CB1 RID: 15537 RVA: 0x00105ABC File Offset: 0x00103CBC
		private float _desiredTotalCompanionCount
		{
			get
			{
				return (float)Town.AllTowns.Count * 0.6f;
			}
		}

		// Token: 0x06003CB2 RID: 15538 RVA: 0x00105AD0 File Offset: 0x00103CD0
		public override void RegisterEvents()
		{
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.HeroOccupationChangedEvent.AddNonSerializedListener(this, new Action<Hero, Occupation>(this.OnHeroOccupationChanged));
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(this.WeeklyTick));
		}

		// Token: 0x06003CB3 RID: 15539 RVA: 0x00105B80 File Offset: 0x00103D80
		private void OnGameLoadFinished()
		{
			this.InitializeCompanionTemplateList();
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (hero.IsWanderer)
				{
					this.AddToAliveCompanions(hero, false);
				}
			}
			foreach (Hero hero2 in Hero.DeadOrDisabledHeroes)
			{
				if (hero2.IsAlive && hero2.IsWanderer)
				{
					this.AddToAliveCompanions(hero2, false);
				}
			}
		}

		// Token: 0x06003CB4 RID: 15540 RVA: 0x00105C34 File Offset: 0x00103E34
		private void DailyTick()
		{
			this.TryKillCompanion();
			this.SwapCompanions();
			this.TrySpawnNewCompanion();
		}

		// Token: 0x06003CB5 RID: 15541 RVA: 0x00105C48 File Offset: 0x00103E48
		private void WeeklyTick()
		{
			foreach (Hero hero in Hero.DeadOrDisabledHeroes.ToList<Hero>())
			{
				if (hero.IsWanderer && hero.DeathDay.ElapsedDaysUntilNow >= 40f)
				{
					Campaign.Current.CampaignObjectManager.UnregisterDeadHero(hero);
				}
			}
		}

		// Token: 0x06003CB6 RID: 15542 RVA: 0x00105CC8 File Offset: 0x00103EC8
		private void RemoveFromAliveCompanions(Hero companion)
		{
			CharacterObject template = companion.Template;
			if (this._aliveCompanionTemplates.Contains(template))
			{
				this._aliveCompanionTemplates.Remove(template);
			}
		}

		// Token: 0x06003CB7 RID: 15543 RVA: 0x00105CF8 File Offset: 0x00103EF8
		private void AddToAliveCompanions(Hero companion, bool isTemplateControlled = false)
		{
			CharacterObject template = companion.Template;
			bool flag = true;
			if (!isTemplateControlled)
			{
				flag = this.IsTemplateKnown(template);
			}
			if (flag && !this._aliveCompanionTemplates.Contains(template))
			{
				this._aliveCompanionTemplates.Add(template);
			}
		}

		// Token: 0x06003CB8 RID: 15544 RVA: 0x00105D37 File Offset: 0x00103F37
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			this.RemoveFromAliveCompanions(victim);
			if (victim.IsWanderer && !victim.HasMet)
			{
				Campaign.Current.CampaignObjectManager.UnregisterDeadHero(victim);
			}
		}

		// Token: 0x06003CB9 RID: 15545 RVA: 0x00105D60 File Offset: 0x00103F60
		private void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
		{
			if (oldOccupation == Occupation.Wanderer)
			{
				this.RemoveFromAliveCompanions(hero);
				return;
			}
			if (hero.Occupation == Occupation.Wanderer)
			{
				this.AddToAliveCompanions(hero, false);
			}
		}

		// Token: 0x06003CBA RID: 15546 RVA: 0x00105D81 File Offset: 0x00103F81
		private void OnHeroCreated(Hero hero, bool showNotification = true)
		{
			if (hero.IsAlive && hero.IsWanderer)
			{
				this.AddToAliveCompanions(hero, true);
			}
		}

		// Token: 0x06003CBB RID: 15547 RVA: 0x00105D9C File Offset: 0x00103F9C
		private void TryKillCompanion()
		{
			if (MBRandom.RandomFloat <= 0.1f && this._aliveCompanionTemplates.Count > 0)
			{
				CharacterObject randomElementInefficiently = this._aliveCompanionTemplates.GetRandomElementInefficiently<CharacterObject>();
				Hero hero = null;
				foreach (Hero hero2 in Hero.AllAliveHeroes)
				{
					if (hero2.Template == randomElementInefficiently && hero2.IsWanderer)
					{
						hero = hero2;
						break;
					}
				}
				if (hero != null && hero.CompanionOf == null && (hero.CurrentSettlement == null || hero.CurrentSettlement != Hero.MainHero.CurrentSettlement))
				{
					KillCharacterAction.ApplyByRemove(hero, false, true);
				}
			}
		}

		// Token: 0x06003CBC RID: 15548 RVA: 0x00105E54 File Offset: 0x00104054
		private void TrySpawnNewCompanion()
		{
			if ((float)this._aliveCompanionTemplates.Count < this._desiredTotalCompanionCount)
			{
				Town randomElementWithPredicate = Town.AllTowns.GetRandomElementWithPredicate(delegate(Town x)
				{
					if (x.Settlement != Hero.MainHero.CurrentSettlement && x.Settlement.SiegeEvent == null)
					{
						return x.Settlement.HeroesWithoutParty.AllQ((Hero y) => !y.IsWanderer || y.CompanionOf != null);
					}
					return false;
				});
				Settlement settlement = ((randomElementWithPredicate != null) ? randomElementWithPredicate.Settlement : null);
				if (settlement != null)
				{
					this.CreateCompanionAndAddToSettlement(settlement);
				}
			}
		}

		// Token: 0x06003CBD RID: 15549 RVA: 0x00105EB8 File Offset: 0x001040B8
		private void SwapCompanions()
		{
			int num = Town.AllTowns.Count / 2;
			int num2 = MBRandom.RandomInt(Town.AllTowns.Count % 2);
			Town town = Town.AllTowns[num2 + MBRandom.RandomInt(num)];
			Hero hero = (from x in town.Settlement.HeroesWithoutParty
				where x.IsWanderer && x.CompanionOf == null
				select x).GetRandomElementInefficiently<Hero>();
			for (int i = 1; i < 2; i++)
			{
				Town town2 = Town.AllTowns[i * num + num2 + MBRandom.RandomInt(num)];
				IEnumerable<Hero> enumerable = from x in town2.Settlement.HeroesWithoutParty
					where x.IsWanderer && x.CompanionOf == null
					select x;
				Hero hero2 = null;
				if (enumerable.Any<Hero>())
				{
					hero2 = enumerable.GetRandomElementInefficiently<Hero>();
					LeaveSettlementAction.ApplyForCharacterOnly(hero2);
				}
				if (hero != null)
				{
					EnterSettlementAction.ApplyForCharacterOnly(hero, town2.Settlement);
				}
				hero = hero2;
			}
			if (hero != null)
			{
				EnterSettlementAction.ApplyForCharacterOnly(hero, town.Settlement);
			}
		}

		// Token: 0x06003CBE RID: 15550 RVA: 0x00105FCB File Offset: 0x001041CB
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003CBF RID: 15551 RVA: 0x00105FD0 File Offset: 0x001041D0
		private void OnNewGameCreated(CampaignGameStarter starter)
		{
			this.InitializeCompanionTemplateList();
			List<Town> list = Town.AllTowns.ToListQ<Town>();
			list.Shuffle<Town>();
			int num = 0;
			while ((float)num < this._desiredTotalCompanionCount)
			{
				this.CreateCompanionAndAddToSettlement(list[num].Settlement);
				num++;
			}
		}

		// Token: 0x06003CC0 RID: 15552 RVA: 0x00106018 File Offset: 0x00104218
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this.InitializeCompanionTemplateList();
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (hero.IsWanderer)
				{
					this.AddToAliveCompanions(hero, false);
				}
			}
			foreach (Hero hero2 in Hero.DeadOrDisabledHeroes)
			{
				if (hero2.IsAlive && hero2.IsWanderer)
				{
					this.AddToAliveCompanions(hero2, false);
				}
			}
		}

		// Token: 0x06003CC1 RID: 15553 RVA: 0x001060CC File Offset: 0x001042CC
		private void AdjustEquipment(Hero hero)
		{
			this.AdjustEquipmentImp(hero.BattleEquipment);
			this.AdjustEquipmentImp(hero.CivilianEquipment);
		}

		// Token: 0x06003CC2 RID: 15554 RVA: 0x001060E8 File Offset: 0x001042E8
		private void AdjustEquipmentImp(Equipment equipment)
		{
			ItemModifier @object = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
			ItemModifier object2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
			ItemModifier object3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
			{
				EquipmentElement equipmentElement = equipment[equipmentIndex];
				if (equipmentElement.Item != null)
				{
					if (equipmentElement.Item.ArmorComponent != null)
					{
						equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, @object, null, false);
					}
					else if (equipmentElement.Item.HorseComponent != null)
					{
						equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object3, null, false);
					}
					else if (equipmentElement.Item.WeaponComponent != null)
					{
						equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, object2, null, false);
					}
				}
			}
		}

		// Token: 0x06003CC3 RID: 15555 RVA: 0x001061BC File Offset: 0x001043BC
		private void InitializeCompanionTemplateList()
		{
			foreach (CharacterObject characterObject in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
			{
				if (characterObject.IsTemplate && characterObject.Occupation == Occupation.Wanderer)
				{
					this._companionsOfTemplates[this.GetTemplateTypeOfCompanion(characterObject)].Add(characterObject);
				}
			}
		}

		// Token: 0x06003CC4 RID: 15556 RVA: 0x00106238 File Offset: 0x00104438
		private CompanionsCampaignBehavior.CompanionTemplateType GetTemplateTypeOfCompanion(CharacterObject character)
		{
			CompanionsCampaignBehavior.CompanionTemplateType result = CompanionsCampaignBehavior.CompanionTemplateType.Combat;
			int num = 20;
			foreach (SkillObject skill in Skills.All)
			{
				int skillValue = character.GetSkillValue(skill);
				if (skillValue > num)
				{
					CompanionsCampaignBehavior.CompanionTemplateType templateTypeForSkill = this.GetTemplateTypeForSkill(skill);
					if (templateTypeForSkill != CompanionsCampaignBehavior.CompanionTemplateType.Combat)
					{
						num = skillValue;
						result = templateTypeForSkill;
					}
				}
			}
			return result;
		}

		// Token: 0x06003CC5 RID: 15557 RVA: 0x001062B0 File Offset: 0x001044B0
		private void CreateCompanionAndAddToSettlement(Settlement settlement)
		{
			CharacterObject companionTemplate = this.GetCompanionTemplateToSpawn();
			if (companionTemplate != null)
			{
				Town randomElementWithPredicate = Town.AllTowns.GetRandomElementWithPredicate((Town x) => x.Culture == companionTemplate.Culture);
				Settlement settlement2 = ((randomElementWithPredicate != null) ? randomElementWithPredicate.Settlement : null);
				if (settlement2 == null)
				{
					settlement2 = Town.AllTowns.GetRandomElement<Town>().Settlement;
				}
				Hero hero = HeroCreator.CreateSpecialHero(companionTemplate, settlement2, null, null, Campaign.Current.Models.AgeModel.HeroComesOfAge + 5 + MBRandom.RandomInt(12));
				this.AdjustEquipment(hero);
				hero.ChangeState(Hero.CharacterStates.Active);
				EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
			}
		}

		// Token: 0x06003CC6 RID: 15558 RVA: 0x00106350 File Offset: 0x00104550
		private CompanionsCampaignBehavior.CompanionTemplateType GetCompanionTemplateTypeToSpawn()
		{
			CompanionsCampaignBehavior.CompanionTemplateType result = CompanionsCampaignBehavior.CompanionTemplateType.Combat;
			float num = -1f;
			foreach (KeyValuePair<CompanionsCampaignBehavior.CompanionTemplateType, List<CharacterObject>> keyValuePair in this._companionsOfTemplates)
			{
				float templateTypeScore = this.GetTemplateTypeScore(keyValuePair.Key);
				if (templateTypeScore > 0f)
				{
					int num2 = 0;
					foreach (CharacterObject item in keyValuePair.Value)
					{
						if (this._aliveCompanionTemplates.Contains(item))
						{
							num2++;
						}
					}
					float num3 = (float)num2 / this._desiredTotalCompanionCount;
					float num4 = (templateTypeScore - num3) / templateTypeScore;
					if (num2 < keyValuePair.Value.Count && num4 > num)
					{
						num = num4;
						result = keyValuePair.Key;
					}
				}
			}
			return result;
		}

		// Token: 0x06003CC7 RID: 15559 RVA: 0x00106450 File Offset: 0x00104650
		private bool IsTemplateKnown(CharacterObject companionTemplate)
		{
			foreach (KeyValuePair<CompanionsCampaignBehavior.CompanionTemplateType, List<CharacterObject>> keyValuePair in this._companionsOfTemplates)
			{
				for (int i = 0; i < keyValuePair.Value.Count; i++)
				{
					if (companionTemplate == keyValuePair.Value[i])
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06003CC8 RID: 15560 RVA: 0x001064C4 File Offset: 0x001046C4
		private CharacterObject GetCompanionTemplateToSpawn()
		{
			List<CharacterObject> list = this._companionsOfTemplates[this.GetCompanionTemplateTypeToSpawn()];
			list.Shuffle<CharacterObject>();
			CharacterObject result = null;
			foreach (CharacterObject characterObject in list)
			{
				if (!this._aliveCompanionTemplates.Contains(characterObject))
				{
					result = characterObject;
					break;
				}
			}
			return result;
		}

		// Token: 0x06003CC9 RID: 15561 RVA: 0x00106538 File Offset: 0x00104738
		private float GetTemplateTypeScore(CompanionsCampaignBehavior.CompanionTemplateType templateType)
		{
			switch (templateType)
			{
			case CompanionsCampaignBehavior.CompanionTemplateType.Engineering:
				return 0.05882353f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Tactics:
				return 0.11764706f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Leadership:
				return 0.0882353f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Steward:
				return 0.0882353f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Trade:
				return 0.0882353f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Roguery:
				return 0.11764706f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Medicine:
				return 0.0882353f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Smithing:
				return 0.05882353f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Scouting:
				return 0.14705883f;
			case CompanionsCampaignBehavior.CompanionTemplateType.Combat:
				return 0.14705883f;
			default:
				return 0f;
			}
		}

		// Token: 0x06003CCA RID: 15562 RVA: 0x001065B8 File Offset: 0x001047B8
		private CompanionsCampaignBehavior.CompanionTemplateType GetTemplateTypeForSkill(SkillObject skill)
		{
			CompanionsCampaignBehavior.CompanionTemplateType result = CompanionsCampaignBehavior.CompanionTemplateType.Combat;
			if (skill == DefaultSkills.Engineering)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Engineering;
			}
			else if (skill == DefaultSkills.Tactics)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Tactics;
			}
			else if (skill == DefaultSkills.Leadership)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Leadership;
			}
			else if (skill == DefaultSkills.Steward)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Steward;
			}
			else if (skill == DefaultSkills.Trade)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Trade;
			}
			else if (skill == DefaultSkills.Roguery)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Roguery;
			}
			else if (skill == DefaultSkills.Medicine)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Medicine;
			}
			else if (skill == DefaultSkills.Crafting)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Smithing;
			}
			else if (skill == DefaultSkills.Scouting)
			{
				result = CompanionsCampaignBehavior.CompanionTemplateType.Scouting;
			}
			return result;
		}

		// Token: 0x0400125E RID: 4702
		private const int CompanionMoveRandomIndex = 2;

		// Token: 0x0400125F RID: 4703
		private const float DesiredCompanionPerTown = 0.6f;

		// Token: 0x04001260 RID: 4704
		private const float KillChance = 0.1f;

		// Token: 0x04001261 RID: 4705
		private const int SkillThresholdValue = 20;

		// Token: 0x04001262 RID: 4706
		private const int RemoveWandererAfterDays = 40;

		// Token: 0x04001263 RID: 4707
		private IReadOnlyDictionary<CompanionsCampaignBehavior.CompanionTemplateType, List<CharacterObject>> _companionsOfTemplates = new Dictionary<CompanionsCampaignBehavior.CompanionTemplateType, List<CharacterObject>>
		{
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Engineering,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Tactics,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Leadership,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Steward,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Trade,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Roguery,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Medicine,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Smithing,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Scouting,
				new List<CharacterObject>()
			},
			{
				CompanionsCampaignBehavior.CompanionTemplateType.Combat,
				new List<CharacterObject>()
			}
		};

		// Token: 0x04001264 RID: 4708
		private HashSet<CharacterObject> _aliveCompanionTemplates = new HashSet<CharacterObject>();

		// Token: 0x04001265 RID: 4709
		private const float EngineerScore = 2f;

		// Token: 0x04001266 RID: 4710
		private const float TacticsScore = 4f;

		// Token: 0x04001267 RID: 4711
		private const float LeadershipScore = 3f;

		// Token: 0x04001268 RID: 4712
		private const float StewardScore = 3f;

		// Token: 0x04001269 RID: 4713
		private const float TradeScore = 3f;

		// Token: 0x0400126A RID: 4714
		private const float RogueryScore = 4f;

		// Token: 0x0400126B RID: 4715
		private const float MedicineScore = 3f;

		// Token: 0x0400126C RID: 4716
		private const float SmithingScore = 2f;

		// Token: 0x0400126D RID: 4717
		private const float ScoutingScore = 5f;

		// Token: 0x0400126E RID: 4718
		private const float CombatScore = 5f;

		// Token: 0x0400126F RID: 4719
		private const float AllScore = 34f;

		// Token: 0x020007CD RID: 1997
		private enum CompanionTemplateType
		{
			// Token: 0x04001F20 RID: 7968
			Engineering,
			// Token: 0x04001F21 RID: 7969
			Tactics,
			// Token: 0x04001F22 RID: 7970
			Leadership,
			// Token: 0x04001F23 RID: 7971
			Steward,
			// Token: 0x04001F24 RID: 7972
			Trade,
			// Token: 0x04001F25 RID: 7973
			Roguery,
			// Token: 0x04001F26 RID: 7974
			Medicine,
			// Token: 0x04001F27 RID: 7975
			Smithing,
			// Token: 0x04001F28 RID: 7976
			Scouting,
			// Token: 0x04001F29 RID: 7977
			Combat
		}
	}
}

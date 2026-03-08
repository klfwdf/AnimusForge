using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200041F RID: 1055
	public class OrderOfBattleCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060042C9 RID: 17097 RVA: 0x00142498 File Offset: 0x00140698
		public OrderOfBattleCampaignBehavior()
		{
			this._siegeFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			this._siegeArmyFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			this._fieldBattleFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			this._fieldBattleArmyFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
		}

		// Token: 0x060042CA RID: 17098 RVA: 0x001424CC File Offset: 0x001406CC
		public override void RegisterEvents()
		{
			CampaignEvents.OnHeroUnregisteredEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroUnregistered));
		}

		// Token: 0x060042CB RID: 17099 RVA: 0x001424E8 File Offset: 0x001406E8
		public override void SyncData(IDataStore dataStore)
		{
			if (dataStore.SyncData<List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>>("_siegeFormationInfos", ref this._siegeFormationInfos) && this._siegeFormationInfos == null)
			{
				this._siegeFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			}
			if (dataStore.SyncData<List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>>("_siegeArmyFormationInfos", ref this._siegeArmyFormationInfos) && this._siegeArmyFormationInfos == null)
			{
				this._siegeArmyFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			}
			if (dataStore.SyncData<List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>>("_formationInfos", ref this._fieldBattleFormationInfos) && this._fieldBattleFormationInfos == null)
			{
				this._fieldBattleFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			}
			if (dataStore.SyncData<List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>>("_fieldBattleArmyFormationInfos", ref this._fieldBattleArmyFormationInfos) && this._fieldBattleArmyFormationInfos == null)
			{
				this._fieldBattleArmyFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>();
			}
		}

		// Token: 0x060042CC RID: 17100 RVA: 0x00142590 File Offset: 0x00140790
		public OrderOfBattleCampaignBehavior.OrderOfBattleFormationData GetFormationDataAtIndex(int formationIndex, bool isSiegeBattle, bool isInArmy)
		{
			if (isSiegeBattle)
			{
				if (isInArmy)
				{
					if (this._siegeArmyFormationInfos.Count > formationIndex)
					{
						return this._siegeArmyFormationInfos[formationIndex];
					}
					return null;
				}
				else
				{
					if (this._siegeFormationInfos.Count > formationIndex)
					{
						return this._siegeFormationInfos[formationIndex];
					}
					return null;
				}
			}
			else if (isInArmy)
			{
				if (this._fieldBattleArmyFormationInfos.Count > formationIndex)
				{
					return this._fieldBattleArmyFormationInfos[formationIndex];
				}
				return null;
			}
			else
			{
				if (this._fieldBattleFormationInfos.Count > formationIndex)
				{
					return this._fieldBattleFormationInfos[formationIndex];
				}
				return null;
			}
		}

		// Token: 0x060042CD RID: 17101 RVA: 0x00142619 File Offset: 0x00140819
		public void SetFormationInfos(List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> formationInfos, bool isSiegeBattle, bool isInArmy)
		{
			if (isSiegeBattle)
			{
				if (isInArmy)
				{
					this._siegeArmyFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>(formationInfos);
					return;
				}
				this._siegeFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>(formationInfos);
				return;
			}
			else
			{
				if (isInArmy)
				{
					this._fieldBattleArmyFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>(formationInfos);
					return;
				}
				this._fieldBattleFormationInfos = new List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData>(formationInfos);
				return;
			}
		}

		// Token: 0x060042CE RID: 17102 RVA: 0x00142658 File Offset: 0x00140858
		private void OnHeroUnregistered(Hero hero)
		{
			int i = this._siegeFormationInfos.Count - 1;
			while (i >= 0)
			{
				OrderOfBattleCampaignBehavior.OrderOfBattleFormationData orderOfBattleFormationData = this._siegeFormationInfos[i];
				if (orderOfBattleFormationData.Captain == hero)
				{
					goto IL_3E;
				}
				Hero[] heroTroops = orderOfBattleFormationData.HeroTroops;
				if (heroTroops != null && heroTroops.Contains(hero))
				{
					goto IL_3E;
				}
				IL_90:
				i--;
				continue;
				IL_3E:
				List<Hero> list = orderOfBattleFormationData.HeroTroops.ToList<Hero>();
				list.Remove(hero);
				Hero captain = ((orderOfBattleFormationData.Captain == hero) ? null : orderOfBattleFormationData.Captain);
				this._siegeFormationInfos[i] = new OrderOfBattleCampaignBehavior.OrderOfBattleFormationData(captain, list, orderOfBattleFormationData.FormationClass, orderOfBattleFormationData.PrimaryClassWeight, orderOfBattleFormationData.SecondaryClassWeight, orderOfBattleFormationData.Filters);
				goto IL_90;
			}
			int j = this._fieldBattleFormationInfos.Count - 1;
			while (j >= 0)
			{
				OrderOfBattleCampaignBehavior.OrderOfBattleFormationData orderOfBattleFormationData2 = this._fieldBattleFormationInfos[j];
				if (orderOfBattleFormationData2.Captain == hero)
				{
					goto IL_DE;
				}
				Hero[] heroTroops2 = orderOfBattleFormationData2.HeroTroops;
				if (heroTroops2 != null && heroTroops2.Contains(hero))
				{
					goto IL_DE;
				}
				IL_13D:
				j--;
				continue;
				IL_DE:
				List<Hero> list2 = orderOfBattleFormationData2.HeroTroops.ToList<Hero>();
				list2.Remove(hero);
				Hero captain2 = ((orderOfBattleFormationData2.Captain == hero) ? null : orderOfBattleFormationData2.Captain);
				this._fieldBattleFormationInfos[j] = new OrderOfBattleCampaignBehavior.OrderOfBattleFormationData(captain2, list2, orderOfBattleFormationData2.FormationClass, orderOfBattleFormationData2.PrimaryClassWeight, orderOfBattleFormationData2.SecondaryClassWeight, orderOfBattleFormationData2.Filters);
				goto IL_13D;
			}
		}

		// Token: 0x0400130D RID: 4877
		private List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> _siegeFormationInfos;

		// Token: 0x0400130E RID: 4878
		private List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> _siegeArmyFormationInfos;

		// Token: 0x0400130F RID: 4879
		private List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> _fieldBattleFormationInfos;

		// Token: 0x04001310 RID: 4880
		private List<OrderOfBattleCampaignBehavior.OrderOfBattleFormationData> _fieldBattleArmyFormationInfos;

		// Token: 0x02000821 RID: 2081
		public class OrderOfBattleFormationData
		{
			// Token: 0x06006639 RID: 26169 RVA: 0x001C23D4 File Offset: 0x001C05D4
			public OrderOfBattleFormationData(Hero captain, List<Hero> heroTroops, DeploymentFormationClass formationClass, int primaryWeight, int secondaryWeight, Dictionary<FormationFilterType, bool> filters)
			{
				this.Captain = captain;
				this.HeroTroops = heroTroops.ToArray();
				this.FormationClass = formationClass;
				this.PrimaryClassWeight = primaryWeight;
				this.SecondaryClassWeight = secondaryWeight;
				this.Filters = new Dictionary<FormationFilterType, bool>();
				foreach (FormationFilterType key in filters.Keys)
				{
					this.Filters.Add(key, filters[key]);
				}
			}

			// Token: 0x0600663A RID: 26170 RVA: 0x001C2470 File Offset: 0x001C0670
			internal static void AutoGeneratedStaticCollectObjectsOrderOfBattleFormationData(object o, List<object> collectedObjects)
			{
				((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x0600663B RID: 26171 RVA: 0x001C247E File Offset: 0x001C067E
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this.Captain);
				collectedObjects.Add(this.Filters);
				collectedObjects.Add(this.HeroTroops);
			}

			// Token: 0x0600663C RID: 26172 RVA: 0x001C24A4 File Offset: 0x001C06A4
			internal static object AutoGeneratedGetMemberValueCaptain(object o)
			{
				return ((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).Captain;
			}

			// Token: 0x0600663D RID: 26173 RVA: 0x001C24B1 File Offset: 0x001C06B1
			internal static object AutoGeneratedGetMemberValueFormationClass(object o)
			{
				return ((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).FormationClass;
			}

			// Token: 0x0600663E RID: 26174 RVA: 0x001C24C3 File Offset: 0x001C06C3
			internal static object AutoGeneratedGetMemberValuePrimaryClassWeight(object o)
			{
				return ((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).PrimaryClassWeight;
			}

			// Token: 0x0600663F RID: 26175 RVA: 0x001C24D5 File Offset: 0x001C06D5
			internal static object AutoGeneratedGetMemberValueSecondaryClassWeight(object o)
			{
				return ((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).SecondaryClassWeight;
			}

			// Token: 0x06006640 RID: 26176 RVA: 0x001C24E7 File Offset: 0x001C06E7
			internal static object AutoGeneratedGetMemberValueFilters(object o)
			{
				return ((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).Filters;
			}

			// Token: 0x06006641 RID: 26177 RVA: 0x001C24F4 File Offset: 0x001C06F4
			internal static object AutoGeneratedGetMemberValueHeroTroops(object o)
			{
				return ((OrderOfBattleCampaignBehavior.OrderOfBattleFormationData)o).HeroTroops;
			}

			// Token: 0x04002291 RID: 8849
			[SaveableField(1)]
			public readonly Hero Captain;

			// Token: 0x04002292 RID: 8850
			[SaveableField(2)]
			public readonly DeploymentFormationClass FormationClass;

			// Token: 0x04002293 RID: 8851
			[SaveableField(3)]
			public readonly int PrimaryClassWeight;

			// Token: 0x04002294 RID: 8852
			[SaveableField(4)]
			public readonly int SecondaryClassWeight;

			// Token: 0x04002295 RID: 8853
			[SaveableField(5)]
			public readonly Dictionary<FormationFilterType, bool> Filters;

			// Token: 0x04002296 RID: 8854
			[SaveableField(6)]
			public readonly Hero[] HeroTroops;
		}
	}
}

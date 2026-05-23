using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200005C RID: 92
	public class CampaignObjectManager
	{
		// Token: 0x170001D1 RID: 465
		// (get) Token: 0x06000914 RID: 2324 RVA: 0x00027167 File Offset: 0x00025367
		// (set) Token: 0x06000915 RID: 2325 RVA: 0x0002716F File Offset: 0x0002536F
		[SaveableProperty(80)]
		public MBReadOnlyList<Settlement> Settlements { get; private set; }

		// Token: 0x170001D2 RID: 466
		// (get) Token: 0x06000916 RID: 2326 RVA: 0x00027178 File Offset: 0x00025378
		public MBReadOnlyList<MobileParty> MobileParties
		{
			get
			{
				return this._mobileParties;
			}
		}

		// Token: 0x170001D3 RID: 467
		// (get) Token: 0x06000917 RID: 2327 RVA: 0x00027180 File Offset: 0x00025380
		public MBReadOnlyList<MobileParty> CaravanParties
		{
			get
			{
				return this._caravanParties;
			}
		}

		// Token: 0x170001D4 RID: 468
		// (get) Token: 0x06000918 RID: 2328 RVA: 0x00027188 File Offset: 0x00025388
		public MBReadOnlyList<MobileParty> PatrolParties
		{
			get
			{
				return this._patrolParties;
			}
		}

		// Token: 0x170001D5 RID: 469
		// (get) Token: 0x06000919 RID: 2329 RVA: 0x00027190 File Offset: 0x00025390
		public MBReadOnlyList<MobileParty> MilitiaParties
		{
			get
			{
				return this._militiaParties;
			}
		}

		// Token: 0x170001D6 RID: 470
		// (get) Token: 0x0600091A RID: 2330 RVA: 0x00027198 File Offset: 0x00025398
		public MBReadOnlyList<MobileParty> GarrisonParties
		{
			get
			{
				return this._garrisonParties;
			}
		}

		// Token: 0x170001D7 RID: 471
		// (get) Token: 0x0600091B RID: 2331 RVA: 0x000271A0 File Offset: 0x000253A0
		public MBReadOnlyList<MobileParty> BanditParties
		{
			get
			{
				return this._banditParties;
			}
		}

		// Token: 0x170001D8 RID: 472
		// (get) Token: 0x0600091C RID: 2332 RVA: 0x000271A8 File Offset: 0x000253A8
		public MBReadOnlyList<MobileParty> VillagerParties
		{
			get
			{
				return this._villagerParties;
			}
		}

		// Token: 0x170001D9 RID: 473
		// (get) Token: 0x0600091D RID: 2333 RVA: 0x000271B0 File Offset: 0x000253B0
		public MBReadOnlyList<MobileParty> LordParties
		{
			get
			{
				return this._lordParties;
			}
		}

		// Token: 0x170001DA RID: 474
		// (get) Token: 0x0600091E RID: 2334 RVA: 0x000271B8 File Offset: 0x000253B8
		public MBReadOnlyList<MobileParty> CustomParties
		{
			get
			{
				return this._customParties;
			}
		}

		// Token: 0x170001DB RID: 475
		// (get) Token: 0x0600091F RID: 2335 RVA: 0x000271C0 File Offset: 0x000253C0
		public MBReadOnlyList<MobileParty> PartiesWithoutPartyComponent
		{
			get
			{
				return this._partiesWithoutPartyComponent;
			}
		}

		// Token: 0x170001DC RID: 476
		// (get) Token: 0x06000920 RID: 2336 RVA: 0x000271C8 File Offset: 0x000253C8
		public MBReadOnlyList<Hero> AliveHeroes
		{
			get
			{
				return this._aliveHeroes;
			}
		}

		// Token: 0x170001DD RID: 477
		// (get) Token: 0x06000921 RID: 2337 RVA: 0x000271D0 File Offset: 0x000253D0
		public MBReadOnlyList<Hero> DeadOrDisabledHeroes
		{
			get
			{
				return this._deadOrDisabledHeroes;
			}
		}

		// Token: 0x170001DE RID: 478
		// (get) Token: 0x06000922 RID: 2338 RVA: 0x000271D8 File Offset: 0x000253D8
		public MBReadOnlyList<Clan> Clans
		{
			get
			{
				return this._clans;
			}
		}

		// Token: 0x170001DF RID: 479
		// (get) Token: 0x06000923 RID: 2339 RVA: 0x000271E0 File Offset: 0x000253E0
		public MBReadOnlyList<Kingdom> Kingdoms
		{
			get
			{
				return this._kingdoms;
			}
		}

		// Token: 0x170001E0 RID: 480
		// (get) Token: 0x06000924 RID: 2340 RVA: 0x000271E8 File Offset: 0x000253E8
		public MBReadOnlyList<IFaction> Factions
		{
			get
			{
				return this._factions;
			}
		}

		// Token: 0x06000925 RID: 2341 RVA: 0x000271F0 File Offset: 0x000253F0
		public CampaignObjectManager()
		{
			this._objects = new CampaignObjectManager.ICampaignObjectType[5];
			this._mobileParties = new MBList<MobileParty>();
			this._caravanParties = new MBList<MobileParty>();
			this._patrolParties = new MBList<MobileParty>();
			this._militiaParties = new MBList<MobileParty>();
			this._garrisonParties = new MBList<MobileParty>();
			this._customParties = new MBList<MobileParty>();
			this._banditParties = new MBList<MobileParty>();
			this._villagerParties = new MBList<MobileParty>();
			this._lordParties = new MBList<MobileParty>();
			this._partiesWithoutPartyComponent = new MBList<MobileParty>();
			this._deadOrDisabledHeroes = new MBList<Hero>();
			this._aliveHeroes = new MBList<Hero>();
			this._clans = new MBList<Clan>();
			this._kingdoms = new MBList<Kingdom>();
			this._factions = new MBList<IFaction>();
		}

		// Token: 0x06000926 RID: 2342 RVA: 0x000272B4 File Offset: 0x000254B4
		private void InitializeManagerObjectLists()
		{
			this._objects[4] = new CampaignObjectManager.CampaignObjectType<MobileParty>(this._mobileParties);
			this._objects[0] = new CampaignObjectManager.CampaignObjectType<Hero>(this._deadOrDisabledHeroes);
			this._objects[1] = new CampaignObjectManager.CampaignObjectType<Hero>(this._aliveHeroes);
			this._objects[2] = new CampaignObjectManager.CampaignObjectType<Clan>(this._clans);
			this._objects[3] = new CampaignObjectManager.CampaignObjectType<Kingdom>(this._kingdoms);
			this._objectTypesAndNextIds = new Dictionary<Type, uint>();
			foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
			{
				uint maxObjectSubId = campaignObjectType.GetMaxObjectSubId();
				uint num;
				if (this._objectTypesAndNextIds.TryGetValue(campaignObjectType.ObjectClass, out num))
				{
					if (num <= maxObjectSubId)
					{
						this._objectTypesAndNextIds[campaignObjectType.ObjectClass] = maxObjectSubId + 1U;
					}
				}
				else
				{
					this._objectTypesAndNextIds.Add(campaignObjectType.ObjectClass, maxObjectSubId + 1U);
				}
			}
		}

		// Token: 0x06000927 RID: 2343 RVA: 0x00027390 File Offset: 0x00025590
		[LoadInitializationCallback]
		private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
		{
			this._objects = new CampaignObjectManager.ICampaignObjectType[5];
			this._factions = new MBList<IFaction>();
			this._caravanParties = new MBList<MobileParty>();
			this._patrolParties = new MBList<MobileParty>();
			this._militiaParties = new MBList<MobileParty>();
			this._garrisonParties = new MBList<MobileParty>();
			this._customParties = new MBList<MobileParty>();
			this._banditParties = new MBList<MobileParty>();
			this._villagerParties = new MBList<MobileParty>();
			this._lordParties = new MBList<MobileParty>();
			this._partiesWithoutPartyComponent = new MBList<MobileParty>();
		}

		// Token: 0x06000928 RID: 2344 RVA: 0x00027418 File Offset: 0x00025618
		internal void PreAfterLoad()
		{
			CampaignObjectManager.ICampaignObjectType[] objects = this._objects;
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].PreAfterLoad();
			}
		}

		// Token: 0x06000929 RID: 2345 RVA: 0x00027444 File Offset: 0x00025644
		internal void AfterLoad()
		{
			CampaignObjectManager.ICampaignObjectType[] objects = this._objects;
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].AfterLoad();
			}
		}

		// Token: 0x0600092A RID: 2346 RVA: 0x00027470 File Offset: 0x00025670
		internal void InitializeOnLoad()
		{
			this.Settlements = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
			foreach (Clan item in this._clans)
			{
				if (!this._factions.Contains(item))
				{
					this._factions.Add(item);
				}
			}
			foreach (Kingdom item2 in this._kingdoms)
			{
				if (!this._factions.Contains(item2))
				{
					this._factions.Add(item2);
				}
			}
			foreach (MobileParty mobileParty in this._mobileParties)
			{
				mobileParty.UpdatePartyComponentFlags();
				this.AddPartyToAppropriateList(mobileParty);
			}
			this.InitializeManagerObjectLists();
		}

		// Token: 0x0600092B RID: 2347 RVA: 0x00027590 File Offset: 0x00025790
		internal void InitializeOnNewGame()
		{
			List<Hero> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<Hero>();
			MBReadOnlyList<MobileParty> objectTypeList2 = MBObjectManager.Instance.GetObjectTypeList<MobileParty>();
			MBReadOnlyList<Clan> objectTypeList3 = MBObjectManager.Instance.GetObjectTypeList<Clan>();
			MBReadOnlyList<Kingdom> objectTypeList4 = MBObjectManager.Instance.GetObjectTypeList<Kingdom>();
			this.Settlements = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
			foreach (Hero hero in objectTypeList)
			{
				if (hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled)
				{
					if (!this._deadOrDisabledHeroes.Contains(hero))
					{
						this._deadOrDisabledHeroes.Add(hero);
					}
				}
				else if (!this._aliveHeroes.Contains(hero))
				{
					this._aliveHeroes.Add(hero);
				}
			}
			foreach (Clan item in objectTypeList3)
			{
				if (!this._clans.Contains(item))
				{
					this._clans.Add(item);
				}
				if (!this._factions.Contains(item))
				{
					this._factions.Add(item);
				}
			}
			foreach (Kingdom item2 in objectTypeList4)
			{
				if (!this._kingdoms.Contains(item2))
				{
					this._kingdoms.Add(item2);
				}
				if (!this._factions.Contains(item2))
				{
					this._factions.Add(item2);
				}
			}
			foreach (MobileParty mobileParty in objectTypeList2)
			{
				this._mobileParties.Add(mobileParty);
				this.AddPartyToAppropriateList(mobileParty);
			}
			this.InitializeManagerObjectLists();
			this.InitializeCachedData();
		}

		// Token: 0x0600092C RID: 2348 RVA: 0x0002779C File Offset: 0x0002599C
		private void InitializeCachedData()
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsVillage)
				{
					settlement.OwnerClan.OnBoundVillageAdded(settlement.Village);
				}
			}
		}

		// Token: 0x0600092D RID: 2349 RVA: 0x00027800 File Offset: 0x00025A00
		internal void AddMobileParty(MobileParty party)
		{
			party.Id = new MBGUID(14U, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<MobileParty>());
			this._mobileParties.Add(party);
			this.OnItemAdded<MobileParty>(CampaignObjectManager.CampaignObjects.MobileParty, party);
			this.AddPartyToAppropriateList(party);
		}

		// Token: 0x0600092E RID: 2350 RVA: 0x00027839 File Offset: 0x00025A39
		internal void RemoveMobileParty(MobileParty party)
		{
			this._mobileParties.Remove(party);
			this.OnItemRemoved<MobileParty>(CampaignObjectManager.CampaignObjects.MobileParty, party);
			this.RemovePartyFromAppropriateList(party);
		}

		// Token: 0x0600092F RID: 2351 RVA: 0x00027857 File Offset: 0x00025A57
		internal void BeforePartyComponentChanged(MobileParty party)
		{
			this.RemovePartyFromAppropriateList(party);
		}

		// Token: 0x06000930 RID: 2352 RVA: 0x00027860 File Offset: 0x00025A60
		internal void AfterPartyComponentChanged(MobileParty party)
		{
			this.AddPartyToAppropriateList(party);
		}

		// Token: 0x06000931 RID: 2353 RVA: 0x00027869 File Offset: 0x00025A69
		internal void AddHero(Hero hero)
		{
			hero.Id = new MBGUID(32U, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Hero>());
			this.OnHeroAdded(hero);
		}

		// Token: 0x06000932 RID: 2354 RVA: 0x0002788E File Offset: 0x00025A8E
		internal void UnregisterDeadHero(Hero hero)
		{
			this._deadOrDisabledHeroes.Remove(hero);
			this.OnItemRemoved<Hero>(CampaignObjectManager.CampaignObjects.DeadOrDisabledHeroes, hero);
			CampaignEventDispatcher.Instance.OnHeroUnregistered(hero);
		}

		// Token: 0x06000933 RID: 2355 RVA: 0x000278B0 File Offset: 0x00025AB0
		private void OnHeroAdded(Hero hero)
		{
			if (hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled)
			{
				this._deadOrDisabledHeroes.Add(hero);
				this.OnItemAdded<Hero>(CampaignObjectManager.CampaignObjects.DeadOrDisabledHeroes, hero);
				return;
			}
			this._aliveHeroes.Add(hero);
			this.OnItemAdded<Hero>(CampaignObjectManager.CampaignObjects.AliveHeroes, hero);
		}

		// Token: 0x06000934 RID: 2356 RVA: 0x000278F0 File Offset: 0x00025AF0
		internal void HeroStateChanged(Hero hero, Hero.CharacterStates oldState)
		{
			bool flag = oldState == Hero.CharacterStates.Dead || oldState == Hero.CharacterStates.Disabled;
			bool flag2 = hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled;
			if (flag != flag2)
			{
				if (flag2)
				{
					if (this._aliveHeroes.Contains(hero))
					{
						this._aliveHeroes.Remove(hero);
					}
				}
				else if (this._deadOrDisabledHeroes.Contains(hero))
				{
					this._deadOrDisabledHeroes.Remove(hero);
				}
				this.OnHeroAdded(hero);
			}
		}

		// Token: 0x06000935 RID: 2357 RVA: 0x00027963 File Offset: 0x00025B63
		internal void AddClan(Clan clan)
		{
			clan.Id = new MBGUID(18U, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Clan>());
			this._clans.Add(clan);
			this.OnItemAdded<Clan>(CampaignObjectManager.CampaignObjects.Clans, clan);
			this._factions.Add(clan);
		}

		// Token: 0x06000936 RID: 2358 RVA: 0x000279A1 File Offset: 0x00025BA1
		internal void RemoveClan(Clan clan)
		{
			if (this._clans.Contains(clan))
			{
				this._clans.Remove(clan);
				this.OnItemRemoved<Clan>(CampaignObjectManager.CampaignObjects.Clans, clan);
			}
			if (this._factions.Contains(clan))
			{
				this._factions.Remove(clan);
			}
		}

		// Token: 0x06000937 RID: 2359 RVA: 0x000279E1 File Offset: 0x00025BE1
		internal void AddKingdom(Kingdom kingdom)
		{
			kingdom.Id = new MBGUID(20U, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Kingdom>());
			this._kingdoms.Add(kingdom);
			this.OnItemAdded<Kingdom>(CampaignObjectManager.CampaignObjects.Kingdoms, kingdom);
			this._factions.Add(kingdom);
		}

		// Token: 0x06000938 RID: 2360 RVA: 0x00027A20 File Offset: 0x00025C20
		private void AddPartyToAppropriateList(MobileParty party)
		{
			if (party.IsBandit)
			{
				this._banditParties.Add(party);
				return;
			}
			if (party.IsCaravan)
			{
				this._caravanParties.Add(party);
				return;
			}
			if (party.IsPatrolParty)
			{
				this._patrolParties.Add(party);
				return;
			}
			if (party.IsLordParty)
			{
				this._lordParties.Add(party);
				return;
			}
			if (party.IsMilitia)
			{
				this._militiaParties.Add(party);
				return;
			}
			if (party.IsVillager)
			{
				this._villagerParties.Add(party);
				return;
			}
			if (party.IsCustomParty)
			{
				this._customParties.Add(party);
				return;
			}
			if (party.IsGarrison)
			{
				this._garrisonParties.Add(party);
				return;
			}
			this._partiesWithoutPartyComponent.Add(party);
		}

		// Token: 0x06000939 RID: 2361 RVA: 0x00027AE4 File Offset: 0x00025CE4
		private void RemovePartyFromAppropriateList(MobileParty party)
		{
			if (party.IsBandit)
			{
				this._banditParties.Remove(party);
				return;
			}
			if (party.IsCaravan)
			{
				this._caravanParties.Remove(party);
				return;
			}
			if (party.IsPatrolParty)
			{
				this._patrolParties.Remove(party);
				return;
			}
			if (party.IsLordParty)
			{
				this._lordParties.Remove(party);
				return;
			}
			if (party.IsMilitia)
			{
				this._militiaParties.Remove(party);
				return;
			}
			if (party.IsVillager)
			{
				this._villagerParties.Remove(party);
				return;
			}
			if (party.IsCustomParty)
			{
				this._customParties.Remove(party);
				return;
			}
			if (party.IsGarrison)
			{
				this._garrisonParties.Remove(party);
				return;
			}
			this._partiesWithoutPartyComponent.Remove(party);
		}

		// Token: 0x0600093A RID: 2362 RVA: 0x00027BB0 File Offset: 0x00025DB0
		private void OnItemAdded<T>(CampaignObjectManager.CampaignObjects targetList, T obj) where T : MBObjectBase
		{
			CampaignObjectManager.CampaignObjectType<T> campaignObjectType = (CampaignObjectManager.CampaignObjectType<T>)this._objects[(int)targetList];
			if (campaignObjectType != null)
			{
				campaignObjectType.OnItemAdded(obj);
			}
		}

		// Token: 0x0600093B RID: 2363 RVA: 0x00027BD8 File Offset: 0x00025DD8
		private void OnItemRemoved<T>(CampaignObjectManager.CampaignObjects targetList, T obj) where T : MBObjectBase
		{
			CampaignObjectManager.CampaignObjectType<T> campaignObjectType = (CampaignObjectManager.CampaignObjectType<T>)this._objects[(int)targetList];
			if (campaignObjectType != null)
			{
				campaignObjectType.UnregisterItem(obj);
			}
		}

		// Token: 0x0600093C RID: 2364 RVA: 0x00027C00 File Offset: 0x00025E00
		public T FindFirst<T>(Predicate<T> predicate) where T : MBObjectBase
		{
			foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
			{
				if (typeof(T) == campaignObjectType.ObjectClass)
				{
					T t = ((CampaignObjectManager.CampaignObjectType<T>)campaignObjectType).FindFirst(predicate);
					if (t != null)
					{
						return t;
					}
				}
			}
			return default(T);
		}

		// Token: 0x0600093D RID: 2365 RVA: 0x00027C60 File Offset: 0x00025E60
		public MBReadOnlyList<T> FindAll<T>(Predicate<T> predicate) where T : MBObjectBase
		{
			MBList<T> mblist = new MBList<T>();
			foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
			{
				if (typeof(T) == campaignObjectType.ObjectClass)
				{
					MBReadOnlyList<T> mbreadOnlyList = ((CampaignObjectManager.CampaignObjectType<T>)campaignObjectType).FindAll(predicate);
					if (mbreadOnlyList != null)
					{
						mblist.AddRange(mbreadOnlyList);
					}
				}
			}
			return mblist;
		}

		// Token: 0x0600093E RID: 2366 RVA: 0x00027CC0 File Offset: 0x00025EC0
		private uint GetNextUniqueObjectIdOfType<T>() where T : MBObjectBase
		{
			uint num;
			if (this._objectTypesAndNextIds.TryGetValue(typeof(T), out num))
			{
				this._objectTypesAndNextIds[typeof(T)] = num + 1U;
			}
			return num;
		}

		// Token: 0x0600093F RID: 2367 RVA: 0x00027D00 File Offset: 0x00025F00
		public T Find<T>(string id) where T : MBObjectBase
		{
			foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
			{
				if (campaignObjectType != null && typeof(T) == campaignObjectType.ObjectClass)
				{
					T t = ((CampaignObjectManager.CampaignObjectType<T>)campaignObjectType).Find(id);
					if (t != null)
					{
						return t;
					}
				}
			}
			return default(T);
		}

		// Token: 0x06000940 RID: 2368 RVA: 0x00027D64 File Offset: 0x00025F64
		public string FindNextUniqueStringId<T>(string id) where T : MBObjectBase
		{
			List<CampaignObjectManager.CampaignObjectType<T>> list = new List<CampaignObjectManager.CampaignObjectType<T>>();
			foreach (CampaignObjectManager.ICampaignObjectType campaignObjectType in this._objects)
			{
				if (campaignObjectType != null && typeof(T) == campaignObjectType.ObjectClass)
				{
					list.Add(campaignObjectType as CampaignObjectManager.CampaignObjectType<T>);
				}
			}
			return CampaignObjectManager.CampaignObjectType<T>.FindNextUniqueStringId(list, id);
		}

		// Token: 0x06000941 RID: 2369 RVA: 0x00027DBD File Offset: 0x00025FBD
		internal static void AutoGeneratedStaticCollectObjectsCampaignObjectManager(object o, List<object> collectedObjects)
		{
			((CampaignObjectManager)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x06000942 RID: 2370 RVA: 0x00027DCC File Offset: 0x00025FCC
		protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
			collectedObjects.Add(this._deadOrDisabledHeroes);
			collectedObjects.Add(this._aliveHeroes);
			collectedObjects.Add(this._clans);
			collectedObjects.Add(this._kingdoms);
			collectedObjects.Add(this._mobileParties);
			collectedObjects.Add(this.Settlements);
		}

		// Token: 0x06000943 RID: 2371 RVA: 0x00027E21 File Offset: 0x00026021
		internal static object AutoGeneratedGetMemberValueSettlements(object o)
		{
			return ((CampaignObjectManager)o).Settlements;
		}

		// Token: 0x06000944 RID: 2372 RVA: 0x00027E2E File Offset: 0x0002602E
		internal static object AutoGeneratedGetMemberValue_deadOrDisabledHeroes(object o)
		{
			return ((CampaignObjectManager)o)._deadOrDisabledHeroes;
		}

		// Token: 0x06000945 RID: 2373 RVA: 0x00027E3B File Offset: 0x0002603B
		internal static object AutoGeneratedGetMemberValue_aliveHeroes(object o)
		{
			return ((CampaignObjectManager)o)._aliveHeroes;
		}

		// Token: 0x06000946 RID: 2374 RVA: 0x00027E48 File Offset: 0x00026048
		internal static object AutoGeneratedGetMemberValue_clans(object o)
		{
			return ((CampaignObjectManager)o)._clans;
		}

		// Token: 0x06000947 RID: 2375 RVA: 0x00027E55 File Offset: 0x00026055
		internal static object AutoGeneratedGetMemberValue_kingdoms(object o)
		{
			return ((CampaignObjectManager)o)._kingdoms;
		}

		// Token: 0x06000948 RID: 2376 RVA: 0x00027E62 File Offset: 0x00026062
		internal static object AutoGeneratedGetMemberValue_mobileParties(object o)
		{
			return ((CampaignObjectManager)o)._mobileParties;
		}

		// Token: 0x040002C8 RID: 712
		internal const uint HeroObjectManagerTypeID = 32U;

		// Token: 0x040002C9 RID: 713
		internal const uint MobilePartyObjectManagerTypeID = 14U;

		// Token: 0x040002CA RID: 714
		internal const uint ClanObjectManagerTypeID = 18U;

		// Token: 0x040002CB RID: 715
		internal const uint KingdomObjectManagerTypeID = 20U;

		// Token: 0x040002CC RID: 716
		private CampaignObjectManager.ICampaignObjectType[] _objects;

		// Token: 0x040002CD RID: 717
		private Dictionary<Type, uint> _objectTypesAndNextIds;

		// Token: 0x040002CE RID: 718
		[SaveableField(20)]
		private readonly MBList<Hero> _deadOrDisabledHeroes;

		// Token: 0x040002CF RID: 719
		[SaveableField(30)]
		private readonly MBList<Hero> _aliveHeroes;

		// Token: 0x040002D0 RID: 720
		[SaveableField(40)]
		private readonly MBList<Clan> _clans;

		// Token: 0x040002D1 RID: 721
		[SaveableField(50)]
		private readonly MBList<Kingdom> _kingdoms;

		// Token: 0x040002D2 RID: 722
		private MBList<IFaction> _factions;

		// Token: 0x040002D3 RID: 723
		[SaveableField(71)]
		private MBList<MobileParty> _mobileParties;

		// Token: 0x040002D4 RID: 724
		private MBList<MobileParty> _caravanParties;

		// Token: 0x040002D5 RID: 725
		private MBList<MobileParty> _patrolParties;

		// Token: 0x040002D6 RID: 726
		private MBList<MobileParty> _militiaParties;

		// Token: 0x040002D7 RID: 727
		private MBList<MobileParty> _garrisonParties;

		// Token: 0x040002D8 RID: 728
		private MBList<MobileParty> _banditParties;

		// Token: 0x040002D9 RID: 729
		private MBList<MobileParty> _villagerParties;

		// Token: 0x040002DA RID: 730
		private MBList<MobileParty> _customParties;

		// Token: 0x040002DB RID: 731
		private MBList<MobileParty> _lordParties;

		// Token: 0x040002DC RID: 732
		private MBList<MobileParty> _partiesWithoutPartyComponent;

		// Token: 0x02000513 RID: 1299
		private interface ICampaignObjectType : IEnumerable
		{
			// Token: 0x17000EC1 RID: 3777
			// (get) Token: 0x06004B71 RID: 19313
			Type ObjectClass { get; }

			// Token: 0x06004B72 RID: 19314
			void PreAfterLoad();

			// Token: 0x06004B73 RID: 19315
			void AfterLoad();

			// Token: 0x06004B74 RID: 19316
			uint GetMaxObjectSubId();
		}

		// Token: 0x02000514 RID: 1300
		private class CampaignObjectType<T> : CampaignObjectManager.ICampaignObjectType, IEnumerable, IEnumerable<T> where T : MBObjectBase
		{
			// Token: 0x17000EC2 RID: 3778
			// (get) Token: 0x06004B75 RID: 19317 RVA: 0x00177926 File Offset: 0x00175B26
			// (set) Token: 0x06004B76 RID: 19318 RVA: 0x0017792E File Offset: 0x00175B2E
			public uint MaxCreatedPostfixIndex { get; private set; }

			// Token: 0x06004B77 RID: 19319 RVA: 0x00177938 File Offset: 0x00175B38
			public CampaignObjectType(IEnumerable<T> registeredObjects)
			{
				this._registeredObjects = registeredObjects;
				foreach (T t in this._registeredObjects)
				{
					ValueTuple<string, uint> idParts = CampaignObjectManager.CampaignObjectType<T>.GetIdParts(t.StringId);
					if (idParts.Item2 > this.MaxCreatedPostfixIndex)
					{
						this.MaxCreatedPostfixIndex = idParts.Item2;
					}
				}
			}

			// Token: 0x17000EC3 RID: 3779
			// (get) Token: 0x06004B78 RID: 19320 RVA: 0x001779B4 File Offset: 0x00175BB4
			Type CampaignObjectManager.ICampaignObjectType.ObjectClass
			{
				get
				{
					return typeof(T);
				}
			}

			// Token: 0x06004B79 RID: 19321 RVA: 0x001779C0 File Offset: 0x00175BC0
			public void PreAfterLoad()
			{
				foreach (T t in this._registeredObjects.ToList<T>())
				{
					t.PreAfterLoadInternal();
				}
			}

			// Token: 0x06004B7A RID: 19322 RVA: 0x00177A1C File Offset: 0x00175C1C
			public void AfterLoad()
			{
				foreach (T t in this._registeredObjects.ToList<T>())
				{
					t.IsReady = true;
					t.AfterLoadInternal();
				}
			}

			// Token: 0x06004B7B RID: 19323 RVA: 0x00177A84 File Offset: 0x00175C84
			IEnumerator<T> IEnumerable<!0>.GetEnumerator()
			{
				return this._registeredObjects.GetEnumerator();
			}

			// Token: 0x06004B7C RID: 19324 RVA: 0x00177A91 File Offset: 0x00175C91
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this._registeredObjects.GetEnumerator();
			}

			// Token: 0x06004B7D RID: 19325 RVA: 0x00177AA0 File Offset: 0x00175CA0
			public uint GetMaxObjectSubId()
			{
				uint num = 0U;
				foreach (T t in this._registeredObjects)
				{
					if (t.Id.SubId > num)
					{
						num = t.Id.SubId;
					}
				}
				return num;
			}

			// Token: 0x06004B7E RID: 19326 RVA: 0x00177B14 File Offset: 0x00175D14
			public void OnItemAdded(T item)
			{
				ValueTuple<string, uint> idParts = CampaignObjectManager.CampaignObjectType<T>.GetIdParts(item.StringId);
				if (idParts.Item2 > this.MaxCreatedPostfixIndex)
				{
					this.MaxCreatedPostfixIndex = idParts.Item2;
				}
				this.RegisterItem(item);
			}

			// Token: 0x06004B7F RID: 19327 RVA: 0x00177B53 File Offset: 0x00175D53
			private void RegisterItem(T item)
			{
				item.IsReady = true;
			}

			// Token: 0x06004B80 RID: 19328 RVA: 0x00177B61 File Offset: 0x00175D61
			public void UnregisterItem(T item)
			{
				item.IsReady = false;
			}

			// Token: 0x06004B81 RID: 19329 RVA: 0x00177B70 File Offset: 0x00175D70
			public T Find(string id)
			{
				foreach (T t in this._registeredObjects)
				{
					if (t.StringId == id)
					{
						return t;
					}
				}
				return default(T);
			}

			// Token: 0x06004B82 RID: 19330 RVA: 0x00177BD8 File Offset: 0x00175DD8
			public T FindFirst(Predicate<T> predicate)
			{
				foreach (T t in this._registeredObjects)
				{
					if (predicate(t))
					{
						return t;
					}
				}
				return default(T);
			}

			// Token: 0x06004B83 RID: 19331 RVA: 0x00177C38 File Offset: 0x00175E38
			public MBReadOnlyList<T> FindAll(Predicate<T> predicate)
			{
				MBList<T> mblist = new MBList<T>();
				foreach (T t in this._registeredObjects)
				{
					if (predicate == null || predicate(t))
					{
						mblist.Add(t);
					}
				}
				return mblist;
			}

			// Token: 0x06004B84 RID: 19332 RVA: 0x00177C98 File Offset: 0x00175E98
			public static string FindNextUniqueStringId(List<CampaignObjectManager.CampaignObjectType<T>> lists, string id)
			{
				if (!CampaignObjectManager.CampaignObjectType<T>.Exist(lists, id))
				{
					return id;
				}
				ValueTuple<string, uint> idParts = CampaignObjectManager.CampaignObjectType<T>.GetIdParts(id);
				string item = idParts.Item1;
				uint num = idParts.Item2;
				num = MathF.Max(num, lists.Max((CampaignObjectManager.CampaignObjectType<T> x) => x.MaxCreatedPostfixIndex));
				num += 1U;
				return item + num;
			}

			// Token: 0x06004B85 RID: 19333 RVA: 0x00177D00 File Offset: 0x00175F00
			[return: TupleElementNames(new string[] { "str", "number" })]
			private static ValueTuple<string, uint> GetIdParts(string stringId)
			{
				int num = stringId.Length - 1;
				while (num > 0 && char.IsDigit(stringId[num]))
				{
					num--;
				}
				string item = stringId.Substring(0, num + 1);
				uint item2 = 0U;
				if (num < stringId.Length - 1)
				{
					uint.TryParse(stringId.Substring(num + 1, stringId.Length - num - 1), out item2);
				}
				return new ValueTuple<string, uint>(item, item2);
			}

			// Token: 0x06004B86 RID: 19334 RVA: 0x00177D68 File Offset: 0x00175F68
			private static bool Exist(List<CampaignObjectManager.CampaignObjectType<T>> lists, string id)
			{
				using (List<CampaignObjectManager.CampaignObjectType<T>>.Enumerator enumerator = lists.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Find(id) != null)
						{
							return true;
						}
					}
				}
				return false;
			}

			// Token: 0x04001594 RID: 5524
			private readonly IEnumerable<T> _registeredObjects;
		}

		// Token: 0x02000515 RID: 1301
		private enum CampaignObjects
		{
			// Token: 0x04001597 RID: 5527
			DeadOrDisabledHeroes,
			// Token: 0x04001598 RID: 5528
			AliveHeroes,
			// Token: 0x04001599 RID: 5529
			Clans,
			// Token: 0x0400159A RID: 5530
			Kingdoms,
			// Token: 0x0400159B RID: 5531
			MobileParty,
			// Token: 0x0400159C RID: 5532
			ObjectCount
		}
	}
}

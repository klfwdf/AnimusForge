using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace TaleWorlds.CampaignSystem;

public class CampaignObjectManager
{
	private interface ICampaignObjectType : IEnumerable
	{
		Type ObjectClass { get; }

		void PreAfterLoad();

		void AfterLoad();

		uint GetMaxObjectSubId();
	}

	private class CampaignObjectType<T> : ICampaignObjectType, IEnumerable, IEnumerable<T> where T : MBObjectBase
	{
		private readonly IEnumerable<T> _registeredObjects;

		public uint MaxCreatedPostfixIndex { get; private set; }

		Type ICampaignObjectType.ObjectClass => typeof(T);

		public CampaignObjectType(IEnumerable<T> registeredObjects)
		{
			_registeredObjects = registeredObjects;
			foreach (T registeredObject in _registeredObjects)
			{
				(string, uint) idParts = GetIdParts(registeredObject.StringId);
				if (idParts.Item2 > MaxCreatedPostfixIndex)
				{
					MaxCreatedPostfixIndex = idParts.Item2;
				}
			}
		}

		public void PreAfterLoad()
		{
			foreach (T item in _registeredObjects.ToList())
			{
				item.PreAfterLoadInternal();
			}
		}

		public void AfterLoad()
		{
			foreach (T item in _registeredObjects.ToList())
			{
				item.IsReady = true;
				item.AfterLoadInternal();
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _registeredObjects.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _registeredObjects.GetEnumerator();
		}

		public uint GetMaxObjectSubId()
		{
			uint num = 0u;
			foreach (T registeredObject in _registeredObjects)
			{
				if (registeredObject.Id.SubId > num)
				{
					num = registeredObject.Id.SubId;
				}
			}
			return num;
		}

		public void OnItemAdded(T item)
		{
			(string, uint) idParts = GetIdParts(item.StringId);
			if (idParts.Item2 > MaxCreatedPostfixIndex)
			{
				MaxCreatedPostfixIndex = idParts.Item2;
			}
			RegisterItem(item);
		}

		private void RegisterItem(T item)
		{
			item.IsReady = true;
		}

		public void UnregisterItem(T item)
		{
			item.IsReady = false;
		}

		public T Find(string id)
		{
			foreach (T registeredObject in _registeredObjects)
			{
				if (registeredObject.StringId == id)
				{
					return registeredObject;
				}
			}
			return null;
		}

		public T FindFirst(Predicate<T> predicate)
		{
			foreach (T registeredObject in _registeredObjects)
			{
				if (predicate(registeredObject))
				{
					return registeredObject;
				}
			}
			return null;
		}

		public MBReadOnlyList<T> FindAll(Predicate<T> predicate)
		{
			MBList<T> mBList = new MBList<T>();
			foreach (T registeredObject in _registeredObjects)
			{
				if (predicate == null || predicate(registeredObject))
				{
					mBList.Add(registeredObject);
				}
			}
			return mBList;
		}

		public static string FindNextUniqueStringId(List<CampaignObjectType<T>> lists, string id)
		{
			if (!Exist(lists, id))
			{
				return id;
			}
			(string str, uint number) idParts = GetIdParts(id);
			string item = idParts.str;
			uint item2 = idParts.number;
			item2 = TaleWorlds.Library.MathF.Max(item2, lists.Max((CampaignObjectType<T> x) => x.MaxCreatedPostfixIndex));
			item2++;
			return item + item2;
		}

		private static (string str, uint number) GetIdParts(string stringId)
		{
			int num = stringId.Length - 1;
			while (num > 0 && char.IsDigit(stringId[num]))
			{
				num--;
			}
			string item = stringId.Substring(0, num + 1);
			uint result = 0u;
			if (num < stringId.Length - 1)
			{
				uint.TryParse(stringId.Substring(num + 1, stringId.Length - num - 1), out result);
			}
			return (str: item, number: result);
		}

		private static bool Exist(List<CampaignObjectType<T>> lists, string id)
		{
			foreach (CampaignObjectType<T> list in lists)
			{
				if (list.Find(id) != null)
				{
					return true;
				}
			}
			return false;
		}
	}

	private enum CampaignObjects
	{
		DeadOrDisabledHeroes,
		AliveHeroes,
		Clans,
		Kingdoms,
		MobileParty,
		ObjectCount
	}

	internal const uint HeroObjectManagerTypeID = 32u;

	internal const uint MobilePartyObjectManagerTypeID = 14u;

	internal const uint ClanObjectManagerTypeID = 18u;

	internal const uint KingdomObjectManagerTypeID = 20u;

	private ICampaignObjectType[] _objects;

	private Dictionary<Type, uint> _objectTypesAndNextIds;

	[SaveableField(20)]
	private readonly MBList<Hero> _deadOrDisabledHeroes;

	[SaveableField(30)]
	private readonly MBList<Hero> _aliveHeroes;

	[SaveableField(40)]
	private readonly MBList<Clan> _clans;

	[SaveableField(50)]
	private readonly MBList<Kingdom> _kingdoms;

	private MBList<IFaction> _factions;

	[SaveableField(71)]
	private MBList<MobileParty> _mobileParties;

	private MBList<MobileParty> _caravanParties;

	private MBList<MobileParty> _patrolParties;

	private MBList<MobileParty> _militiaParties;

	private MBList<MobileParty> _garrisonParties;

	private MBList<MobileParty> _banditParties;

	private MBList<MobileParty> _villagerParties;

	private MBList<MobileParty> _customParties;

	private MBList<MobileParty> _lordParties;

	private MBList<MobileParty> _partiesWithoutPartyComponent;

	[SaveableProperty(80)]
	public MBReadOnlyList<Settlement> Settlements { get; private set; }

	public MBReadOnlyList<MobileParty> MobileParties => _mobileParties;

	public MBReadOnlyList<MobileParty> CaravanParties => _caravanParties;

	public MBReadOnlyList<MobileParty> PatrolParties => _patrolParties;

	public MBReadOnlyList<MobileParty> MilitiaParties => _militiaParties;

	public MBReadOnlyList<MobileParty> GarrisonParties => _garrisonParties;

	public MBReadOnlyList<MobileParty> BanditParties => _banditParties;

	public MBReadOnlyList<MobileParty> VillagerParties => _villagerParties;

	public MBReadOnlyList<MobileParty> LordParties => _lordParties;

	public MBReadOnlyList<MobileParty> CustomParties => _customParties;

	public MBReadOnlyList<MobileParty> PartiesWithoutPartyComponent => _partiesWithoutPartyComponent;

	public MBReadOnlyList<Hero> AliveHeroes => _aliveHeroes;

	public MBReadOnlyList<Hero> DeadOrDisabledHeroes => _deadOrDisabledHeroes;

	public MBReadOnlyList<Clan> Clans => _clans;

	public MBReadOnlyList<Kingdom> Kingdoms => _kingdoms;

	public MBReadOnlyList<IFaction> Factions => _factions;

	public CampaignObjectManager()
	{
		_objects = new ICampaignObjectType[5];
		_mobileParties = new MBList<MobileParty>();
		_caravanParties = new MBList<MobileParty>();
		_patrolParties = new MBList<MobileParty>();
		_militiaParties = new MBList<MobileParty>();
		_garrisonParties = new MBList<MobileParty>();
		_customParties = new MBList<MobileParty>();
		_banditParties = new MBList<MobileParty>();
		_villagerParties = new MBList<MobileParty>();
		_lordParties = new MBList<MobileParty>();
		_partiesWithoutPartyComponent = new MBList<MobileParty>();
		_deadOrDisabledHeroes = new MBList<Hero>();
		_aliveHeroes = new MBList<Hero>();
		_clans = new MBList<Clan>();
		_kingdoms = new MBList<Kingdom>();
		_factions = new MBList<IFaction>();
	}

	private void InitializeManagerObjectLists()
	{
		_objects[4] = new CampaignObjectType<MobileParty>(_mobileParties);
		_objects[0] = new CampaignObjectType<Hero>(_deadOrDisabledHeroes);
		_objects[1] = new CampaignObjectType<Hero>(_aliveHeroes);
		_objects[2] = new CampaignObjectType<Clan>(_clans);
		_objects[3] = new CampaignObjectType<Kingdom>(_kingdoms);
		_objectTypesAndNextIds = new Dictionary<Type, uint>();
		ICampaignObjectType[] objects = _objects;
		foreach (ICampaignObjectType campaignObjectType in objects)
		{
			uint maxObjectSubId = campaignObjectType.GetMaxObjectSubId();
			if (_objectTypesAndNextIds.TryGetValue(campaignObjectType.ObjectClass, out var value))
			{
				if (value <= maxObjectSubId)
				{
					_objectTypesAndNextIds[campaignObjectType.ObjectClass] = maxObjectSubId + 1;
				}
			}
			else
			{
				_objectTypesAndNextIds.Add(campaignObjectType.ObjectClass, maxObjectSubId + 1);
			}
		}
	}

	[LoadInitializationCallback]
	private void OnLoad(MetaData metaData, ObjectLoadData objectLoadData)
	{
		_objects = new ICampaignObjectType[5];
		_factions = new MBList<IFaction>();
		_caravanParties = new MBList<MobileParty>();
		_patrolParties = new MBList<MobileParty>();
		_militiaParties = new MBList<MobileParty>();
		_garrisonParties = new MBList<MobileParty>();
		_customParties = new MBList<MobileParty>();
		_banditParties = new MBList<MobileParty>();
		_villagerParties = new MBList<MobileParty>();
		_lordParties = new MBList<MobileParty>();
		_partiesWithoutPartyComponent = new MBList<MobileParty>();
	}

	internal void PreAfterLoad()
	{
		ICampaignObjectType[] objects = _objects;
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].PreAfterLoad();
		}
	}

	internal void AfterLoad()
	{
		ICampaignObjectType[] objects = _objects;
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].AfterLoad();
		}
	}

	internal void InitializeOnLoad()
	{
		Settlements = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
		foreach (Clan clan in _clans)
		{
			if (!_factions.Contains(clan))
			{
				_factions.Add(clan);
			}
		}
		foreach (Kingdom kingdom in _kingdoms)
		{
			if (!_factions.Contains(kingdom))
			{
				_factions.Add(kingdom);
			}
		}
		foreach (MobileParty mobileParty in _mobileParties)
		{
			mobileParty.UpdatePartyComponentFlags();
			AddPartyToAppropriateList(mobileParty);
		}
		InitializeManagerObjectLists();
	}

	internal void InitializeOnNewGame()
	{
		MBReadOnlyList<Hero> objectTypeList = MBObjectManager.Instance.GetObjectTypeList<Hero>();
		MBReadOnlyList<MobileParty> objectTypeList2 = MBObjectManager.Instance.GetObjectTypeList<MobileParty>();
		MBReadOnlyList<Clan> objectTypeList3 = MBObjectManager.Instance.GetObjectTypeList<Clan>();
		MBReadOnlyList<Kingdom> objectTypeList4 = MBObjectManager.Instance.GetObjectTypeList<Kingdom>();
		Settlements = MBObjectManager.Instance.GetObjectTypeList<Settlement>();
		foreach (Hero item in objectTypeList)
		{
			if (item.HeroState == Hero.CharacterStates.Dead || item.HeroState == Hero.CharacterStates.Disabled)
			{
				if (!_deadOrDisabledHeroes.Contains(item))
				{
					_deadOrDisabledHeroes.Add(item);
				}
			}
			else if (!_aliveHeroes.Contains(item))
			{
				_aliveHeroes.Add(item);
			}
		}
		foreach (Clan item2 in objectTypeList3)
		{
			if (!_clans.Contains(item2))
			{
				_clans.Add(item2);
			}
			if (!_factions.Contains(item2))
			{
				_factions.Add(item2);
			}
		}
		foreach (Kingdom item3 in objectTypeList4)
		{
			if (!_kingdoms.Contains(item3))
			{
				_kingdoms.Add(item3);
			}
			if (!_factions.Contains(item3))
			{
				_factions.Add(item3);
			}
		}
		foreach (MobileParty item4 in objectTypeList2)
		{
			_mobileParties.Add(item4);
			AddPartyToAppropriateList(item4);
		}
		InitializeManagerObjectLists();
		InitializeCachedData();
	}

	private void InitializeCachedData()
	{
		foreach (Settlement item in Settlement.All)
		{
			if (item.IsVillage)
			{
				item.OwnerClan.OnBoundVillageAdded(item.Village);
			}
		}
	}

	internal void AddMobileParty(MobileParty party)
	{
		party.Id = new MBGUID(14u, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<MobileParty>());
		_mobileParties.Add(party);
		OnItemAdded(CampaignObjects.MobileParty, party);
		AddPartyToAppropriateList(party);
	}

	internal void RemoveMobileParty(MobileParty party)
	{
		_mobileParties.Remove(party);
		OnItemRemoved(CampaignObjects.MobileParty, party);
		RemovePartyFromAppropriateList(party);
	}

	internal void BeforePartyComponentChanged(MobileParty party)
	{
		RemovePartyFromAppropriateList(party);
	}

	internal void AfterPartyComponentChanged(MobileParty party)
	{
		AddPartyToAppropriateList(party);
	}

	internal void AddHero(Hero hero)
	{
		hero.Id = new MBGUID(32u, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Hero>());
		OnHeroAdded(hero);
	}

	internal void UnregisterDeadHero(Hero hero)
	{
		_deadOrDisabledHeroes.Remove(hero);
		OnItemRemoved(CampaignObjects.DeadOrDisabledHeroes, hero);
		CampaignEventDispatcher.Instance.OnHeroUnregistered(hero);
	}

	private void OnHeroAdded(Hero hero)
	{
		if (hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled)
		{
			_deadOrDisabledHeroes.Add(hero);
			OnItemAdded(CampaignObjects.DeadOrDisabledHeroes, hero);
		}
		else
		{
			_aliveHeroes.Add(hero);
			OnItemAdded(CampaignObjects.AliveHeroes, hero);
		}
	}

	internal void HeroStateChanged(Hero hero, Hero.CharacterStates oldState)
	{
		bool num = oldState == Hero.CharacterStates.Dead || oldState == Hero.CharacterStates.Disabled;
		bool flag = hero.HeroState == Hero.CharacterStates.Dead || hero.HeroState == Hero.CharacterStates.Disabled;
		if (num == flag)
		{
			return;
		}
		if (flag)
		{
			if (_aliveHeroes.Contains(hero))
			{
				_aliveHeroes.Remove(hero);
			}
		}
		else if (_deadOrDisabledHeroes.Contains(hero))
		{
			_deadOrDisabledHeroes.Remove(hero);
		}
		OnHeroAdded(hero);
	}

	internal void AddClan(Clan clan)
	{
		clan.Id = new MBGUID(18u, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Clan>());
		_clans.Add(clan);
		OnItemAdded(CampaignObjects.Clans, clan);
		_factions.Add(clan);
	}

	internal void RemoveClan(Clan clan)
	{
		if (_clans.Contains(clan))
		{
			_clans.Remove(clan);
			OnItemRemoved(CampaignObjects.Clans, clan);
		}
		if (_factions.Contains(clan))
		{
			_factions.Remove(clan);
		}
	}

	internal void AddKingdom(Kingdom kingdom)
	{
		kingdom.Id = new MBGUID(20u, Campaign.Current.CampaignObjectManager.GetNextUniqueObjectIdOfType<Kingdom>());
		_kingdoms.Add(kingdom);
		OnItemAdded(CampaignObjects.Kingdoms, kingdom);
		_factions.Add(kingdom);
	}

	private void AddPartyToAppropriateList(MobileParty party)
	{
		if (party.IsBandit)
		{
			_banditParties.Add(party);
		}
		else if (party.IsCaravan)
		{
			_caravanParties.Add(party);
		}
		else if (party.IsPatrolParty)
		{
			_patrolParties.Add(party);
		}
		else if (party.IsLordParty)
		{
			_lordParties.Add(party);
		}
		else if (party.IsMilitia)
		{
			_militiaParties.Add(party);
		}
		else if (party.IsVillager)
		{
			_villagerParties.Add(party);
		}
		else if (party.IsCustomParty)
		{
			_customParties.Add(party);
		}
		else if (party.IsGarrison)
		{
			_garrisonParties.Add(party);
		}
		else
		{
			_partiesWithoutPartyComponent.Add(party);
		}
	}

	private void RemovePartyFromAppropriateList(MobileParty party)
	{
		if (party.IsBandit)
		{
			_banditParties.Remove(party);
		}
		else if (party.IsCaravan)
		{
			_caravanParties.Remove(party);
		}
		else if (party.IsPatrolParty)
		{
			_patrolParties.Remove(party);
		}
		else if (party.IsLordParty)
		{
			_lordParties.Remove(party);
		}
		else if (party.IsMilitia)
		{
			_militiaParties.Remove(party);
		}
		else if (party.IsVillager)
		{
			_villagerParties.Remove(party);
		}
		else if (party.IsCustomParty)
		{
			_customParties.Remove(party);
		}
		else if (party.IsGarrison)
		{
			_garrisonParties.Remove(party);
		}
		else
		{
			_partiesWithoutPartyComponent.Remove(party);
		}
	}

	private void OnItemAdded<T>(CampaignObjects targetList, T obj) where T : MBObjectBase
	{
		((CampaignObjectType<T>)_objects[(int)targetList])?.OnItemAdded(obj);
	}

	private void OnItemRemoved<T>(CampaignObjects targetList, T obj) where T : MBObjectBase
	{
		((CampaignObjectType<T>)_objects[(int)targetList])?.UnregisterItem(obj);
	}

	public T FindFirst<T>(Predicate<T> predicate) where T : MBObjectBase
	{
		ICampaignObjectType[] objects = _objects;
		foreach (ICampaignObjectType campaignObjectType in objects)
		{
			if (typeof(T) == campaignObjectType.ObjectClass)
			{
				T val = ((CampaignObjectType<T>)campaignObjectType).FindFirst(predicate);
				if (val != null)
				{
					return val;
				}
			}
		}
		return null;
	}

	public MBReadOnlyList<T> FindAll<T>(Predicate<T> predicate) where T : MBObjectBase
	{
		MBList<T> mBList = new MBList<T>();
		ICampaignObjectType[] objects = _objects;
		foreach (ICampaignObjectType campaignObjectType in objects)
		{
			if (typeof(T) == campaignObjectType.ObjectClass)
			{
				MBReadOnlyList<T> mBReadOnlyList = ((CampaignObjectType<T>)campaignObjectType).FindAll(predicate);
				if (mBReadOnlyList != null)
				{
					mBList.AddRange(mBReadOnlyList);
				}
			}
		}
		return mBList;
	}

	private uint GetNextUniqueObjectIdOfType<T>() where T : MBObjectBase
	{
		if (_objectTypesAndNextIds.TryGetValue(typeof(T), out var value))
		{
			_objectTypesAndNextIds[typeof(T)] = value + 1;
		}
		return value;
	}

	public T Find<T>(string id) where T : MBObjectBase
	{
		ICampaignObjectType[] objects = _objects;
		foreach (ICampaignObjectType campaignObjectType in objects)
		{
			if (campaignObjectType != null && typeof(T) == campaignObjectType.ObjectClass)
			{
				T val = ((CampaignObjectType<T>)campaignObjectType).Find(id);
				if (val != null)
				{
					return val;
				}
			}
		}
		return null;
	}

	public string FindNextUniqueStringId<T>(string id) where T : MBObjectBase
	{
		List<CampaignObjectType<T>> list = new List<CampaignObjectType<T>>();
		ICampaignObjectType[] objects = _objects;
		foreach (ICampaignObjectType campaignObjectType in objects)
		{
			if (campaignObjectType != null && typeof(T) == campaignObjectType.ObjectClass)
			{
				list.Add(campaignObjectType as CampaignObjectType<T>);
			}
		}
		return CampaignObjectType<T>.FindNextUniqueStringId(list, id);
	}

	internal static void AutoGeneratedStaticCollectObjectsCampaignObjectManager(object o, List<object> collectedObjects)
	{
		((CampaignObjectManager)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		collectedObjects.Add(_deadOrDisabledHeroes);
		collectedObjects.Add(_aliveHeroes);
		collectedObjects.Add(_clans);
		collectedObjects.Add(_kingdoms);
		collectedObjects.Add(_mobileParties);
		collectedObjects.Add(Settlements);
	}

	internal static object AutoGeneratedGetMemberValueSettlements(object o)
	{
		return ((CampaignObjectManager)o).Settlements;
	}

	internal static object AutoGeneratedGetMemberValue_deadOrDisabledHeroes(object o)
	{
		return ((CampaignObjectManager)o)._deadOrDisabledHeroes;
	}

	internal static object AutoGeneratedGetMemberValue_aliveHeroes(object o)
	{
		return ((CampaignObjectManager)o)._aliveHeroes;
	}

	internal static object AutoGeneratedGetMemberValue_clans(object o)
	{
		return ((CampaignObjectManager)o)._clans;
	}

	internal static object AutoGeneratedGetMemberValue_kingdoms(object o)
	{
		return ((CampaignObjectManager)o)._kingdoms;
	}

	internal static object AutoGeneratedGetMemberValue_mobileParties(object o)
	{
		return ((CampaignObjectManager)o)._mobileParties;
	}
}

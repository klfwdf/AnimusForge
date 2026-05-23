using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core;

public class VirtualPlayer
{
	private const string DefaultPlayerBannerCode = "11.8.1.4345.4345.770.774.1.0.0.133.7.5.512.512.784.769.1.0.0";

	private static Dictionary<Type, object> _peerComponents;

	private static Dictionary<Type, uint> _peerComponentIds;

	private static Dictionary<uint, Type> _peerComponentTypes;

	private string _bannerCode;

	public readonly ICommunicator Communicator;

	private EntitySystem<PeerComponent> _peerEntitySystem;

	public Dictionary<int, List<int>> UsedCosmetics;

	public static Dictionary<Type, object> PeerComponents => _peerComponents;

	public string BannerCode
	{
		get
		{
			if (_bannerCode == null)
			{
				_bannerCode = "11.8.1.4345.4345.770.774.1.0.0.133.7.5.512.512.784.769.1.0.0";
			}
			return _bannerCode;
		}
		set
		{
			_bannerCode = value;
		}
	}

	public BodyProperties BodyProperties { get; set; }

	public int Race { get; set; }

	public bool IsFemale { get; set; }

	public PlayerId Id { get; set; }

	public int Index { get; private set; }

	public bool IsMine => MBNetwork.MyPeer == this;

	public string UserName { get; private set; }

	public int ChosenBadgeIndex { get; set; }

	static VirtualPlayer()
	{
		_peerComponents = new Dictionary<Type, object>();
		FindPeerComponents();
	}

	private static void FindPeerComponents()
	{
		Debug.Print("Searching Peer Components");
		_peerComponentIds = new Dictionary<Type, uint>();
		_peerComponentTypes = new Dictionary<uint, Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		List<Type> list = new List<Type>();
		Assembly[] array = assemblies;
		foreach (Assembly assembly in array)
		{
			if (CheckAssemblyForPeerComponent(assembly))
			{
				List<Type> typesSafe = assembly.GetTypesSafe();
				list.AddRange(typesSafe.Where((Type q) => typeof(PeerComponent).IsAssignableFrom(q) && typeof(PeerComponent) != q));
			}
		}
		foreach (Type item in list)
		{
			uint dJB = (uint)Common.GetDJB2(item.Name);
			_peerComponentIds.Add(item, dJB);
			_peerComponentTypes.Add(dJB, item);
		}
		Debug.Print("Found " + list.Count + " peer components");
	}

	private static bool CheckAssemblyForPeerComponent(Assembly assembly)
	{
		Assembly assembly2 = Assembly.GetAssembly(typeof(PeerComponent));
		if (assembly == assembly2)
		{
			return true;
		}
		AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
		for (int i = 0; i < referencedAssemblies.Length; i++)
		{
			if (referencedAssemblies[i].FullName == assembly2.FullName)
			{
				return true;
			}
		}
		return false;
	}

	private static void EnsurePeerTypeList<T>() where T : PeerComponent
	{
		if (!_peerComponents.ContainsKey(typeof(T)))
		{
			_peerComponents.Add(typeof(T), new List<T>());
		}
	}

	private static void EnsurePeerTypeList(Type type)
	{
		if (!_peerComponents.ContainsKey(type))
		{
			IList value = Activator.CreateInstance(typeof(List<>).MakeGenericType(type)) as IList;
			_peerComponents.Add(type, value);
		}
	}

	public static List<T> Peers<T>() where T : PeerComponent
	{
		EnsurePeerTypeList<T>();
		return _peerComponents[typeof(T)] as List<T>;
	}

	public static void Reset()
	{
		_peerComponents = new Dictionary<Type, object>();
	}

	public VirtualPlayer(int index, string name, PlayerId playerID, ICommunicator communicator)
	{
		_peerEntitySystem = new EntitySystem<PeerComponent>();
		UserName = name;
		Index = index;
		Id = playerID;
		Communicator = communicator;
	}

	public T AddComponent<T>() where T : PeerComponent, new()
	{
		T val = _peerEntitySystem.AddComponent<T>();
		val.Peer = this;
		val.TypeId = _peerComponentIds[typeof(T)];
		EnsurePeerTypeList<T>();
		(_peerComponents[typeof(T)] as List<T>).Add(val);
		Communicator.OnAddComponent(val);
		val.Initialize();
		return val;
	}

	public PeerComponent AddComponent(Type peerComponentType)
	{
		PeerComponent peerComponent = _peerEntitySystem.AddComponent(peerComponentType);
		peerComponent.Peer = this;
		peerComponent.TypeId = _peerComponentIds[peerComponentType];
		EnsurePeerTypeList(peerComponentType);
		(_peerComponents[peerComponentType] as IList).Add(peerComponent);
		Communicator.OnAddComponent(peerComponent);
		peerComponent.Initialize();
		return peerComponent;
	}

	public PeerComponent AddComponent(uint componentId)
	{
		return AddComponent(_peerComponentTypes[componentId]);
	}

	public PeerComponent GetComponent(uint componentId)
	{
		return GetComponent(_peerComponentTypes[componentId]);
	}

	public T GetComponent<T>() where T : PeerComponent
	{
		return _peerEntitySystem.GetComponent<T>();
	}

	public PeerComponent GetComponent(Type peerComponentType)
	{
		return _peerEntitySystem.GetComponent(peerComponentType);
	}

	public void RemoveComponent<T>(bool synched = true) where T : PeerComponent
	{
		T component = _peerEntitySystem.GetComponent<T>();
		if (component != null)
		{
			_peerEntitySystem.RemoveComponent(component);
			(_peerComponents[typeof(T)] as List<T>).Remove(component);
			if (synched)
			{
				Communicator.OnRemoveComponent(component);
			}
		}
	}

	public void RemoveComponent(PeerComponent component)
	{
		_peerEntitySystem.RemoveComponent(component);
		(_peerComponents[component.GetType()] as IList).Remove(component);
		Communicator.OnRemoveComponent(component);
	}

	public void OnDisconnect()
	{
	}

	public void SynchronizeComponentsTo(VirtualPlayer peer)
	{
		foreach (PeerComponent component in _peerEntitySystem.Components)
		{
			Communicator.OnSynchronizeComponentTo(peer, component);
		}
	}

	public void UpdateIndexForReconnectingPlayer(int playerIndex)
	{
		Index = playerIndex;
	}
}

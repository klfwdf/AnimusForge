using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.Core
{
	// Token: 0x020000DD RID: 221
	public class VirtualPlayer
	{
		// Token: 0x170003D8 RID: 984
		// (get) Token: 0x06000B41 RID: 2881 RVA: 0x00024746 File Offset: 0x00022946
		public static Dictionary<Type, object> PeerComponents
		{
			get
			{
				return VirtualPlayer._peerComponents;
			}
		}

		// Token: 0x06000B42 RID: 2882 RVA: 0x0002474D File Offset: 0x0002294D
		static VirtualPlayer()
		{
			VirtualPlayer.FindPeerComponents();
		}

		// Token: 0x06000B43 RID: 2883 RVA: 0x00024760 File Offset: 0x00022960
		private static void FindPeerComponents()
		{
			Debug.Print("Searching Peer Components", 0, Debug.DebugColor.White, 17592186044416UL);
			VirtualPlayer._peerComponentIds = new Dictionary<Type, uint>();
			VirtualPlayer._peerComponentTypes = new Dictionary<uint, Type>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			List<Type> list = new List<Type>();
			foreach (Assembly assembly in assemblies)
			{
				if (VirtualPlayer.CheckAssemblyForPeerComponent(assembly))
				{
					List<Type> typesSafe = assembly.GetTypesSafe(null);
					list.AddRange(from q in typesSafe
						where typeof(PeerComponent).IsAssignableFrom(q) && typeof(PeerComponent) != q
						select q);
				}
			}
			foreach (Type type in list)
			{
				uint djb = (uint)Common.GetDJB2(type.Name);
				VirtualPlayer._peerComponentIds.Add(type, djb);
				VirtualPlayer._peerComponentTypes.Add(djb, type);
			}
			Debug.Print("Found " + list.Count + " peer components", 0, Debug.DebugColor.White, 17592186044416UL);
		}

		// Token: 0x06000B44 RID: 2884 RVA: 0x00024888 File Offset: 0x00022A88
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

		// Token: 0x06000B45 RID: 2885 RVA: 0x000248DD File Offset: 0x00022ADD
		private static void EnsurePeerTypeList<T>() where T : PeerComponent
		{
			if (!VirtualPlayer._peerComponents.ContainsKey(typeof(T)))
			{
				VirtualPlayer._peerComponents.Add(typeof(T), new List<T>());
			}
		}

		// Token: 0x06000B46 RID: 2886 RVA: 0x00024910 File Offset: 0x00022B10
		private static void EnsurePeerTypeList(Type type)
		{
			if (!VirtualPlayer._peerComponents.ContainsKey(type))
			{
				IList value = Activator.CreateInstance(typeof(List<>).MakeGenericType(new Type[] { type })) as IList;
				VirtualPlayer._peerComponents.Add(type, value);
			}
		}

		// Token: 0x06000B47 RID: 2887 RVA: 0x0002495A File Offset: 0x00022B5A
		public static List<T> Peers<T>() where T : PeerComponent
		{
			VirtualPlayer.EnsurePeerTypeList<T>();
			return VirtualPlayer._peerComponents[typeof(T)] as List<T>;
		}

		// Token: 0x06000B48 RID: 2888 RVA: 0x0002497A File Offset: 0x00022B7A
		public static void Reset()
		{
			VirtualPlayer._peerComponents = new Dictionary<Type, object>();
		}

		// Token: 0x170003D9 RID: 985
		// (get) Token: 0x06000B49 RID: 2889 RVA: 0x00024986 File Offset: 0x00022B86
		// (set) Token: 0x06000B4A RID: 2890 RVA: 0x000249A1 File Offset: 0x00022BA1
		public string BannerCode
		{
			get
			{
				if (this._bannerCode == null)
				{
					this._bannerCode = "11.8.1.4345.4345.770.774.1.0.0.133.7.5.512.512.784.769.1.0.0";
				}
				return this._bannerCode;
			}
			set
			{
				this._bannerCode = value;
			}
		}

		// Token: 0x170003DA RID: 986
		// (get) Token: 0x06000B4B RID: 2891 RVA: 0x000249AA File Offset: 0x00022BAA
		// (set) Token: 0x06000B4C RID: 2892 RVA: 0x000249B2 File Offset: 0x00022BB2
		public BodyProperties BodyProperties { get; set; }

		// Token: 0x170003DB RID: 987
		// (get) Token: 0x06000B4D RID: 2893 RVA: 0x000249BB File Offset: 0x00022BBB
		// (set) Token: 0x06000B4E RID: 2894 RVA: 0x000249C3 File Offset: 0x00022BC3
		public int Race { get; set; }

		// Token: 0x170003DC RID: 988
		// (get) Token: 0x06000B4F RID: 2895 RVA: 0x000249CC File Offset: 0x00022BCC
		// (set) Token: 0x06000B50 RID: 2896 RVA: 0x000249D4 File Offset: 0x00022BD4
		public bool IsFemale { get; set; }

		// Token: 0x170003DD RID: 989
		// (get) Token: 0x06000B51 RID: 2897 RVA: 0x000249DD File Offset: 0x00022BDD
		// (set) Token: 0x06000B52 RID: 2898 RVA: 0x000249E5 File Offset: 0x00022BE5
		public PlayerId Id { get; set; }

		// Token: 0x170003DE RID: 990
		// (get) Token: 0x06000B53 RID: 2899 RVA: 0x000249EE File Offset: 0x00022BEE
		// (set) Token: 0x06000B54 RID: 2900 RVA: 0x000249F6 File Offset: 0x00022BF6
		public int Index { get; private set; }

		// Token: 0x170003DF RID: 991
		// (get) Token: 0x06000B55 RID: 2901 RVA: 0x000249FF File Offset: 0x00022BFF
		public bool IsMine
		{
			get
			{
				return MBNetwork.MyPeer == this;
			}
		}

		// Token: 0x170003E0 RID: 992
		// (get) Token: 0x06000B56 RID: 2902 RVA: 0x00024A09 File Offset: 0x00022C09
		// (set) Token: 0x06000B57 RID: 2903 RVA: 0x00024A11 File Offset: 0x00022C11
		public string UserName { get; private set; }

		// Token: 0x170003E1 RID: 993
		// (get) Token: 0x06000B58 RID: 2904 RVA: 0x00024A1A File Offset: 0x00022C1A
		// (set) Token: 0x06000B59 RID: 2905 RVA: 0x00024A22 File Offset: 0x00022C22
		public int ChosenBadgeIndex { get; set; }

		// Token: 0x06000B5A RID: 2906 RVA: 0x00024A2B File Offset: 0x00022C2B
		public VirtualPlayer(int index, string name, PlayerId playerID, ICommunicator communicator)
		{
			this._peerEntitySystem = new EntitySystem<PeerComponent>();
			this.UserName = name;
			this.Index = index;
			this.Id = playerID;
			this.Communicator = communicator;
		}

		// Token: 0x06000B5B RID: 2907 RVA: 0x00024A5C File Offset: 0x00022C5C
		public T AddComponent<T>() where T : PeerComponent, new()
		{
			T t = this._peerEntitySystem.AddComponent<T>();
			t.Peer = this;
			t.TypeId = VirtualPlayer._peerComponentIds[typeof(T)];
			VirtualPlayer.EnsurePeerTypeList<T>();
			(VirtualPlayer._peerComponents[typeof(T)] as List<T>).Add(t);
			this.Communicator.OnAddComponent(t);
			t.Initialize();
			return t;
		}

		// Token: 0x06000B5C RID: 2908 RVA: 0x00024AE4 File Offset: 0x00022CE4
		public PeerComponent AddComponent(Type peerComponentType)
		{
			PeerComponent peerComponent = this._peerEntitySystem.AddComponent(peerComponentType);
			peerComponent.Peer = this;
			peerComponent.TypeId = VirtualPlayer._peerComponentIds[peerComponentType];
			VirtualPlayer.EnsurePeerTypeList(peerComponentType);
			(VirtualPlayer._peerComponents[peerComponentType] as IList).Add(peerComponent);
			this.Communicator.OnAddComponent(peerComponent);
			peerComponent.Initialize();
			return peerComponent;
		}

		// Token: 0x06000B5D RID: 2909 RVA: 0x00024B46 File Offset: 0x00022D46
		public PeerComponent AddComponent(uint componentId)
		{
			return this.AddComponent(VirtualPlayer._peerComponentTypes[componentId]);
		}

		// Token: 0x06000B5E RID: 2910 RVA: 0x00024B59 File Offset: 0x00022D59
		public PeerComponent GetComponent(uint componentId)
		{
			return this.GetComponent(VirtualPlayer._peerComponentTypes[componentId]);
		}

		// Token: 0x06000B5F RID: 2911 RVA: 0x00024B6C File Offset: 0x00022D6C
		public T GetComponent<T>() where T : PeerComponent
		{
			return this._peerEntitySystem.GetComponent<T>();
		}

		// Token: 0x06000B60 RID: 2912 RVA: 0x00024B79 File Offset: 0x00022D79
		public PeerComponent GetComponent(Type peerComponentType)
		{
			return this._peerEntitySystem.GetComponent(peerComponentType);
		}

		// Token: 0x06000B61 RID: 2913 RVA: 0x00024B88 File Offset: 0x00022D88
		public void RemoveComponent<T>(bool synched = true) where T : PeerComponent
		{
			T component = this._peerEntitySystem.GetComponent<T>();
			if (component != null)
			{
				this._peerEntitySystem.RemoveComponent(component);
				(VirtualPlayer._peerComponents[typeof(T)] as List<T>).Remove(component);
				if (synched)
				{
					this.Communicator.OnRemoveComponent(component);
				}
			}
		}

		// Token: 0x06000B62 RID: 2914 RVA: 0x00024BEE File Offset: 0x00022DEE
		public void RemoveComponent(PeerComponent component)
		{
			this._peerEntitySystem.RemoveComponent(component);
			(VirtualPlayer._peerComponents[component.GetType()] as IList).Remove(component);
			this.Communicator.OnRemoveComponent(component);
		}

		// Token: 0x06000B63 RID: 2915 RVA: 0x00024C23 File Offset: 0x00022E23
		public void OnDisconnect()
		{
		}

		// Token: 0x06000B64 RID: 2916 RVA: 0x00024C28 File Offset: 0x00022E28
		public void SynchronizeComponentsTo(VirtualPlayer peer)
		{
			foreach (PeerComponent component in this._peerEntitySystem.Components)
			{
				this.Communicator.OnSynchronizeComponentTo(peer, component);
			}
		}

		// Token: 0x06000B65 RID: 2917 RVA: 0x00024C88 File Offset: 0x00022E88
		public void UpdateIndexForReconnectingPlayer(int playerIndex)
		{
			this.Index = playerIndex;
		}

		// Token: 0x04000683 RID: 1667
		private const string DefaultPlayerBannerCode = "11.8.1.4345.4345.770.774.1.0.0.133.7.5.512.512.784.769.1.0.0";

		// Token: 0x04000684 RID: 1668
		private static Dictionary<Type, object> _peerComponents = new Dictionary<Type, object>();

		// Token: 0x04000685 RID: 1669
		private static Dictionary<Type, uint> _peerComponentIds;

		// Token: 0x04000686 RID: 1670
		private static Dictionary<uint, Type> _peerComponentTypes;

		// Token: 0x04000687 RID: 1671
		private string _bannerCode;

		// Token: 0x0400068C RID: 1676
		public readonly ICommunicator Communicator;

		// Token: 0x0400068D RID: 1677
		private EntitySystem<PeerComponent> _peerEntitySystem;

		// Token: 0x04000691 RID: 1681
		public Dictionary<int, List<int>> UsedCosmetics;
	}
}

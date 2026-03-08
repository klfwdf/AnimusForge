using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.NameMarker
{
	// Token: 0x0200002F RID: 47
	public static class MissionNameMarkerFactory
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060003B4 RID: 948 RVA: 0x0000FB0C File Offset: 0x0000DD0C
		// (remove) Token: 0x060003B5 RID: 949 RVA: 0x0000FB40 File Offset: 0x0000DD40
		public static event Action OnProvidersChanged;

		// Token: 0x060003B7 RID: 951 RVA: 0x0000FBA8 File Offset: 0x0000DDA8
		public static MissionNameMarkerFactory.INameMarkerProviderContext PushContext(string name, bool addDefaultProviders)
		{
			MissionNameMarkerFactory.NameMarkerProviderContext nameMarkerProviderContext = new MissionNameMarkerFactory.NameMarkerProviderContext(false, name, new Action(MissionNameMarkerFactory.FireProvidersChangedEvent));
			if (addDefaultProviders)
			{
				MissionNameMarkerFactory.NameMarkerProviderContext nameMarkerProviderContext2 = MissionNameMarkerFactory.DefaultContext as MissionNameMarkerFactory.NameMarkerProviderContext;
				for (int i = 0; i < nameMarkerProviderContext2.ProviderTypes.Count; i++)
				{
					nameMarkerProviderContext.AddProvider(nameMarkerProviderContext2.ProviderTypes[i]);
				}
			}
			MissionNameMarkerFactory._registeredContexts.Add(nameMarkerProviderContext);
			return nameMarkerProviderContext;
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x0000FC0C File Offset: 0x0000DE0C
		public static void PopContext(string contextId)
		{
			for (int i = 0; i < MissionNameMarkerFactory._registeredContexts.Count; i++)
			{
				if (MissionNameMarkerFactory._registeredContexts[i].Id == contextId)
				{
					MissionNameMarkerFactory.PopContext(MissionNameMarkerFactory._registeredContexts[i]);
					return;
				}
			}
			Debug.FailedAssert("Trying to pop a name marker context that was not pushed: " + contextId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "PopContext", 54);
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x0000FC73 File Offset: 0x0000DE73
		public static void PopContext(MissionNameMarkerFactory.INameMarkerProviderContext context)
		{
			if (context.IsDefaultContext)
			{
				Debug.FailedAssert("Default name marker context cannot be removed", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "PopContext", 61);
				return;
			}
			MissionNameMarkerFactory._registeredContexts.Remove(context);
		}

		// Token: 0x060003BA RID: 954 RVA: 0x0000FCA0 File Offset: 0x0000DEA0
		private static void FireProvidersChangedEvent()
		{
			Action onProvidersChanged = MissionNameMarkerFactory.OnProvidersChanged;
			if (onProvidersChanged == null)
			{
				return;
			}
			onProvidersChanged();
		}

		// Token: 0x060003BB RID: 955 RVA: 0x0000FCB4 File Offset: 0x0000DEB4
		public static List<MissionNameMarkerProvider> CollectProviders()
		{
			List<MissionNameMarkerProvider> list = new List<MissionNameMarkerProvider>();
			MissionNameMarkerFactory.NameMarkerProviderContext nameMarkerProviderContext = MissionNameMarkerFactory._registeredContexts[MissionNameMarkerFactory._registeredContexts.Count - 1] as MissionNameMarkerFactory.NameMarkerProviderContext;
			for (int i = 0; i < nameMarkerProviderContext.ProviderTypes.Count; i++)
			{
				MissionNameMarkerProvider item = Activator.CreateInstance(nameMarkerProviderContext.ProviderTypes[i]) as MissionNameMarkerProvider;
				list.Add(item);
			}
			return list;
		}

		// Token: 0x060003BC RID: 956 RVA: 0x0000FD18 File Offset: 0x0000DF18
		public static void UpdateProviders(MissionNameMarkerProvider[] existingProviders, out List<MissionNameMarkerProvider> addedProviders, out List<MissionNameMarkerProvider> removedProviders)
		{
			addedProviders = new List<MissionNameMarkerProvider>();
			removedProviders = new List<MissionNameMarkerProvider>();
			MissionNameMarkerFactory.NameMarkerProviderContext nameMarkerProviderContext = MissionNameMarkerFactory._registeredContexts[MissionNameMarkerFactory._registeredContexts.Count - 1] as MissionNameMarkerFactory.NameMarkerProviderContext;
			for (int i = 0; i < existingProviders.Length; i++)
			{
				bool flag = true;
				MissionNameMarkerProvider missionNameMarkerProvider = existingProviders[i];
				for (int j = 0; j < nameMarkerProviderContext.ProviderTypes.Count; j++)
				{
					Type right = nameMarkerProviderContext.ProviderTypes[j];
					if (missionNameMarkerProvider.GetType() == right)
					{
						flag = false;
					}
				}
				if (flag)
				{
					removedProviders.Add(missionNameMarkerProvider);
				}
			}
			for (int k = 0; k < nameMarkerProviderContext.ProviderTypes.Count; k++)
			{
				bool flag2 = true;
				Type type = nameMarkerProviderContext.ProviderTypes[k];
				for (int l = 0; l < existingProviders.Length; l++)
				{
					if (existingProviders[l].GetType() == type)
					{
						flag2 = false;
					}
				}
				if (flag2)
				{
					MissionNameMarkerProvider item = Activator.CreateInstance(type) as MissionNameMarkerProvider;
					addedProviders.Add(item);
				}
			}
		}

		// Token: 0x040001E2 RID: 482
		public static readonly MissionNameMarkerFactory.INameMarkerProviderContext DefaultContext = new MissionNameMarkerFactory.NameMarkerProviderContext(true, "DefaultNameMarkerContext", new Action(MissionNameMarkerFactory.FireProvidersChangedEvent));

		// Token: 0x040001E4 RID: 484
		private static List<MissionNameMarkerFactory.INameMarkerProviderContext> _registeredContexts = new List<MissionNameMarkerFactory.INameMarkerProviderContext> { MissionNameMarkerFactory.DefaultContext };

		// Token: 0x0200009B RID: 155
		public interface INameMarkerProviderContext
		{
			// Token: 0x170001C9 RID: 457
			// (get) Token: 0x0600068B RID: 1675
			string Id { get; }

			// Token: 0x170001CA RID: 458
			// (get) Token: 0x0600068C RID: 1676
			bool IsDefaultContext { get; }

			// Token: 0x0600068D RID: 1677
			void AddProvider<T>() where T : MissionNameMarkerProvider, new();

			// Token: 0x0600068E RID: 1678
			void RemoveProvider<T>() where T : MissionNameMarkerProvider, new();
		}

		// Token: 0x0200009C RID: 156
		private class NameMarkerProviderContext : MissionNameMarkerFactory.INameMarkerProviderContext
		{
			// Token: 0x170001CB RID: 459
			// (get) Token: 0x0600068F RID: 1679 RVA: 0x00016A6A File Offset: 0x00014C6A
			// (set) Token: 0x06000690 RID: 1680 RVA: 0x00016A72 File Offset: 0x00014C72
			public string Id { get; private set; }

			// Token: 0x170001CC RID: 460
			// (get) Token: 0x06000691 RID: 1681 RVA: 0x00016A7B File Offset: 0x00014C7B
			// (set) Token: 0x06000692 RID: 1682 RVA: 0x00016A83 File Offset: 0x00014C83
			public bool IsDefaultContext { get; private set; }

			// Token: 0x170001CD RID: 461
			// (get) Token: 0x06000693 RID: 1683 RVA: 0x00016A8C File Offset: 0x00014C8C
			// (set) Token: 0x06000694 RID: 1684 RVA: 0x00016A94 File Offset: 0x00014C94
			public List<Type> ProviderTypes { get; private set; }

			// Token: 0x06000695 RID: 1685 RVA: 0x00016A9D File Offset: 0x00014C9D
			public NameMarkerProviderContext(bool isDefault, string id, Action onProvidersChanged)
			{
				this._onProvidersChanged = onProvidersChanged;
				this.IsDefaultContext = isDefault;
				this.Id = id;
				this.ProviderTypes = new List<Type>();
			}

			// Token: 0x06000696 RID: 1686 RVA: 0x00016AC5 File Offset: 0x00014CC5
			public void AddProvider<T>() where T : MissionNameMarkerProvider, new()
			{
				this.AddProvider(typeof(T));
			}

			// Token: 0x06000697 RID: 1687 RVA: 0x00016AD7 File Offset: 0x00014CD7
			public void RemoveProvider<T>() where T : MissionNameMarkerProvider, new()
			{
				this.RemoveProvider(typeof(T));
			}

			// Token: 0x06000698 RID: 1688 RVA: 0x00016AEC File Offset: 0x00014CEC
			public void AddProvider(Type tProvider)
			{
				for (int i = 0; i < this.ProviderTypes.Count; i++)
				{
					if (this.ProviderTypes[i] == tProvider)
					{
						Debug.FailedAssert("Provider of type: " + tProvider.Name + " was already added to name marker context: " + this.Id, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "AddProvider", 182);
						return;
					}
				}
				this.ProviderTypes.Add(tProvider);
				Action onProvidersChanged = this._onProvidersChanged;
				if (onProvidersChanged == null)
				{
					return;
				}
				onProvidersChanged();
			}

			// Token: 0x06000699 RID: 1689 RVA: 0x00016B70 File Offset: 0x00014D70
			public void RemoveProvider(Type tProvider)
			{
				int i = 0;
				while (i < this.ProviderTypes.Count)
				{
					if (this.ProviderTypes[i] == tProvider)
					{
						this.ProviderTypes.Remove(tProvider);
						Action onProvidersChanged = this._onProvidersChanged;
						if (onProvidersChanged == null)
						{
							return;
						}
						onProvidersChanged();
						return;
					}
					else
					{
						i++;
					}
				}
				Debug.FailedAssert("Provider of type: " + tProvider.Name + " was not added to name marker context: " + this.Id, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "RemoveProvider", 203);
			}

			// Token: 0x040003AB RID: 939
			private Action _onProvidersChanged;
		}
	}
}

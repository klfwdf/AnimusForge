using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public static class MissionNameMarkerFactory
{
	public interface INameMarkerProviderContext
	{
		string Id { get; }

		bool IsDefaultContext { get; }

		void AddProvider<T>() where T : MissionNameMarkerProvider, new();

		void RemoveProvider<T>() where T : MissionNameMarkerProvider, new();
	}

	private class NameMarkerProviderContext : INameMarkerProviderContext
	{
		private Action _onProvidersChanged;

		public string Id { get; private set; }

		public bool IsDefaultContext { get; private set; }

		public List<Type> ProviderTypes { get; private set; }

		public NameMarkerProviderContext(bool isDefault, string id, Action onProvidersChanged)
		{
			_onProvidersChanged = onProvidersChanged;
			IsDefaultContext = isDefault;
			Id = id;
			ProviderTypes = new List<Type>();
		}

		public void AddProvider<T>() where T : MissionNameMarkerProvider, new()
		{
			AddProvider(typeof(T));
		}

		public void RemoveProvider<T>() where T : MissionNameMarkerProvider, new()
		{
			RemoveProvider(typeof(T));
		}

		public void AddProvider(Type tProvider)
		{
			for (int i = 0; i < ProviderTypes.Count; i++)
			{
				if (ProviderTypes[i] == tProvider)
				{
					Debug.FailedAssert("Provider of type: " + tProvider.Name + " was already added to name marker context: " + Id, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "AddProvider", 182);
					return;
				}
			}
			ProviderTypes.Add(tProvider);
			_onProvidersChanged?.Invoke();
		}

		public void RemoveProvider(Type tProvider)
		{
			for (int i = 0; i < ProviderTypes.Count; i++)
			{
				if (ProviderTypes[i] == tProvider)
				{
					ProviderTypes.Remove(tProvider);
					_onProvidersChanged?.Invoke();
					return;
				}
			}
			Debug.FailedAssert("Provider of type: " + tProvider.Name + " was not added to name marker context: " + Id, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "RemoveProvider", 203);
		}
	}

	public static readonly INameMarkerProviderContext DefaultContext;

	private static List<INameMarkerProviderContext> _registeredContexts;

	public static event Action OnProvidersChanged;

	static MissionNameMarkerFactory()
	{
		DefaultContext = new NameMarkerProviderContext(isDefault: true, "DefaultNameMarkerContext", FireProvidersChangedEvent);
		_registeredContexts = new List<INameMarkerProviderContext> { DefaultContext };
	}

	public static INameMarkerProviderContext PushContext(string name, bool addDefaultProviders)
	{
		NameMarkerProviderContext nameMarkerProviderContext = new NameMarkerProviderContext(isDefault: false, name, FireProvidersChangedEvent);
		if (addDefaultProviders)
		{
			NameMarkerProviderContext nameMarkerProviderContext2 = DefaultContext as NameMarkerProviderContext;
			for (int i = 0; i < nameMarkerProviderContext2.ProviderTypes.Count; i++)
			{
				nameMarkerProviderContext.AddProvider(nameMarkerProviderContext2.ProviderTypes[i]);
			}
		}
		_registeredContexts.Add(nameMarkerProviderContext);
		return nameMarkerProviderContext;
	}

	public static void PopContext(string contextId)
	{
		for (int i = 0; i < _registeredContexts.Count; i++)
		{
			if (_registeredContexts[i].Id == contextId)
			{
				PopContext(_registeredContexts[i]);
				return;
			}
		}
		Debug.FailedAssert("Trying to pop a name marker context that was not pushed: " + contextId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "PopContext", 54);
	}

	public static void PopContext(INameMarkerProviderContext context)
	{
		if (context.IsDefaultContext)
		{
			Debug.FailedAssert("Default name marker context cannot be removed", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Missions\\NameMarker\\MissionNameMarkerFactory.cs", "PopContext", 61);
		}
		else
		{
			_registeredContexts.Remove(context);
		}
	}

	private static void FireProvidersChangedEvent()
	{
		MissionNameMarkerFactory.OnProvidersChanged?.Invoke();
	}

	public static List<MissionNameMarkerProvider> CollectProviders()
	{
		List<MissionNameMarkerProvider> list = new List<MissionNameMarkerProvider>();
		NameMarkerProviderContext nameMarkerProviderContext = _registeredContexts[_registeredContexts.Count - 1] as NameMarkerProviderContext;
		for (int i = 0; i < nameMarkerProviderContext.ProviderTypes.Count; i++)
		{
			MissionNameMarkerProvider item = Activator.CreateInstance(nameMarkerProviderContext.ProviderTypes[i]) as MissionNameMarkerProvider;
			list.Add(item);
		}
		return list;
	}

	public static void UpdateProviders(MissionNameMarkerProvider[] existingProviders, out List<MissionNameMarkerProvider> addedProviders, out List<MissionNameMarkerProvider> removedProviders)
	{
		addedProviders = new List<MissionNameMarkerProvider>();
		removedProviders = new List<MissionNameMarkerProvider>();
		NameMarkerProviderContext nameMarkerProviderContext = _registeredContexts[_registeredContexts.Count - 1] as NameMarkerProviderContext;
		for (int i = 0; i < existingProviders.Length; i++)
		{
			bool flag = true;
			MissionNameMarkerProvider missionNameMarkerProvider = existingProviders[i];
			for (int j = 0; j < nameMarkerProviderContext.ProviderTypes.Count; j++)
			{
				Type type = nameMarkerProviderContext.ProviderTypes[j];
				if (missionNameMarkerProvider.GetType() == type)
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
			Type type2 = nameMarkerProviderContext.ProviderTypes[k];
			for (int l = 0; l < existingProviders.Length; l++)
			{
				if (existingProviders[l].GetType() == type2)
				{
					flag2 = false;
				}
			}
			if (flag2)
			{
				MissionNameMarkerProvider item = Activator.CreateInstance(type2) as MissionNameMarkerProvider;
				addedProviders.Add(item);
			}
		}
	}
}

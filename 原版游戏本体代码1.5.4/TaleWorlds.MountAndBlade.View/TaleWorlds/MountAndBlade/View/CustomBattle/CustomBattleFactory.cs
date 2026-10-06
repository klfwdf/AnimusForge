using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.View.CustomBattle;

public static class CustomBattleFactory
{
	private static readonly List<Type> _providers = new List<Type>();

	public static void RegisterProvider<T>() where T : ICustomBattleProvider, new()
	{
		Type typeFromHandle = typeof(T);
		for (int i = 0; i < _providers.Count; i++)
		{
			if (_providers[i].GetType() == typeFromHandle)
			{
				Debug.FailedAssert("Custom battle provider was already registered: " + typeFromHandle.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\CustomBattle\\CustomBattleFactory.cs", "RegisterProvider", 18);
				return;
			}
		}
		if (typeFromHandle.Name.ToLowerInvariant().Contains("naval"))
		{
			_providers.Insert(0, typeFromHandle);
		}
		else
		{
			_providers.Add(typeFromHandle);
		}
	}

	public static void StartCustomBattleWithProvider<T>() where T : ICustomBattleProvider, new()
	{
		(Activator.CreateInstance(typeof(T)) as ICustomBattleProvider).StartCustomBattle();
	}

	public static void StartCustomBattle()
	{
		if (_providers.Count > 0)
		{
			(Activator.CreateInstance(_providers[0]) as ICustomBattleProvider).StartCustomBattle();
		}
	}

	public static int GetProviderCount()
	{
		return _providers.Count;
	}

	public static List<ICustomBattleProvider> CollectProviders()
	{
		List<ICustomBattleProvider> list = new List<ICustomBattleProvider>();
		for (int i = 0; i < _providers.Count; i++)
		{
			ICustomBattleProvider item = Activator.CreateInstance(_providers[i]) as ICustomBattleProvider;
			list.Add(item);
		}
		return list;
	}

	public static ICustomBattleProvider CollectNextProvider(Type currentProviderType)
	{
		int index = (_providers.IndexOf(currentProviderType) + 1) % _providers.Count;
		return Activator.CreateInstance(_providers[index]) as ICustomBattleProvider;
	}
}

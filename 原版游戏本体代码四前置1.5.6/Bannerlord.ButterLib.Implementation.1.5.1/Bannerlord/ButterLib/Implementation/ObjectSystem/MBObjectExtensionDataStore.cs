using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.ObjectSystem;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Bannerlord.ButterLib.Implementation.ObjectSystem;

internal sealed class MBObjectExtensionDataStore : CampaignBehaviorBase, IMBObjectExtensionDataStore
{
	private sealed record DataKey([field: SaveableField(0)] MBGUID ObjectId, [field: SaveableField(1)] string? Key)
	{
		internal static DataKey Make(MBObjectBase obj, string key)
		{
			return new DataKey(obj.Id, key);
		}

		public override string ToString()
		{
			return $"{ObjectId}::{Key}";
		}
	}

	private sealed class SavedTypeDefiner : SaveableTypeDefiner
	{
		public SavedTypeDefiner()
			: base(222444700)
		{
		}

		protected override void DefineClassTypes()
		{
			AddClassDefinition(typeof(DataKey), 1);
		}

		protected override void DefineContainerDefinitions()
		{
			ConstructContainerDefinition(typeof(ConcurrentDictionary<DataKey, object>));
			ConstructContainerDefinition(typeof(ConcurrentDictionary<DataKey, bool>));
		}
	}

	private ConcurrentDictionary<DataKey, object?> _vars = new ConcurrentDictionary<DataKey, object>();

	private ConcurrentDictionary<DataKey, bool> _flags = new ConcurrentDictionary<DataKey, bool>();

	public override void RegisterEvents()
	{
	}

	public override void SyncData(IDataStore dataStore)
	{
		(ButterLibSubModule.Instance?.GetServiceProvider()?.GetService<IMBObjectKeeper>())?.Sync(dataStore);
		if (dataStore.IsSaving)
		{
			HashSet<MBGUID> cache = new HashSet<MBGUID>();
			ReleaseOrphanedEntries(_vars, cache);
			ReleaseOrphanedEntries(_flags, cache);
		}
		dataStore.SyncData("Vars", ref _vars);
		if (_vars == null)
		{
			_vars = new ConcurrentDictionary<DataKey, object>();
		}
		dataStore.SyncData("Flags", ref _flags);
		if (_flags == null)
		{
			_flags = new ConcurrentDictionary<DataKey, bool>();
		}
	}

	private static void ReleaseOrphanedEntries<T>(IDictionary<DataKey, T> dict, ISet<MBGUID> cache)
	{
		foreach (DataKey key in dict.Keys)
		{
			if (cache.Contains(key.ObjectId))
			{
				dict.Remove(key);
			}
			else if (DoesNotExist(key.ObjectId))
			{
				cache.Add(key.ObjectId);
				dict.Remove(key);
			}
		}
		static bool DoesNotExist(MBGUID objectId)
		{
			try
			{
				return MBObjectManager.Instance.GetObject(objectId) == null;
			}
			catch (Exception ex) when (ex is MBTypeNotRegisteredException)
			{
				return true;
			}
		}
	}

	public bool HasVariable(MBObjectBase @object, string name)
	{
		return _vars.ContainsKey(DataKey.Make(@object, name));
	}

	public bool RemoveVariable(MBObjectBase @object, string name)
	{
		object value;
		return _vars.TryRemove(DataKey.Make(@object, name), out value);
	}

	public void SetVariable(MBObjectBase @object, string name, object? data)
	{
		_vars[DataKey.Make(@object, name)] = data;
	}

	public bool TryGetVariable<T>(MBObjectBase @object, string name, out T? value)
	{
		bool flag = _vars.TryGetValue(DataKey.Make(@object, name), out object value2);
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3 = ((value2 is T || value2 == null) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			value = (T)value2;
			return true;
		}
		value = default(T);
		return false;
	}

	public bool HasFlag(MBObjectBase @object, string name)
	{
		bool value;
		return _flags.TryGetValue(DataKey.Make(@object, name), out value) & value;
	}

	public bool RemoveFlag(MBObjectBase @object, string name)
	{
		bool value;
		return _flags.TryRemove(DataKey.Make(@object, name), out value);
	}

	public void SetFlag(MBObjectBase @object, string name)
	{
		_flags[DataKey.Make(@object, name)] = true;
	}
}

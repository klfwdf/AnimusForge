using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.BUTR.Shared.Utils;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Extensions;
using Bannerlord.UIExtenderEx.Patches;
using Bannerlord.UIExtenderEx.Utils;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.Components;

internal class ViewModelComponent
{
	private readonly string _moduleName;

	private readonly Harmony _harmony;

	public readonly ConcurrentDictionary<Type, List<Type>> Mixins = new ConcurrentDictionary<Type, List<Type>>();

	internal readonly ConditionalWeakTable<ViewModel, List<IViewModelMixin>> MixinInstanceCache = new ConditionalWeakTable<ViewModel, List<IViewModelMixin>>();

	internal readonly ConditionalWeakTable<ViewModel, List<string>> MixinInstanceRefreshFromConstructorCache = new ConditionalWeakTable<ViewModel, List<string>>();

	private readonly ConcurrentDictionary<Type, bool> _mixinTypeEnabled = new ConcurrentDictionary<Type, bool>();

	private readonly ConcurrentDictionary<Type, List<PropertyInfo>> _mixinTypePropertyCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();

	private readonly ConcurrentDictionary<Type, List<MethodInfo>> _mixinTypeMethodCache = new ConcurrentDictionary<Type, List<MethodInfo>>();

	public ViewModelComponent(string moduleName)
	{
		_moduleName = moduleName;
		_harmony = new Harmony("bannerlord.uiextender.ex.viewmodels." + _moduleName);
	}

	public void Enable()
	{
		foreach (Type key in _mixinTypeEnabled.Keys)
		{
			_mixinTypeEnabled[key] = true;
		}
	}

	public void Disable()
	{
		foreach (Type key in _mixinTypeEnabled.Keys)
		{
			_mixinTypeEnabled[key] = false;
		}
	}

	public void Enable(Type mixinType)
	{
		if (_mixinTypeEnabled.ContainsKey(mixinType))
		{
			_mixinTypeEnabled[mixinType] = true;
		}
	}

	public void Disable(Type mixinType)
	{
		if (_mixinTypeEnabled.ContainsKey(mixinType))
		{
			_mixinTypeEnabled[mixinType] = false;
		}
	}

	public void RegisterViewModelMixin(Type mixinType, string? refreshMethodName = null, bool handleDerived = false)
	{
		Type viewModelType = GetViewModelType(mixinType);
		if ((object)viewModelType == null)
		{
			MessageUtils.Fail($"Failed to find base type for mixin {mixinType}, should be specialized as T of ViewModelMixin<T>!");
			return;
		}
		if (handleDerived)
		{
			foreach (Type item in from t in AccessTools2.AllTypes()
				where viewModelType.IsAssignableFrom(t)
				select t)
			{
				Patch(item);
			}
			return;
		}
		Patch(viewModelType);
		void Patch(Type viewModelType_)
		{
			Mixins.GetOrAdd(viewModelType_, (Type _) => new List<Type>()).Add(mixinType);
			_mixinTypeEnabled[mixinType] = false;
			ViewModelWithMixinPatch.Patch(_harmony, viewModelType_, refreshMethodName);
		}
	}

	public void Deregister()
	{
		foreach (MethodBase patchedMethod in _harmony.GetPatchedMethods())
		{
			if (patchedMethod is MethodInfo methodInfo)
			{
				MethodBase originalMethod = Harmony.GetOriginalMethod(methodInfo);
				if ((object)originalMethod != null)
				{
					_harmony.Unpatch(originalMethod, methodInfo);
				}
			}
		}
		Mixins.Clear();
		_mixinTypeEnabled.Clear();
		_mixinTypePropertyCache.Clear();
		_mixinTypeMethodCache.Clear();
	}

	public void InitializeMixinsForVMInstance(ViewModel instance)
	{
		List<IViewModelMixin> mixins = MixinInstanceCache.GetOrAdd(instance, (ViewModel _) => new List<IViewModelMixin>());
		Type type = instance.GetType();
		if (!Mixins.TryGetValue(type, out List<Type> value))
		{
			return;
		}
		List<IViewModelMixin> list = (from mixinType in value
			where _mixinTypeEnabled.TryGetValue(mixinType, out var value2) & value2
			where mixins.All((IViewModelMixin mixin) => mixin.GetType() != mixinType)
			select Activator.CreateInstance(mixinType, instance) as IViewModelMixin into mixin
			where mixin != null
			select mixin).Cast<IViewModelMixin>().ToList();
		mixins.AddRange(list);
		foreach (IViewModelMixin item in list)
		{
			List<PropertyInfo> orAdd = _mixinTypePropertyCache.GetOrAdd(item.GetType(), (Type x) => (from p in x.GetProperties()
				where p.CustomAttributes.Any((CustomAttributeData a) => a.AttributeType == typeof(DataSourceProperty))
				select p).ToList());
			foreach (PropertyInfo item2 in orAdd)
			{
				instance.AddProperty(item2.Name, new WrappedPropertyInfo(item2, item));
			}
			List<MethodInfo> orAdd2 = _mixinTypeMethodCache.GetOrAdd(item.GetType(), (Type x) => (from p in x.GetMethods()
				where p.CustomAttributes.Any((CustomAttributeData a) => a.AttributeType == typeof(DataSourceMethodAttribute))
				select p).ToList());
			foreach (MethodInfo item3 in orAdd2)
			{
				instance.AddMethod(item3.Name, new WrappedMethodInfo(item3, item));
			}
		}
	}

	private static Type? GetViewModelType(Type mixinType)
	{
		Type type = null;
		Type type2 = mixinType;
		while ((object)type2 != null)
		{
			if (typeof(IViewModelMixin).IsAssignableFrom(type2))
			{
				type = type2.GetGenericArguments().FirstOrDefault();
				if ((object)type != null)
				{
					break;
				}
			}
			type2 = type2.BaseType;
		}
		return type;
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.BUTR.Shared.Utils;
using Bannerlord.UIExtenderEx.Extensions;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx;

internal abstract class BUTRViewModelMixin<TViewModelMixin, TViewModel> : IViewModelMixin where TViewModelMixin : BUTRViewModelMixin<TViewModelMixin, TViewModel> where TViewModel : ViewModel
{
	private readonly WeakReference<TViewModel> _vm;

	protected TViewModel? ViewModel
	{
		get
		{
			if (!_vm.TryGetTarget(out var target))
			{
				return null;
			}
			return target;
		}
	}

	public TViewModelMixin Mixin => (TViewModelMixin)this;

	protected BUTRViewModelMixin(TViewModel vm)
	{
		_vm = new WeakReference<TViewModel>(vm);
		SetVMProperty("Mixin", GetType().Name);
		PropertyInfo[] properties = GetType().GetProperties(AccessTools.all);
		foreach (PropertyInfo propertyInfo in properties)
		{
			BUTRDataSourcePropertyAttribute customAttribute = propertyInfo.GetCustomAttribute<BUTRDataSourcePropertyAttribute>();
			if (customAttribute == null)
			{
				continue;
			}
			MethodInfo? getMethod = propertyInfo.GetMethod;
			if ((object)getMethod == null || !getMethod.IsPrivate)
			{
				MethodInfo? setMethod = propertyInfo.SetMethod;
				if ((object)setMethod == null || !setMethod.IsPrivate)
				{
					WrappedPropertyInfo wrappedPropertyInfo = new WrappedPropertyInfo(propertyInfo, this);
					vm.AddProperty(customAttribute.OverrideName ?? propertyInfo.Name, wrappedPropertyInfo);
					wrappedPropertyInfo.PropertyChanged += delegate(object _, PropertyChangedEventArgs e)
					{
						ViewModel?.OnPropertyChanged(e.PropertyName);
					};
					continue;
				}
			}
			throw new Exception();
		}
		MethodInfo[] methods = GetType().GetMethods(AccessTools.all);
		foreach (MethodInfo methodInfo in methods)
		{
			BUTRDataSourceMethodAttribute customAttribute2 = methodInfo.GetCustomAttribute<BUTRDataSourceMethodAttribute>();
			if (customAttribute2 != null)
			{
				if (methodInfo.IsPrivate)
				{
					throw new Exception();
				}
				WrappedMethodInfo methodInfo2 = new WrappedMethodInfo(methodInfo, this);
				vm.AddMethod(customAttribute2.OverrideName ?? methodInfo.Name, methodInfo2);
			}
		}
	}

	public abstract void OnRefresh();

	public abstract void OnFinalize();

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		object obj = ViewModel?.GetPropertyValue(propertyName);
		ViewModel?.OnPropertyChanged(propertyName);
	}

	protected void OnPropertyChangedWithValue(object? value, [CallerMemberName] string? propertyName = null)
	{
		((ViewModel)ViewModel)?.OnPropertyChangedWithValue(value, propertyName);
	}

	protected bool SetField<T>(ref T field, T value, string propertyName)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChangedWithValue(value, propertyName);
		return true;
	}

	protected void SetVMProperty(string property, string? overrideName = null)
	{
		WrappedPropertyInfo wrappedPropertyInfo = new WrappedPropertyInfo(AccessTools2.Property(GetType(), property), this);
		ViewModel?.AddProperty(overrideName ?? property, wrappedPropertyInfo);
		wrappedPropertyInfo.PropertyChanged += delegate(object _, PropertyChangedEventArgs e)
		{
			ViewModel?.OnPropertyChanged(e.PropertyName);
		};
	}

	protected void SetVMMethod(string method, string? overrideName = null)
	{
		WrappedMethodInfo methodInfo = new WrappedMethodInfo(AccessTools2.Method(GetType(), method), this);
		ViewModel?.AddMethod(overrideName ?? method, methodInfo);
	}
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bannerlord.UIExtenderEx.Extensions;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.ViewModels;

public abstract class BaseViewModelMixin<TViewModel> : IViewModelMixin where TViewModel : ViewModel
{
	private delegate void OnPropertyChangedWithValueDelegate0(ViewModel instance, object value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate2(ViewModel instance, bool value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate3(ViewModel instance, int value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate4(ViewModel instance, float value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate5(ViewModel instance, uint value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate6(ViewModel instance, Color value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate7(ViewModel instance, double value, [CallerMemberName] string? propertyName = null);

	private delegate void OnPropertyChangedWithValueDelegate8(ViewModel instance, Vec2 value, [CallerMemberName] string? propertyName = null);

	private static readonly OnPropertyChangedWithValueDelegate0? OnPropertyChangedWithValue0 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate0>(typeof(ViewModel), "OnPropertyChangedWithValue");

	private static readonly ConcurrentDictionary<Type, OnPropertyChangedWithValueDelegate0?> OnPropertyChangedWithValue1 = new ConcurrentDictionary<Type, OnPropertyChangedWithValueDelegate0>();

	private static readonly OnPropertyChangedWithValueDelegate2? OnPropertyChangedWithValue2 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate2>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(bool),
		typeof(string)
	});

	private static readonly OnPropertyChangedWithValueDelegate3? OnPropertyChangedWithValue3 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate3>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(int),
		typeof(string)
	});

	private static readonly OnPropertyChangedWithValueDelegate4? OnPropertyChangedWithValue4 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate4>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(float),
		typeof(string)
	});

	private static readonly OnPropertyChangedWithValueDelegate5? OnPropertyChangedWithValue5 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate5>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(uint),
		typeof(string)
	});

	private static readonly OnPropertyChangedWithValueDelegate6? OnPropertyChangedWithValue6 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate6>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(Color),
		typeof(string)
	});

	private static readonly OnPropertyChangedWithValueDelegate7? OnPropertyChangedWithValue7 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate7>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(double),
		typeof(string)
	});

	private static readonly OnPropertyChangedWithValueDelegate8? OnPropertyChangedWithValue8 = AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate8>(typeof(ViewModel), "OnPropertyChangedWithValue", new Type[2]
	{
		typeof(Vec2),
		typeof(string)
	});

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

	protected BaseViewModelMixin(TViewModel vm)
	{
		_vm = new WeakReference<TViewModel>(vm);
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		ViewModel?.OnPropertyChanged(propertyName);
	}

	protected void OnPropertyChangedWithValue(object value, [CallerMemberName] string? propertyName = null)
	{
		if (ViewModel == null)
		{
			return;
		}
		if (OnPropertyChangedWithValue0 != null)
		{
			OnPropertyChangedWithValue0(ViewModel, value, propertyName);
			return;
		}
		if (!(value is bool value2))
		{
			if (!(value is int value3))
			{
				if (!(value is float value4))
				{
					if (!(value is uint value5))
					{
						if (!(value is Color value6))
						{
							if (!(value is double value7))
							{
								if (value is Vec2 value8 && OnPropertyChangedWithValue8 != null)
								{
									OnPropertyChangedWithValue8(ViewModel, value8, propertyName);
									return;
								}
							}
							else if (OnPropertyChangedWithValue7 != null)
							{
								OnPropertyChangedWithValue7(ViewModel, value7, propertyName);
								return;
							}
						}
						else if (OnPropertyChangedWithValue6 != null)
						{
							OnPropertyChangedWithValue6(ViewModel, value6, propertyName);
							return;
						}
					}
					else if (OnPropertyChangedWithValue5 != null)
					{
						OnPropertyChangedWithValue5(ViewModel, value5, propertyName);
						return;
					}
				}
				else if (OnPropertyChangedWithValue4 != null)
				{
					OnPropertyChangedWithValue4(ViewModel, value4, propertyName);
					return;
				}
			}
			else if (OnPropertyChangedWithValue3 != null)
			{
				OnPropertyChangedWithValue3(ViewModel, value3, propertyName);
				return;
			}
		}
		else if (OnPropertyChangedWithValue2 != null)
		{
			OnPropertyChangedWithValue2(ViewModel, value2, propertyName);
			return;
		}
		OnPropertyChangedWithValue1.GetOrAdd(value.GetType(), ValueFactory)?.Invoke(ViewModel, value, propertyName);
		static OnPropertyChangedWithValueDelegate0 ValueFactory(Type x)
		{
			return AccessTools2.GetDelegate<OnPropertyChangedWithValueDelegate0>(AccessTools.GetDeclaredMethods(typeof(ViewModel)).FirstOrDefault((MethodInfo methodInfo) => methodInfo.IsGenericMethod && methodInfo.Name == "OnPropertyChangedWithValue")?.MakeGenericMethod(x));
		}
	}

	public virtual void OnRefresh()
	{
	}

	public virtual void OnFinalize()
	{
	}

	protected TValue? GetPrivate<TValue>(string name)
	{
		return _vm.PrivateValue<TValue>(name);
	}

	protected void SetPrivate<TValue>(string name, TValue? value)
	{
		_vm.PrivateValueSet(name, value);
	}

	protected bool SetField<T>(ref T field, T value, string propertyName)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}

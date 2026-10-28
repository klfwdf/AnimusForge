using System;
using System.Collections.Generic;
using System.ComponentModel;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using MCM.UI.GUI.ViewModels;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace MCM.UI.UIExtenderEx;

[ViewModelMixin]
internal sealed class OptionsVMMixin : BaseViewModelMixin<OptionsVM>
{
	private delegate void ExecuteDoneDelegate(OptionsVM instance);

	private delegate void ExecuteCancelDelegate(OptionsVM instance);

	private static readonly ExecuteDoneDelegate? ExecuteDoneMethod;

	private static readonly ExecuteCancelDelegate? ExecuteCancelMethod;

	private static readonly AccessTools.FieldRef<OptionsVM, List<ViewModel>>? _categories;

	private readonly ModOptionsVM _modOptions = new ModOptionsVM();

	private bool _modOptionsSelected;

	private int _descriptionWidth = 650;

	[DataSourceProperty]
	public WeakReference<OptionsVMMixin> MCMMixin => new WeakReference<OptionsVMMixin>(this);

	[DataSourceProperty]
	public ModOptionsVM ModOptions => _modOptions;

	[DataSourceProperty]
	public int DescriptionWidth
	{
		get
		{
			return _descriptionWidth;
		}
		private set
		{
			SetField(ref _descriptionWidth, value, "DescriptionWidth");
		}
	}

	[DataSourceProperty]
	public bool ModOptionsSelected
	{
		get
		{
			return _modOptionsSelected;
		}
		set
		{
			if (SetField(ref _modOptionsSelected, value, "ModOptionsSelected"))
			{
				_modOptions.IsDisabled = !value;
				_modOptionsSelected = value;
				DescriptionWidth = ((!ModOptionsSelected) ? 650 : 0);
			}
		}
	}

	static OptionsVMMixin()
	{
		ExecuteDoneMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<ExecuteDoneDelegate>(typeof(OptionsVM), "ExecuteDone");
		ExecuteCancelMethod = HarmonyLib.BUTR.Extensions.AccessTools2.GetDelegate<ExecuteCancelDelegate>(typeof(OptionsVM), "ExecuteCancel");
		_categories = HarmonyLib.BUTR.Extensions.AccessTools2.FieldRefAccess<OptionsVM, List<ViewModel>>("_categories");
		Harmony harmony = new Harmony("bannerlord.mcm.ui.optionsvm");
		harmony.CreateReversePatcher(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(OptionsVM), "ExecuteCloseOptions"), new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((OptionsVM x) => OriginalExecuteCloseOptions(x)))).Patch();
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(OptionsVM), "ExecuteCloseOptions"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((OptionsVM x) => ExecuteCloseOptionsPostfix(x)), 300));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.Method(typeof(OptionsVM), "RefreshValues"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((OptionsVM x) => RefreshValuesPostfix(x)), 300));
		harmony.Patch(HarmonyLib.BUTR.Extensions.AccessTools2.PropertySetter(typeof(OptionsVM), "CategoryIndex"), null, new HarmonyMethod(HarmonyLib.BUTR.Extensions.SymbolExtensions2.GetMethodInfo((OptionsVM x) => SetSelectedCategoryPostfix(x)), 300));
	}

	private static void ExecuteCloseOptionsPostfix(OptionsVM __instance)
	{
		if (__instance.GetPropertyValue("MCMMixin") is WeakReference<OptionsVMMixin> weakReference && weakReference.TryGetTarget(out var target))
		{
			target?.ExecuteCloseOptions();
		}
	}

	private static void RefreshValuesPostfix(OptionsVM __instance)
	{
		if (__instance.GetPropertyValue("ModOptions") is ModOptionsVM modOptionsVM)
		{
			modOptionsVM.RefreshValues();
		}
	}

	private static void SetSelectedCategoryPostfix(OptionsVM __instance)
	{
		if (__instance.GetPropertyValue("MCMMixin") is WeakReference<OptionsVMMixin> weakReference && weakReference.TryGetTarget(out var target))
		{
			target.ModOptionsSelected = __instance.CategoryIndex == 4;
		}
	}

	private static void OriginalExecuteCloseOptions(OptionsVM instance)
	{
		throw new NotImplementedException("It's a stub");
	}

	public OptionsVMMixin(OptionsVM vm)
		: base(vm)
	{
		vm.PropertyChanged += OptionsVM_PropertyChanged;
		AccessTools.FieldRef<OptionsVM, List<ViewModel>>? categories = _categories;
		List<ViewModel> list = ((categories != null) ? categories(vm) : null) ?? new List<ViewModel>();
		list.Insert(5, _modOptions);
	}

	private void OptionsVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		_modOptions.OnPropertyChanged(e.PropertyName);
	}

	public override void OnFinalize()
	{
		if (base.ViewModel != null)
		{
			base.ViewModel.PropertyChanged -= OptionsVM_PropertyChanged;
		}
		base.OnFinalize();
	}

	[DataSourceMethod]
	public void ExecuteCloseOptions()
	{
		ModOptions.ExecuteCancelInternal(popScreen: false);
		if (base.ViewModel != null)
		{
			try
			{
				OriginalExecuteCloseOptions(base.ViewModel);
			}
			catch
			{
			}
		}
		OnFinalize();
	}

	[DataSourceMethod]
	public void ExecuteDone()
	{
		ModOptions.ExecuteDoneInternal(popScreen: false, delegate
		{
			if (base.ViewModel != null)
			{
				ExecuteDoneMethod?.Invoke(base.ViewModel);
			}
		});
	}

	[DataSourceMethod]
	public void ExecuteCancel()
	{
		ModOptions.ExecuteCancelInternal(popScreen: false, delegate
		{
			if (base.ViewModel != null)
			{
				ExecuteCancelMethod?.Invoke(base.ViewModel);
			}
		});
	}
}

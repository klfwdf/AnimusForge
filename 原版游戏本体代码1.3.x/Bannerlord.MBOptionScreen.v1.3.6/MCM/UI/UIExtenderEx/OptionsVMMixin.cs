using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using MCM.UI.GUI.ViewModels;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

namespace MCM.UI.UIExtenderEx
{
	// Token: 0x02000019 RID: 25
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	[ViewModelMixin]
	internal sealed class OptionsVMMixin : BaseViewModelMixin<OptionsVM>
	{
		// Token: 0x06000090 RID: 144 RVA: 0x000036E8 File Offset: 0x000018E8
		static OptionsVMMixin()
		{
			Harmony harmony = new Harmony("bannerlord.mcm.ui.optionsvm");
			harmony.CreateReversePatcher(AccessTools2.Method(typeof(OptionsVM), "ExecuteCloseOptions", null, null, true), new HarmonyMethod(SymbolExtensions2.GetMethodInfo<OptionsVM>((OptionsVM x) => OptionsVMMixin.OriginalExecuteCloseOptions(x)))).Patch(HarmonyReversePatchType.Original);
			harmony.Patch(AccessTools2.Method(typeof(OptionsVM), "ExecuteCloseOptions", null, null, true), null, new HarmonyMethod(SymbolExtensions2.GetMethodInfo<OptionsVM>((OptionsVM x) => OptionsVMMixin.ExecuteCloseOptionsPostfix(x)), 300, null, null, null), null, null);
			harmony.Patch(AccessTools2.Method(typeof(OptionsVM), "RefreshValues", null, null, true), null, new HarmonyMethod(SymbolExtensions2.GetMethodInfo<OptionsVM>((OptionsVM x) => OptionsVMMixin.RefreshValuesPostfix(x)), 300, null, null, null), null, null);
			harmony.Patch(AccessTools2.PropertySetter(typeof(OptionsVM), "CategoryIndex", true), null, new HarmonyMethod(SymbolExtensions2.GetMethodInfo<OptionsVM>((OptionsVM x) => OptionsVMMixin.SetSelectedCategoryPostfix(x)), 300, null, null, null), null, null);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00003934 File Offset: 0x00001B34
		private static void ExecuteCloseOptionsPostfix(OptionsVM __instance)
		{
			WeakReference<OptionsVMMixin> weakReference = __instance.GetPropertyValue("MCMMixin") as WeakReference<OptionsVMMixin>;
			OptionsVMMixin mixin;
			if (weakReference != null && weakReference.TryGetTarget(out mixin) && mixin != null)
			{
				mixin.ExecuteCloseOptions();
			}
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00003968 File Offset: 0x00001B68
		private static void RefreshValuesPostfix(OptionsVM __instance)
		{
			ModOptionsVM modOptions = __instance.GetPropertyValue("ModOptions") as ModOptionsVM;
			if (modOptions != null)
			{
				modOptions.RefreshValues();
			}
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00003990 File Offset: 0x00001B90
		private static void SetSelectedCategoryPostfix(OptionsVM __instance)
		{
			WeakReference<OptionsVMMixin> weakReference = __instance.GetPropertyValue("MCMMixin") as WeakReference<OptionsVMMixin>;
			OptionsVMMixin mixin;
			if (weakReference != null && weakReference.TryGetTarget(out mixin))
			{
				mixin.ModOptionsSelected = __instance.CategoryIndex == 4;
			}
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000039CA File Offset: 0x00001BCA
		private static void OriginalExecuteCloseOptions(OptionsVM instance)
		{
			throw new NotImplementedException("It's a stub");
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000095 RID: 149 RVA: 0x000039D6 File Offset: 0x00001BD6
		[DataSourceProperty]
		public WeakReference<OptionsVMMixin> MCMMixin
		{
			get
			{
				return new WeakReference<OptionsVMMixin>(this);
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000096 RID: 150 RVA: 0x000039DE File Offset: 0x00001BDE
		[DataSourceProperty]
		public ModOptionsVM ModOptions
		{
			get
			{
				return this._modOptions;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000097 RID: 151 RVA: 0x000039E6 File Offset: 0x00001BE6
		// (set) Token: 0x06000098 RID: 152 RVA: 0x000039EE File Offset: 0x00001BEE
		[DataSourceProperty]
		public int DescriptionWidth
		{
			get
			{
				return this._descriptionWidth;
			}
			private set
			{
				base.SetField<int>(ref this._descriptionWidth, value, "DescriptionWidth");
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000099 RID: 153 RVA: 0x00003A03 File Offset: 0x00001C03
		// (set) Token: 0x0600009A RID: 154 RVA: 0x00003A0C File Offset: 0x00001C0C
		[DataSourceProperty]
		public bool ModOptionsSelected
		{
			get
			{
				return this._modOptionsSelected;
			}
			set
			{
				if (!base.SetField<bool>(ref this._modOptionsSelected, value, "ModOptionsSelected"))
				{
					return;
				}
				this._modOptions.IsDisabled = !value;
				this._modOptionsSelected = value;
				this.DescriptionWidth = (this.ModOptionsSelected ? 0 : 650);
			}
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00003A5C File Offset: 0x00001C5C
		public unsafe OptionsVMMixin(OptionsVM vm)
			: base(vm)
		{
			vm.PropertyChanged += this.OptionsVM_PropertyChanged;
			AccessTools.FieldRef<OptionsVM, List<ViewModel>> categories = OptionsVMMixin._categories;
			List<ViewModel> list = ((categories != null) ? (*categories(vm)) : null) ?? new List<ViewModel>();
			list.Insert(5, this._modOptions);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00003AC2 File Offset: 0x00001CC2
		private void OptionsVM_PropertyChanged([Nullable(2)] object sender, PropertyChangedEventArgs e)
		{
			this._modOptions.OnPropertyChanged(e.PropertyName);
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00003AD5 File Offset: 0x00001CD5
		public override void OnFinalize()
		{
			if (base.ViewModel != null)
			{
				base.ViewModel.PropertyChanged -= this.OptionsVM_PropertyChanged;
			}
			base.OnFinalize();
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00003AFC File Offset: 0x00001CFC
		[DataSourceMethod]
		public void ExecuteCloseOptions()
		{
			this.ModOptions.ExecuteCancelInternal(false, null);
			if (base.ViewModel != null)
			{
				try
				{
					OptionsVMMixin.OriginalExecuteCloseOptions(base.ViewModel);
				}
				catch
				{
				}
			}
			this.OnFinalize();
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00003B48 File Offset: 0x00001D48
		[DataSourceMethod]
		public void ExecuteDone()
		{
			this.ModOptions.ExecuteDoneInternal(false, delegate
			{
				if (base.ViewModel != null)
				{
					OptionsVMMixin.ExecuteDoneDelegate executeDoneMethod = OptionsVMMixin.ExecuteDoneMethod;
					if (executeDoneMethod == null)
					{
						return;
					}
					executeDoneMethod(base.ViewModel);
				}
			});
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00003B62 File Offset: 0x00001D62
		[DataSourceMethod]
		public void ExecuteCancel()
		{
			this.ModOptions.ExecuteCancelInternal(false, delegate
			{
				if (base.ViewModel != null)
				{
					OptionsVMMixin.ExecuteCancelDelegate executeCancelMethod = OptionsVMMixin.ExecuteCancelMethod;
					if (executeCancelMethod == null)
					{
						return;
					}
					executeCancelMethod(base.ViewModel);
				}
			});
		}

		// Token: 0x04000020 RID: 32
		[Nullable(2)]
		private static readonly OptionsVMMixin.ExecuteDoneDelegate ExecuteDoneMethod = AccessTools2.GetDelegate<OptionsVMMixin.ExecuteDoneDelegate>(typeof(OptionsVM), "ExecuteDone", null, null, true);

		// Token: 0x04000021 RID: 33
		[Nullable(2)]
		private static readonly OptionsVMMixin.ExecuteCancelDelegate ExecuteCancelMethod = AccessTools2.GetDelegate<OptionsVMMixin.ExecuteCancelDelegate>(typeof(OptionsVM), "ExecuteCancel", null, null, true);

		// Token: 0x04000022 RID: 34
		[Nullable(new byte[] { 2, 1, 1, 1 })]
		private static readonly AccessTools.FieldRef<OptionsVM, List<ViewModel>> _categories = AccessTools2.FieldRefAccess<OptionsVM, List<ViewModel>>("_categories", true);

		// Token: 0x04000023 RID: 35
		private readonly ModOptionsVM _modOptions = new ModOptionsVM();

		// Token: 0x04000024 RID: 36
		private bool _modOptionsSelected;

		// Token: 0x04000025 RID: 37
		private int _descriptionWidth = 650;

		// Token: 0x0200007F RID: 127
		// (Invoke) Token: 0x060004C3 RID: 1219
		[NullableContext(0)]
		private delegate void ExecuteDoneDelegate(OptionsVM instance);

		// Token: 0x02000080 RID: 128
		// (Invoke) Token: 0x060004C7 RID: 1223
		[NullableContext(0)]
		private delegate void ExecuteCancelDelegate(OptionsVM instance);
	}
}

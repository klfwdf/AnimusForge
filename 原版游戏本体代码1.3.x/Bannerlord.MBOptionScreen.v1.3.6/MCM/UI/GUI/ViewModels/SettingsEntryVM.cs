using System;
using System.Runtime.CompilerServices;
using MCM.Abstractions;
using TaleWorlds.Library;

namespace MCM.UI.GUI.ViewModels
{
	// Token: 0x0200001F RID: 31
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SettingsEntryVM : ViewModel
	{
		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060000ED RID: 237 RVA: 0x00004FDA File Offset: 0x000031DA
		public string Id
		{
			get
			{
				UnavailableSetting unavailableSetting = this.UnavailableSetting;
				string result;
				if ((result = ((unavailableSetting != null) ? unavailableSetting.Id : null)) == null)
				{
					SettingsVM settingsVM = this.SettingsVM;
					result = ((settingsVM != null) ? settingsVM.SettingsDefinition.SettingsId : null) ?? "ERROR";
				}
				return result;
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000EE RID: 238 RVA: 0x00005012 File Offset: 0x00003212
		[DataSourceProperty]
		public string DisplayName
		{
			get
			{
				UnavailableSetting unavailableSetting = this.UnavailableSetting;
				string result;
				if ((result = ((unavailableSetting != null) ? unavailableSetting.DisplayName : null)) == null)
				{
					SettingsVM settingsVM = this.SettingsVM;
					result = ((settingsVM != null) ? settingsVM.DisplayName : null) ?? "ERROR";
				}
				return result;
			}
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000EF RID: 239 RVA: 0x00005048 File Offset: 0x00003248
		// (set) Token: 0x060000F0 RID: 240 RVA: 0x0000507E File Offset: 0x0000327E
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				bool? isSelected = this._isSelected;
				if (isSelected == null)
				{
					SettingsVM settingsVM = this.SettingsVM;
					return settingsVM != null && settingsVM.IsSelected;
				}
				return isSelected.GetValueOrDefault();
			}
			set
			{
				if (this.SettingsVM != null)
				{
					this.SettingsVM.IsSelected = value;
				}
				else
				{
					this._isSelected = new bool?(value);
				}
				base.OnPropertyChanged("IsSelected");
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000F1 RID: 241 RVA: 0x000050AD File Offset: 0x000032AD
		[Nullable(2)]
		public SettingsVM SettingsVM
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000F2 RID: 242 RVA: 0x000050B5 File Offset: 0x000032B5
		[Nullable(2)]
		public UnavailableSetting UnavailableSetting
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x000050BD File Offset: 0x000032BD
		public SettingsEntryVM(UnavailableSetting unavailableSetting, Action<SettingsEntryVM> command)
		{
			this.UnavailableSetting = unavailableSetting;
			this._isSelected = new bool?(false);
			this._executeSelect = command;
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x000050DF File Offset: 0x000032DF
		public SettingsEntryVM(SettingsVM settingsVM, Action<SettingsEntryVM> command)
		{
			this.SettingsVM = settingsVM;
			this._executeSelect = command;
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x000050F5 File Offset: 0x000032F5
		public void ExecuteSelect()
		{
			this._executeSelect(this);
		}

		// Token: 0x0400003A RID: 58
		private readonly Action<SettingsEntryVM> _executeSelect;

		// Token: 0x0400003B RID: 59
		private bool? _isSelected;
	}
}
